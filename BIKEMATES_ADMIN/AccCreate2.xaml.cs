using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages.Account;

public partial class AccCreate2 : ContentPage
{
    private readonly AccountCreationDraft _draft;

    public AccCreate2()
        : this(new AccountCreationDraft())
    {
    }

    public AccCreate2(AccountCreationDraft draft)
    {
        _draft = draft;
        InitializeComponent();
    }

    private async void OnPickValidIdClicked(object sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select valid ID"
            });

            if (file is null)
            {
                return;
            }

            _draft.ValidIdPath = file.FullPath ?? file.FileName;
            ValidIdLabel.Text = file.FileName;
        }
        catch
        {
            await DisplayAlert("Upload Failed", "Unable to select a valid ID file.", "OK");
        }
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        _draft.Province = ProvinceEntry.Text ?? string.Empty;
        _draft.City = CityEntry.Text ?? string.Empty;
        _draft.Barangay = BarangayEntry.Text ?? string.Empty;
        _draft.Address = AddressEntry.Text ?? string.Empty;
        _draft.ZipCode = ZipCodeEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_draft.Province) ||
            string.IsNullOrWhiteSpace(_draft.City) ||
            string.IsNullOrWhiteSpace(_draft.Barangay) ||
            string.IsNullOrWhiteSpace(_draft.Address) ||
            string.IsNullOrWhiteSpace(_draft.ZipCode) ||
            string.IsNullOrWhiteSpace(_draft.ValidIdPath))
        {
            await DisplayAlert("Missing Information", "Please complete your address and upload a valid ID.", "OK");
            return;
        }

        await Navigation.PushAsync(new AccCreate3(_draft));
    }
}
