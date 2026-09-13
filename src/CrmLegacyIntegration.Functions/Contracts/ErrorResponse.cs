using CrmLegacyIntegration.Core.Validation;

namespace CrmLegacyIntegration.Functions.Contracts;

/// <summary>
/// Every validation problem found, not just the first. Each error carries a
/// machine-readable <see cref="ValidationError.Code"/> (e.g.
/// "INVALID_MEMBERSHIP_TYPE") alongside the human-readable message, so the
/// CRM can branch on the failure reason instead of string-matching prose.
/// </summary>
public record ErrorResponse(IReadOnlyList<ValidationError> Errors);
