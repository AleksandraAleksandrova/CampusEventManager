using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using CampusEventManager.ApiService.Data;
using CampusEventManager.ApiService.Models;
using CampusEventManager.ApiService.Dtos;

namespace CampusEventManager.ApiService.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly EventDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private static readonly JsonSerializerOptions _enumOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public EventsController(EventDbContext db, IHttpClientFactory httpFactory)
    {
        _db = db;
        _httpFactory = httpFactory;
    }

    [HttpGet]
    public async Task<IActionResult> ListPublished()
    {
        var events = await _db.Events
            .Where(e => e.Status == EventStatus.Published)
            .OrderBy(e => e.StartDateTime)
            .ToListAsync();
        return Ok(events);
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<IActionResult> ListMine()
    {
        var uid = GetUserId();
        if (uid is null) return Unauthorized();
        var events = await _db.Events
            .Where(e => e.CreatedByUserId == uid)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
        return Ok(events);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        return Ok(ev);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var uid = GetUserId();
        if (uid is null) return Unauthorized();

        var validation = ValidateEventDto(dto.Title, dto.StartDateTime, dto.EndDateTime, dto.IsVirtual, dto.Location, dto.MeetingUrl, dto.Capacity);
        if (validation is not null) return BadRequest(validation);

        var ev = new Event
        {
            Title = dto.Title,
            Description = dto.Description,
            StartDateTime = dto.StartDateTime.ToUniversalTime(),
            EndDateTime = dto.EndDateTime.ToUniversalTime(),
            IsVirtual = dto.IsVirtual,
            Location = dto.IsVirtual ? null : dto.Location,
            MeetingUrl = dto.IsVirtual ? dto.MeetingUrl : null,
            Capacity = dto.Capacity,
            Status = EventStatus.Published,
            CreatedByUserId = uid,
            CreatedAt = DateTime.UtcNow
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        _ = SendCreatorNotificationAsync(uid, ev.Title, "Event created",
            $"\"{ev.Title}\" was successfully created.");

        return CreatedAtAction(nameof(Get), new { id = ev.Id }, ev);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var uid = GetUserId();
        if (uid is null) return Unauthorized();

        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        if (ev.CreatedByUserId != uid) return Forbid();
        if (ev.Status == EventStatus.Cancelled || ev.Status == EventStatus.Completed)
            return BadRequest(new { error = "Cannot modify a cancelled or completed event." });

        var validation = ValidateEventDto(dto.Title, dto.StartDateTime, dto.EndDateTime, dto.IsVirtual, dto.Location, dto.MeetingUrl, dto.Capacity);
        if (validation is not null) return BadRequest(validation);

        ev.Title = dto.Title;
        ev.Description = dto.Description;
        ev.StartDateTime = dto.StartDateTime.ToUniversalTime();
        ev.EndDateTime = dto.EndDateTime.ToUniversalTime();
        ev.IsVirtual = dto.IsVirtual;
        ev.Location = dto.IsVirtual ? null : dto.Location;
        ev.MeetingUrl = dto.IsVirtual ? dto.MeetingUrl : null;
        ev.Capacity = dto.Capacity;
        ev.UpdatedByUserId = uid;
        ev.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _ = SendCreatorNotificationAsync(uid, ev.Title, "Event updated",
            $"\"{ev.Title}\" has been updated.");
        _ = NotifyRegistrantsAsync(ev.Id, ev.Title, "Event updated",
            $"\"{ev.Title}\" has been updated.");

        return Ok(ev);
    }

    [Authorize]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var uid = GetUserId();
        if (uid is null) return Unauthorized();

        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        if (ev.CreatedByUserId != uid) return Forbid();
        if (ev.Status == EventStatus.Cancelled) return BadRequest(new { error = "Event already cancelled." });
        if (ev.Status == EventStatus.Completed) return BadRequest(new { error = "Event already completed." });

        ev.Status = EventStatus.Cancelled;
        ev.UpdatedByUserId = uid;
        ev.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _ = NotifyRegistrantsAsync(ev.Id, ev.Title, "Event cancelled",
            $"\"{ev.Title}\" has been cancelled by the organizer.");

        return Ok(ev);
    }

    [Authorize]
    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var uid = GetUserId();
        if (uid is null) return Unauthorized();

        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        if (ev.CreatedByUserId != uid) return Forbid();
        if (ev.Status == EventStatus.Cancelled) return BadRequest(new { error = "Cancelled event cannot be completed." });
        if (ev.Status == EventStatus.Completed) return BadRequest(new { error = "Event already completed." });
        if (DateTime.UtcNow < ev.EndDateTime.ToUniversalTime())
            return BadRequest(new { error = "Event cannot be marked completed until after its end time." });

        ev.Status = EventStatus.Completed;
        ev.UpdatedByUserId = uid;
        ev.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _ = NotifyRegistrantsAsync(ev.Id, ev.Title, "Event completed",
            $"\"{ev.Title}\" has been marked as completed.");

        return Ok(ev);
    }

    private async Task SendCreatorNotificationAsync(string? creatorId, string eventTitle, string type, string message)
    {
        if (string.IsNullOrEmpty(creatorId) || !int.TryParse(creatorId, out var uid)) return;
        try
        {
            var notifyClient = _httpFactory.CreateClient("notificationservice");
            await notifyClient.PostAsJsonAsync("/api/notifications", new { recipientUserId = uid, type, message });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SendCreatorNotification] {ex.Message}");
        }
    }

    // Fetch registrant user IDs from RegistrationService, then post a notification for each.
    // Fire-and-forget: failures are logged but do not affect the response.
    private async Task NotifyRegistrantsAsync(int eventId, string eventTitle, string type, string message)
    {
        try
        {
            var regClient = _httpFactory.CreateClient("registrationservice");
            var resp = await regClient.GetAsync($"/api/registrations/event/{eventId}");
            if (!resp.IsSuccessStatusCode) return;

            var userIds = await resp.Content.ReadFromJsonAsync<List<int>>(_enumOptions);
            if (userIds is null || userIds.Count == 0) return;

            var notifyClient = _httpFactory.CreateClient("notificationservice");
            foreach (var userId in userIds)
            {
                try
                {
                    await notifyClient.PostAsJsonAsync("/api/notifications", new
                    {
                        recipientUserId = userId,
                        type,
                        message
                    });
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[NotifyRegistrants] Failed to notify user {userId}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[NotifyRegistrants] Failed to fetch registrants for event {eventId}: {ex.Message}");
        }
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    private static object? ValidateEventDto(string title, DateTime start, DateTime end, bool isVirtual, string? location, string? meetingUrl, int? capacity)
    {
        if (string.IsNullOrWhiteSpace(title)) return new { error = "Title is required." };
        var s = start.ToUniversalTime();
        var e = end.ToUniversalTime();
        if (s >= e) return new { error = "EndDateTime must be after StartDateTime." };
        if (s < DateTime.UtcNow) return new { error = "Start time must be in the future." };
        if (isVirtual && string.IsNullOrWhiteSpace(meetingUrl)) return new { error = "MeetingUrl is required for virtual events." };
        if (!isVirtual && string.IsNullOrWhiteSpace(location)) return new { error = "Location is required for in-person events." };
        if (capacity.HasValue && capacity <= 0) return new { error = "Capacity must be greater than zero." };
        return null;
    }
}
