using Microsoft.EntityFrameworkCore;

namespace NotificationService.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<Models.Notification> Notifications => Set<Models.Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Models.Notification>(b =>
        {
            b.HasKey(n => n.Id);
            b.Property(n => n.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
