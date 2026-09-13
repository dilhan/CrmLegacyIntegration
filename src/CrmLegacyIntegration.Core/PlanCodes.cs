namespace CrmLegacyIntegration.Core;

/// <summary>
/// The closed set of membership types the CRM can send, and the plan code
/// the legacy system expects for each. Shared by
/// <see cref="Validation.RegistrationValidator"/> (to check membershipType
/// is recognized) and <see cref="Mapping.LegacyPayloadMapper"/> (to
/// produce plan_code), so the two can't drift apart.
/// </summary>
public static class PlanCodes
{
    public static readonly IReadOnlyDictionary<string, string> ByMembershipType =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Single"] = "S",
            ["Couple"] = "C",
            ["Family"] = "F"
        };

    public static bool TryGetPlanCode(string membershipType, out string planCode) =>
        ByMembershipType.TryGetValue(membershipType, out planCode!);
}
