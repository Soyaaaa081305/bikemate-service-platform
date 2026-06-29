using BIKEMATES_ADMIN.Pages.Main;
using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN;

public partial class MenuPage : ContentPage
{
	public MenuPage()
	{
		InitializeComponent();
        ApplySession();
	}

    private void ApplySession()
    {
        var user = AppSession.CurrentUser;
        var adminName = user is null
            ? "Admin"
            : $"{user.FirstName} {user.LastName}".Trim();
        var shopName = string.IsNullOrWhiteSpace(user?.ShopName)
            ? "Assigned Bikeshop"
            : user!.ShopName!;
        AdminNameLabel.Text = $"{adminName} - {shopName}";
        ShopNameLabel.Text = shopName;
    }

    private async Task GoTo(Page page) => await Navigation.PushAsync(page);

    private async void OnCloseClicked(object sender, EventArgs e) => await Navigation.PopAsync();
    private async void OnHomeClicked(object sender, EventArgs e) => await GoTo(new MainPage());
    private async void OnShopProfileClicked(object sender, EventArgs e) => await GoTo(new ShopProfile());
    private async void OnLogisticsClicked(object sender, EventArgs e) => await GoTo(new Logistics());
    private async void OnCalendarClicked(object sender, EventArgs e) => await GoTo(new Calendar());
    private async void OnOperationsClicked(object sender, EventArgs e) => await GoTo(new Operations());
    private async void OnDispatchClicked(object sender, EventArgs e) => await GoTo(new DispatchAndRequest());
    private async void OnAdminsClicked(object sender, EventArgs e) => await GoTo(new Admins());
    private async void OnMessagesClicked(object sender, EventArgs e) => await GoTo(new Messages());
    private async void OnReportsClicked(object sender, EventArgs e) => await GoTo(new Reports());
    private async void OnNotificationsClicked(object sender, EventArgs e) => await GoTo(new Notifications());
    private async void OnSettingsClicked(object sender, EventArgs e) => await GoTo(new Settings());
    private async void OnHelpClicked(object sender, EventArgs e) => await GoTo(new HelpAndSupport());
    private async void OnSignOutClicked(object sender, EventArgs e) => await GoTo(new SignOut());
    private async void OnProductsClicked(object sender, EventArgs e) => await GoTo(new Products());
}
