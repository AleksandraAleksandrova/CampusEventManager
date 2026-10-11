using System;

namespace CampusEventManager.Web.Models;

public class EventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsVirtual { get; set; }
    public string? Location { get; set; }
    public string? MeetingUrl { get; set; }
    public int? Capacity { get; set; }
    public string Status { get; set; } = null!;
    public string CreatedByUserId { get; set; } = null!;
}
