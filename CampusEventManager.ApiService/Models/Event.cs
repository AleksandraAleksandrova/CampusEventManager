using System;

namespace CampusEventManager.ApiService.Models;

public enum EventStatus
{
    Published,
    Cancelled,
    Completed
}

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    // Stored in UTC
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsVirtual { get; set; }
    public string? Location { get; set; }
    public string? MeetingUrl { get; set; }
    public int? Capacity { get; set; }
    public EventStatus Status { get; set; }
    public string CreatedByUserId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
