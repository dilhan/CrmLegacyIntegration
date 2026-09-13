using CrmLegacyIntegration.Core.Models;

namespace CrmLegacyIntegration.Core.Validation;

/// <summary>
/// Either every failing rule found (never just the first), or the
/// normalized <see cref="ValidatedRegistration"/> ready for mapping.
/// Validation returns this instead of throwing, so a single response can
/// tell the caller everything wrong with the request at once. Exceptions
/// stay reserved for genuinely unexpected failures, not expected bad input.
/// </summary>
public class ValidationResult
{
    public IReadOnlyList<string> Errors { get; }
    public ValidatedRegistration? Value { get; }
    public bool IsValid => Value is not null;

    private ValidationResult(ValidatedRegistration? value, IReadOnlyList<string> errors)
    {
        Value = value;
        Errors = errors;
    }

    public static ValidationResult Success(ValidatedRegistration value) =>
        new(value, Array.Empty<string>());

    public static ValidationResult Failure(IReadOnlyList<string> errors) =>
        new(null, errors);
}
