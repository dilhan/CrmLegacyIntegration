using System.Net;
using Azure.Core.Serialization;
using CrmLegacyIntegration.Core.Models;
using CrmLegacyIntegration.Core.Security;
using CrmLegacyIntegration.Functions.Contracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace CrmLegacyIntegration.Functions.Functions;

/// <summary>
/// POST /api/secure/member-registrations — identical mapping to
/// MapMemberRegistrationFunction, but relies on application-code auth/authz
/// (a JWT bearer token with a required role) instead of a Functions-level
/// key, to show what that looks like. Both share
/// <see cref="MemberRegistrationHandler"/> for the actual parse/validate/map
/// logic; they only differ in how the caller is checked. The validation
/// itself (<see cref="JwtBearerAuthenticator"/>) is host-free and unit
/// tested like everything else in Core.
/// </summary>
public class MapMemberRegistrationSecureFunction
{
    private readonly ILogger<MapMemberRegistrationSecureFunction> _logger;
    private readonly JwtBearerAuthenticator _authenticator;

    public MapMemberRegistrationSecureFunction(
        ILogger<MapMemberRegistrationSecureFunction> logger,
        JwtBearerAuthenticator authenticator)
    {
        _logger = logger;
        _authenticator = authenticator;
    }

    [Function("MapMemberRegistrationSecure")]
    [OpenApiOperation(operationId: "MapMemberRegistrationSecure", tags: new[] { "Member Registrations" },
        Summary = "Same mapping, behind a bearer token (auth/authz demo)",
        Description = "Identical to POST /member-registrations, but requires a valid bearer token carrying the " +
                      "configured role instead of a function key. Call POST /login first to get a token.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CrmRegistration), Required = true,
        Description = "The CRM's member registration.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LegacyPayload),
        Description = "The legacy system's payload.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse),
        Description = "One or more validation errors.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Missing, malformed, or expired bearer token.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = "Token is valid but missing the required role.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "secure/member-registrations")]
        HttpRequestData req)
    {
        var authResult = _authenticator.Authenticate(ExtractBearerToken(req));

        if (!authResult.IsAuthorized)
        {
            var statusCode = authResult.Outcome == JwtAuthenticationOutcome.MissingRequiredRole
                ? HttpStatusCode.Forbidden
                : HttpStatusCode.Unauthorized;

            _logger.LogInformation("Rejected secure request: {Outcome}.", authResult.Outcome);

            var response = req.CreateResponse(statusCode);
            // Must pass statusCode explicitly: the overload without it resets the
            // response to 200 OK in this Worker version (Microsoft-documented),
            // discarding the CreateResponse(statusCode) above.
            await response.WriteAsJsonAsync(
                new { error = DescribeOutcome(authResult.Outcome) },
                new JsonObjectSerializer(Core.Json.LegacyJsonSerialization.ApiDefault),
                statusCode);
            return response;
        }

        return await MemberRegistrationHandler.HandleAsync(req, _logger);
    }

    private static string? ExtractBearerToken(HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("Authorization", out var values))
            return null;

        var header = values.FirstOrDefault();
        if (header is null || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        return header["Bearer ".Length..].Trim();
    }

    private static string DescribeOutcome(JwtAuthenticationOutcome outcome) => outcome switch
    {
        JwtAuthenticationOutcome.MissingToken => "Missing Authorization: Bearer <token> header.",
        JwtAuthenticationOutcome.InvalidToken => "Bearer token is malformed or has an invalid signature.",
        JwtAuthenticationOutcome.Expired => "Bearer token has expired.",
        JwtAuthenticationOutcome.MissingRequiredRole => "Bearer token does not carry the required role.",
        _ => "Unauthorized."
    };
}
