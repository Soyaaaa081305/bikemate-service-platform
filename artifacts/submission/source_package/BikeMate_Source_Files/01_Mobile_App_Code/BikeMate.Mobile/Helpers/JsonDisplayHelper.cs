using System.Text.Json;

namespace BikeMate.Helpers;

internal static class JsonDisplayHelper
{
    public static string Scalar(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "Yes",
            JsonValueKind.False => "No",
            JsonValueKind.Null => "None",
            JsonValueKind.Undefined => "",
            _ => value.ToString()
        };
    }

    public static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var chars = new List<char> { char.ToUpperInvariant(value[0]) };
        foreach (var c in value.Skip(1))
        {
            if (char.IsUpper(c))
            {
                chars.Add(' ');
            }

            chars.Add(c);
        }

        return new string(chars.ToArray());
    }

    public static string? FirstScalar(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
            {
                return Scalar(value);
            }

            var property = element.EnumerateObject()
                .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(property.Name))
            {
                return Scalar(property.Value);
            }
        }

        return null;
    }
}
