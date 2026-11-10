namespace RegistrationService.Dtos;

public enum EventStatus
{
    Published,
    Cancelled,
    Completed
}

public class EventInfoDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public EventStatus Status { get; set; }
    public int? Capacity { get; set; }
    public string? CreatedByUserId { get; set; }
}
