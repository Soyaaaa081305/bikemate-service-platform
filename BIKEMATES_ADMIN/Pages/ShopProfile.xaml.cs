using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages;

public partial class ShopProfile : ContentPage
{
    private AdminShopProfile? _profile;
    private bool _loaded;

    public ShopProfile()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded)
        {
            _loaded = true;
            await LoadProfileAsync();
        }
    }

    private async Task LoadProfileAsync()
    {
        try
        {
            _profile = await BikeMateDatabaseService.GetShopProfileAsync();
            ApplyProfile(_profile);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Shop Profile", $"Unable to load profile from API: {ex.Message}", "OK");
        }
    }

    private void ApplyProfile(AdminShopProfile profile)
    {
        ShopTitleLabel.Text = profile.ShopName;
        ShopStatusLabel.Text = $"Status: {profile.ShopStatus}";
        ShopLocationLabel.Text = string.Join(", ", new[] { profile.AddressLine, profile.City, profile.Province }.Where(x => !string.IsNullOrWhiteSpace(x)));
        ShopNameEntry.Text = profile.ShopName;
        ShopDescriptionEditor.Text = profile.ShopDescription;
        ShopAddressEntry.Text = profile.AddressLine;
        ShopCityEntry.Text = profile.City;
        ShopProvinceEntry.Text = profile.Province;
        ContactNumberEntry.Text = profile.ContactNumber;
    }

    private async void SaveProfile_Clicked(object sender, EventArgs e)
    {
        if (_profile is null)
        {
            await DisplayAlert("Shop Profile", "Profile is not loaded yet.", "OK");
            return;
        }

        try
        {
            _profile = await BikeMateDatabaseService.UpdateShopProfileAsync(_profile with
            {
                ShopName = ShopNameEntry.Text?.Trim() ?? string.Empty,
                ShopDescription = ShopDescriptionEditor.Text?.Trim(),
                AddressLine = ShopAddressEntry.Text?.Trim(),
                City = ShopCityEntry.Text?.Trim(),
                Province = ShopProvinceEntry.Text?.Trim(),
                ContactNumber = ContactNumberEntry.Text?.Trim()
            });
            ApplyProfile(_profile);
            await DisplayAlert("Saved", "Shop profile was updated through the API.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Shop Profile", ex.Message, "OK");
        }
    }

    private async void Reload_Clicked(object sender, EventArgs e) => await LoadProfileAsync();
}


