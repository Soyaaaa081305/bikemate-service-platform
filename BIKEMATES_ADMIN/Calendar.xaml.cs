using BIKEMATES_ADMIN.Pages.Main;

namespace BIKEMATES_ADMIN;

public partial class Calendar : ContentPage
{
    private readonly List<ScheduleItem> _schedules = new();

    public Calendar()
    {
        InitializeComponent();
        MechanicPicker.ItemsSource = new[] { "Unassigned", "Mark", "Isaiah", "Jose", "Mechanic Team A" };
        MechanicPicker.SelectedIndex = 0;
        SeedSchedule();
        RefreshSchedule();
    }

    private void SeedSchedule()
    {
        var today = DateTime.Today;
        _schedules.Add(new ScheduleItem(today, "Pickup order preparation", "Mark"));
        _schedules.Add(new ScheduleItem(today, "Brake tune-up booking", "Mechanic Team A"));
        _schedules.Add(new ScheduleItem(today.AddDays(1), "Wheelset installation", "Isaiah"));
    }

    private async void OnAddScheduleClicked(object sender, EventArgs e)
    {
        var title = BookingTitleEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            await DisplayAlert("Schedule Needed", "Enter a booking or operation title.", "OK");
            return;
        }

        var mechanic = MechanicPicker.SelectedItem?.ToString() ?? "Unassigned";
        _schedules.Add(new ScheduleItem(ScheduleDatePicker.Date ?? DateTime.Today, title, mechanic));
        BookingTitleEntry.Text = string.Empty;
        RefreshSchedule();
    }

    private void OnTodayClicked(object sender, EventArgs e)
    {
        ScheduleDatePicker.Date = DateTime.Today;
        RefreshSchedule();
    }

    private void OnDateSelected(object sender, DateChangedEventArgs e) => RefreshSchedule();

    private void RefreshSchedule()
    {
        var date = (ScheduleDatePicker.Date ?? DateTime.Today).Date;
        SelectedDateLabel.Text = date == DateTime.Today
            ? "Today"
            : date.ToString("MMMM d, yyyy");

        var items = _schedules
            .Where(schedule => schedule.Date.Date == date)
            .Select(schedule => $"{schedule.Title} - {schedule.Mechanic}")
            .ToList();

        ScheduleListLabel.Text = items.Count == 0
            ? "No schedule yet for this date."
            : string.Join(Environment.NewLine, items);
    }

    private async void OnHomeClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MainPage());
    private async void OnDispatchClicked(object sender, EventArgs e) => await Navigation.PushAsync(new DispatchAndRequest());
    private async void OnMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MenuPage());

    private sealed record ScheduleItem(DateTime Date, string Title, string Mechanic);
}
