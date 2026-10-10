using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusEventManager.ApiService.Data;
using CampusEventManager.ApiService.Models;
using CampusEventManager.ApiService.Dtos;

namespace CampusEventManager.ApiService.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly EventDbContext _db;

    public EventsController(EventDbContext db)
    {
        _db = db;
    }

    // GET: api/events
    // Lists published events only
    [HttpGet]
    public async Task<IActionResult> ListPublished()
    {
        var events = await _db.Events
            .Where(e => e.Status == EventStatus.Published)
            .OrderBy(e => e.StartDateTime)
            .Select(e => new EventListItemDto(e.Id, e.Title, e.StartDateTime, e.EndDateTime, e.IsVirtual, e.Location, e.MeetingUrl, e.Status))
            .ToListAsync();

        return Ok(events);
    }

    // GET: api/events/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        return Ok(ev);
    }

    // POST: api/events
    // Creates and publishes an event immediately
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var validation = ValidateEventDto(dto.Title, dto.StartDateTime, dto.EndDateTime, dto.IsVirtual, dto.Location, dto.MeetingUrl, dto.Capacity);
        if (validation is not null) return BadRequest(validation);

        var now = DateTime.UtcNow;

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
            CreatedByUserId = dto.CreatedByUserId,
            CreatedAt = now
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = ev.Id }, ev);
    }

    // PUT: api/events/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        if (ev.Status == EventStatus.Cancelled || ev.Status == EventStatus.Completed)
            return BadRequest("Cannot modify a cancelled or completed event.");

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
        if (!string.IsNullOrWhiteSpace(dto.UpdatedByUserId))
        {
            ev.UpdatedByUserId = dto.UpdatedByUserId;
        }
        ev.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ev);
    }

    // POST: api/events/{id}/cancel
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelRequest? req)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        if (ev.Status == EventStatus.Cancelled) return BadRequest("Event already cancelled.");
        if (ev.Status == EventStatus.Completed) return BadRequest("Event already completed.");

        ev.Status = EventStatus.Cancelled;
        if (req?.UpdatedByUserId is not null)
        {
            ev.UpdatedByUserId = req.UpdatedByUserId;
            ev.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(ev);
    }

    // POST: api/events/{id}/complete
    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        if (ev.Status == EventStatus.Cancelled) return BadRequest("Cancelled event cannot be completed.");
        if (ev.Status == EventStatus.Completed) return BadRequest("Event already completed.");
        if (DateTime.UtcNow < ev.EndDateTime.ToUniversalTime()) return BadRequest("Event cannot be marked completed until after its end time.");

        ev.Status = EventStatus.Completed;
        ev.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ev);
    }

    private object? ValidateEventDto(string title, DateTime start, DateTime end, bool isVirtual, string? location, string? meetingUrl, int? capacity)
    {
        if (string.IsNullOrWhiteSpace(title)) return new { error = "Title is required." };

        var now = DateTime.UtcNow;
        var s = start.ToUniversalTime();
        var e = end.ToUniversalTime();
        if (s >= e) return new { error = "EndDateTime must be after StartDateTime." };
        if (s < now || e < now) return new { error = "Start and End times must be in the future." };

        if (isVirtual)
        {
            if (string.IsNullOrWhiteSpace(meetingUrl)) return new { error = "MeetingUrl is required for virtual events." };
        }
        else
        {
            if (string.IsNullOrWhiteSpace(location)) return new { error = "Location is required for in-person events." };
        }

        if (capacity.HasValue && capacity <= 0) return new { error = "Capacity must be greater than zero when provided." };

        return null;
    }

    public class CancelRequest
    {
        public string? UpdatedByUserId { get; set; }
    }
}
