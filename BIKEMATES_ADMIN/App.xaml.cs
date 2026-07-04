namespace BIKEMATES_ADMIN;

public partial class App : Application
{
    public App()
    {
        Services.CrashLogService.Install("BikeMate Shop");
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var navigation = new NavigationPage(new Pages.Account.Login())
        {
            BarBackgroundColor = Colors.White,
            BarTextColor = Color.FromArgb("#242424")
        };

        return new Window(navigation);
    }

    public static void SetRootPage(Page page)
    {
        if (Current?.Windows.Count > 0)
        {
            Current.Windows[0].Page = page;
        }
    }
}
