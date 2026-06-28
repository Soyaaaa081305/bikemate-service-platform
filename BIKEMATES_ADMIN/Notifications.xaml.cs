using BIKEMATES_ADMIN.Pages.Main;

namespace BIKEMATES_ADMIN;

public partial class Notifications : ContentPage
{
    private readonly List<ShopNotification> _notifications = new();

    public Notifications()
    {
        InitializeComponent();
        _notifications.Add(new ShopNotification("New customer booking for today.", false));
        _notifications.Add(new ShopNotification("Four items are below low-stock level.", false));
        _notifications.Add(new ShopNotification("Payment confirmation received.", true));
        RefreshNotifications();
    }

    private void RefreshNotifications()
    {
        var unread = _notifications.Count(notification => !notification.IsRead);
        UnreadCountLabel.Text = $"{unread} unread alerts";
        NotificationListLabel.Text = string.Join(
            Environment.NewLine,
            _notifications.Select(notification => $"{(notification.IsRead ? "Read" : "New")}: {notification.Message}"));
    }

    private void OnMarkAllReadClicked(object sender, EventArgs e)
    {
        for (var index = 0; index < _notifications.Count; index++)
        {
            _notifications[index] = _notifications[index] with { IsRead = true };
        }

        RefreshNotifications();
    }

    private void OnRefreshClicked(object sender, EventArgs e) => RefreshNotifications();

    private async void OnHomeClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MainPage());
    private async void OnMessagesClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Messages());
    private async void OnSettingsClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Settings());
    private async void OnMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MenuPage());

    private sealed record ShopNotification(string Message, bool IsRead);
}
