using Microsoft.EntityFrameworkCore;
using CampusEventManager.ApiService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var connection = builder.Configuration.GetConnectionString("EventDatabase") ?? "Data Source=events.db";
builder.Services.AddDbContext<EventDbContext>(options => options.UseSqlite(connection));

builder.Services.AddOpenApi();

// HTTP clients for service-to-service notification calls
builder.Services.AddHttpClient("registrationservice", c => c.BaseAddress = new Uri("https+http://registrationservice"));
builder.Services.AddHttpClient("notificationservice", c => c.BaseAddress = new Uri("https+http://notificationservice"));

// JWT — same issuer/audience/key as RegistrationService (tokens issued there are validated here)
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]   ?? "campus-event-manager";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "campus-event-manager";
var jwtKeyRaw   = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKeyRaw))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Jwt:Key must be configured in non-Development environments.");
    jwtKeyRaw = "dev-only-key-32bytes-padding!!!!!";
}

var jwtKeyBytes = System.Text.Encoding.UTF8.GetBytes(jwtKeyRaw);
if (jwtKeyBytes.Length < 32)
    throw new InvalidOperationException($"Jwt:Key must be at least 32 bytes for HS256. Got {jwtKeyBytes.Length}.");

builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(jwtKeyBytes),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
    db.Database.EnsureCreated();
    if (app.Environment.IsDevelopment())
        CampusEventManager.ApiService.Data.EventSeed.EnsureSeeded(db);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapGet("/", () => "Event Management API is running.");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
