namespace CrmLegacyIntegration.Core.Models;

/// <summary>
/// The registration payload exactly as posted by the CRM (Dynamics 365) —
/// no parsing or normalization yet. Unknown JSON properties are ignored by
/// System.Text.Json's default behavior; see JsonContractTests for a test
/// that pins that down rather than leaving it as an accident.
/// </summary>
public class CrmRegistration
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? MembershipType { get; set; }
    public string? RegisteredAt { get; set; }
}
