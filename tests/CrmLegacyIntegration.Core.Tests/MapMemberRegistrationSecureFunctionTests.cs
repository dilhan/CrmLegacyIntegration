using System.Net;
using CrmLegacyIntegration.Core.Security;
using CrmLegacyIntegration.Core.Tests.TestDoubles;
using CrmLegacyIntegration.Functions.Functions;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CrmLegacyIntegration.Core.Tests;

/// <summary>Verifies the secure endpoint returns the correct HTTP status code for each auth outcome.</summary>
public class MapMemberRegistrationSecureFunctionTests
{
    private static readonly JwtOptions Options = new();

    private static FunctionContext CreateContext() => new Mock<FunctionContext>().Object;

    private static MapMemberRegistrationSecureFunction CreateFunction() =>
        new(NullLogger<MapMemberRegistrationSecureFunction>.Instance, new JwtBearerAuthenticator(Options));

    private static FakeHttpRequestData RequestWithBearerToken(FunctionContext context, string? token, string body = "{}")
    {
        var headers = new HttpHeadersCollection();
        if (token is not null)
            headers.Add("Authorization", $"Bearer {token}");

        return new FakeHttpRequestData(context, body, headers);
    }

    [Fact]
    public async Task Run_MissingBearerToken_ReturnsUnauthorized()
    {
        var context = CreateContext();
        var request = new FakeHttpRequestData(context, "{}");

        var response = (FakeHttpResponseData)await CreateFunction().Run(request, context);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Run_MalformedBearerToken_ReturnsUnauthorized()
    {
        var context = CreateContext();
        var request = RequestWithBearerToken(context, "not-a-jwt");

        var response = (FakeHttpResponseData)await CreateFunction().Run(request, context);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Run_TokenMissingRequiredRole_ReturnsForbidden()
    {
        var context = CreateContext();
        var tokenWithoutRole = new JwtAccessTokenIssuer(new JwtOptions { RequiredRole = "some.other.role" }).IssueToken();
        var request = RequestWithBearerToken(context, tokenWithoutRole);

        var response = (FakeHttpResponseData)await CreateFunction().Run(request, context);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Run_ValidToken_DelegatesToHandlerAndReturnsOk()
    {
        var context = CreateContext();
        var validToken = new JwtAccessTokenIssuer(Options).IssueToken();
        const string body = """
            {
              "firstName": "Alex",
              "lastName": "Nguyen",
              "dateOfBirth": "1990-04-12",
              "email": "alex.nguyen@example.com",
              "membershipType": "Single"
            }
            """;
        var request = RequestWithBearerToken(context, validToken, body);

        var response = (FakeHttpResponseData)await CreateFunction().Run(request, context);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
