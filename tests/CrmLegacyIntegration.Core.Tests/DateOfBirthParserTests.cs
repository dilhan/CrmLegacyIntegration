using CrmLegacyIntegration.Core.Validation;
using FluentAssertions;
using Xunit;

namespace CrmLegacyIntegration.Core.Tests;

public class DateOfBirthParserTests
{
    [Theory]
    [InlineData("1990-04-12", 1990, 4, 12)]   // canonical format
    [InlineData("1990/04/12", 1990, 4, 12)]   // tolerated fallback
    [InlineData("04/12/1990", 1990, 4, 12)]   // tolerated fallback (MM/dd/yyyy)
    [InlineData("12-04-1990", 1990, 4, 12)]   // tolerated fallback (dd-MM-yyyy)
    public void TryParse_KnownFormat_ReturnsExpectedDate(string raw, int year, int month, int day)
    {
        var success = DateOfBirthParser.TryParse(raw, out var result);

        success.Should().BeTrue();
        result.Should().Be(new DateOnly(year, month, day));
    }

    [Theory]
    [InlineData("")]
    [InlineData((string?)null)]
    [InlineData("not-a-date")]
    [InlineData("1990.04.12")]
    [InlineData("32/13/2020")]
    public void TryParse_UnknownOrInvalidFormat_ReturnsFalse(string? raw)
    {
        var success = DateOfBirthParser.TryParse(raw, out _);

        success.Should().BeFalse();
    }
}
