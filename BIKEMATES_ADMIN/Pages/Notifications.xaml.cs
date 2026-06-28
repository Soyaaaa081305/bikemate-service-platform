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
        }
        catch (Exception ex)
        {
            await DisplayAlert("Notifications", $"Unable to load notifications from API: {ex.Message}", "OK");
        }
    }

    private async void MarkAllAsRead_Clicked(object sender, EventArgs e)
    {
        try
        {
            await BikeMateDatabaseService.MarkAllNotificationsReadAsync();
            await LoadNotificationsAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Notifications", ex.Message, "OK");
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



