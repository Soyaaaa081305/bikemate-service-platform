using System.Security.Cryptography;
using BIKEMATES_ADMIN.Pages.Main;
using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN;

public partial class Admins : ContentPage
{
    private const string LastCodeKey = "admin_last_code";
    private const string LastExpiryKey = "admin_last_code_expiry";

    public Admins()
    {
        InitializeComponent();
        LoadOwnerState();
        LoadLastCode();
        AdminListLabel.Text = BuildAdminList();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadOwnerState();
    }

    private void LoadOwnerState()
    {
        var user = AppSession.CurrentUser;
        var isOwner = user?.IsOwner ?? true;
        OwnerOnlyLabel.Text = isOwner
            ? "Owner access active. Generated codes expire after 3 days and cannot be cancelled."
            : "Only the shop owner can generate admin account codes.";
        GenerateCodeButton.IsEnabled = isOwner;
    }

    private void LoadLastCode()
    {
        GeneratedCodeEntry.Text = Preferences.Default.Get(LastCodeKey, string.Empty);
        var expiry = Preferences.Default.Get(LastExpiryKey, string.Empty);
        ExpiryLabel.Text = string.IsNullOrWhiteSpace(expiry)
            ? "No active generated code."
            : $"Expires: {expiry}";
    }

    private async void OnGenerateCodeClicked(object sender, EventArgs e)
    {
        var confirmed = await DisplayAlert(
            "Generate Code",
            "Once generated, this code cannot be cancelled and will expire in 3 days.",
            "Generate",
            "Cancel");
        if (!confirmed)
        {
            return;
        }

        var code = $"BM-{RandomNumberGenerator.GetInt32(1000, 9999)}-{RandomNumberGenerator.GetInt32(1000, 9999)}";
        var expiresAt = DateTimeOffset.Now.AddDays(3).ToString("MMMM d, yyyy h:mm tt");
        Preferences.Default.Set(LastCodeKey, code);
        Preferences.Default.Set(LastExpiryKey, expiresAt);
        GeneratedCodeEntry.Text = code;
        ExpiryLabel.Text = $"Expires: {expiresAt}";
    }

    private async void OnCopyCodeClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GeneratedCodeEntry.Text))
        {
            await DisplayAlert("No Code", "Generate a code first.", "OK");
            return;
        }

        await Clipboard.Default.SetTextAsync(GeneratedCodeEntry.Text);
        await DisplayAlert("Copied", "The account creation code was copied.", "OK");
    }

    private async void OnSendEmailClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GeneratedCodeEntry.Text))
        {
            await DisplayAlert("No Code", "Generate a code first.", "OK");
            return;
        }

        await Navigation.PushAsync(new Pages.Popups.SendToEmailPage(GeneratedCodeEntry.Text));
    }

    private async void OnPromoteOwnerClicked(object sender, EventArgs e)
        => await DisplayAlert("Owner Tools", "Owner promotion will be connected after admin account listing is available from the API.", "OK");

    private async void OnDeactivateClicked(object sender, EventArgs e)
        => await DisplayAlert("Owner Tools", "Admin deactivation will be connected after admin account listing is available from the API.", "OK");

    private static string BuildAdminList()
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return "Current owner account\nOther admins will appear here after API listing is connected.";
        }

        return $"{user.FirstName} {user.LastName} - Owner\nOther admins will appear here after API listing is connected.";
    }

    private async void OnHomeClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MainPage());
    private async void OnMessagesClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Messages());
    private async void OnReportsClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Reports());
    private async void OnMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MenuPage());
}
