using System.Net;
using System.Text.Json;
using Azure.Core.Serialization;
using CrmLegacyIntegration.Core.Json;
using CrmLegacyIntegration.Core.Mapping;
using CrmLegacyIntegration.Core.Models;
using CrmLegacyIntegration.Core.Validation;
using CrmLegacyIntegration.Functions.Contracts;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CrmLegacyIntegration.Functions.Functions;

/// <summary>
/// The shared parse -&gt; validate -&gt; map -&gt; respond logic used by both the
/// function-key-protected and JWT-protected mapping endpoints. Each HTTP
/// trigger is a thin adapter that differs only in how it checks the
/// caller before delegating here — every actual rule lives in Core.
/// </summary>
public static class MemberRegistrationHandler
{
    public static async Task<HttpResponseData> HandleAsync(HttpRequestData req, ILogger logger)
    {
        CrmRegistration? registration;
        try
        {
            registration = await JsonSerializer.DeserializeAsync<CrmRegistration>(
                req.Body, LegacyJsonSerialization.CrmRead);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Request body was not valid JSON.");
            return await WriteErrorResponse(req, HttpStatusCode.BadRequest,
                new[] { new ValidationError(null, "INVALID_JSON", "Request body is not valid JSON.") });
        }

        var validation = RegistrationValidator.Validate(registration);
        if (!validation.IsValid)
        {
            logger.LogInformation("Registration failed validation with {Count} error(s).", validation.Errors.Count);
            return await WriteErrorResponse(req, HttpStatusCode.BadRequest, validation.Errors);
        }

        var payload = LegacyPayloadMapper.Map(validation.Value!);

        // Deliberately not logging the email address — only the outcome
        // and membership type — since the request payload is personal data.
        logger.LogInformation(
            "Mapped registration for membershipType={MembershipType} to legacy payload.",
            validation.Value!.MembershipType);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(payload, new JsonObjectSerializer(LegacyJsonSerialization.LegacyWrite), HttpStatusCode.OK);
        return response;
    }

    private static async Task<HttpResponseData> WriteErrorResponse(
        HttpRequestData req, HttpStatusCode statusCode, IEnumerable<ValidationError> errors)
    {
        var response = req.CreateResponse(statusCode);
        // WriteAsJsonAsync overloads that don't take an explicit HttpStatusCode reset the
        // response to 200 OK in this Worker version (Microsoft-documented), silently
        // discarding the CreateResponse(statusCode) above — always pass it explicitly.
        await response.WriteAsJsonAsync(new ErrorResponse(errors.ToList()), new JsonObjectSerializer(LegacyJsonSerialization.ApiDefault), statusCode);
        return response;
    }
}
