using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages.Account;

public partial class AccCreate3 : ContentPage
{
    private readonly AccountCreationDraft _draft;

    public AccCreate3()
        : this(new AccountCreationDraft())
    {
    }

    public AccCreate3(AccountCreationDraft draft)
    {
        _draft = draft;
        InitializeComponent();
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        _draft.ShopName = ShopNameEntry.Text ?? string.Empty;
        _draft.ShopProvince = ProvinceEntry.Text ?? string.Empty;
        _draft.ShopCity = CityEntry.Text ?? string.Empty;
        _draft.ShopBarangay = BarangayEntry.Text ?? string.Empty;
        _draft.ShopAddress = AddressEntry.Text ?? string.Empty;
        _draft.ShopZipCode = ZipCodeEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_draft.ShopName) ||
            string.IsNullOrWhiteSpace(_draft.ShopProvince) ||
            string.IsNullOrWhiteSpace(_draft.ShopCity) ||
            string.IsNullOrWhiteSpace(_draft.ShopBarangay) ||
            string.IsNullOrWhiteSpace(_draft.ShopAddress) ||
            string.IsNullOrWhiteSpace(_draft.ShopZipCode))
        {
            await DisplayAlert("Missing Information", "Please complete the bike shop information.", "OK");
            return;
        }

        ContinueButton.IsEnabled = false;

        try
        {
            var exists = await BikeMateDatabaseService.ShopExistsForAccountCreationAsync(_draft);
            await Navigation.PushAsync(exists ? new AccCreate4(_draft) : new AccCreateShopNotFound());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Shop Lookup Failed", ex.Message, "OK");
        }
        finally
        {
            ContinueButton.IsEnabled = true;
        }
    }
}
