using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

using BikeMate.Controls;
using BikeMate.Helpers;
using BikeMate.Services;

#if ANDROID
using Android.Webkit;
using BikeMate.Platforms.Android;
#endif

namespace BikeMate
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("PublicSans.ttf", "PublicSans");
                    fonts.AddFont("Inter.ttf", "Inter");
                    fonts.AddFont("PTSansCaption-Regular.ttf", "PTSansCaption");
                    fonts.AddFont("PTSansCaption-Bold.ttf", "PTSansCaptionBold");
                })
                .ConfigureMauiHandlers(handlers =>
                {
                    AppTypography.ConfigureHandlers(handlers);

#if ANDROID
                    WebViewHandler.Mapper.AppendToMapping(nameof(AgoraCallWebView), (handler, view) =>
                    {
                        if (view is not AgoraCallWebView || handler.PlatformView is null)
                        {
                            return;
                        }

                        handler.PlatformView.Settings.JavaScriptEnabled = true;
                        handler.PlatformView.Settings.DomStorageEnabled = true;
                        handler.PlatformView.Settings.DatabaseEnabled = true;
                        handler.PlatformView.Settings.AllowContentAccess = true;
                        handler.PlatformView.Settings.AllowFileAccess = true;
                        handler.PlatformView.Settings.LoadsImagesAutomatically = true;
                        handler.PlatformView.Settings.MediaPlaybackRequiresUserGesture = false;
                        handler.PlatformView.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
                        handler.PlatformView.SetWebChromeClient(new BikeMateMediaWebChromeClient());
                    });
#endif
                });

            builder.Services.AddHttpClient("BikeMateApi", client =>
            {
                client.BaseAddress = new Uri(ApiConfig.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(20);
                ApiConfig.AddRequiredHeaders(client);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();

                if (ApiConfig.UsesLocalDevelopmentCertificate)
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                return handler;
            });

            builder.Services.AddSingleton<IEmergencyCallService, EmergencyCallService>();
#if ANDROID
            builder.Services.AddSingleton<IBookingReminderService, AndroidBookingReminderService>();
#else
            builder.Services.AddSingleton<IBookingReminderService, NoOpBookingReminderService>();
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }

#if ANDROID
    internal sealed class BikeMateMediaWebChromeClient : WebChromeClient
    {
        public override void OnPermissionRequest(PermissionRequest? request)
        {
            request?.Grant(request.GetResources());
        }
    }
#endif
}
