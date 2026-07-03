using Microsoft.Extensions.Logging;

using BikeMate.Helpers;
using BikeMate.Services;

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
                .ConfigureMauiHandlers(AppTypography.ConfigureHandlers);

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

            builder.Services.AddSingleton<ILocalIdCardScannerService, LocalIdCardScannerService>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
