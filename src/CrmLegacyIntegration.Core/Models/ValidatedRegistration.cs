namespace CrmLegacyIntegration.Core.Models;

/// <summary>
/// A CRM registration that has already passed validation: required fields
/// are present, the date of birth parses to a real calendar date, the
/// email is at least plausible, and the membership type is one of the
/// known values. <see cref="Mapping.LegacyPayloadMapper"/> works from this
/// type, never from the raw <see cref="CrmRegistration"/>, so mapping
/// never has to re-check any of that itself.
/// </summary>
public record ValidatedRegistration(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string MembershipType,
    DateTimeOffset? RegisteredAt);
