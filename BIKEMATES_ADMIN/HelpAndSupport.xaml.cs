using BIKEMATES_ADMIN.Pages.Main;

namespace BIKEMATES_ADMIN;

public partial class HelpAndSupport : ContentPage
{
    public HelpAndSupport()
    {
        InitializeComponent();
        HelpResultLabel.Text = "Search for account codes, products, dispatch, reports, or settings.";
    }

    private void OnSearchClicked(object sender, EventArgs e)
    {
        var topic = SearchEntry.Text?.Trim().ToLowerInvariant();
        HelpResultLabel.Text = topic switch
        {
            "account codes" or "admins" or "code" => "Owner admins can generate account creation codes from the Admins page.",
            "products" or "inventory" => "Use Products to add, edit, or delete shop inventory.",
            "dispatch" or "request" => "Use Dispatch and Request to accept customer jobs and assign mechanics.",
            "reports" => "Use Reports for sales, dispatch, inventory, and admin activity snapshots.",
            _ => "No exact help article found. Submit a support ticket and the team can review it."
        };
    }

    private async void OnSubmitTicketClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TicketEditor.Text))
        {
            await DisplayAlert("Ticket Needed", "Describe the issue first.", "OK");
            return;
        }

        TicketEditor.Text = string.Empty;
        await DisplayAlert("Ticket Submitted", "Your support request was saved for review.", "OK");
    }

    private async void OnHomeClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MainPage());
    private async void OnSettingsClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Settings());
    private async void OnMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MenuPage());
}
