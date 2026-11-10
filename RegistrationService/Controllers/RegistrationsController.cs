using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RegistrationService.Controllers;

[ApiController]
[Route("api/registrations")]
public class RegistrationsController : ControllerBase
{
    private readonly RegistrationService.Data.RegistrationDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private static readonly JsonSerializerOptions _enumOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public RegistrationsController(RegistrationService.Data.RegistrationDbContext db, IHttpClientFactory httpFactory)
    {
        _db = db;
        _httpFactory = httpFactory;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRegistrationRequest req)
    {
        var uid = GetUserId();
        if (uid is null) return Unauthorized();

        if (req.Role == RegistrationService.Models.RegistrationRole.Organizer)
            return BadRequest(new { error = "Cannot self-assign Organizer role." });

        var apiClient = _httpFactory.CreateClient("apiservice");
        var eventResp = await apiClient.GetAsync($"/api/events/{req.EventId}");
        if (eventResp.StatusCode == System.Net.HttpStatusCode.NotFound)
            return NotFound(new { error = "Event not found." });
        if (!eventResp.IsSuccessStatusCode)
            return StatusCode(502, new { error = "Failed to validate event." });

        var ev = await eventResp.Content.ReadFromJsonAsync<RegistrationService.Dtos.EventInfoDto>(_enumOpts);
        if (ev == null) return StatusCode(502, new { error = "Invalid event response." });
        if (ev.Status != RegistrationService.Dtos.EventStatus.Published)
            return BadRequest(new { error = "Event is not published." });

        if (req.Role == RegistrationService.Models.RegistrationRole.Attendee && ev.Capacity.HasValue)
        {
            var count = await _db.Registrations.CountAsync(r =>
                r.EventId == req.EventId && r.Role == RegistrationService.Models.RegistrationRole.Attendee);
            if (count >= ev.Capacity.Value)
                return BadRequest(new { error = "Event capacity reached." });
        }

        var existing = await _db.Registrations.SingleOrDefaultAsync(r => r.UserId == uid.Value && r.EventId == req.EventId);
        if (existing != null) return Conflict(new { error = "You are already registered for this event." });

        var reg = new RegistrationService.Models.Registration
        {
            UserId = uid.Value,
            EventId = req.EventId,
            Role = req.Role,
            RegisteredAt = DateTime.UtcNow
        };

        _db.Registrations.Add(reg);
        await _db.SaveChangesAsync();

        // Notifications — best-effort, do not fail the registration
        _ = SendRegistrationNotificationsAsync(uid.Value, req.EventId, ev.Title ?? $"Event {req.EventId}", req.Role, ev.CreatedByUserId);

        return CreatedAtAction(nameof(GetMyRegistrations), new { id = reg.Id }, reg);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyRegistrations()
    {
        var uid = GetUserId();
        if (uid is null) return Unauthorized();
        var regs = await _db.Registrations.Where(r => r.UserId == uid.Value).ToListAsync();
        return Ok(regs);
    }

    // Returns user IDs registered for an event (called by ApiService for lifecycle notifications)
    [HttpGet("event/{eventId:int}")]
    public async Task<IActionResult> GetUserIdsForEvent(int eventId)
    {
        var userIds = await _db.Registrations
            .Where(r => r.EventId == eventId)
            .Select(r => r.UserId)
            .Distinct()
            .ToListAsync();
        return Ok(userIds);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var uid = GetUserId();
        if (uid is null) return Unauthorized();

        var reg = await _db.Registrations.FindAsync(id);
        if (reg == null) return NotFound();
        if (reg.UserId != uid.Value) return Forbid();

        // Fetch event info before deleting so we can use the title in notifications
        string eventTitle = $"Event {reg.EventId}";
        string? organizerUserId = null;
        try
        {
            var apiClient = _httpFactory.CreateClient("apiservice");
            var evResp = await apiClient.GetAsync($"/api/events/{reg.EventId}");
            if (evResp.IsSuccessStatusCode)
            {
                var evInfo = await evResp.Content.ReadFromJsonAsync<RegistrationService.Dtos.EventInfoDto>(_enumOpts);
                if (evInfo?.Title is not null) eventTitle = evInfo.Title;
                organizerUserId = evInfo?.CreatedByUserId;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CancelRegistration] Could not fetch event info: {ex.Message}");
        }

        var roleName = reg.Role.ToString().ToLowerInvariant();
        _db.Registrations.Remove(reg);
        await _db.SaveChangesAsync();

        _ = SendCancellationNotificationsAsync(uid.Value, reg.EventId, eventTitle, roleName, organizerUserId);

        return NoContent();
    }

    private async Task SendRegistrationNotificationsAsync(
        int userId, int eventId, string eventTitle,
        RegistrationService.Models.RegistrationRole role, string? organizerUserId)
    {
        try
        {
            var notifyClient = _httpFactory.CreateClient("notificationservice");
            var roleName = role == RegistrationService.Models.RegistrationRole.Volunteer ? "volunteer" : "attendee";
            var titleStr = role == RegistrationService.Models.RegistrationRole.Volunteer
                ? "Volunteer registration confirmed"
                : "Registration confirmed";

            // Notify the registering user
            await PostNotification(notifyClient, userId,
                titleStr,
                $"You are now registered as a {roleName} for \"{eventTitle}\".");

            // Notify the organizer (if different from the registering user)
            if (!string.IsNullOrEmpty(organizerUserId) && int.TryParse(organizerUserId, out var orgId) && orgId != userId)
            {
                await PostNotification(notifyClient, orgId,
                    "New participant",
                    $"A user has registered as a {roleName} for \"{eventTitle}\".");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SendRegistrationNotifications] event={eventId}: {ex.Message}");
        }
    }

    private async Task SendCancellationNotificationsAsync(
        int userId, int eventId, string eventTitle, string roleName, string? organizerUserId)
    {
        try
        {
            var notifyClient = _httpFactory.CreateClient("notificationservice");

            await PostNotification(notifyClient, userId,
                "Registration cancelled",
                $"Your registration for \"{eventTitle}\" has been cancelled.");

            if (!string.IsNullOrEmpty(organizerUserId) && int.TryParse(organizerUserId, out var orgId) && orgId != userId)
            {
                await PostNotification(notifyClient, orgId,
                    "Registration cancelled",
                    $"A {roleName} has cancelled their registration for \"{eventTitle}\".");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SendCancellationNotifications] event={eventId}: {ex.Message}");
        }
    }

    private static async Task PostNotification(HttpClient client, int recipientUserId, string type, string message)
    {
        try
        {
            var resp = await client.PostAsJsonAsync("/api/notifications", new { recipientUserId, type, message });
            if (!resp.IsSuccessStatusCode)
                Console.Error.WriteLine($"[PostNotification] user={recipientUserId} status={resp.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[PostNotification] user={recipientUserId}: {ex.Message}");
        }
    }

    private int? GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(raw, out var id) ? id : null;
    }

    public record CreateRegistrationRequest(int EventId, RegistrationService.Models.RegistrationRole Role);
}
