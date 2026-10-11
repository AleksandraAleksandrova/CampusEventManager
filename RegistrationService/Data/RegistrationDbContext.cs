using Microsoft.EntityFrameworkCore;

namespace RegistrationService.Data;

public class RegistrationDbContext : DbContext
{
    public RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : base(options)
    {
    }

    public DbSet<Models.User> Users => Set<Models.User>();
    public DbSet<Models.Registration> Registrations => Set<Models.Registration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Models.User>(b =>
        {
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.CanCreateEvents).HasDefaultValue(false);
        });

        modelBuilder.Entity<Models.Registration>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasIndex(r => new { r.UserId, r.EventId }).IsUnique();
            b.HasOne<Models.User>()
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
