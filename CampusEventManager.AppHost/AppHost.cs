// Ensure minimal environment variables for the Aspire dashboard when running locally.
// These values are defaults for development only and are not secure for production.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    // Provide a default URL list so the dashboard can be configured.
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:5200;https://localhost:7200");
}

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"))
    && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL")))
{
    // Set a default OTLP HTTP endpoint for the Aspire dashboard exporters (development only).
    Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:4318");
}

// Allow unsecured (http) transport for local development when not explicitly set.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT")))
{
    Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
}

var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.CampusEventManager_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

// Start NotificationService before RegistrationService so notifications are available for registrations.
var notificationService = builder.AddProject<Projects.NotificationService>("notificationservice")
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

var registrationService = builder.AddProject<Projects.RegistrationService>("registrationservice")
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WithReference(notificationService)
    .WaitFor(apiService)
    .WaitFor(notificationService);

builder.AddProject<Projects.CampusEventManager_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WithReference(registrationService)
    .WithReference(notificationService)
    .WaitFor(apiService)
    .WaitFor(registrationService)
    .WaitFor(notificationService);
builder.Build().Run();
