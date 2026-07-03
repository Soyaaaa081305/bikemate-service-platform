using BIKEMATES_ADMIN.Services;
using BIKEMATES_ADMIN.Pages;

namespace BIKEMATES_ADMIN.Pages.Account;

public partial class Login : ContentPage
{
    private AccountCreationDraft? _pendingApplication;
    private bool _pendingPromptShown;

    public Login() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshPendingApplicationStateAsync();

        if (_pendingApplication is not null && !_pendingPromptShown)
        {
            _pendingPromptShown = true;
            await PromptPendingApplicationAsync(_pendingApplication);
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        LoginButton.IsEnabled = false;

        try
        {
            await BikeMateDatabaseService.LoginAsync(
                EmailEntry.Text ?? string.Empty,
                PasswordEntry.Text ?? string.Empty);

            BikeMateDatabaseService.ClearSubmittedShopApplication(EmailEntry.Text);
            _pendingApplication = null;
            PendingApplicationPanel.IsVisible = false;

            var setup = await BikeMateDatabaseService.GetShopSetupStatusAsync();
            BIKEMATES_ADMIN.App.SetRootPage(setup.IsComplete
                ? new AppShell()
                : new NavigationPage(new ShopSetupPage(setup))
                {
                    BarBackgroundColor = Colors.White,
                    BarTextColor = Color.FromArgb("#242424")
                });
        }
        catch (Exception ex)
        {
            var snapshot = await BikeMateDatabaseService.RefreshSubmittedShopApplicationAsync(EmailEntry.Text);
            if (snapshot is not null &&
                ex.Message.Contains("pending BikeMate admin approval", StringComparison.OrdinalIgnoreCase))
            {
                await PromptPendingApplicationAsync(snapshot, ex.Message);

                return;
            }

            await DisplayAlertAsync("Login Failed", ex.Message, "OK");
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        await RefreshPendingApplicationStateAsync();
        if (_pendingApplication is not null)
        {
            await PromptPendingApplicationAsync(_pendingApplication);
            return;
        }

        await Navigation.PushAsync(new AccCreate0());
    }

    private async void OnViewPendingApplicationClicked(object? sender, EventArgs e)
    {
        await RefreshPendingApplicationStateAsync();
        if (_pendingApplication is null)
        {
            await DisplayAlertAsync(
                "No pending submission",
                "Please sign in with your shop-admin account. If sign-in still does not work, contact BikeMate admin so your application status can be checked.",
                "OK");
            return;
        }

        await Navigation.PushAsync(new ShopApplicationReviewPage(_pendingApplication));
    }

    private async void OnForgotPasswordClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(new ForgotPasswordPage(EmailEntry.Text ?? string.Empty));

    private void OnEmailChanged(object? sender, TextChangedEventArgs e)
    {
        RefreshPendingApplicationState();
    }

    private async void OnConnectionSettingsClicked(object? sender, EventArgs e)
    {
        var current = BikeMateDatabaseService.CurrentApiBaseUrl;
        var action = await DisplayActionSheetAsync(
            "BikeMate API connection",
            "Cancel",
            null,
            "Set API URL",
            "Use packaged default",
            "Show current URL");

        if (action == "Set API URL")
        {
            var entered = await DisplayPromptAsync(
                "API URL",
                "Enter the API base URL. Examples: https://your-domain.com/api/ or http://192.168.1.10:5000/api/.",
                "Save",
                "Cancel",
                current);

            if (string.IsNullOrWhiteSpace(entered))
            {
                return;
            }

            try
            {
                BikeMateDatabaseService.SaveApiBaseUrlOverride(entered);
                await DisplayAlertAsync("Connection Saved", $"BIKEMATES ADMIN will use {BikeMateDatabaseService.CurrentApiBaseUrl}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Invalid API URL", ex.Message, "OK");
            }
        }
        else if (action == "Use packaged default")
        {
            BikeMateDatabaseService.ClearApiBaseUrlOverride();
            await DisplayAlertAsync("Connection Reset", $"BIKEMATES ADMIN will use {BikeMateDatabaseService.PackagedDefaultApiBaseUrl}", "OK");
        }
        else if (action == "Show current URL")
        {
            var mode = BikeMateDatabaseService.HasApiBaseUrlOverride ? "Custom override" : "Packaged default";
            await DisplayAlertAsync("Current API URL", $"{mode}\n{BikeMateDatabaseService.CurrentApiBaseUrl}", "OK");
        }
    }

    private void RefreshPendingApplicationState()
    {
        var typedEmail = EmailEntry.Text;
        _pendingApplication = BikeMateDatabaseService.TryGetSubmittedShopApplication(typedEmail);
        ApplyPendingApplicationState();
    }

    private async Task RefreshPendingApplicationStateAsync()
    {
        var typedEmail = EmailEntry.Text;
        _pendingApplication = await BikeMateDatabaseService.RefreshSubmittedShopApplicationAsync(typedEmail);
        ApplyPendingApplicationState();
    }

    private void ApplyPendingApplicationState()
    {
        if (_pendingApplication is not null && !IsWaitingForApproval(_pendingApplication))
        {
            BikeMateDatabaseService.ClearSubmittedShopApplication(_pendingApplication.Email);
            _pendingApplication = null;
        }

        PendingApplicationPanel.IsVisible = _pendingApplication is not null;
        if (_pendingApplication is null)
        {
            _pendingPromptShown = false;
            return;
        }

        var shopName = string.IsNullOrWhiteSpace(_pendingApplication.ShopName)
            ? "Your shop-admin application"
            : _pendingApplication.ShopName;
        var status = string.IsNullOrWhiteSpace(_pendingApplication.ApplicationStatus)
            ? "waiting for BikeMate admin approval"
            : _pendingApplication.ApplicationStatus;
        var otp = _pendingApplication.EmailVerified ? "Email verified" : "Email verification still pending";
        PendingApplicationLabel.Text = $"{shopName} is {status}. {otp}. Submitted details are view-only until BikeMate admin completes the review.";
    }

    private async Task PromptPendingApplicationAsync(AccountCreationDraft draft, string? message = null)
    {
        var details = string.IsNullOrWhiteSpace(message)
            ? "This shop-admin application has already been submitted and is waiting for BikeMate admin approval."
            : message;

        await DisplayAlertAsync(
            "Application under review",
            $"{details}\n\nYou can view the submitted details, but editing is locked until admin approval or until BikeMate asks for a corrected submission.",
            "View details");

        await Navigation.PushAsync(new ShopApplicationReviewPage(draft));
    }

    private static bool IsWaitingForApproval(AccountCreationDraft draft)
    {
        var status = draft.ApplicationStatus?.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(status) ||
            status is "pending" or "submitted" or "under review" or "for review" or "waiting for approval";
    }
}
