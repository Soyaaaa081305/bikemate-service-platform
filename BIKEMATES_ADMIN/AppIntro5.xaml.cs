namespace BIKEMATES_ADMIN.Pages.Intro;

public partial class AppIntro5 : ContentPage
{
    public AppIntro5() => InitializeComponent();

    private async void OnGetStartedClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(new Account.Login());

    private async void OnLoginClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(new Account.Login());

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(new Account.AccCreate0());

    private async void OnRegisterShopClicked(object? sender, EventArgs e)
        => await DisplayAlertAsync(
            "Shop approval",
            "Create your shop-admin account in this app, upload the required ID and business documents, then wait for BikeMate web admin approval. You can sign in after approval.",
            "OK");
}
