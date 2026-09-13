namespace CrmLegacyIntegration.Core.Security;

/// <summary>
/// Configuration for the demo JWT issuer/authenticator. Bound from the
/// "Jwt" section of configuration — in practice, Jwt__SigningKey and
/// Jwt__RequiredRole in the Functions host's local.settings.json Values.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// HMAC-SHA256 signing key, shared between issuer and authenticator.
    /// The value below is a local-dev-only default; never hardcode a real
    /// signing key in source. See the README's note on real-deployment auth.
    /// </summary>
    public string SigningKey { get; set; } = "local-dev-only-signing-key-replace-me-32bytes";

    /// <summary>The role claim required to call the secure endpoint.</summary>
    public string RequiredRole { get; set; } = "member-registrations.write";

    /// <summary>How long a token minted by POST /api/login stays valid.</summary>
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(1);
}
