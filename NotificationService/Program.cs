using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var connection = builder.Configuration.GetConnectionString("NotificationDatabase") ?? "Data Source=notifications.db";
builder.Services.AddDbContext<NotificationService.Data.NotificationDbContext>(options =>
    options.UseSqlite(connection));

// JWT — same issuer/audience/key as RegistrationService
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

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationService.Data.NotificationDbContext>();
    db.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
