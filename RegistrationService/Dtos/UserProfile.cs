namespace RegistrationService.Dtos;

public class UserProfile
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool CanCreateEvents { get; set; }
}
