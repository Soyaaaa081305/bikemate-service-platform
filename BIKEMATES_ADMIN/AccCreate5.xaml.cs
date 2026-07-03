namespace BIKEMATES_ADMIN.Pages.Account;

public partial class AccCreate5 : ContentPage
{
    private readonly string? _shopName;
    private readonly string? _email;
    private readonly BIKEMATES_ADMIN.Services.AccountCreationDraft? _submittedDraft;
    private bool _verified;

    public AccCreate5()
        : this(null, null, null)
    {
    }

    public AccCreate5(string? shopName, string? email, BIKEMATES_ADMIN.Services.AccountCreationDraft? submittedDraft = null)
    {
        _shopName = shopName;
        _email = email;
        _submittedDraft = submittedDraft ?? BIKEMATES_ADMIN.Services.BikeMateDatabaseService.TryGetSubmittedShopApplication(email);
        InitializeComponent();
        ApplicationDetailsButton.IsEnabled = _submittedDraft is not null;
        ApplicationDetailsButton.Text = _submittedDraft is null
            ? "No Submitted Application Saved"
            : "View Submitted Application";
        if (!string.IsNullOrWhiteSpace(shopName) || !string.IsNullOrWhiteSpace(email))
        {
            SuccessDetailsLabel.Text = $"Enter the OTP sent to {FormatEmail(email)}. After verification, {FormatShop(shopName)} will be placed under BikeMate admin review.";
        }
    }

    private async void OnVerifyClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            await DisplayAlertAsync("Email Missing", "BikeMate could not find the application email. Please return to login and try again.", "OK");
            return;
        }

        var otp = OtpEntry.Text?.Trim() ?? string.Empty;
        if (otp.Length != 6 || !otp.All(char.IsDigit))
        {
            await DisplayAlertAsync("Invalid OTP", "Enter the 6-digit code sent to your email.", "OK");
            return;
        }

        SetOtpBusy(true, "Verifying...");
        try
        {
            await BIKEMATES_ADMIN.Services.BikeMateDatabaseService.VerifyEmailOtpAsync(_email, otp);
            _verified = true;
            if (_submittedDraft is not null)
            {
                _submittedDraft.EmailVerified = true;
                _submittedDraft.UpdatedAt = DateTime.UtcNow.ToString("O");
                BIKEMATES_ADMIN.Services.BikeMateDatabaseService.SaveSubmittedShopApplication(_submittedDraft);
            }

            TitleLabel.Text = "Application Submitted";
            SuccessDetailsLabel.Text = $"Your email is verified. {FormatShop(_shopName)} is now waiting for BikeMate admin approval. You can sign in once the application is approved.";
            OtpInputFrame.IsVisible = false;
            VerifyButton.IsVisible = false;
            ResendButton.IsVisible = false;
            ProceedButton.Text = "Proceed to Login";
            ProceedButton.IsEnabled = true;
            ProceedButton.BackgroundColor = Color.FromArgb("#FF6B2C");
            ProceedButton.TextColor = Colors.White;
            await DisplayAlertAsync("Email verified", "Your shop-admin email was verified. BikeMate admin will review the submitted shop details next.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Verification Failed", ex.Message, "OK");
        }
        finally
        {
            SetOtpBusy(false, "Verify Email");
        }
    }

    private async void OnResendClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            await DisplayAlertAsync("Email Missing", "BikeMate could not find the application email. Please return to login and try again.", "OK");
            return;
        }

        ResendButton.IsEnabled = false;
        try
        {
            await BIKEMATES_ADMIN.Services.BikeMateDatabaseService.ResendEmailOtpAsync(_email);
            await DisplayAlertAsync("OTP sent", "BikeMate sent a new verification code to your email. Use the latest code to continue the application review flow.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Resend Failed", ex.Message, "OK");
        }
        finally
        {
            ResendButton.IsEnabled = true;
        }
    }

    private async void OnViewApplicationClicked(object? sender, EventArgs e)
    {
        if (_submittedDraft is null)
        {
            await DisplayAlertAsync("No Application Saved", "BikeMate could not find a local copy of the submitted application on this device.", "OK");
            return;
        }

        await Navigation.PushAsync(new ShopApplicationReviewPage(_submittedDraft));
    }

    private void OnProceedToLoginClicked(object? sender, EventArgs e)
    {
        if (!_verified)
        {
            return;
        }

        BIKEMATES_ADMIN.App.SetRootPage(new NavigationPage(new Login()));
    }

    private static string FormatEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? "your email" : email;

    private static string FormatShop(string? shopName)
        => string.IsNullOrWhiteSpace(shopName) ? "your approved bike shop" : shopName;

    private void SetOtpBusy(bool isBusy, string text)
    {
        if (_verified)
        {
            return;
        }

        VerifyButton.IsEnabled = !isBusy;
        ResendButton.IsEnabled = !isBusy;
        VerifyButton.Text = text;
    }
}
