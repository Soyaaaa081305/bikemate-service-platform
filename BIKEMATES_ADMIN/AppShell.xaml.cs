namespace BIKEMATES_ADMIN;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(Pages.MenuPage), typeof(Pages.MenuPage));
        Routing.RegisterRoute(nameof(Pages.MainPage), typeof(Pages.MainPage));
        Routing.RegisterRoute(nameof(Pages.ShopProfile), typeof(Pages.ShopProfile));
        Routing.RegisterRoute(nameof(Pages.Products), typeof(Pages.Products));
        Routing.RegisterRoute(nameof(Pages.Logistics), typeof(Pages.Logistics));
        Routing.RegisterRoute(nameof(Pages.Calendar), typeof(Pages.Calendar));
        Routing.RegisterRoute(nameof(Pages.Operations), typeof(Pages.Operations));
        Routing.RegisterRoute(nameof(Pages.Admins), typeof(Pages.Admins));
        Routing.RegisterRoute(nameof(Pages.Messages), typeof(Pages.Messages));
        Routing.RegisterRoute(nameof(Pages.Reports), typeof(Pages.Reports));
        Routing.RegisterRoute(nameof(Pages.Notifications), typeof(Pages.Notifications));
        Routing.RegisterRoute(nameof(Pages.Settings), typeof(Pages.Settings));
        Routing.RegisterRoute(nameof(Pages.HelpSupport), typeof(Pages.HelpSupport));
        Routing.RegisterRoute(nameof(Pages.SignOut), typeof(Pages.SignOut));
    }
}
