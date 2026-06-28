namespace BIKEMATES_ADMIN.Pages.Popups;

public partial class SendToEmailPage : ContentPage
{
    private readonly string _accessCode;

    public SendToEmailPage()
        : this(string.Empty)
    {
    }

    public SendToEmailPage(string accessCode)
    {
        _accessCode = accessCode;
        InitializeComponent();
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        var email = (EmailEntry.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Missing Email", "Please enter the admin email address.", "OK");
            return;
        }

        var subject = Uri.EscapeDataString("BikeMates Account Creation Code");
        var body = Uri.EscapeDataString($"Your BikeMates account creation code is: {_accessCode}");
        var mailto = new Uri($"mailto:{email}?subject={subject}&body={body}");

        try
        {
            await Launcher.Default.OpenAsync(mailto);
        }
        catch
        {
            await Clipboard.Default.SetTextAsync(_accessCode);
            await DisplayAlert("Email App Unavailable", "The code was copied so you can send it manually.", "OK");
        }
    }

    private async void OnReturnClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
