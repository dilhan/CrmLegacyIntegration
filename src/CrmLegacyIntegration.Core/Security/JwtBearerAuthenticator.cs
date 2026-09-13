using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CrmLegacyIntegration.Core.Security;

public enum JwtAuthenticationOutcome
{
    Valid,
    MissingToken,
    InvalidToken,
    Expired,
    MissingRequiredRole
}

public record JwtAuthenticationResult(JwtAuthenticationOutcome Outcome)
{
    public bool IsAuthorized => Outcome == JwtAuthenticationOutcome.Valid;
}

/// <summary>
/// Validates an HS256 JWT's signature, expiry, and required role claim.
/// Host-free and independently unit tested, same as the rest of Core. A
/// real deployment would validate against an identity provider's
/// published signing keys (e.g. Entra ID via JWKS) instead of a shared
/// secret — see the README.
/// </summary>
public class JwtBearerAuthenticator
{
    private readonly JwtOptions _options;

    public JwtBearerAuthenticator(JwtOptions options)
    {
        _options = options;
    }

    public JwtAuthenticationResult Authenticate(string? bearerToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
            return new JwtAuthenticationResult(JwtAuthenticationOutcome.MissingToken);

        var parts = bearerToken.Split('.');
        if (parts.Length != 3)
            return new JwtAuthenticationResult(JwtAuthenticationOutcome.InvalidToken);

        var (headerSegment, payloadSegment, signatureSegment) = (parts[0], parts[1], parts[2]);

        var expectedSignature = Sign($"{headerSegment}.{payloadSegment}");
        if (!ConstantTimeEquals(expectedSignature, signatureSegment))
            return new JwtAuthenticationResult(JwtAuthenticationOutcome.InvalidToken);

        Dictionary<string, JsonElement>? claims;
        try
        {
            var payloadBytes = Base64UrlDecode(payloadSegment);
            claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadBytes);
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            return new JwtAuthenticationResult(JwtAuthenticationOutcome.InvalidToken);
        }

        if (claims is null)
            return new JwtAuthenticationResult(JwtAuthenticationOutcome.InvalidToken);

        if (!claims.TryGetValue("exp", out var expClaim) || !expClaim.TryGetInt64(out var expUnix))
            return new JwtAuthenticationResult(JwtAuthenticationOutcome.InvalidToken);

        if (DateTimeOffset.FromUnixTimeSeconds(expUnix) < DateTimeOffset.UtcNow)
            return new JwtAuthenticationResult(JwtAuthenticationOutcome.Expired);

        var role = claims.TryGetValue("role", out var roleClaim) ? roleClaim.GetString() : null;
        if (!string.Equals(role, _options.RequiredRole, StringComparison.Ordinal))
            return new JwtAuthenticationResult(JwtAuthenticationOutcome.MissingRequiredRole);

        return new JwtAuthenticationResult(JwtAuthenticationOutcome.Valid);
    }

    private string Sign(string signingInput)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput));
        return JwtAccessTokenIssuer.Base64UrlEncode(hash);
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        if (bytesA.Length != bytesB.Length) return false;
        return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }

    private static byte[] Base64UrlDecode(string segment)
    {
        var padded = segment.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty
        };
        return Convert.FromBase64String(padded);
    }
}
