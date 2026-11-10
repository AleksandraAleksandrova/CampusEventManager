using CampusEventManager.Web;
using CampusEventManager.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

// Removed example WeatherApiClient registration (not used in this project)

// Registration and notification clients
builder.Services.AddScoped<CampusEventManager.Web.Services.AuthService>();
builder.Services.AddScoped<CampusEventManager.Web.Services.TokenStorage>();

// Register auth header handler and add it to registration and notification clients.
builder.Services.AddTransient<CampusEventManager.Web.Services.AuthHeaderHandler>();
builder.Services.AddHttpClient("registration", client => client.BaseAddress = new Uri("https+http://registrationservice"))
    .AddHttpMessageHandler<CampusEventManager.Web.Services.AuthHeaderHandler>();
builder.Services.AddHttpClient("notification", client => client.BaseAddress = new Uri("https+http://notificationservice"))
    .AddHttpMessageHandler<CampusEventManager.Web.Services.AuthHeaderHandler>();
builder.Services.AddHttpClient("apiservice", client => client.BaseAddress = new Uri("https+http://apiservice"))
    .AddHttpMessageHandler<CampusEventManager.Web.Services.AuthHeaderHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
