namespace BikeMate.Core.Services;

public static class RepairConcernMatcher
{
    private static readonly IReadOnlyDictionary<string, string[]> ConcernTerms =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Tire Problem"] = ["tire", "tyre", "flat", "puncture", "tube", "wheel"],
            ["Brake Adjustment"] = ["brake", "braking", "rotor", "disc", "pad", "caliper"],
            ["Gear Shifting Issue"] = ["gear", "shift", "shifting", "shifter", "drivetrain", "drive train", "derailleur", "transmission", "gearbox", "clutch"],
            ["Accessory Installation"] = ["accessory", "installation", "install", "electrical upgrade", "light", "rack", "mount"],
            ["Chain Maintenance"] = ["chain", "sprocket", "lubrication", "lubricate"],
            ["General Tune-up"] = ["tune-up", "tune up", "tuneup", "preventive maintenance", "periodic maintenance", "general inspection", "maintenance"]
        };

    public static bool Matches(
        string? concern,
        string? categoryName,
        string? serviceName,
        string? serviceDescription = null)
    {
        if (string.IsNullOrWhiteSpace(concern))
        {
            return true;
        }

        var cleanConcern = concern.Split(':', 2)[0].Trim();
        var searchableText = string.Join(
            " ",
            new[] { categoryName, serviceName, serviceDescription }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        if (searchableText.Contains(cleanConcern, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var terms = ConcernTerms.TryGetValue(cleanConcern, out var mappedTerms)
            ? mappedTerms
            : cleanConcern.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return terms.Any(term => searchableText.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
