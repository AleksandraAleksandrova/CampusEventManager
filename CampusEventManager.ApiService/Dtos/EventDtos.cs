using System;
using System.ComponentModel.DataAnnotations;
using CampusEventManager.ApiService.Models;

namespace CampusEventManager.ApiService.Dtos;

public record EventListItemDto(int Id, string Title, DateTime StartDateTime, DateTime EndDateTime, bool IsVirtual, string? Location, string? MeetingUrl, EventStatus Status);

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
    [Required]
    public string CreatedByUserId { get; set; } = null!; // Reference to Registration API user
}

public class UpdateEventDto
{
    [Required]
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    [Required]
    // Expecting local or offset-aware DateTime; controller will convert to UTC explicitly
    public DateTime StartDateTime { get; set; }
    [Required]
    public DateTime EndDateTime { get; set; }
    public bool IsVirtual { get; set; }
    public string? Location { get; set; }
    public string? MeetingUrl { get; set; }
    public int? Capacity { get; set; }
    public string? UpdatedByUserId { get; set; }
}
