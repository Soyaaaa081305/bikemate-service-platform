using BIKEMATES_ADMIN.Pages.Main;

namespace BIKEMATES_ADMIN;

public partial class Settings : ContentPage
{
    private const string PushAlertsKey = "settings_push_alerts";
    private const string DailyReportsKey = "settings_daily_reports";
    private const string DefaultCityKey = "settings_default_city";
    private const string ContactNumberKey = "settings_contact_number";

    public Settings()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        PushAlertsSwitch.IsToggled = Preferences.Default.Get(PushAlertsKey, true);
        DailyReportsSwitch.IsToggled = Preferences.Default.Get(DailyReportsKey, false);
        DefaultCityEntry.Text = Preferences.Default.Get(DefaultCityKey, string.Empty);
        ContactNumberEntry.Text = Preferences.Default.Get(ContactNumberKey, string.Empty);
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        Preferences.Default.Set(PushAlertsKey, PushAlertsSwitch.IsToggled);
        Preferences.Default.Set(DailyReportsKey, DailyReportsSwitch.IsToggled);
        Preferences.Default.Set(DefaultCityKey, DefaultCityEntry.Text?.Trim() ?? string.Empty);
        Preferences.Default.Set(ContactNumberKey, ContactNumberEntry.Text?.Trim() ?? string.Empty);
        await DisplayAlert("Settings Saved", "Your admin app preferences were saved.", "OK");
    }

    private async void OnHomeClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MainPage());
    private async void OnHelpClicked(object sender, EventArgs e) => await Navigation.PushAsync(new HelpAndSupport());
    private async void OnSignOutClicked(object sender, EventArgs e) => await Navigation.PushAsync(new SignOut());
    private async void OnMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MenuPage());
}
