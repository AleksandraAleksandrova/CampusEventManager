using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure EF Core with SQLite for the registration service. Use a connection string from
// configuration or fall back to a local file 'registrations.db'.
var connection = builder.Configuration.GetConnectionString("RegistrationDatabase") ?? "Data Source=registrations.db";
builder.Services.AddDbContext<RegistrationService.Data.RegistrationDbContext>(options =>
    options.UseSqlite(connection));

// Authentication and JWT
builder.Services.AddAuthentication("Bearer").AddJwtBearer("Bearer", options =>
{
    // For now, validate issuer/audience/key from configuration.
    options.RequireHttpsMetadata = false;
});

builder.Services.AddAuthorization();

// Password hasher
builder.Services.AddScoped<IPasswordHasher<RegistrationService.Models.User>, PasswordHasher<RegistrationService.Models.User>>();

// Configure JWT validation
builder.Services.Configure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>("Bearer", options =>
{
    var issuer = builder.Configuration["Jwt:Issuer"] ?? "registration.local";
    var audience = builder.Configuration["Jwt:Audience"] ?? "registration.local";
    var key = builder.Configuration["Jwt:Key"] ?? "dev_signing_key_not_secure";

    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)),
        ValidateLifetime = true
    };
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Ensure DB is created in development
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RegistrationService.Data.RegistrationDbContext>();
    db.Database.EnsureCreated();
}

// Seed demo accounts in development only
if (app.Environment.IsDevelopment())
{
    RegistrationService.Data.DemoSeed.EnsureSeeded(app.Services);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
