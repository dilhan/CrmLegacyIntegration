using System.Text.Json;

namespace CrmLegacyIntegration.Core.Json;

/// <summary>
/// The single definition of the wire formats used by both the Functions
/// host and the tests, so "what does the JSON actually look like" is
/// defined and verified in exactly one place instead of drifting between
/// production code and test fixtures.
/// </summary>
public static class LegacyJsonSerialization
{
    /// <summary>For reading the CRM's incoming registration JSON (camelCase, case-insensitive).</summary>
    public static readonly JsonSerializerOptions CrmRead = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// For writing the legacy system's outgoing payload. This is the one
    /// place naming actually matters for the exercise's contract:
    /// snake_case, exactly as the legacy API expects.
    /// </summary>
    public static readonly JsonSerializerOptions LegacyWrite = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    /// <summary>
    /// For everything else this API itself returns — login tokens, error
    /// lists — which has nothing to do with the legacy system's contract
    /// and stays ordinary camelCase.
    /// </summary>
    public static readonly JsonSerializerOptions ApiDefault = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
}
