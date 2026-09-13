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
    public IReadOnlyList<ValidationError> Errors { get; }
    public ValidatedRegistration? Value { get; }
    public bool IsValid => Value is not null;

    private ValidationResult(ValidatedRegistration? value, IReadOnlyList<ValidationError> errors)
    {
        Value = value;
        Errors = errors;
    }

    public static ValidationResult Success(ValidatedRegistration value) =>
        new(value, Array.Empty<ValidationError>());

    public static ValidationResult Failure(IReadOnlyList<ValidationError> errors) =>
        new(null, errors);
}
