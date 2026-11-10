# CampusEventManager

A .NET 10 / Aspire microservices demo for Technical University of Sofia.

## Architecture

| Project | Role | Database |
|---|---|---|
| `CampusEventManager.AppHost` | Aspire orchestration | — |
| `CampusEventManager.Web` | Blazor Server frontend | — |
| `CampusEventManager.ApiService` | Event management REST API | `events.db` (SQLite) |
| `RegistrationService` | User accounts, login, JWT, registrations | `registrations.db` (SQLite) |
| `NotificationService` | User notifications | `notifications.db` (SQLite) |
| `CampusEventManager.ServiceDefaults` | Shared Aspire configuration | — |

## Prerequisites

- .NET 10 SDK (preview)
- Visual Studio 2022 17.12+ or `dotnet` CLI

## JWT configuration (required before first run)

All three backend services share the same JWT issuer, audience, and signing key. The key must be **at least 32 bytes (256 bits)** for HS256.

The key is stored in **User Secrets** — never in source control. Run these three commands once after cloning:

```
cd CampusEventManager

dotnet user-secrets set "Jwt:Key" "CampusEventMgr-Dev-Key-2024-Secure!" --project RegistrationService\RegistrationService.csproj

dotnet user-secrets set "Jwt:Key" "CampusEventMgr-Dev-Key-2024-Secure!" --project CampusEventManager.ApiService\CampusEventManager.ApiService.csproj

dotnet user-secrets set "Jwt:Key" "CampusEventMgr-Dev-Key-2024-Secure!" --project NotificationService\NotificationService.csproj
```

> The key is treated as **UTF-8 text** in all three services. If you change it, update all three secrets to the same value.

The issuer and audience default to `campus-event-manager`. You can override them with `Jwt:Issuer` and `Jwt:Audience` User Secrets if needed (keep them identical across all three services).

In non-Development environments the application **throws at startup** if `Jwt:Key` is absent or shorter than 32 bytes.

## Running the application

```
dotnet run --project CampusEventManager.AppHost
```

The Aspire dashboard opens automatically. The web frontend URL is shown there (typically `https://localhost:7xxx`).

## Demo accounts (seeded on first startup in Development)

| Email | Password | Can create events |
|---|---|---|
| `creator@example.com` | `Creator123!` | Yes |
| `user@example.com` | `User123!` | No |

Accounts are only seeded when the `registrations.db` file does not yet contain any users.

## Manual test sequence

1. Open the web frontend URL.
2. The landing page (`/`) shows Login and Register buttons.
3. Log in as `user@example.com` / `User123!`. You are redirected to `/events`.
4. The sidebar shows Events, My Registrations, Notifications, Logout. No Manage Events link.
5. Click an event to open its detail page. Register as Attendee. A success message appears.
6. Navigate to My Registrations. The registration is listed. Cancel it.
7. Navigate to Notifications. A "RegistrationConfirmed" notification should appear.
8. Log out. The sidebar shows Login and Register again.
9. Log in as `creator@example.com` / `Creator123!`.
10. The sidebar now shows Manage Events.
11. Create an event (fill Title, Start, End, Location or Meeting URL). It appears in the list.
12. Edit the event. Save. Changes are reflected.
13. Cancel the event. Status changes to Cancelled.
14. Log in as `user@example.com` and verify the cancelled event shows a warning instead of registration buttons.

## Known limitations

- **Capacity race condition**: the check-then-insert pattern for attendee capacity is not atomic. Concurrent registrations near the capacity limit may both succeed. A database-level unique constraint prevents duplicate registrations for the same user+event.
- **Mark Complete**: requires the event's end time to have passed. Use a past end time when testing this flow.
- **Auth restoration flash**: on first page load there is a brief "Loading..." state while the JWT is restored from `localStorage` via JS interop. This is inherent to Blazor Server + JS interop.
