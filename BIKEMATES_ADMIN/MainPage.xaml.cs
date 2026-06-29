using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages.Main;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        ApplySession();
    }

    private void ApplySession()
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            WelcomeLabel.Text = "Welcome, Admin";
            AssignedShopLabel.Text = "Assigned Bikeshop";
            HeaderShopNameLabel.Text = "Assigned Bikeshop";
            return;
        }

        var firstName = string.IsNullOrWhiteSpace(user.FirstName) ? "Admin" : user.FirstName;
        var shopName = string.IsNullOrWhiteSpace(user.ShopName) ? "Assigned Bikeshop" : user.ShopName;

        WelcomeLabel.Text = $"Welcome {firstName} <-admin";
        AssignedShopLabel.Text = $"{shopName} <-Assigned bikeshop";
        HeaderShopNameLabel.Text = shopName;
    }

    private async void OnNewProductClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new Products());

    private async void OnDispatchClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new DispatchAndRequest());

    private async void OnMenuClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new MenuPage());

    private async void OnMessagesClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new Messages());

    private async void OnProfileClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new ShopProfile());
}
