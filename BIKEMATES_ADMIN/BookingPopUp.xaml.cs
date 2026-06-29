using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages.Popups;

public partial class BookingPopUp : ContentPage
{
    private readonly string _accessCode;

    private BookingPopUp(string title, string message, string imageSource, string accessCode = "")
    {
        InitializeComponent();

        _accessCode = accessCode;
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        StatusImage.Source = imageSource;
        AccessCodeEntry.Text = accessCode;
        CodePanel.IsVisible = !string.IsNullOrWhiteSpace(accessCode);
        SendEmailButton.IsVisible = !string.IsNullOrWhiteSpace(accessCode);
    }

    public BookingPopUp()
        : this("This Shop is already Registered", "Please contact your shop admin for the account creation code.", "shop_not_found.png")
    {
    }

    public static BookingPopUp CreateAlreadyRegistered()
    {
        return new BookingPopUp(
            "This Shop is already Registered",
            "Please contact your shop admin for the account creation code.",
            "shop_not_found.png");
    }

    public static BookingPopUp CreateRegistered(ShopRegistrationResult result)
    {
        return new BookingPopUp(
            "Bike shop Successfully Registered",
            "Account Creation Code",
            "account_success.png",
            result.AccessCode);
    }

    private async void OnCopyClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_accessCode))
        {
            return;
        }

        await Clipboard.Default.SetTextAsync(_accessCode);
        await DisplayAlert("Copied", "The account creation code was copied.", "OK");
    }

    private async void OnSendEmailClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SendToEmailPage(_accessCode));
    }

    private void OnReturnClicked(object sender, EventArgs e)
    {
        Application.Current!.MainPage = new NavigationPage(new BIKEMATES_ADMIN.Pages.Intro.AppIntro5());
    }
}
