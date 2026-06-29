using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages;

public partial class SignOut : ContentPage
{
    public SignOut()
    {
        InitializeComponent();
    }

    private async void ConfirmSignOut_Clicked(object sender, EventArgs e)
    {
        AppSession.CurrentUser = null;
        AppSession.AccessToken = null;
        await DisplayAlert("Signed Out", "You have been signed out.", "OK");
        Application.Current!.MainPage = new NavigationPage(new Intro.AppIntro());
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Home");
    }
}


