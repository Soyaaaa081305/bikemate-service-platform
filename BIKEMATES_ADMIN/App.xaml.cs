namespace BIKEMATES_ADMIN;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new NavigationPage(new Pages.Intro.AppIntro());
    }
}
