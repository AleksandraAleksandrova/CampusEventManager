using Microsoft.EntityFrameworkCore;
using CampusEventManager.ApiService.Data;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Controllers
builder.Services.AddControllers();

// Configure EF Core with SQLite. Connection string comes from configuration or falls back to a local file.
var connection = builder.Configuration.GetConnectionString("EventDatabase") ?? "Data Source=events.db";
builder.Services.AddDbContext<EventDbContext>(options => options.UseSqlite(connection));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Ensure database is created for local development
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Event Management API is running. Use /api/events to browse events.");

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
