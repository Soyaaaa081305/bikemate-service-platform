using System.Collections.ObjectModel;
using System.Globalization;
using BIKEMATES_ADMIN.Services;
using Microsoft.Maui.Graphics;

namespace BIKEMATES_ADMIN.Pages;

public partial class Calendar : ContentPage
{
    public ObservableCollection<BookingItem> AllBookings { get; } = new();
    public ObservableCollection<BookingItem> VisibleBookings { get; } = new();

    private string _filter = "active";
    private string _searchText = string.Empty;
    private bool _loaded;

    public Calendar()
    {
        InitializeComponent();
        BindingContext = this;
        ApplyFilterStyles();
        BookingStatusLabel.Text = "Pull current bookings from the shop API.";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded)
        {
            _loaded = true;
            await LoadBookingsAsync();
        }
    }

    private async Task LoadBookingsAsync()
    {
        ReloadButton.IsEnabled = false;
        ReloadButton.Text = "Loading";
        BookingStatusLabel.Text = "Loading customer bookings...";

        try
        {
            AllBookings.Clear();
            foreach (var booking in await BikeMateDatabaseService.GetBookingsAsync())
            {
                AllBookings.Add(BookingItem.FromApi(booking));
            }

            FilterBookings();
            var activeCount = AllBookings.Count(item => !item.IsHistory);
            BookingsSubtitleLabel.Text = $"{activeCount} active booking(s) from the shop API.";
            BookingStatusLabel.Text = AllBookings.Count == 0
                ? "No customer bookings returned yet."
                : "Use Message or the next action for each booking.";
        }
        catch (Exception ex)
        {
            BookingStatusLabel.Text = $"Unable to load bookings: {ex.Message}";
        }
        finally
        {
            ReloadButton.IsEnabled = true;
            ReloadButton.Text = "Reload";
        }
    }

    private void FilterBookings()
    {
        VisibleBookings.Clear();
        var search = _searchText.Trim();

        foreach (var booking in AllBookings
            .Where(MatchesFilter)
            .Where(item => string.IsNullOrWhiteSpace(search) ||
                item.SearchText.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.SortDate))
        {
            VisibleBookings.Add(booking);
        }
    }

    private bool MatchesFilter(BookingItem item)
    {
        return _filter switch
        {
            "today" => item.IsToday,
            "history" => item.IsHistory,
            _ => !item.IsHistory
        };
    }

    private void SelectFilter(string filter)
    {
        _filter = filter;
        ApplyFilterStyles();
        FilterBookings();
    }

    private void ApplyFilterStyles()
    {
        ApplyFilterButton(ActiveFilterButton, _filter == "active");
        ApplyFilterButton(TodayFilterButton, _filter == "today");
        ApplyFilterButton(HistoryFilterButton, _filter == "history");
    }

    private static void ApplyFilterButton(Button button, bool selected)
    {
        button.BackgroundColor = selected ? Color.FromArgb("#FF6B2C") : Colors.White;
        button.TextColor = selected ? Colors.White : Color.FromArgb("#242424");
        button.BorderColor = selected ? Color.FromArgb("#FF6B2C") : Color.FromArgb("#D1D5DB");
    }

    private async void Reload_Clicked(object? sender, EventArgs e) => await LoadBookingsAsync();
    private void ActiveFilter_Clicked(object? sender, EventArgs e) => SelectFilter("active");
    private void TodayFilter_Clicked(object? sender, EventArgs e) => SelectFilter("today");
    private void HistoryFilter_Clicked(object? sender, EventArgs e) => SelectFilter("history");

    private void BookingSearchBar_TextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? string.Empty;
        FilterBookings();
    }

    private async void MessageBooking_Clicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminTabs/MessagesTab");
    }

    private async void PrimaryBookingAction_Clicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: BookingItem item })
        {
            return;
        }

        if (item.IsHistory)
        {
            await DisplayAlertAsync("Booking closed", $"BM-{item.RequestId:000000} is already {item.StatusText.ToLowerInvariant()}.", "OK");
            return;
        }

        var nextStatus = item.NextStatus;
        if (string.IsNullOrWhiteSpace(nextStatus))
        {
            await Shell.Current.GoToAsync(nameof(Operations));
            return;
        }

        var confirm = await DisplayAlertAsync(
            "Update booking status",
            $"Move BM-{item.RequestId:000000} to {BookingItem.FormatStatus(nextStatus)}?",
            "Update",
            "Cancel");
        if (!confirm)
        {
            return;
        }

        try
        {
            await BikeMateDatabaseService.UpdateRequestStatusAsync(item.RequestId, nextStatus, "Updated from shop-admin mobile app.");
            await LoadBookingsAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Booking update failed", ex.Message, "OK");
        }
    }
}

public sealed record BookingItem(
    int RequestId,
    string Status,
    string CustomerName,
    string? MechanicName,
    string ServiceName,
    string IssueDescription,
    string? ServiceLocationAddress,
    DateTime? ScheduledAt,
    decimal EstimatedTotal,
    decimal FinalTotal,
    DateTime CreatedAt)
{
    public string StatusText => FormatStatus(Status);
    public bool IsHistory => Status.Equals("completed", StringComparison.OrdinalIgnoreCase) ||
        Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase) ||
        Status.Equals("rejected", StringComparison.OrdinalIgnoreCase) ||
        Status.Equals("ServiceCompleted", StringComparison.OrdinalIgnoreCase);
    public bool IsToday => (ScheduledAt ?? CreatedAt).ToLocalTime().Date == DateTime.Today;
    public DateTime SortDate => ScheduledAt ?? CreatedAt;
    public string CustomerLine => string.IsNullOrWhiteSpace(MechanicName)
        ? $"Customer: {CustomerName} | Mechanic not assigned"
        : $"Customer: {CustomerName} | Mechanic: {MechanicName}";
    public string TimingText => ScheduledAt?.ToLocalTime().ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture) ??
        $"Requested {CreatedAt.ToLocalTime():MMM d, h:mm tt}";
    public string TotalText => string.Format(CultureInfo.GetCultureInfo("en-PH"), "PHP {0:N0}", FinalTotal > 0 ? FinalTotal : EstimatedTotal);
    public string LocationText => string.IsNullOrWhiteSpace(ServiceLocationAddress) ? "Location not provided" : ServiceLocationAddress;
    public string IssuePreview => CleanIssue(IssueDescription);
    public string SearchText => $"{RequestId} {Status} {CustomerName} {MechanicName} {ServiceName} {IssueDescription} {ServiceLocationAddress}";
    public Color StatusColor => Status.Trim().ToLowerInvariant() switch
    {
        "pending" or "submitted" => Color.FromArgb("#F97316"),
        "accepted" or "assigned" or "confirmed" => Color.FromArgb("#2563EB"),
        "en_route" or "arrived" or "in_progress" => Color.FromArgb("#7C3AED"),
        "completed" or "servicecompleted" or "paid" => Color.FromArgb("#16A34A"),
        "cancelled" or "rejected" => Color.FromArgb("#DC2626"),
        _ => Color.FromArgb("#64748B")
    };
    public string PrimaryActionText => IsHistory ? "Closed" : NextStatus is null ? "Services" : FormatStatus(NextStatus);
    public string? NextStatus => Status.Trim().ToLowerInvariant() switch
    {
        "pending" or "submitted" => "accepted",
        "accepted" or "assigned" or "confirmed" => "in_progress",
        "en_route" or "arrived" or "in_progress" => "completed",
        _ => null
    };

    public static BookingItem FromApi(AdminServiceRequest request)
    {
        return new BookingItem(
            request.RequestId,
            request.CurrentStatus,
            request.CustomerName,
            request.MechanicName,
            request.ServiceName ?? "Bike service",
            request.IssueDescription,
            request.ServiceLocationAddress,
            request.ScheduledAt,
            request.EstimatedTotal,
            request.FinalTotal,
            request.CreatedAt);
    }

    public static string FormatStatus(string status)
    {
        return string.Join(" ", status
            .Replace("_", " ", StringComparison.Ordinal)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word.ToLowerInvariant())));
    }

    private static string CleanIssue(string issue)
    {
        var vehicleIndex = issue.IndexOf("\nVehicle:", StringComparison.OrdinalIgnoreCase);
        return (vehicleIndex >= 0 ? issue[..vehicleIndex] : issue).Trim();
    }
}
