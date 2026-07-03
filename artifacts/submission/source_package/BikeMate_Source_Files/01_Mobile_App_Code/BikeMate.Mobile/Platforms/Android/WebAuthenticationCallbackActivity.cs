using Android.App;
using Android.Content;
using Android.Content.PM;
using BikeMate.Helpers;
using Microsoft.Maui.Authentication;

namespace BikeMate;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true, Theme = "@style/Maui.SplashTheme")]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = GoogleAuthConfig.RedirectScheme,
    DataPath = GoogleAuthConfig.RedirectPath)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "bikemate",
    DataHost = "auth",
    DataPath = "/google")]
public sealed class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        Android.Util.Log.Debug("BikeMateAuth", $"Google callback received: {Intent?.DataString}");
        base.OnCreate(savedInstanceState);
    }
}
