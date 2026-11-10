using System.ComponentModel.DataAnnotations;

namespace CampusEventManager.ApiService.Dtos;

public record EventListItemDto(int Id, string Title, DateTime StartDateTime, DateTime EndDateTime, bool IsVirtual, string? Location, string? MeetingUrl, string Status);

public class CreateEventDto
{
    [Required]
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    [Required]
    public DateTime StartDateTime { get; set; }
    [Required]
    public DateTime EndDateTime { get; set; }
    public bool IsVirtual { get; set; }
    public string? Location { get; set; }
    public string? MeetingUrl { get; set; }
    public int? Capacity { get; set; }
}

public class UpdateEventDto
{
    [Required]
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    [Required]
    public DateTime StartDateTime { get; set; }
    [Required]
    public DateTime EndDateTime { get; set; }
    public bool IsVirtual { get; set; }
    public string? Location { get; set; }
    public string? MeetingUrl { get; set; }
    public int? Capacity { get; set; }
}
