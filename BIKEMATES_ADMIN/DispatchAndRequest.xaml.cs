using BIKEMATES_ADMIN.Pages.Main;

namespace BIKEMATES_ADMIN;

public partial class DispatchAndRequest : ContentPage
{
    private readonly List<ServiceRequest> _requests = new();

    public DispatchAndRequest()
    {
        InitializeComponent();
        MechanicPicker.ItemsSource = new[]
        {
            "Jose Reyes - Green: Available",
            "Ramon Lee - Red: Unavailable",
            "Marco Cruz - Orange: Dispatched",
            "Alex Lim - Yellow: Arrived"
        };
        MechanicPicker.SelectedIndex = 0;
        SeedRequests();
        RefreshRequests();
    }

    private void SeedRequests()
    {
        _requests.Add(new ServiceRequest("Brake adjustment", "Pending", "BGC customer - 2:00 PM"));
        _requests.Add(new ServiceRequest("Install new crankset", "Accepted", "Makati customer - tomorrow"));
        _requests.Add(new ServiceRequest("Flat tire repair", "Pending", "Pasig customer - urgent"));
    }

    private void RefreshRequests()
    {
        RequestPicker.ItemsSource = null;
        RequestPicker.ItemsSource = _requests.Select(request => $"{request.Title} - {request.Status}").ToList();
        RequestStatusLabel.Text = string.Join(Environment.NewLine, _requests.Select(request => $"{request.Title}: {request.Status}"));
    }

    private void OnRequestSelected(object sender, EventArgs e)
    {
        if (RequestPicker.SelectedIndex < 0 || RequestPicker.SelectedIndex >= _requests.Count)
        {
            RequestDetailsLabel.Text = "Choose a request to view details.";
            return;
        }

        var request = _requests[RequestPicker.SelectedIndex];
        RequestDetailsLabel.Text = $"{request.Title}{Environment.NewLine}{request.Details}{Environment.NewLine}Status: {request.Status}";
    }

    private async void OnAcceptClicked(object sender, EventArgs e) => await UpdateSelectedRequestAsync("Accepted");

    private async void OnAssignClicked(object sender, EventArgs e)
    {
        if (RequestPicker.SelectedIndex < 0 || RequestPicker.SelectedIndex >= _requests.Count)
        {
            await DisplayAlert("Select Request", "Choose a customer request first.", "OK");
            return;
        }

        var mechanic = MechanicPicker.SelectedItem?.ToString() ?? "selected mechanic";
        _requests[RequestPicker.SelectedIndex] = _requests[RequestPicker.SelectedIndex] with { Status = $"Assigned to {mechanic}" };
        RefreshRequests();
        OnRequestSelected(sender, e);
    }

    private async Task UpdateSelectedRequestAsync(string status)
    {
        if (RequestPicker.SelectedIndex < 0 || RequestPicker.SelectedIndex >= _requests.Count)
        {
            await DisplayAlert("Select Request", "Choose a customer request first.", "OK");
            return;
        }

        _requests[RequestPicker.SelectedIndex] = _requests[RequestPicker.SelectedIndex] with { Status = status };
        RefreshRequests();
        OnRequestSelected(this, EventArgs.Empty);
    }

    private async void OnMessageCustomerClicked(object sender, EventArgs e)
        => await DisplayAlert("Message Customer", "This opens the customer conversation once messaging is connected.", "OK");

    private void OnRefreshClicked(object sender, EventArgs e) => RefreshRequests();
    private async void OnViewMapClicked(object sender, EventArgs e) => await Navigation.PushAsync(new DispatchMapDetails());

    private async void OnHomeClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MainPage());
    private async void OnCalendarClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Calendar());
    private async void OnOperationsClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Operations());
    private async void OnMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MenuPage());

    private sealed record ServiceRequest(string Title, string Status, string Details);
}
