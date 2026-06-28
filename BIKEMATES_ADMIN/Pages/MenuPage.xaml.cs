using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages;

public partial class MenuPage : ContentPage
{
    public MenuPage()
    {
        InitializeComponent();
        ApplySession();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplySession();
    }

    private void ApplySession()
    {
        var user = AppSession.CurrentUser;
        var firstName = string.IsNullOrWhiteSpace(user?.FirstName) ? "Admin" : user!.FirstName;
        var lastName = user?.LastName ?? string.Empty;
        var fullName = $"{firstName} {lastName}".Trim();
        var shopName = string.IsNullOrWhiteSpace(user?.ShopName) ? "No assigned bike shop yet" : user!.ShopName!;
        var role = user?.IsOwner == true ? "Owner" : "Admin";

        AdminNameLabel.Text = fullName;
        AdminShopLabel.Text = $"{role} - {shopName}";
        InitialsLabel.Text = BuildInitials(firstName, lastName);
    }

    private async void Home_Clicked(object sender, EventArgs e) => await GoToTabAsync("Home");
    private async void ShopProfile_Clicked(object sender, EventArgs e) => await GoToTabAsync("Profile");
    private async void Products_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(Products));
    private async void Logistics_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(Logistics));
    private async void Calendar_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(Calendar));
    private async void Operations_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(Operations));
    private async void Dispatch_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(DispatchRequest));
    private async void Admins_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(Admins));
    private async void Messages_Clicked(object sender, EventArgs e) => await GoToTabAsync("MessagesTab");
    private async void Reports_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(Reports));
    private async void Notifications_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(Notifications));
    private async void Settings_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(Settings));
    private async void Help_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(HelpSupport));

    private static Task GoToTabAsync(string route)
    {
        return Shell.Current.GoToAsync($"//{route}");
    }

    private static string BuildInitials(string firstName, string lastName)
    {
        var first = string.IsNullOrWhiteSpace(firstName) ? "A" : firstName.Trim()[0].ToString();
        var last = string.IsNullOrWhiteSpace(lastName) ? "D" : lastName.Trim()[0].ToString();
        return $"{first}{last}".ToUpperInvariant();
    }
}


