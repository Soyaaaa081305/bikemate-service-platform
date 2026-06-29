using BIKEMATES_ADMIN.Pages.Popups;
using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages.Register;

public partial class Register1 : ContentPage
{
    private readonly ShopRegistrationDraft _draft;

    public Register1()
        : this(new ShopRegistrationDraft())
    {
    }

    public Register1(ShopRegistrationDraft draft)
    {
        _draft = draft;
        InitializeComponent();
    }

    private async void OnPickPermitClicked(object sender, EventArgs e)
    {
        var file = await PickFileAsync("Select business permit");
        if (file is null)
        {
            return;
        }

        _draft.BusinessPermitPath = file.FullPath ?? file.FileName;
        BusinessPermitLabel.Text = file.FileName;
    }

    private async void OnPickShopImageClicked(object sender, EventArgs e)
    {
        var file = await PickFileAsync("Select shop image");
        if (file is null)
        {
            return;
        }

        _draft.ShopImagePath = file.FullPath ?? file.FileName;
        ShopImageLabel.Text = file.FileName;
    }

    private async void OnFinishClicked(object sender, EventArgs e)
    {
        _draft.DtiRegistrationNumber = DtiEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_draft.BusinessPermitPath) ||
            string.IsNullOrWhiteSpace(_draft.ShopImagePath) ||
            string.IsNullOrWhiteSpace(_draft.DtiRegistrationNumber))
        {
            await DisplayAlert("Missing Requirements", "Please upload the permit, shop image, and DTI registration number.", "OK");
            return;
        }

        FinishButton.IsEnabled = false;

        try
        {
            var result = await BikeMateDatabaseService.RegisterShopAsync(_draft);
            await Clipboard.Default.SetTextAsync(result.AccessCode);
            await Navigation.PushAsync(BookingPopUp.CreateRegistered(result));
        }
        catch (Exception ex) when (ex.Message.Contains("already registered", StringComparison.OrdinalIgnoreCase))
        {
            await Navigation.PushAsync(BookingPopUp.CreateAlreadyRegistered());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Shop Registration Failed", ex.Message, "OK");
        }
        finally
        {
            FinishButton.IsEnabled = true;
        }
    }

    private static async Task<FileResult?> PickFileAsync(string title)
    {
        try
        {
            return await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = title
            });
        }
        catch
        {
            return null;
        }
    }
}
