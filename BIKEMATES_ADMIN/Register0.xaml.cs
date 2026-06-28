using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages.Register;

public partial class Register0 : ContentPage
{
    private readonly ShopRegistrationDraft _draft;

    public Register0()
        : this(new ShopRegistrationDraft())
    {
    }

    public Register0(ShopRegistrationDraft draft)
    {
        _draft = draft;
        InitializeComponent();
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        _draft.ShopName = ShopNameEntry.Text ?? string.Empty;
        _draft.OwnerName = OwnerNameEntry.Text ?? string.Empty;
        _draft.ShopDescription = ShopDescriptionEditor.Text ?? string.Empty;
        _draft.ShopAddress = AddressEntry.Text ?? string.Empty;
        _draft.City = CityEntry.Text ?? string.Empty;
        _draft.Province = ProvinceEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_draft.ShopName) ||
            string.IsNullOrWhiteSpace(_draft.OwnerName) ||
            string.IsNullOrWhiteSpace(_draft.ShopDescription) ||
            string.IsNullOrWhiteSpace(_draft.ShopAddress) ||
            string.IsNullOrWhiteSpace(_draft.City) ||
            string.IsNullOrWhiteSpace(_draft.Province))
        {
            await DisplayAlert("Missing Information", "Please complete all required shop information.", "OK");
            return;
        }

        await Navigation.PushAsync(new Register1(_draft));
    }
}
