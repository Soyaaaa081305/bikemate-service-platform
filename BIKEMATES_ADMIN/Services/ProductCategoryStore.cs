namespace BIKEMATES_ADMIN.Services;

public static class ProductCategoryStore
{
    private const string PreferenceKey = "bikemate_shop_product_categories";

    private static readonly string[] DefaultCategories =
    [
        "Frames",
        "Drivetrain",
        "Wheels",
        "Brakes",
        "Cockpit",
        "Accessories",
        "Wearables",
        "Electrical",
        "Suspension",
        "Tires",
        "Modification Parts"
    ];

    public static IReadOnlyList<string> Load(IEnumerable<string>? shopCategories = null)
    {
        var categories = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in DefaultCategories)
        {
            AddClean(categories, category);
        }

        foreach (var category in ReadSavedCategories())
        {
            AddClean(categories, category);
        }

        if (shopCategories is not null)
        {
            foreach (var category in shopCategories)
            {
                AddClean(categories, category);
            }
        }

        return categories.ToList();
    }

    public static string Add(string category)
    {
        var clean = Clean(category);
        var categories = new SortedSet<string>(Load(), StringComparer.OrdinalIgnoreCase)
        {
            clean
        };
        Preferences.Set(PreferenceKey, string.Join("|", categories));
        return clean;
    }

    public static string Clean(string? category)
    {
        var clean = category?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(clean))
        {
            throw new InvalidOperationException("Category name is required.");
        }

        if (clean.Length > 80)
        {
            throw new InvalidOperationException("Product category must be 80 characters or fewer.");
        }

        return clean;
    }

    private static IEnumerable<string> ReadSavedCategories()
    {
        return (Preferences.Get(PreferenceKey, string.Empty) ?? string.Empty)
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static void AddClean(ISet<string> categories, string? category)
    {
        var clean = category?.Trim();
        if (!string.IsNullOrWhiteSpace(clean))
        {
            categories.Add(clean);
        }
    }
}
