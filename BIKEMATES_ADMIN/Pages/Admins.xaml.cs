using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages;

public partial class Admins : ContentPage
{
    public Admins()
    {
        InitializeComponent();
    }

    private async void GenerateCode_Clicked(object sender, EventArgs e)
    {
        if (AppSession.CurrentUser?.IsOwner != true)
        {
            await DisplayAlert("Owner Only", "Only the shop owner can generate admin account creation codes.", "OK");
            return;
        }

        await DisplayAlert(
            "API Required",
            "The current API generates the first shop access code during bike shop registration. Owner-generated admin codes need a real API/database endpoint before this button can create one.",
            "OK");
    }

    private async void CopyCode_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GeneratedCodeEntry.Text))
        {
            await DisplayAlert("No Code", "There is no API-generated admin code to copy yet.", "OK");
            return;
        }

        await Clipboard.Default.SetTextAsync(GeneratedCodeEntry.Text);
        await DisplayAlert("Copied", "Account creation code copied.", "OK");
    }
}



