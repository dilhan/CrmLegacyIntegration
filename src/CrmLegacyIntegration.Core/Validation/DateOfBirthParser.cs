using System.Globalization;

namespace CrmLegacyIntegration.Core.Validation;

/// <summary>
/// Parses a date of birth from the CRM's documented format (yyyy-MM-dd),
/// tolerating a small set of common alternative formats before giving up.
/// Never throws — a date matching none of the known formats is reported
/// back as "could not parse" for the validator to turn into a clean error,
/// never a thrown <see cref="FormatException"/>.
/// </summary>
public static class DateOfBirthParser
{
    public const string CanonicalFormat = "yyyy-MM-dd";

    private static readonly string[] TolerableFormats =
    {
        CanonicalFormat,
        "yyyy/MM/dd",
        "MM/dd/yyyy",
        "dd-MM-yyyy"
    };

    public static bool TryParse(string? raw, out DateOnly dateOfBirth)
    {
        dateOfBirth = default;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        foreach (var format in TolerableFormats)
        {
            if (DateOnly.TryParseExact(raw, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOfBirth))
                return true;
        }

        return false;
    }
}
