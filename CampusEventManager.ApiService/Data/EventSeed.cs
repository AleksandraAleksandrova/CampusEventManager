using CampusEventManager.ApiService.Data;
using CampusEventManager.ApiService.Models;

namespace CampusEventManager.ApiService.Data;

public static class EventSeed
{
    // Seeds demo events only when the events table is empty.
    // creatorUserId must match the seeded creator user's ID in RegistrationService (always 1 for the first seeded user).
    public static void EnsureSeeded(EventDbContext db)
    {
        if (db.Events.Any()) return;

        const string creatorId = "1"; // Demo Creator seeded by RegistrationService
        var now = DateTime.UtcNow;

        var events = new[]
        {
            // Past event 1 — Published, ended → Complete button visible
            new Event
            {
                Title = "Introduction to Cloud Computing",
                Description = "A hands-on workshop covering the fundamentals of cloud infrastructure, IaaS, PaaS and SaaS with live demos on AWS.",
                StartDateTime = now.AddDays(-10).Date.AddHours(10),
                EndDateTime   = now.AddDays(-10).Date.AddHours(12),
                IsVirtual = true,
                MeetingUrl = "https://meet.example.com/cloud-intro",
                Capacity = 60,
                Status = EventStatus.Published,
                CreatedByUserId = creatorId,
                CreatedAt = now.AddDays(-20)
            },
            // Past event 2 — Published, ended → Complete button visible
            new Event
            {
                Title = "Career Fair — Spring 2025",
                Description = "Meet recruiters from over 30 companies. Bring your CV and dress professionally. Open to all students.",
                StartDateTime = now.AddDays(-3).Date.AddHours(9),
                EndDateTime   = now.AddDays(-3).Date.AddHours(17),
                IsVirtual = false,
                Location = "Main Hall, Building A",
                Capacity = 300,
                Status = EventStatus.Published,
                CreatedByUserId = creatorId,
                CreatedAt = now.AddDays(-14)
            },
            // Future event — Published, not ended → Complete button NOT visible
            new Event
            {
                Title = "Web Development Bootcamp",
                Description = "A two-day intensive bootcamp covering HTML, CSS, JavaScript and React. No prior experience required.",
                StartDateTime = now.AddDays(7).Date.AddHours(9),
                EndDateTime   = now.AddDays(8).Date.AddHours(17),
                IsVirtual = false,
                Location = "Lab 204, Engineering Building",
                Capacity = 30,
                Status = EventStatus.Published,
                CreatedByUserId = creatorId,
                CreatedAt = now.AddDays(-5)
            },
            // Already completed event — demonstrates final state
            new Event
            {
                Title = "Orientation Day — New Students",
                Description = "Welcome session for all new students. Campus tour, introduction to student services and Q&A with faculty.",
                StartDateTime = now.AddDays(-30).Date.AddHours(10),
                EndDateTime   = now.AddDays(-30).Date.AddHours(14),
                IsVirtual = false,
                Location = "Auditorium, Main Campus",
                Capacity = 200,
                Status = EventStatus.Completed,
                CreatedByUserId = creatorId,
                CreatedAt = now.AddDays(-45)
            }
        };

        db.Events.AddRange(events);
        db.SaveChanges();
    }
}
