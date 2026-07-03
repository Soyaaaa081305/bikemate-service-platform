namespace BIKEMATES_ADMIN.Services;

public static class ProductCategoryStore
{
    private const string PreferenceKey = "bikemate_shop_product_categories";
    private const string HiddenPreferenceKey = "bikemate_shop_hidden_product_categories";

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
        var hiddenCategories = new HashSet<string>(ReadHiddenCategories(), StringComparer.OrdinalIgnoreCase);
        foreach (var category in DefaultCategories)
        {
            if (!hiddenCategories.Contains(category))
            {
                AddClean(categories, category);
            }
        }

        foreach (var category in ReadSavedCategories())
        {
            if (!hiddenCategories.Contains(category))
            {
                AddClean(categories, category);
            }
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
        var hiddenCategories = new SortedSet<string>(ReadHiddenCategories(), StringComparer.OrdinalIgnoreCase);
        hiddenCategories.Remove(clean);
        Preferences.Set(PreferenceKey, string.Join("|", categories));
        Preferences.Set(HiddenPreferenceKey, string.Join("|", hiddenCategories));
        return clean;
    }

    public static void Remove(string category)
    {
        var clean = Clean(category);
        var categories = new SortedSet<string>(ReadSavedCategories(), StringComparer.OrdinalIgnoreCase);
        var hiddenCategories = new SortedSet<string>(ReadHiddenCategories(), StringComparer.OrdinalIgnoreCase)
        {
            clean
        };
        categories.Remove(clean);
        Preferences.Set(PreferenceKey, string.Join("|", categories));
        Preferences.Set(HiddenPreferenceKey, string.Join("|", hiddenCategories));
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

    private static IEnumerable<string> ReadHiddenCategories()
    {
        return (Preferences.Get(HiddenPreferenceKey, string.Empty) ?? string.Empty)
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
