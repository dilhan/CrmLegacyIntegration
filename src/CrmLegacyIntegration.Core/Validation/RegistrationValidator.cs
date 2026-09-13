using System.Text.RegularExpressions;
using CrmLegacyIntegration.Core.Models;

namespace CrmLegacyIntegration.Core.Validation;

public static class RegistrationValidator
{
    // A pragmatic "plausible email" check — not a full RFC 5322 validator,
    // which is intentionally out of scope for this exercise.
    private static readonly Regex EmailPattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static ValidationResult Validate(CrmRegistration? registration)
    {
        if (registration is null)
            return ValidationResult.Failure(new[] { "Request body is required." });

        var errors = new List<string>();

        var firstName = registration.FirstName?.Trim();
        if (string.IsNullOrWhiteSpace(firstName))
            errors.Add("firstName is required.");

        var lastName = registration.LastName?.Trim();
        if (string.IsNullOrWhiteSpace(lastName))
            errors.Add("lastName is required.");

        var dateOfBirth = default(DateOnly);
        if (string.IsNullOrWhiteSpace(registration.DateOfBirth))
        {
            errors.Add("dateOfBirth is required.");
        }
        else if (!DateOfBirthParser.TryParse(registration.DateOfBirth, out dateOfBirth))
        {
            errors.Add(
                $"dateOfBirth '{registration.DateOfBirth}' could not be parsed. Expected " +
                $"{DateOfBirthParser.CanonicalFormat} (or one of a small set of tolerated alternative formats).");
        }

        var email = registration.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add("email is required.");
        }
        else if (!EmailPattern.IsMatch(email))
        {
            errors.Add($"email '{email}' is not a valid email address.");
        }

        var membershipType = registration.MembershipType?.Trim();
        if (string.IsNullOrWhiteSpace(membershipType))
        {
            errors.Add("membershipType is required.");
        }
        else if (!PlanCodes.ByMembershipType.ContainsKey(membershipType))
        {
            errors.Add(
                $"membershipType '{membershipType}' is not recognized. Expected one of: " +
                $"{string.Join(", ", PlanCodes.ByMembershipType.Keys)}.");
        }

        // registeredAt isn't part of the legacy contract and isn't in the
        // brief's list of required fields, so a present-but-unparsable
        // value is tolerated (simply dropped) rather than rejected.
        DateTimeOffset? registeredAt = null;
        if (!string.IsNullOrWhiteSpace(registration.RegisteredAt) &&
            DateTimeOffset.TryParse(registration.RegisteredAt, out var parsedRegisteredAt))
        {
            registeredAt = parsedRegisteredAt;
        }

        if (errors.Count > 0)
            return ValidationResult.Failure(errors);

        return ValidationResult.Success(new ValidatedRegistration(
            firstName!,
            lastName!,
            dateOfBirth,
            email!,
            membershipType!,
            registeredAt));
    }
}
