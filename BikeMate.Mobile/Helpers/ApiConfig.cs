using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
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
        "https://api-production-02d4.up.railway.app/api/";
#else
        "https://api-production-02d4.up.railway.app/api/";
#endif

    public static string BaseUrl => ResolveBaseUrl();

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
        var http = CreatePooledHttpClientOrFallback();
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

    private static HttpClient CreatePooledHttpClientOrFallback()
    {
        var httpFactory = Application.Current?.Handler?.MauiContext?.Services.GetService<IHttpClientFactory>();
        var http = httpFactory?.CreateClient("BikeMateApi");
        if (http is null)
        {
            return CreateHttpClient();
        }

        if (string.Equals(http.BaseAddress?.ToString(), BaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            return http;
        }

        http.Dispose();
        return CreateHttpClient();
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
        Preferences.Default.Remove(ApiBaseUrlPreferenceKey);

        var environmentValue = Environment.GetEnvironmentVariable(ApiBaseUrlEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return EnsureApiPath(environmentValue);
        }

        var buildConfiguredValue = ReadBuildConfiguredApiBaseUrl();
        return EnsureApiPath(string.IsNullOrWhiteSpace(buildConfiguredValue) ? DefaultBaseUrl : buildConfiguredValue);
    }

    private static string? ReadBuildConfiguredApiBaseUrl()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, "BikeMateApiBaseUrl", StringComparison.OrdinalIgnoreCase))
            ?.Value;
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
