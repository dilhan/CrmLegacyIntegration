using CrmLegacyIntegration.Core.Security;
using FluentAssertions;
using Xunit;

namespace CrmLegacyIntegration.Core.Tests;

public class JwtAccessTokenIssuerTests
{
    private static JwtOptions Options() => new()
    {
        SigningKey = "unit-test-signing-key-32-bytes-min",
        RequiredRole = "member-registrations.write",
        TokenLifetime = TimeSpan.FromMinutes(30)
    };

    [Fact]
    public void IssueToken_ReturnsWellFormedJwt()
    {
        var issuer = new JwtAccessTokenIssuer(Options());

        var token = issuer.IssueToken();

        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void IssueToken_IsAcceptedByAuthenticatorWithSameKey()
    {
        var options = Options();
        var issuer = new JwtAccessTokenIssuer(options);
        var authenticator = new JwtBearerAuthenticator(options);

        var token = issuer.IssueToken();
        var result = authenticator.Authenticate(token);

        result.IsAuthorized.Should().BeTrue();
    }
}
