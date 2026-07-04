using BikeMate.Core.Constants;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace BikeMate.Helpers;

public static class AppNavigation
{
    public const string ForceLoginPreferenceKey = "bikemate_force_login_after_logout";
    public const string LoginMessagePreferenceKey = "bikemate_login_message";
    private const string ShopAdminAppPackageName = "com.bikemate.shop";
    private static int _handlingUnauthorized;

    public static async Task NavigateByRoleAsync(string? role)
    {
        if (string.Equals(role, AppRoles.ShopAdmin, StringComparison.OrdinalIgnoreCase))
        {
            await OpenShopAdminAppAsync();
            return;
        }

        var route = role switch
        {
            AppRoles.Mechanic => "//MechanicDashboardPage",
            AppRoles.SystemAdmin => "//AdminDashboardPage",
            _ => "//CustomerHomePage"
        };

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Task.Yield();
#if ANDROID
            await Task.Delay(75);
#endif
            var shell = Shell.Current;
            if (shell is null)
            {
                if (Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = new AppShell();
                    shell = Shell.Current;
                }
                else
                {
                    return;
                }
            }

            if (shell is not null)
            {
                await shell.GoToAsync(route);
            }
        });
    }

    public static async Task OpenShopAdminAppAsync()
    {
        ClearSavedSession("Shop admin accounts use the separate BikeMate Shop app.");

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Task.Yield();
            var opened = TryLaunchShopAdminApp();
            if (opened)
            {
                return;
            }

            var page = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
            {
                await page.DisplayAlertAsync(
                    "Open BikeMate Shop",
                    "Shop admin accounts now use the separate BikeMate Shop app. Install or run that app, then sign in with the shop owner account you created.",
                    "OK");
            }
        });
    }

    private static bool TryLaunchShopAdminApp()
    {
#if ANDROID
        var context = Android.App.Application.Context;
        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(ShopAdminAppPackageName);
        if (launchIntent is null)
        {
            return false;
        }

        launchIntent.AddFlags(Android.Content.ActivityFlags.NewTask | Android.Content.ActivityFlags.ClearTop);
        context.StartActivity(launchIntent);
        return true;
#else
        return false;
#endif
    }

    public static async Task SignOutAsync(string? loginMessage = null)
    {
        ClearSavedSession(loginMessage ?? "You have been signed out.");

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Task.Yield();
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }

            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("//MainPage");
            }
        });
    }

    public static async Task<bool> ConfirmAndSignOutAsync(Page page, string? loginMessage = null)
    {
        var confirmed = await page.DisplayAlertAsync(
            "Log out?",
            "You will need to sign in again to continue using BikeMate.",
            "Log out",
            "Cancel");
        if (!confirmed)
        {
            return false;
        }

        await SignOutAsync(loginMessage);
        return true;
    }

    public static void ClearSavedSession(string loginMessage)
    {
        SecureStorage.Default.Remove("access_token");
        SecureStorage.Default.Remove("primary_role");
        SecureStorage.Default.Remove("user_id");
        Preferences.Default.Set(ForceLoginPreferenceKey, true);
        Preferences.Default.Set(LoginMessagePreferenceKey, loginMessage);
    }

    public static async Task HandleUnauthorizedAsync()
    {
        if (Interlocked.CompareExchange(ref _handlingUnauthorized, 1, 0) != 0)
        {
            return;
        }

        try
        {
            await SignOutAsync("Your session expired. Please sign in again.");
        }
        finally
        {
            await Task.Delay(500);
            Interlocked.Exchange(ref _handlingUnauthorized, 0);
        }
    }

    public static string InferRoleFromEmail(string email)
    {
        email = email.Trim().ToLowerInvariant();
        if (email.Contains("mechanic")) return AppRoles.Mechanic;
        if (email.Contains("shop")) return AppRoles.ShopAdmin;
        if (email.Contains("admin")) return AppRoles.SystemAdmin;
        return AppRoles.Customer;
    }
}
