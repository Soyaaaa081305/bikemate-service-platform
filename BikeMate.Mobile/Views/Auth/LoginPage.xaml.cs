using System.Net.Http.Json;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using BikeMate.Services;

namespace BikeMate.Views.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        AppVisualPolish.Apply((View)Content);
    }

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        await SignInAsync();
    }

    private async void OnGoogleClicked(object? sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            var auth = await GoogleSignInService.SignInAsync(AppRoles.Customer);
            await GoogleSignInService.StoreAuthAsync(auth);
            var role = GoogleSignInService.PickPrimaryRole(auth.User.Roles);
            if (role == AppRoles.Customer &&
                string.Equals(auth.User.AccountStatus, "pending", StringComparison.OrdinalIgnoreCase))
            {
                await Shell.Current.GoToAsync(nameof(GoogleAccountSetupPage));
                return;
            }

            await AppNavigation.NavigateByRoleAsync(role);
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync("Google sign-in cancelled", "Google sign-in was cancelled. You can try again anytime.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Google sign-in failed: {ex}");
            await DisplayAlertAsync("Google sign-in unavailable", ex.Message, "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    private async void OnForgotPasswordClicked(object? sender, EventArgs e)
    {
        var email = Uri.EscapeDataString(EmailEntry.Text?.Trim() ?? string.Empty);
        await Shell.Current.GoToAsync($"{nameof(PasswordResetPage)}?email={email}");
    }

    private async void OnConnectionSettingsClicked(object? sender, EventArgs e)
    {
        var current = ApiConfig.BaseUrl;
        var action = await DisplayActionSheet(
            "BikeMate API connection",
            "Cancel",
            null,
            "Set API URL",
            "Use packaged default",
            "Show current URL");

        if (action == "Set API URL")
        {
            var entered = await DisplayPromptAsync(
                "API URL",
                "Enter the API base URL. Examples: https://your-domain.com/api/ or http://192.168.1.10:5000/api/.",
                "Save",
                "Cancel",
                current);

            if (string.IsNullOrWhiteSpace(entered))
            {
                return;
            }

            try
            {
                ApiConfig.SaveBaseUrlOverride(entered);
                await DisplayAlertAsync("Connection saved", $"BikeMate will use {ApiConfig.BaseUrl}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Invalid API URL", ex.Message, "OK");
            }
        }
        else if (action == "Use packaged default")
        {
            ApiConfig.ClearBaseUrlOverride();
            await DisplayAlertAsync("Connection reset", $"BikeMate will use {ApiConfig.DeviceDefaultBaseUrl}", "OK");
        }
        else if (action == "Show current URL")
        {
            var mode = ApiConfig.HasBaseUrlOverride ? "Custom override" : "Packaged default";
            await DisplayAlertAsync("Current API URL", $"{mode}\n{ApiConfig.BaseUrl}", "OK");
        }
    }

    private async void OnPasswordCompleted(object? sender, EventArgs e)
    {
        await SignInAsync();
    }

    private async Task SignInAsync()
    {
        SetBusy(true);
        var navigatedAway = false;

        try
        {
            using var http = ApiConfig.CreateHttpClient();
            var response = await http.PostAsJsonAsync("auth/login", new LoginRequestDto(EmailEntry.Text ?? string.Empty, PasswordEntry.Text ?? string.Empty));

            if (response.IsSuccessStatusCode)
            {
                var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                if (auth is not null)
                {
                    await SecureStorage.Default.SetAsync("access_token", auth.AccessToken);
                    var role = PickPrimaryRole(auth.User.Roles);
                    await SecureStorage.Default.SetAsync("primary_role", role);
                    await SecureStorage.Default.SetAsync("user_id", auth.User.UserId.ToString());
                    navigatedAway = role != AppRoles.ShopAdmin;
                    await AppNavigation.NavigateByRoleAsync(role);
                    return;
                }
            }

            var error = await ApiClientHelper.ReadErrorMessageAsync(response);
            await DisplayAlertAsync("Sign in failed", string.IsNullOrWhiteSpace(error) ? "Check your email and password." : error, "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sign in failed: {ex}");
            await DisplayAlertAsync("Sign in unavailable", $"Could not reach the BikeMate API at {ApiConfig.BaseUrl}. Check connection settings and try again.", "OK");
        }
        finally
        {
            if (!navigatedAway)
            {
                SetBusy(false);
            }
        }
    }

    private void SetBusy(bool value)
    {
        BusyIndicator.IsVisible = value;
        BusyIndicator.IsRunning = value;
        SignInButton.IsEnabled = !value;
        GoogleButton.IsEnabled = !value;
    }

    private static string PickPrimaryRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains(AppRoles.SystemAdmin)) return AppRoles.SystemAdmin;
        if (roles.Contains(AppRoles.ShopAdmin)) return AppRoles.ShopAdmin;
        if (roles.Contains(AppRoles.Mechanic)) return AppRoles.Mechanic;
        return AppRoles.Customer;
    }
}
