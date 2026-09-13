using CrmLegacyIntegration.Core.Security;
using FluentAssertions;
using Xunit;

namespace CrmLegacyIntegration.Core.Tests;

public class JwtBearerAuthenticatorTests
{
    private static JwtOptions Options() => new()
    {
        SigningKey = "unit-test-signing-key-32-bytes-min",
        RequiredRole = "member-registrations.write",
        TokenLifetime = TimeSpan.FromMinutes(30)
    };

    [Fact]
    public void Authenticate_MissingToken_ReturnsMissingToken()
    {
        var authenticator = new JwtBearerAuthenticator(Options());

        var result = authenticator.Authenticate(null);

        result.Outcome.Should().Be(JwtAuthenticationOutcome.MissingToken);
        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public void Authenticate_MalformedToken_ReturnsInvalidToken()
    {
        var authenticator = new JwtBearerAuthenticator(Options());

        var result = authenticator.Authenticate("not-a-jwt");

        result.Outcome.Should().Be(JwtAuthenticationOutcome.InvalidToken);
    }

    [Fact]
    public void Authenticate_TokenSignedWithDifferentKey_ReturnsInvalidToken()
    {
        var issuer = new JwtAccessTokenIssuer(new JwtOptions { SigningKey = "a-completely-different-key-value" });
        var authenticator = new JwtBearerAuthenticator(Options());

        var token = issuer.IssueToken();
        var result = authenticator.Authenticate(token);

        result.Outcome.Should().Be(JwtAuthenticationOutcome.InvalidToken);
    }

    [Fact]
    public void Authenticate_ExpiredToken_ReturnsExpired()
    {
        var options = Options();
        options.TokenLifetime = TimeSpan.FromSeconds(-1); // already expired the moment it's issued
        var issuer = new JwtAccessTokenIssuer(options);
        var authenticator = new JwtBearerAuthenticator(options);

        var token = issuer.IssueToken();
        var result = authenticator.Authenticate(token);

        result.Outcome.Should().Be(JwtAuthenticationOutcome.Expired);
    }

    [Fact]
    public void Authenticate_ValidTokenMissingRequiredRole_ReturnsMissingRequiredRole()
    {
        var issuerOptions = Options();
        issuerOptions.RequiredRole = "some.other.role";
        var issuer = new JwtAccessTokenIssuer(issuerOptions);

        var authenticator = new JwtBearerAuthenticator(Options()); // requires member-registrations.write

        var token = issuer.IssueToken();
        var result = authenticator.Authenticate(token);

        result.Outcome.Should().Be(JwtAuthenticationOutcome.MissingRequiredRole);
    }

    [Fact]
    public void Authenticate_ValidTokenWithRequiredRole_ReturnsValid()
    {
        var options = Options();
        var issuer = new JwtAccessTokenIssuer(options);
        var authenticator = new JwtBearerAuthenticator(options);

        var token = issuer.IssueToken();
        var result = authenticator.Authenticate(token);

        result.Outcome.Should().Be(JwtAuthenticationOutcome.Valid);
        result.IsAuthorized.Should().BeTrue();
    }
}
