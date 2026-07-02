using System.Collections.ObjectModel;
using BIKEMATES_ADMIN.Services;
using Microsoft.Maui.Graphics;

namespace BIKEMATES_ADMIN.Pages;

public partial class MainPage : ContentPage
{
    public ObservableCollection<MechanicStatusItem> Mechanics { get; } = new();

    private bool _loaded;

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
        ApplySession();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ApplySession();
        if (!_loaded)
        {
            _loaded = true;
            await LoadDashboardAsync();
        }
    }

    private void ApplySession()
    {
        var user = AppSession.CurrentUser;
        var firstName = string.IsNullOrWhiteSpace(user?.FirstName) ? "Admin" : user!.FirstName;
        var shopName = string.IsNullOrWhiteSpace(user?.ShopName) ? "No assigned bike shop yet" : user!.ShopName!;

        WelcomeLabel.Text = $"Welcome, {firstName}";
        RolePillLabel.Text = user?.IsOwner == true ? "OWNER" : "ADMIN";
        ShopNameLabel.Text = shopName;
    }

    private async Task LoadDashboardAsync()
    {
        try
        {
            ApiStatusLabel.Text = "SYNC";
            var dashboard = await BikeMateDatabaseService.GetAdminDashboardAsync();
            ShopNameLabel.Text = dashboard.Profile.ShopName;
            RevenueLabel.Text = $"PHP {dashboard.MonthlyRevenue:N2}";
            ServicesLabel.Text = dashboard.Services.ToString();
            ActiveBookingsLabel.Text = dashboard.ActiveBookings.ToString();
            TodaysBookingsLabel.Text = dashboard.TodaysBookings.ToString();
            LowStockLabel.Text = dashboard.InventoryAlerts.ToString();

            Mechanics.Clear();
            foreach (var mechanic in await BikeMateDatabaseService.GetMechanicsAsync())
            {
                Mechanics.Add(MechanicStatusItem.FromApi(mechanic));
            }

            ActivityLabel.Text = dashboard.ActiveBookings == 0
                ? "No active bookings yet. Customer service requests will appear here after the customer app creates them."
                : $"{dashboard.ActiveBookings} active booking(s) require monitoring.";
            ApiStatusLabel.Text = "LIVE";
        }
        catch (Exception ex)
        {
            ApiStatusLabel.Text = "OFFLINE";
            ActivityLabel.Text = $"Unable to load dashboard from API: {ex.Message}";
        }
        finally
        {
            DashboardRefreshView.IsRefreshing = false;
        }
    }

    private async void DashboardRefreshView_Refreshing(object sender, EventArgs e) => await LoadDashboardAsync();
    private async void Products_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(Products));
    private async void Operations_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(Operations));
    private async void Calendar_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(Calendar));
    private async void Reports_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(Reports));
    private async void Messages_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//AdminTabs/MessagesTab");
    private async void Notifications_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(Notifications));
}

public sealed record MechanicStatusItem(string Initials, string Name, string Status, Color StatusColor)
{
    public static MechanicStatusItem FromApi(AdminMechanic mechanic)
    {
        var status = string.IsNullOrWhiteSpace(mechanic.AvailabilityStatus)
            ? "unavailable"
            : mechanic.AvailabilityStatus.Trim();
        return new MechanicStatusItem(
            BuildInitials(mechanic.FullName),
            mechanic.FullName,
            status,
            StatusToColor(status));
    }

    public static Color StatusToColor(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "available" or "online" => Color.FromArgb("#16A34A"),
            "dispatched" or "on_job" or "accepted" or "en_route" or "in_progress" => Color.FromArgb("#FF7A2D"),
            "arrived" => Color.FromArgb("#CA8A04"),
            _ => Color.FromArgb("#DC2626")
        };
    }

    private static string BuildInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "M";
        if (parts.Length == 1) return parts[0][0].ToString().ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}



