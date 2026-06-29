using Microsoft.Win32;

namespace BIKEMATES_ADMIN.Pages.Intro;

public partial class AppIntro5 : ContentPage
{
    public AppIntro5() => InitializeComponent();

    private async void OnGetStartedClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new Account.Login());

    private async void OnLoginClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new Account.Login());

    private async void OnCreateAccountClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new Account.AccCreate0());

    private async void OnRegisterShopClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new Register.Register0());
}