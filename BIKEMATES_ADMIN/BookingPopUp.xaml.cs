using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages.Popups;

public partial class BookingPopUp : ContentPage
{
    private BookingPopUp(string title, string message, string imageSource, bool showReviewPanel = false)
    {
        InitializeComponent();

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        StatusImage.Source = imageSource;
        ReviewPanel.IsVisible = showReviewPanel;
    }

    public BookingPopUp()
        : this("Shop Application Already Exists", "Sign in to view the application status. If the account still cannot be accessed, contact BikeMate admin so ownership can be checked.", "shop_not_found.png")
    {
    }

    public static BookingPopUp CreateAlreadyRegistered()
    {
        return new BookingPopUp(
            "Shop Application Already Exists",
            "Sign in to view the application status. If the account still cannot be accessed, contact BikeMate admin so ownership can be checked.",
            "shop_not_found.png");
    }

    public static BookingPopUp CreateRegistered(ShopApplicationResult result)
    {
        return new BookingPopUp(
            "Bike Shop Application Submitted",
            "Your shop details were sent to BikeMate admin for review. Shop tools remain locked until the application is approved.",
            "account_success.png",
            true);
    }

    private void OnReturnClicked(object? sender, EventArgs e)
    {
        BIKEMATES_ADMIN.App.SetRootPage(new NavigationPage(new BIKEMATES_ADMIN.Pages.Account.Login()));
    }
}
