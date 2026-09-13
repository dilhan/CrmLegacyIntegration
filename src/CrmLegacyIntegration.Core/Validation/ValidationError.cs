namespace CrmLegacyIntegration.Core.Validation;

/// <summary>
/// One failing validation rule. <see cref="Code"/> is a small closed set of
/// machine-readable identifiers (e.g. "REQUIRED", "INVALID_EMAIL",
/// "INVALID_MEMBERSHIP_TYPE") a caller can branch on without string-matching
/// <see cref="Message"/>, which stays free to reword for humans without
/// becoming a breaking change. <see cref="Field"/> is the offending
/// CrmRegistration property name (camelCase, matching the wire format), or
/// null for an error that isn't about a single field (e.g. an unparsable
/// request body).
/// </summary>
public record ValidationError(string? Field, string Code, string Message);
