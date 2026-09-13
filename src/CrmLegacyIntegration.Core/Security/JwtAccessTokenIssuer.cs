using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CrmLegacyIntegration.Core.Security;

/// <summary>
/// Mints a minimal HS256 JWT carrying a single role claim, for the demo
/// POST /api/login endpoint. This is intentionally not a real login: it
/// takes no credentials and issues a token unconditionally for the
/// configured required role. A real login endpoint would authenticate the
/// caller (username/password, client credentials, ...) before issuing
/// anything.
/// </summary>
public class JwtAccessTokenIssuer
{
    private readonly JwtOptions _options;

    public JwtAccessTokenIssuer(JwtOptions options)
    {
        _options = options;
    }

    public string IssueToken()
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(_options.TokenLifetime);

        var header = new { alg = "HS256", typ = "JWT" };
        var payload = new Dictionary<string, object>
        {
            ["role"] = _options.RequiredRole,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = expires.ToUnixTimeSeconds()
        };

        var headerSegment = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
        var payloadSegment = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        var signingInput = $"{headerSegment}.{payloadSegment}";
        var signature = Sign(signingInput);

        return $"{signingInput}.{signature}";
    }

    private string Sign(string signingInput)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput));
        return Base64UrlEncode(hash);
    }

    internal static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
