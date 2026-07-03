using System.Globalization;

namespace BikeMate.Api.Helpers;

public static class AgeRequirement
{
    public const int MinimumAge = 18;

    public static DateTime RequireAdult(string? birthdateText, string label)
    {
        if (!DateTime.TryParse(birthdateText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var birthdate))
        {
            throw new InvalidOperationException($"{label} birthdate is required.");
        }

        return RequireAdult(birthdate, label);
    }

    public static DateTime RequireAdult(DateTime? birthdate, string label)
    {
        if (birthdate is null)
        {
            throw new InvalidOperationException($"{label} birthdate is required.");
        }

        var date = birthdate.Value.Date;
        if (CalculateAge(date, DateTime.Today) < MinimumAge)
        {
            throw new InvalidOperationException($"{label} must be at least {MinimumAge} years old.");
        }

        return date;
    }

    public static int CalculateAge(DateTime birthdate, DateTime today)
    {
        var age = today.Year - birthdate.Year;
        if (birthdate.Date > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
