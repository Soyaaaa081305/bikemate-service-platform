using System.Net.Http.Json;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using BikeMate.Services;
using Microsoft.Maui.ApplicationModel;

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
            if (role == AppRoles.Customer && await ShouldOpenGoogleCustomerSetupAsync(auth))
            {
                SetBusy(false);
                await NavigateToGoogleSetupAsync();
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
            await DisplayAlertAsync("Sign in unavailable", "Could not reach BikeMate right now. Check your internet connection and try again.", "OK");
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

    private static async Task<bool> ShouldOpenGoogleCustomerSetupAsync(AuthResponseDto auth)
    {
        if (string.Equals(auth.User.AccountStatus, "pending", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            var customer = await CustomerApiClient.GetCustomerAsync();
            return IsGoogleCustomerSetupIncomplete(customer);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Google customer setup check failed: {ex}");
            return true;
        }
    }

    private static bool IsGoogleCustomerSetupIncomplete(CustomerMeDto customer)
    {
        var address = customer.Addresses.FirstOrDefault(item => item.IsDefault) ?? customer.Addresses.FirstOrDefault();
        var motorcycle = customer.Motorcycles.FirstOrDefault();

        return string.IsNullOrWhiteSpace(customer.PhoneNumber) ||
            string.IsNullOrWhiteSpace(customer.Sex) ||
            customer.Birthdate is null ||
            string.IsNullOrWhiteSpace(customer.ValidIdImageUrl) ||
            address is null ||
            string.IsNullOrWhiteSpace(address.AddressLine) ||
            string.IsNullOrWhiteSpace(address.City) ||
            string.IsNullOrWhiteSpace(address.Province) ||
            string.IsNullOrWhiteSpace(address.PostalCode) ||
            motorcycle is null ||
            string.IsNullOrWhiteSpace(motorcycle.Brand) ||
            string.IsNullOrWhiteSpace(motorcycle.Model) ||
            string.IsNullOrWhiteSpace(motorcycle.PlateNumber);
    }

    private static async Task NavigateToGoogleSetupAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Task.Yield();

            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page ??= new AppShell();
            }

            if (Shell.Current is not null)
            {
                try
                {
                    await Shell.Current.GoToAsync(nameof(GoogleAccountSetupPage));
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Google setup route failed: {ex}");
                    await Shell.Current.Navigation.PushAsync(new GoogleAccountSetupPage());
                    return;
                }
            }

            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new NavigationPage(new GoogleAccountSetupPage());
            }
        });
    }

    private static string PickPrimaryRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains(AppRoles.SystemAdmin)) return AppRoles.SystemAdmin;
        if (roles.Contains(AppRoles.ShopAdmin)) return AppRoles.ShopAdmin;
        if (roles.Contains(AppRoles.Mechanic)) return AppRoles.Mechanic;
        return AppRoles.Customer;
    }
}
