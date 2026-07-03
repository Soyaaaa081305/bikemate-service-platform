namespace BIKEMATES_ADMIN.Pages;

public partial class Settings : ContentPage
{
    public Settings()
    {
        InitializeComponent();
    }

    private async void Reports_Clicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Reports));
    }

    private async void ChangePassword_Clicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Change Password", "Password changes are handled through the BikeMate account recovery flow. Sign out, then use Forgot Password on the login screen.", "OK");
    }

    private async void Sessions_Clicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Login Sessions", "Session management is controlled by the BikeMate API token lifecycle. Sign out on shared devices after shop-admin work.", "OK");
    }

    private async void SignOut_Clicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SignOut));
    }
}


