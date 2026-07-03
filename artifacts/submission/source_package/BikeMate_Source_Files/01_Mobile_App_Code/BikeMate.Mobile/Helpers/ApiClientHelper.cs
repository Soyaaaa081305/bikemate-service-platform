using System.Net.Http.Json;
using System.Text.Json;

namespace BikeMate.Helpers;

internal static class ApiClientHelper
{
    public static async Task<T> GetAsync<T>(HttpClient http, string endpoint, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(endpoint, cancellationToken);
        return await ReadAsync<T>(response, cancellationToken);
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadErrorMessageAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty response.");
    }

    public static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(error)
            ? $"API request failed with {(int)response.StatusCode}."
            : HumanizeError(error);
    }

    private static string HumanizeError(string payload)
    {
        try
        {
            using var json = JsonDocument.Parse(payload);
            if (json.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var propertyName in new[] { "error", "message", "title", "detail" })
                {
                    if (json.RootElement.TryGetProperty(propertyName, out var value) &&
                        value.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(value.GetString()))
                    {
                        return value.GetString()!;
                    }
                }
            }
        }
        catch (JsonException)
        {
            return payload;
        }

        return payload;
    }
}
