using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages.Account;

public partial class AccCreate4 : ContentPage
{
    private readonly AccountCreationDraft _draft;

    public AccCreate4()
        : this(new AccountCreationDraft())
    {
    }

    public AccCreate4(AccountCreationDraft draft)
    {
        _draft = draft;
        InitializeComponent();
        LoadDraft();
    }

    protected override void OnDisappearing()
    {
        SaveDraft();
        base.OnDisappearing();
    }

    private async void OnCreateAccountClicked(object sender, EventArgs e)
    {
        SaveDraft();

        if (string.IsNullOrWhiteSpace(_draft.BusinessPermitPath) ||
            string.IsNullOrWhiteSpace(_draft.ShopImagePath) ||
            string.IsNullOrWhiteSpace(_draft.DtiRegistrationNumber))
        {
            await DisplayAlert("Missing Requirements", "Please upload the business permit, cover photo / shop image, and DTI registration number.", "OK");
            return;
        }

        if (!_draft.ShopTermsAccepted)
        {
            await DisplayAlert("Terms Required", "Please read and accept the shop application terms before submitting.", "OK");
            return;
        }

        CreateAccountButton.IsEnabled = false;
        CreateAccountButton.Text = "Submitting...";

        try
        {
            var submitted = await BikeMateDatabaseService.SubmitShopOwnerApplicationAsync(_draft);
            _draft.ApplicationStatus = submitted.ShopStatus;
            _draft.EmailVerified = false;
            _draft.SubmittedAt = DateTime.UtcNow.ToString("O");
            BikeMateDatabaseService.SaveSubmittedShopApplication(_draft);
            BIKEMATES_ADMIN.App.SetRootPage(new NavigationPage(new AccCreate5(submitted.ShopName, _draft.Email, _draft)));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Application Failed", ex.Message, "OK");
        }
        finally
        {
            CreateAccountButton.Text = "Submit for Approval";
            CreateAccountButton.IsEnabled = true;
        }
    }

    private async void OnPickPermitClicked(object sender, EventArgs e)
    {
        var file = await PickFileAsync("Select business permit");
        if (file is null) return;

        try
        {
            BusinessPermitLabel.Text = "Uploading business permit...";
            var uploaded = await BikeMateDatabaseService.UploadOnboardingFileAsync(file, "shop-business-permits");
            _draft.BusinessPermitPath = uploaded.Url;
            BusinessPermitLabel.Text = uploaded.FileName;
        }
        catch (Exception ex)
        {
            BusinessPermitLabel.Text = string.IsNullOrWhiteSpace(_draft.BusinessPermitPath)
                ? "No file selected"
                : Path.GetFileName(_draft.BusinessPermitPath);
            await DisplayAlert("Upload Failed", $"Unable to upload the business permit. {ex.Message}", "OK");
        }
    }

    private async void OnPickShopImageClicked(object sender, EventArgs e)
    {
        var file = await PickFileAsync("Select cover photo / shop image");
        if (file is null) return;

        try
        {
            ShopImageLabel.Text = "Uploading cover photo / shop image...";
            var uploaded = await BikeMateDatabaseService.UploadOnboardingFileAsync(file, "shop-images");
            _draft.ShopImagePath = uploaded.Url;
            ShopImageLabel.Text = uploaded.FileName;
        }
        catch (Exception ex)
        {
            ShopImageLabel.Text = string.IsNullOrWhiteSpace(_draft.ShopImagePath)
                ? "No file selected"
                : Path.GetFileName(_draft.ShopImagePath);
            await DisplayAlert("Upload Failed", $"Unable to upload the cover photo / shop image. {ex.Message}", "OK");
        }
    }

    private void LoadDraft()
    {
        DtiEntry.Text = _draft.DtiRegistrationNumber;
        TermsCheckBox.IsChecked = _draft.ShopTermsAccepted;
        if (!string.IsNullOrWhiteSpace(_draft.BusinessPermitPath))
        {
            BusinessPermitLabel.Text = Path.GetFileName(_draft.BusinessPermitPath);
        }

        if (!string.IsNullOrWhiteSpace(_draft.ShopImagePath))
        {
            ShopImageLabel.Text = Path.GetFileName(_draft.ShopImagePath);
        }
    }

    private void SaveDraft()
    {
        _draft.DtiRegistrationNumber = DtiEntry.Text ?? string.Empty;
        _draft.ShopTermsAccepted = TermsCheckBox.IsChecked;
    }

    private void OnTermsChanged(object sender, CheckedChangedEventArgs e)
    {
        _draft.ShopTermsAccepted = e.Value;
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
