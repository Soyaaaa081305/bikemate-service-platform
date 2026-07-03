using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages;

public partial class SignOut : ContentPage
{
    public SignOut()
    {
        InitializeComponent();
    }

    private async void ConfirmSignOut_Clicked(object? sender, EventArgs e)
    {
        AppSession.CurrentUser = null;
        AppSession.AccessToken = null;
        await DisplayAlertAsync("Signed Out", "You have been signed out.", "OK");
        BIKEMATES_ADMIN.App.SetRootPage(new NavigationPage(new Account.Login()));
    }

    private async void Cancel_Clicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminTabs/Home");
    }
}


