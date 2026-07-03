using System.Collections.ObjectModel;
using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages;

public partial class Notifications : ContentPage
{
    public ObservableCollection<NotificationItem> NotificationItems { get; } = new();

    private bool _loaded;

    public Notifications()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded)
        {
            _loaded = true;
            await LoadNotificationsAsync();
        }
    }

    private async Task LoadNotificationsAsync()
    {
        try
        {
            NotificationItems.Clear();
            foreach (var notification in await BikeMateDatabaseService.GetNotificationsAsync())
            {
                NotificationItems.Add(NotificationItem.FromApi(notification));
            }

            NotificationStatusLabel.Text = NotificationItems.Count == 0
                ? "No new alerts returned by the API."
                : $"{NotificationItems.Count} alert(s) loaded from the API.";
            MarkAllButton.IsEnabled = NotificationItems.Any(item => !item.IsRead);
            MarkAllButton.Opacity = MarkAllButton.IsEnabled ? 1 : 0.55;
        }
        catch (Exception ex)
        {
            NotificationStatusLabel.Text = $"Unable to load notifications: {ex.Message}";
            MarkAllButton.IsEnabled = false;
            MarkAllButton.Opacity = 0.55;
        }
    }

    private async void Reload_Clicked(object? sender, EventArgs e) => await LoadNotificationsAsync();

    private async void MarkAllAsRead_Clicked(object? sender, EventArgs e)
    {
        try
        {
            await BikeMateDatabaseService.MarkAllNotificationsReadAsync();
            await LoadNotificationsAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Notifications", ex.Message, "OK");
        }
    }

}

public sealed record NotificationItem(string Title, string Message, bool IsRead, DateTime CreatedAt)
{
    public string CreatedAtText => CreatedAt.ToLocalTime().ToString("g");

    public static NotificationItem FromApi(AdminNotification notification)
    {
        return new NotificationItem(notification.Title, notification.Message, notification.IsRead, notification.CreatedAt);
    }
}



