using System.Net;
using Azure.Core.Serialization;
using CrmLegacyIntegration.Core.Security;
using CrmLegacyIntegration.Functions.Contracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using static CrmLegacyIntegration.Core.Json.LegacyJsonSerialization;

namespace CrmLegacyIntegration.Functions.Functions;

/// <summary>
/// POST /api/login — demo-only login: takes no body and checks no
/// credentials, and mints a token for the configured required role.
/// Exists purely so the secure endpoint is exercisable locally without
/// standing up a real identity provider. A real login endpoint would
/// authenticate the caller first.
/// </summary>
public class LoginFunction
{
    private readonly ILogger<LoginFunction> _logger;
    private readonly JwtAccessTokenIssuer _issuer;

    public LoginFunction(ILogger<LoginFunction> logger, JwtAccessTokenIssuer issuer)
    {
        _logger = logger;
        _issuer = issuer;
    }

    [Function("Login")]
    [OpenApiOperation(operationId: "Login", tags: new[] { "Auth (demo)" },
        Summary = "Mint a demo bearer token",
        Description = "Takes no body and checks no credentials — issues a token for the required role so the " +
                      "secure endpoint is exercisable locally. Not a real login flow.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LoginResponse))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "login")]
        HttpRequestData req,
        FunctionContext context)
    {
        using var _ = _logger.BeginScope(new Dictionary<string, object?> { ["InvocationId"] = context.InvocationId });

        _logger.LogInformation("Issuing a demo access token.");

        var token = _issuer.IssueToken();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new LoginResponse(token), new JsonObjectSerializer(ApiDefault), HttpStatusCode.OK);
        return response;
    }
}
