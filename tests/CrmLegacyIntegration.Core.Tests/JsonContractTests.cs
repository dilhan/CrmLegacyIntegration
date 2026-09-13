using System.Text.Json;
using CrmLegacyIntegration.Core.Json;
using CrmLegacyIntegration.Core.Mapping;
using CrmLegacyIntegration.Core.Models;
using CrmLegacyIntegration.Core.Validation;
using FluentAssertions;
using Xunit;

namespace CrmLegacyIntegration.Core.Tests;

public class JsonContractTests
{
    [Fact]
    public void Deserialize_UnexpectedExtraField_IsIgnoredAndDoesNotThrow()
    {
        const string json = """
            {
              "firstName": "Alex",
              "lastName": "Nguyen",
              "dateOfBirth": "1990-04-12",
              "email": "alex.nguyen@example.com",
              "membershipType": "Single",
              "registeredAt": "2026-06-01T09:00:00Z",
              "loyaltyPoints": 1200,
              "referralSource": "friend"
            }
            """;

        var act = () =>
        {
            var registration = JsonSerializer.Deserialize<CrmRegistration>(json, LegacyJsonSerialization.CrmRead);
            var validation = RegistrationValidator.Validate(registration);
            validation.IsValid.Should().BeTrue();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void Serialize_LegacyPayload_UsesSnakeCaseWireFormat()
    {
        var validated = RegistrationValidator.Validate(new CrmRegistration
        {
            FirstName = "Alex",
            LastName = "Nguyen",
            DateOfBirth = "1990-04-12",
            Email = "alex.nguyen@example.com",
            MembershipType = "Single"
        }).Value!;

        var payload = LegacyPayloadMapper.Map(validated);

        var json = JsonSerializer.Serialize(payload, LegacyJsonSerialization.LegacyWrite);

        json.Should().Contain("\"given_name\"");
        json.Should().Contain("\"family_name\"");
        json.Should().Contain("\"plan_code\"");
        json.Should().Contain("\"dob\"");
        json.Should().Contain("12/04/1990");
        json.Should().Contain("MILKYWAY");
    }
}
