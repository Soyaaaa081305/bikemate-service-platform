using BIKEMATES_ADMIN.Pages.Intro;
using BIKEMATES_ADMIN.Pages.Main;
using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN;

public partial class SignOut : ContentPage
{
    public SignOut()
    {
        InitializeComponent();
    }

    private async void OnConfirmSignOutClicked(object sender, EventArgs e)
    {
        var confirmed = await DisplayAlert("Sign Out", "Return to the introduction screen?", "Sign Out", "Cancel");
        if (!confirmed)
        {
            return;
        }

        AppSession.CurrentUser = null;
        AppSession.AccessToken = null;

        if (Application.Current is not null)
        {
            Application.Current.MainPage = new NavigationPage(new AppIntro());
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e) => await Navigation.PopAsync();
    private async void OnHomeClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MainPage());
    private async void OnSettingsClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Settings());
    private async void OnMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MenuPage());
}
