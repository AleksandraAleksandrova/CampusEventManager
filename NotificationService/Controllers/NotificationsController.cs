using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService.Data.NotificationDbContext _db;

    public NotificationsController(NotificationService.Data.NotificationDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyNotifications()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(uid) || !int.TryParse(uid, out var userId)) return Unauthorized();
        var nots = await _db.Notifications.Where(n => n.RecipientUserId == userId).OrderByDescending(n => n.CreatedAt).ToListAsync();
        return Ok(nots);
    }

    [Authorize]
    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(uid) || !int.TryParse(uid, out var userId)) return Unauthorized();

        var n = await _db.Notifications.FindAsync(id);
        if (n == null) return NotFound();
        if (n.RecipientUserId != userId) return Forbid();

        n.IsRead = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Allow other services to create a notification via POST /api/notifications
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var n = new NotificationService.Models.Notification
        {
            RecipientUserId = req.RecipientUserId,
            Type = req.Type,
            Message = req.Message,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _db.Notifications.Add(n);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyNotifications), new { id = n.Id }, n);
    }

    public record CreateNotificationRequest(int RecipientUserId, string Type, string Message);
}
