namespace CrmLegacyIntegration.Functions.Contracts;

/// <summary>Every validation problem found, not just the first.</summary>
public record ErrorResponse(IReadOnlyList<string> Errors);
