using System;

namespace NotificationService.Models;

public class Notification
{
    public int Id { get; set; }
    public int RecipientUserId { get; set; }
    public string Type { get; set; } = null!;
    public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
