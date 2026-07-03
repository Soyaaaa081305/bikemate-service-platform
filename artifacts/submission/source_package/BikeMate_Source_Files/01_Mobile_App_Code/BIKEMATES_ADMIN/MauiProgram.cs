using BIKEMATES_ADMIN.Helpers;
using BIKEMATES_ADMIN.Services;
using Microsoft.Extensions.Logging;

namespace BIKEMATES_ADMIN;

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

        builder.Services.AddSingleton<ILocalIdCardScannerService, LocalIdCardScannerService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
