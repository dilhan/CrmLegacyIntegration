namespace CrmLegacyIntegration.Core.Models;

/// <summary>
/// The nested payload the legacy membership system's API expects.
/// Property names here are ordinary PascalCase C# — the snake_case wire
/// format (given_name, plan_code, ...) comes entirely from the shared
/// <c>JsonNamingPolicy.SnakeCaseLower</c> policy in
/// <see cref="Json.LegacyJsonSerialization"/>, not from per-property JSON
/// attributes, so there's exactly one place that defines the wire shape.
/// </summary>
public class LegacyPayload
{
    public LegacyMember Member { get; set; } = new();
}

public class LegacyMember
{
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string Dob { get; set; } = string.Empty;
    public LegacyContact Contact { get; set; } = new();
    public string PlanCode { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

public class LegacyContact
{
    public string Email { get; set; } = string.Empty;
}
