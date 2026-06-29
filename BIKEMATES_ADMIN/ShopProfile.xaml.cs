using BIKEMATES_ADMIN.Pages.Main;
using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN;

public partial class ShopProfile : ContentPage
{
	public ShopProfile()
	{
		InitializeComponent();
        LoadProfile();
	}

    private void LoadProfile()
    {
        var shopName = AppSession.CurrentUser?.ShopName ?? "Assigned Bikeshop";
        ShopNameEntry.Text = Preferences.Default.Get("shop_profile_name", shopName);
        DescriptionEditor.Text = Preferences.Default.Get("shop_profile_description", "Bike repair, parts, accessories, and home service.");
        AddressEntry.Text = Preferences.Default.Get("shop_profile_address", "Shop address");
        StatusLabel.Text = "Profile completeness: 82%";
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        Preferences.Default.Set("shop_profile_name", ShopNameEntry.Text ?? string.Empty);
        Preferences.Default.Set("shop_profile_description", DescriptionEditor.Text ?? string.Empty);
        Preferences.Default.Set("shop_profile_address", AddressEntry.Text ?? string.Empty);
        StatusLabel.Text = "Profile saved locally. API sync is next.";
        await DisplayAlert("Saved", "Shop profile changes were saved on this device.", "OK");
    }

    private async void OnPreviewClicked(object sender, EventArgs e)
        => await DisplayAlert("Public Preview", $"{ShopNameEntry.Text}\n\n{DescriptionEditor.Text}\n\n{AddressEntry.Text}", "OK");

    private async void OnHomeClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MainPage());
    private async void OnMessagesClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Messages());
    private async void OnMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MenuPage());
}
