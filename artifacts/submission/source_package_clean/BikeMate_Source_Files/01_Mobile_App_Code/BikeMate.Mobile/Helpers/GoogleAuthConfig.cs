namespace BikeMate.Helpers;

/// <summary>
/// BikeMate mobile uses the API-hosted Google OAuth flow and receives the
/// result through the custom bikemate://auth/google callback.
/// </summary>
public static class GoogleAuthConfig
{
    // These must stay const because Android intent filters require constants.
    public const string RedirectScheme = "bikemate";
    public const string RedirectPath = "/google";
    public const string RedirectUri = "bikemate://auth/google";
    public const string ApiCallbackUri = "bikemate://auth/google";

    public static readonly string AndroidClientId =
        Environment.GetEnvironmentVariable("GOOGLE_ANDROID_CLIENT_ID") ?? string.Empty;

    public static readonly string WebClientId =
        Environment.GetEnvironmentVariable("GOOGLE_WEB_CLIENT_ID") ?? string.Empty;
}
