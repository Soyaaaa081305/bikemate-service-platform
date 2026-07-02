using System.Net;
using System.Net.Http.Headers;
using Microsoft.Maui.Storage;

namespace BikeMate.Helpers;

public enum StoredSessionStatus
{
    Missing,
    Valid,
    Rejected,
    Unavailable
}

public sealed class ApiSessionExpiredException(string message) : InvalidOperationException(message);

public static class ApiConfig
{
    private const string ApiBaseUrlPreferenceKey = "bikemate_api_base_url";
    private const string ApiBaseUrlEnvironmentVariable = "BIKEMATE_API_BASE_URL";

    private const string DefaultBaseUrl =
#if ANDROID
        "https://hungrily-imagines-suffering.ngrok-free.dev/api/";
#else
        "https://localhost:5001/api/";
#endif

    public static string BaseUrl => ResolveBaseUrl();

    public static string DeviceDefaultBaseUrl => EnsureApiPath(DefaultBaseUrl);

    public static bool HasBaseUrlOverride =>
        !string.IsNullOrWhiteSpace(Preferences.Default.Get(ApiBaseUrlPreferenceKey, string.Empty));

    public static bool UsesLocalDevelopmentCertificate =>
        BaseUrl.Contains("10.0.2.2", StringComparison.OrdinalIgnoreCase) ||
        BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
        BaseUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase);

    public static bool UsesNgrokTunnel =>
        Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) &&
        uri.Host.Contains("ngrok", StringComparison.OrdinalIgnoreCase);

    public static string ToPublicUrl(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.AbsolutePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
                ? ToCurrentApiOriginUrl(absoluteUri.PathAndQuery)
                : absoluteUri.ToString();
        }

        var relativePath = value.TrimStart('/');
        return ToCurrentApiOriginUrl(relativePath);
    }

    private static string ToCurrentApiOriginUrl(string path)
    {
        var apiBase = new Uri(BaseUrl, UriKind.Absolute);
        var origin = new Uri($"{apiBase.Scheme}://{apiBase.Authority}/");
        var relativePath = path.TrimStart('/');
        return new Uri(origin, relativePath).ToString();
    }

    public static void AddRequiredHeaders(HttpClient http)
    {
        if (UsesNgrokTunnel)
        {
            http.DefaultRequestHeaders.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
        }
    }

    public static void SaveBaseUrlOverride(string baseUrl)
    {
        var normalized = EnsureApiPath(baseUrl);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Enter a valid BikeMate API URL, for example https://your-ngrok-domain.ngrok-free.dev/api/ or http://127.0.0.1:5000/api/ when using adb reverse.");
        }

        Preferences.Default.Set(ApiBaseUrlPreferenceKey, normalized);
    }

    public static void ClearBaseUrlOverride()
    {
        Preferences.Default.Remove(ApiBaseUrlPreferenceKey);
    }

    public static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler();

        if (UsesLocalDevelopmentCertificate)
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(20)
        };

        AddRequiredHeaders(http);

        return http;
    }

    public static async Task<HttpClient> CreateAuthorizedHttpClientAsync()
    {
        var http = CreateHttpClient();
        var token = await SecureStorage.Default.GetAsync("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            http.Dispose();
            await AppNavigation.HandleUnauthorizedAsync();
            throw new ApiSessionExpiredException("Your BikeMate session has ended. Please sign in again.");
        }

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    public static async Task<StoredSessionStatus> ValidateStoredSessionAsync(CancellationToken cancellationToken = default)
    {
        var token = await SecureStorage.Default.GetAsync("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            return StoredSessionStatus.Missing;
        }

        try
        {
            using var http = CreateHttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await http.GetAsync("auth/me", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return StoredSessionStatus.Valid;
            }

            return response.StatusCode == HttpStatusCode.Unauthorized
                ? StoredSessionStatus.Rejected
                : StoredSessionStatus.Unavailable;
        }
        catch (HttpRequestException)
        {
            return StoredSessionStatus.Unavailable;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StoredSessionStatus.Unavailable;
        }
    }

    public static async Task ThrowIfAuthenticationFailedAsync(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return;
        }

        await AppNavigation.HandleUnauthorizedAsync();
        throw new ApiSessionExpiredException("Your BikeMate session has expired. Please sign in again.");
    }

    private static string ResolveBaseUrl()
    {
        var stored = Preferences.Default.Get(ApiBaseUrlPreferenceKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(stored))
        {
            return EnsureApiPath(stored);
        }

        var environmentValue = Environment.GetEnvironmentVariable(ApiBaseUrlEnvironmentVariable);
        return EnsureApiPath(string.IsNullOrWhiteSpace(environmentValue) ? DefaultBaseUrl : environmentValue);
    }

    private static string EnsureApiPath(string value)
    {
        var trimmed = value.Trim();
        if (!trimmed.EndsWith("/", StringComparison.Ordinal))
        {
            trimmed += "/";
        }

        return trimmed.EndsWith("/api/", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{trimmed.TrimEnd('/')}/api/";
    }
}
