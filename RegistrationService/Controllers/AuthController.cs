using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RegistrationService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegistrationService.Data.RegistrationDbContext _db;
    private readonly IPasswordHasher<RegistrationService.Models.User> _hasher;
    private readonly IConfiguration _config;

    public AuthController(
        RegistrationService.Data.RegistrationDbContext db,
        IPasswordHasher<RegistrationService.Models.User> hasher,
        IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict(new { error = "Email already registered." });

        var user = new RegistrationService.Models.User
        {
            DisplayName = req.DisplayName,
            Email = req.Email,
            CanCreateEvents = false   // public registration never grants creator rights
        };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Me), new { }, new { user.Id, user.DisplayName, user.Email });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == req.Email);
        if (user is null) return Unauthorized();

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        if (result == PasswordVerificationResult.Failed) return Unauthorized();

        return Ok(new { access_token = CreateToken(user) });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(uid) || !int.TryParse(uid, out var id)) return Unauthorized();

        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        return Ok(new { user.Id, user.DisplayName, user.Email, user.CanCreateEvents });
    }

    private string CreateToken(RegistrationService.Models.User user)
    {
        var issuer   = _config["Jwt:Issuer"]   ?? "campus-event-manager";
        var audience = _config["Jwt:Audience"] ?? "campus-event-manager";
        var keyRaw   = _config["Jwt:Key"]      ?? "dev-only-key-32bytes-padding!!!!!";

        var keyBytes = Encoding.UTF8.GetBytes(keyRaw);
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("canCreateEvents", user.CanCreateEvents.ToString().ToLowerInvariant())
        };

        var token = new JwtSecurityToken(
            issuer, audience, claims,
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public record RegisterRequest(string DisplayName, string Email, string Password);
    public record LoginRequest(string Email, string Password);
}
