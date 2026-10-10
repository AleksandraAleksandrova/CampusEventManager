using Microsoft.EntityFrameworkCore;
using CampusEventManager.ApiService.Models;

namespace CampusEventManager.ApiService.Data;

public class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Title).IsRequired();
            b.Property(e => e.CreatedAt).IsRequired();
            b.Property(e => e.CreatedByUserId).IsRequired();
            b.Property(e => e.Status).HasConversion<string>().IsRequired();
        });
    }
}
