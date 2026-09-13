using System.Net;
using CrmLegacyIntegration.Core.Tests.TestDoubles;
using CrmLegacyIntegration.Functions.Functions;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CrmLegacyIntegration.Core.Tests;

/// <summary>
/// Pins down the actual HTTP status code MemberRegistrationHandler returns
/// for each outcome. The rest of this project exercises RegistrationValidator
/// directly and never sees an HttpResponseData, so it can't catch a
/// WriteAsJsonAsync overload silently resetting a 400 back to 200 — this is
/// the layer that can.
/// </summary>
public class MemberRegistrationHandlerTests
{
    private static FunctionContext CreateContext() => new Mock<FunctionContext>().Object;

    private static async Task<FakeHttpResponseData> HandleAsync(string body)
    {
        var context = CreateContext();
        var request = new FakeHttpRequestData(context, body);

        var response = await MemberRegistrationHandler.HandleAsync(request, NullLogger.Instance);

        return (FakeHttpResponseData)response;
    }

    [Fact]
    public async Task HandleAsync_EmptyJsonObject_ReturnsBadRequest()
    {
        var response = await HandleAsync("{}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.ReadBodyAsString().Should().Contain("REQUIRED");
    }

    [Fact]
    public async Task HandleAsync_MalformedJson_ReturnsBadRequest()
    {
        var response = await HandleAsync("{ not valid json");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.ReadBodyAsString().Should().Contain("INVALID_JSON");
    }

    [Fact]
    public async Task HandleAsync_JsonNull_ReturnsBadRequest()
    {
        var response = await HandleAsync("null");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HandleAsync_ValidRegistration_ReturnsOkWithLegacyPayload()
    {
        const string body = """
            {
              "firstName": "Alex",
              "lastName": "Nguyen",
              "dateOfBirth": "1990-04-12",
              "email": "alex.nguyen@example.com",
              "membershipType": "Single"
            }
            """;

        var response = await HandleAsync(body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ReadBodyAsString().Should().Contain("plan_code");
    }
}
