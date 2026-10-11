using System;

namespace RegistrationService.Models;

public enum RegistrationRole
{
    Attendee,
    Volunteer,
    Organizer
}

public class Registration
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }
    public RegistrationRole Role { get; set; }
    public DateTime RegisteredAt { get; set; }
}
