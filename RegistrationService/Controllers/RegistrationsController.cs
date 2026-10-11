using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace RegistrationService.Controllers;

[ApiController]
[Route("api/registrations")]
public class RegistrationsController : ControllerBase
{
    private readonly RegistrationService.Data.RegistrationDbContext _db;
    private readonly HttpClient _http;

    public RegistrationsController(RegistrationService.Data.RegistrationDbContext db, IHttpClientFactory httpFactory)
    {
        _db = db;
        _http = httpFactory.CreateClient();
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRegistrationRequest req)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(uid) || !int.TryParse(uid, out var userId)) return Unauthorized();

        if (req.Role == RegistrationService.Models.RegistrationRole.Organizer)
        {
            return BadRequest(new { error = "Cannot create Organizer registration via this endpoint." });
        }

        // Verify event exists and is published via ApiService (service discovery resolved HttpClient will route)
        var eventResp = await _http.GetAsync($"http://apiservice/api/events/{req.EventId}");
        if (eventResp.StatusCode == System.Net.HttpStatusCode.NotFound) return NotFound(new { error = "Event not found." });
        if (!eventResp.IsSuccessStatusCode) return StatusCode(502, new { error = "Failed to validate event with ApiService." });

        var ev = await eventResp.Content.ReadFromJsonAsync<RegistrationService.Dtos.EventInfoDto>();
        if (ev == null) return StatusCode(502, new { error = "Invalid event response." });
        if (ev.Status != RegistrationService.Dtos.EventStatus.Published) return BadRequest(new { error = "Event is not published." });

        // Capacity check: count attendee registrations
        if (req.Role == RegistrationService.Models.RegistrationRole.Attendee && ev.Capacity.HasValue)
        {
            int capacity = ev.Capacity.Value;
            var attendeeCount = await _db.Registrations.CountAsync(r => r.EventId == req.EventId && r.Role == RegistrationService.Models.RegistrationRole.Attendee);
            if (attendeeCount >= capacity) return BadRequest(new { error = "Event capacity reached." });
        }

        var existing = await _db.Registrations.SingleOrDefaultAsync(r => r.UserId == userId && r.EventId == req.EventId);
        if (existing != null) return Conflict(new { error = "Already registered for event." });

        var reg = new RegistrationService.Models.Registration
        {
            UserId = userId,
            EventId = req.EventId,
            Role = req.Role,
            RegisteredAt = DateTime.UtcNow
        };

        _db.Registrations.Add(reg);
        await _db.SaveChangesAsync();

        // Notify NotificationService about the successful registration.
        // Use service discovery logical name 'notificationservice' so AppHost resolves it.
        try
        {
            var notifyReq = new
            {
                recipientUserId = userId,
                type = "RegistrationConfirmed",
                message = $"You are registered for event {req.EventId}.",
                createdAt = DateTime.UtcNow
            };

            // Best-effort: we do not make registration dependent on notification success.
            await _http.PostAsJsonAsync("http://notificationservice/api/notifications", notifyReq);
        }
        catch
        {
            // Log or ignore; do not fail registration because notification failed.
        }

        return CreatedAtAction(nameof(GetMyRegistrations), new { id = reg.Id }, reg);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyRegistrations()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(uid) || !int.TryParse(uid, out var userId)) return Unauthorized();

        var regs = await _db.Registrations.Where(r => r.UserId == userId).ToListAsync();
        return Ok(regs);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(uid) || !int.TryParse(uid, out var userId)) return Unauthorized();

        var reg = await _db.Registrations.FindAsync(id);
        if (reg == null) return NotFound();
        if (reg.UserId != userId) return Forbid();

        _db.Registrations.Remove(reg);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpPost("{eventId:int}/become-organizer")]
    public async Task<IActionResult> BecomeOrganizer(int eventId)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(uid) || !int.TryParse(uid, out var userId)) return Unauthorized();

        // Validate event and creator via ApiService
        var eventResp = await _http.GetAsync($"http://apiservice/api/events/{eventId}");
        if (!eventResp.IsSuccessStatusCode) return StatusCode(502, new { error = "Failed to validate event with ApiService." });

        var ev = await eventResp.Content.ReadFromJsonAsync<RegistrationService.Dtos.EventInfoDto>();
        if (ev == null) return StatusCode(502, new { error = "Invalid event response." });

        if (ev.CreatedByUserId != uid) return Forbid();

        var existing = await _db.Registrations.SingleOrDefaultAsync(r => r.UserId == userId && r.EventId == eventId);
        if (existing != null) return Conflict(new { error = "Already registered for event." });

        var reg = new RegistrationService.Models.Registration
        {
            UserId = userId,
            EventId = eventId,
            Role = RegistrationService.Models.RegistrationRole.Organizer,
            RegisteredAt = DateTime.UtcNow
        };

        _db.Registrations.Add(reg);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyRegistrations), new { id = reg.Id }, reg);
    }

    public record CreateRegistrationRequest(int EventId, RegistrationService.Models.RegistrationRole Role);
}
