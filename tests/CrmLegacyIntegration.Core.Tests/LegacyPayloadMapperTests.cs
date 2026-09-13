using CrmLegacyIntegration.Core.Mapping;
using CrmLegacyIntegration.Core.Models;
using FluentAssertions;
using Xunit;

namespace CrmLegacyIntegration.Core.Tests;

public class LegacyPayloadMapperTests
{
    private static ValidatedRegistration Registration(
        string membershipType = "Single",
        DateOnly? dateOfBirth = null) => new(
            FirstName: "Alex",
            LastName: "Nguyen",
            DateOfBirth: dateOfBirth ?? new DateOnly(1990, 4, 12),
            Email: "alex.nguyen@example.com",
            MembershipType: membershipType,
            RegisteredAt: DateTimeOffset.Parse("2026-06-01T09:00:00Z"));

    [Fact]
    public void Map_ValidRegistration_ProducesExpectedLegacyShape()
    {
        var result = LegacyPayloadMapper.Map(Registration());

        result.Member.GivenName.Should().Be("Alex");
        result.Member.FamilyName.Should().Be("Nguyen");
        result.Member.Dob.Should().Be("12/04/1990");
        result.Member.Contact.Email.Should().Be("alex.nguyen@example.com");
        result.Member.PlanCode.Should().Be("S");
        result.Member.Source.Should().Be("MILKYWAY");
    }

    [Theory]
    [InlineData("Single", "S")]
    [InlineData("Family", "F")]
    [InlineData("Couple", "C")]
    public void Map_MembershipType_MapsToExpectedPlanCode(string membershipType, string expectedPlanCode)
    {
        var result = LegacyPayloadMapper.Map(Registration(membershipType: membershipType));

        result.Member.PlanCode.Should().Be(expectedPlanCode);
    }

    public static IEnumerable<object[]> DateCases()
    {
        yield return new object[] { new DateOnly(1990, 4, 12), "12/04/1990" };
        yield return new object[] { new DateOnly(2000, 1, 1), "01/01/2000" };
        yield return new object[] { new DateOnly(2024, 2, 29), "29/02/2024" }; // leap day
    }

    [Theory]
    [MemberData(nameof(DateCases))]
    public void Map_DateOfBirth_ConvertsToDdMmYyyy(DateOnly input, string expected)
    {
        var result = LegacyPayloadMapper.Map(Registration(dateOfBirth: input));

        result.Member.Dob.Should().Be(expected);
    }

    [Fact]
    public void Map_Source_IsAlwaysFixedLiteral()
    {
        var result = LegacyPayloadMapper.Map(Registration());

        result.Member.Source.Should().Be("MILKYWAY");
    }
}
