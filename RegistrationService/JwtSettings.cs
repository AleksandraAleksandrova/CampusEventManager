namespace RegistrationService;

/// <summary>
/// Carries the validated JWT signing material built at startup.
/// Registered as a singleton so AuthController never re-reads configuration
/// and never falls back to an insecure default key.
/// </summary>
public sealed class JwtSettings
{
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public byte[] KeyBytes { get; init; } = null!;
}
