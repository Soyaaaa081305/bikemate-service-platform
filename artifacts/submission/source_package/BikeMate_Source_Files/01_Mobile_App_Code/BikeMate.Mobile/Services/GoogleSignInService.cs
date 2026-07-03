using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Authentication;
using Microsoft.Maui.Storage;

namespace BikeMate.Services;

public static class GoogleSignInService
{
    public static async Task<AuthResponseDto> SignInAsync(string role = AppRoles.Customer)
    {
        try
        {
            var startUri = BuildApiGoogleStartUri(role);
            await EnsureBrowserAvailableAsync(startUri);

            var result = await WebAuthenticator.Default.AuthenticateAsync(
                startUri,
                new Uri(GoogleAuthConfig.ApiCallbackUri));

            if (result.Properties.TryGetValue("error", out var error) &&
                !string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            if (!result.Properties.TryGetValue("access_token", out var accessToken) ||
                string.IsNullOrWhiteSpace(accessToken))
            {
                throw new InvalidOperationException("Google sign-in completed, but BikeMate did not receive an access token.");
            }

            using var api = ApiConfig.CreateHttpClient();
            api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await api.GetAsync("auth/me");
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(errorBody)
                    ? "BikeMate API rejected the Google sign-in token."
                    : errorBody);
            }

            var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>()
                ?? throw new InvalidOperationException("BikeMate API did not return the Google account profile.");
            var expiresAt = result.Properties.TryGetValue("expires_at", out var rawExpiresAt) &&
                DateTimeOffset.TryParse(rawExpiresAt, out var parsedExpiresAt)
                    ? parsedExpiresAt
                    : DateTimeOffset.UtcNow.AddDays(7);
            return new AuthResponseDto(accessToken, expiresAt, profile);
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            Debug.WriteLine($"Google sign-in failed: {ex}");
            throw new InvalidOperationException(
                "Google sign-in could not start or complete. Check that the BikeMate API is running, the Google callback URL is registered in Google Cloud Console, and Chrome is enabled on the device.",
                ex);
        }
    }

    private static Uri BuildApiGoogleStartUri(string role)
    {
        var baseUri = new Uri(ApiConfig.BaseUrl, UriKind.Absolute);
        return new Uri(baseUri, $"auth/google/start?role={Uri.EscapeDataString(role)}");
    }

    public static async Task StoreAuthAsync(AuthResponseDto auth)
    {
        var role = PickPrimaryRole(auth.User.Roles);
        await SecureStorage.Default.SetAsync("access_token", auth.AccessToken);
        await SecureStorage.Default.SetAsync("primary_role", role);
        await SecureStorage.Default.SetAsync("user_id", auth.User.UserId.ToString());
    }

    public static string PickPrimaryRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains(AppRoles.SystemAdmin)) return AppRoles.SystemAdmin;
        if (roles.Contains(AppRoles.ShopAdmin)) return AppRoles.ShopAdmin;
        if (roles.Contains(AppRoles.Mechanic)) return AppRoles.Mechanic;
        return AppRoles.Customer;
    }

    private static async Task EnsureBrowserAvailableAsync(Uri authUri)
    {
        try
        {
            if (!await Launcher.Default.CanOpenAsync(authUri))
            {
                throw new InvalidOperationException("This Android emulator cannot open Google sign-in because no browser is available. Use a Google Play emulator image with Chrome enabled, then try again.");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Google browser preflight failed: {ex}");
            throw new InvalidOperationException("This Android emulator cannot open Google sign-in. Check that Chrome or another browser is installed and enabled.", ex);
        }
    }
}
