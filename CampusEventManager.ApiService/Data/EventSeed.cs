using CampusEventManager.ApiService.Data;
using CampusEventManager.ApiService.Models;

namespace CampusEventManager.ApiService.Data;

public static class EventSeed
{
    // Seeds demo events idempotently by title — skips any title that already exists.
    // Does NOT clear or overwrite manually created events.
    public static void EnsureSeeded(EventDbContext db)
    {
        const string creatorId = "1"; // Demo Creator seeded by RegistrationService
        var now = DateTime.UtcNow;

        var seeds = new[]
        {
            // 1. Past ~5 days — Published, physical, has capacity
            new Event
            {
                Title = "Introduction to Git & GitHub Workshop",
                Description = "A practical workshop covering Git fundamentals: branching, merging, pull requests and collaborative workflows on GitHub. Bring your laptop.",
                StartDateTime = now.AddDays(-5).Date.AddHours(14),
                EndDateTime   = now.AddDays(-5).Date.AddHours(17),
                IsVirtual = false,
                Location = "Lab 301, Computer Science Building",
                Capacity = 40,
                Status = EventStatus.Published,
                CreatedByUserId = creatorId,
                CreatedAt = now.AddDays(-20)
            },
            // 2. Past ~2 days — Published, physical, no capacity limit
            new Event
            {
                Title = "Campus Game Night",
                Description = "An evening of board games, card games and video games open to all students. Snacks provided. No registration required — just show up!",
                StartDateTime = now.AddDays(-2).Date.AddHours(18),
                EndDateTime   = now.AddDays(-2).Date.AddHours(22),
                IsVirtual = false,
                Location = "Student Union, Room 105",
                Capacity = null,
                Status = EventStatus.Published,
                CreatedByUserId = creatorId,
                CreatedAt = now.AddDays(-10)
            },
            // 3. Future ~5 days — Published, physical, has capacity
            new Event
            {
                Title = "AI & Software Engineering Meetup",
                Description = "Join fellow students and industry guests for talks on applying AI tools in modern software development. Lightning talks, demos and open discussion.",
                StartDateTime = now.AddDays(5).Date.AddHours(17),
                EndDateTime   = now.AddDays(5).Date.AddHours(20),
                IsVirtual = false,
                Location = "Auditorium B, Engineering Faculty",
                Capacity = 120,
                Status = EventStatus.Published,
                CreatedByUserId = creatorId,
                CreatedAt = now.AddDays(-3)
            },
            // 4. Future ~8 days — Published, virtual
            new Event
            {
                Title = "Online .NET Aspire Workshop",
                Description = "A hands-on online session exploring .NET Aspire: service orchestration, service discovery, health checks and the developer dashboard. Prerequisites: basic C# knowledge.",
                StartDateTime = now.AddDays(8).Date.AddHours(10),
                EndDateTime   = now.AddDays(8).Date.AddHours(13),
                IsVirtual = true,
                MeetingUrl = "https://meet.example.com/dotnet-aspire-workshop",
                Capacity = 80,
                Status = EventStatus.Published,
                CreatedByUserId = creatorId,
                CreatedAt = now.AddDays(-2)
            },
            // 5. Future ~12 days — Published, physical, no capacity limit
            new Event
            {
                Title = "Student Career Networking",
                Description = "Connect with alumni and industry professionals from the tech sector. Bring business cards or a QR code linking to your LinkedIn profile. Open to all years.",
                StartDateTime = now.AddDays(12).Date.AddHours(16),
                EndDateTime   = now.AddDays(12).Date.AddHours(19),
                IsVirtual = false,
                Location = "Main Hall, Building A",
                Capacity = null,
                Status = EventStatus.Published,
                CreatedByUserId = creatorId,
                CreatedAt = now.AddDays(-1)
            },
            // 6. Future — Cancelled, physical
            new Event
            {
                Title = "Student Hackathon",
                Description = "48-hour hackathon for student teams. Unfortunately this event has been cancelled due to insufficient venue availability during the exam period. We apologise for the inconvenience and plan to reschedule for next semester.",
                StartDateTime = now.AddDays(20).Date.AddHours(9),
                EndDateTime   = now.AddDays(22).Date.AddHours(9),
                IsVirtual = false,
                Location = "Innovation Hub, Technology Park",
                Capacity = 100,
                Status = EventStatus.Cancelled,
                CreatedByUserId = creatorId,
                CreatedAt = now.AddDays(-7)
            },
            // 7. Past ~10 days — Completed, physical
            new Event
            {
                Title = "Introduction to UX Design",
                Description = "An introductory session on user experience design principles: user research, wireframing, prototyping and usability testing. Hosted by the Design Society.",
                StartDateTime = now.AddDays(-10).Date.AddHours(13),
                EndDateTime   = now.AddDays(-10).Date.AddHours(16),
                IsVirtual = false,
                Location = "Design Studio, Arts Building",
                Capacity = 50,
                Status = EventStatus.Completed,
                CreatedByUserId = creatorId,
                CreatedAt = now.AddDays(-25)
            }
        };

        var existingTitles = db.Events.Select(e => e.Title).ToHashSet();
        var toAdd = seeds.Where(s => !existingTitles.Contains(s.Title)).ToList();
        if (toAdd.Count == 0) return;

        db.Events.AddRange(toAdd);
        db.SaveChanges();
    }
}
