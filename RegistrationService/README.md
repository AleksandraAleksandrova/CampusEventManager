RegistrationService - Development notes

JWT configuration
- The service reads JWT configuration from configuration keys: Jwt:Issuer, Jwt:Audience, Jwt:Key.
- Defaults are provided for local development when these keys are missing. The default signing key is "dev_signing_key_not_secure" and must NOT be used in production.

Demo accounts
- On first startup in Development environment the service will create two demo accounts if the user table is empty:
  - creator@example.com / Creator123!  (CanCreateEvents = true)
  - user@example.com / User123!        (CanCreateEvents = false)
- Use POST /api/auth/login to obtain a JWT for these accounts.

Seeding and security
- Public registration (POST /api/auth/register) never grants CanCreateEvents. Use the demo creator account to create events in ApiService for development.

Service discovery
- When run under the Aspire AppHost, calls to http://apiservice/... are resolved by Aspire service discovery. When running the RegistrationService standalone replace the host with the ApiService URL.

Database
- The service uses SQLite with default file registrations.db in the service working directory. Local DB files are ignored by .gitignore.

Known limitations
- Capacity enforcement has a race condition: concurrent attendee registrations may bypass capacity checks due to check-then-insert pattern. For production, consider serializing registration using DB-level transactions or optimistic concurrency.
- The service uses a simple JWT signing key for development. Rotate and secure keys for production and deploy proper secret management.
