using CrmLegacyIntegration.Core.Models;
using CrmLegacyIntegration.Core.Validation;
using FluentAssertions;
using Xunit;

namespace CrmLegacyIntegration.Core.Tests;

public class RegistrationValidatorTests
{
    private static CrmRegistration ValidRegistration() => new()
    {
        FirstName = "Alex",
        LastName = "Nguyen",
        DateOfBirth = "1990-04-12",
        Email = "alex.nguyen@example.com",
        MembershipType = "Single",
        RegisteredAt = "2026-06-01T09:00:00Z"
    };

    [Fact]
    public void Validate_ValidRegistration_ReturnsSuccessWithMappedValues()
    {
        var result = RegistrationValidator.Validate(ValidRegistration());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Value.Should().NotBeNull();
        result.Value!.FirstName.Should().Be("Alex");
        result.Value.DateOfBirth.Should().Be(new DateOnly(1990, 4, 12));
        result.Value.MembershipType.Should().Be("Single");
    }

    [Fact]
    public void Validate_NullRegistration_ReturnsFailure()
    {
        var result = RegistrationValidator.Validate(null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Request body"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_MissingFirstName_ReturnsError(string? firstName)
    {
        var registration = ValidRegistration();
        registration.FirstName = firstName;

        var result = RegistrationValidator.Validate(registration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("firstName"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_MissingLastName_ReturnsError(string? lastName)
    {
        var registration = ValidRegistration();
        registration.LastName = lastName;

        var result = RegistrationValidator.Validate(registration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("lastName"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_MissingDateOfBirth_ReturnsError(string? dob)
    {
        var registration = ValidRegistration();
        registration.DateOfBirth = dob;

        var result = RegistrationValidator.Validate(registration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("dateOfBirth"));
    }

    [Theory]
    [InlineData("13/13/2020")]
    [InlineData("not-a-date")]
    [InlineData("1990.04.12")]
    public void Validate_DateOfBirthInUnrecognizedFormat_ReturnsError(string dob)
    {
        var registration = ValidRegistration();
        registration.DateOfBirth = dob;

        var result = RegistrationValidator.Validate(registration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("dateOfBirth"));
    }

    [Theory]
    [InlineData("1990/04/12")]
    [InlineData("04/12/1990")]
    [InlineData("12-04-1990")]
    public void Validate_DateOfBirthInToleratedFallbackFormat_IsValid(string dob)
    {
        var registration = ValidRegistration();
        registration.DateOfBirth = dob;

        var result = RegistrationValidator.Validate(registration);

        result.IsValid.Should().BeTrue();
        result.Value!.DateOfBirth.Should().Be(new DateOnly(1990, 4, 12));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_MissingEmail_ReturnsError(string? email)
    {
        var registration = ValidRegistration();
        registration.Email = email;

        var result = RegistrationValidator.Validate(registration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("email"));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    [InlineData("no-domain@")]
    [InlineData("@no-local-part.com")]
    public void Validate_ImplausibleEmail_ReturnsError(string email)
    {
        var registration = ValidRegistration();
        registration.Email = email;

        var result = RegistrationValidator.Validate(registration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("email"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_MissingMembershipType_ReturnsError(string? type)
    {
        var registration = ValidRegistration();
        registration.MembershipType = type;

        var result = RegistrationValidator.Validate(registration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("membershipType"));
    }

    [Theory]
    [InlineData("Individual")]
    [InlineData("Group")]
    public void Validate_UnknownMembershipType_ReturnsError(string type)
    {
        var registration = ValidRegistration();
        registration.MembershipType = type;

        var result = RegistrationValidator.Validate(registration);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("membershipType"));
    }

    [Theory]
    [InlineData("Single")]
    [InlineData("Family")]
    [InlineData("Couple")]
    [InlineData("family")]
    [InlineData("COUPLE")]
    public void Validate_KnownMembershipType_IsValid(string type)
    {
        var registration = ValidRegistration();
        registration.MembershipType = type;

        var result = RegistrationValidator.Validate(registration);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ReturnsEveryError_NotJustTheFirst()
    {
        var result = RegistrationValidator.Validate(new CrmRegistration()); // everything missing

        result.Errors.Should().HaveCount(5);
    }
}
