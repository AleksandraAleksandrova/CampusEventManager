using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;

namespace RegistrationService.Data;

public static class DemoSeed
{
    // Creates two demo users when the database is empty: one creator and one ordinary user.
    public static void EnsureSeeded(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<RegistrationService.Models.User>>();

        if (db.Users.Any()) return;

        var creator = new RegistrationService.Models.User
        {
            DisplayName = "Demo Creator",
            Email = "creator@example.com",
            CanCreateEvents = true
        };
        creator.PasswordHash = hasher.HashPassword(creator, "Creator123!");

        var user = new RegistrationService.Models.User
        {
            DisplayName = "Demo User",
            Email = "user@example.com",
            CanCreateEvents = false
        };
        user.PasswordHash = hasher.HashPassword(user, "User123!");

        db.Users.AddRange(creator, user);
        db.SaveChanges();
    }
}
