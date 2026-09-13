using System.Globalization;
using CrmLegacyIntegration.Core.Models;

namespace CrmLegacyIntegration.Core.Mapping;

/// <summary>
/// Maps an already-validated registration to the legacy system's payload
/// shape. Takes a <see cref="ValidatedRegistration"/>, never a raw
/// <see cref="CrmRegistration"/>, so there is nothing left to check here —
/// just translation of vocabulary, nesting, and date format.
/// </summary>
public static class LegacyPayloadMapper
{
    private const string LegacySource = "MILKYWAY";

    public static LegacyPayload Map(ValidatedRegistration registration)
    {
        if (!PlanCodes.TryGetPlanCode(registration.MembershipType, out var planCode))
        {
            // Unreachable if RegistrationValidator ran first (it checks the
            // same dictionary), but fail loudly rather than silently emit
            // a blank plan code if this is ever called on unvalidated input.
            throw new InvalidOperationException(
                $"No plan code mapping for membershipType '{registration.MembershipType}'.");
        }

        return new LegacyPayload
        {
            Member = new LegacyMember
            {
                GivenName = registration.FirstName,
                FamilyName = registration.LastName,
                Dob = registration.DateOfBirth.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                Contact = new LegacyContact { Email = registration.Email },
                PlanCode = planCode,
                Source = LegacySource
            }
        };
    }
}
