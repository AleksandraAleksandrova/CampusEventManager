namespace RegistrationService.Models;

public class User
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool CanCreateEvents { get; set; }
}
