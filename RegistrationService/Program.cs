using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();

// Named HTTP clients for service-to-service calls via Aspire service discovery
builder.Services.AddHttpClient("apiservice", c => c.BaseAddress = new Uri("https+http://apiservice"));
builder.Services.AddHttpClient("notificationservice", c => c.BaseAddress = new Uri("https+http://notificationservice"));

var connection = builder.Configuration.GetConnectionString("RegistrationDatabase") ?? "Data Source=registrations.db";
builder.Services.AddDbContext<RegistrationService.Data.RegistrationDbContext>(options =>
    options.UseSqlite(connection));

// JWT key: read from configuration (User Secrets / env var in production).
// The fallback is only used in Development and is exactly 32 bytes (256 bits) to satisfy HS256.
// In Production the key MUST be set explicitly; startup will throw if it is absent or too short.
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]   ?? "campus-event-manager";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "campus-event-manager";
var jwtKeyRaw   = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKeyRaw))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Jwt:Key must be configured in non-Development environments.");
    // 32-byte (256-bit) development-only fallback — never use in production
    jwtKeyRaw = "dev-only-key-32bytes-padding!!!!!";
}

var jwtKeyBytes = System.Text.Encoding.UTF8.GetBytes(jwtKeyRaw);
if (jwtKeyBytes.Length < 32)
    throw new InvalidOperationException($"Jwt:Key must be at least 32 bytes (256 bits) for HS256. Configured key is {jwtKeyBytes.Length} bytes.");

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

// Password hasher
builder.Services.AddScoped<IPasswordHasher<RegistrationService.Models.User>, PasswordHasher<RegistrationService.Models.User>>();

// Register validated JWT settings as a singleton so AuthController uses the same key
// that was validated at startup — no fallback in token generation.
builder.Services.AddSingleton(new RegistrationService.JwtSettings
{
    Issuer = jwtIssuer,
    Audience = jwtAudience,
    KeyBytes = jwtKeyBytes
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RegistrationService.Data.RegistrationDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
    RegistrationService.Data.DemoSeed.EnsureSeeded(app.Services);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
