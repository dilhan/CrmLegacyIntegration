using System.Net;
using CrmLegacyIntegration.Core.Models;
using CrmLegacyIntegration.Functions.Contracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace CrmLegacyIntegration.Functions.Functions;

/// <summary>
/// POST /api/member-registrations — the main mapping endpoint. Least-privilege
/// AuthorizationLevel.Function for a real deployment; Azure Functions Core
/// Tools doesn't enforce that key locally, so it doesn't get in the way
/// of local testing.
/// </summary>
public class MapMemberRegistrationFunction
{
    private readonly ILogger<MapMemberRegistrationFunction> _logger;

    public MapMemberRegistrationFunction(ILogger<MapMemberRegistrationFunction> logger)
    {
        _logger = logger;
    }

    [Function("MapMemberRegistration")]
    [OpenApiOperation(operationId: "MapMemberRegistration", tags: new[] { "Member Registrations" },
        Summary = "Convert a CRM registration into the legacy system's payload",
        Description = "Validates the CRM's member registration and maps it to the legacy membership system's JSON contract.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CrmRegistration), Required = true,
        Description = "The CRM's member registration.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LegacyPayload),
        Description = "The legacy system's payload.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse),
        Description = "One or more validation errors.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "member-registrations")]
        HttpRequestData req,
        FunctionContext context)
    {
        using var _ = _logger.BeginScope(new Dictionary<string, object?> { ["InvocationId"] = context.InvocationId });
        return await MemberRegistrationHandler.HandleAsync(req, _logger);
    }
}
