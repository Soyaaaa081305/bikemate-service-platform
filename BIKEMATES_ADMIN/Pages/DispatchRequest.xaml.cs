using System.Collections.ObjectModel;
using System.ComponentModel;
using BIKEMATES_ADMIN.Services;
using Microsoft.Maui.Graphics;

namespace BIKEMATES_ADMIN.Pages;

public partial class DispatchRequest : ContentPage
{
    public ObservableCollection<DispatchMechanicItem> Mechanics { get; } = new();
    public ObservableCollection<CustomerRequestItem> Requests { get; } = new();

    private DispatchMechanicItem? _selectedMechanic;
    private CustomerRequestItem? _selectedRequest;
    private bool _loaded;

    public DispatchRequest()
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
            await LoadDispatchDataAsync();
        }
    }

    private async Task LoadDispatchDataAsync()
    {
        try
        {
            Mechanics.Clear();
            foreach (var mechanic in await BikeMateDatabaseService.GetMechanicsAsync())
            {
                Mechanics.Add(DispatchMechanicItem.FromApi(mechanic));
            }

            Requests.Clear();
            foreach (var request in await BikeMateDatabaseService.GetBookingsAsync())
            {
                Requests.Add(CustomerRequestItem.FromApi(request));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Dispatch", $"Unable to load dispatch data from API: {ex.Message}", "OK");
        }
    }

    private void MechanicsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedMechanic = e.CurrentSelection.FirstOrDefault() as DispatchMechanicItem;
        UpdateSelectionLabel();
    }

    private void RequestsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedRequest = e.CurrentSelection.FirstOrDefault() as CustomerRequestItem;
        UpdateSelectionLabel();
    }

    private async void AssignMechanic_Clicked(object sender, EventArgs e)
    {
        if (_selectedMechanic is null || _selectedRequest is null)
        {
            await DisplayAlert("Missing Selection", "Select both mechanic and request first.", "OK");
            return;
        }

        if (_selectedMechanic.StatusType == MechanicDispatchStatus.Unavailable)
        {
            await DisplayAlert("Unavailable", "This mechanic is currently unavailable.", "OK");
            return;
        }

        try
        {
            await BikeMateDatabaseService.AssignMechanicAsync(_selectedRequest.RequestId, _selectedMechanic.MechanicId);
            _selectedMechanic.StatusType = MechanicDispatchStatus.Dispatched;
            _selectedRequest.Status = "accepted";
            UpdateSelectionLabel();
            await DisplayAlert("Dispatched", $"{_selectedMechanic.Name} was assigned to {_selectedRequest.CustomerName}.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Dispatch Error", ex.Message, "OK");
        }
    }

    private async void ViewDispatchDetails_Clicked(object sender, EventArgs e)
    {
        if (_selectedRequest is null)
        {
            await DisplayAlert("No Request", "Select a customer request first.", "OK");
            return;
        }

        DispatchDetails.SelectedRequest = _selectedRequest.SourceRequest;
        DispatchDetails.SelectedMechanic = _selectedMechanic;
        await Shell.Current.GoToAsync(nameof(DispatchDetails));
    }

    private async void MarkArrived_Clicked(object sender, EventArgs e)
    {
        if (_selectedRequest is null)
        {
            await DisplayAlert("No Request", "Select a customer request before changing status.", "OK");
            return;
        }

        try
        {
            await BikeMateDatabaseService.UpdateRequestStatusAsync(_selectedRequest.RequestId, "arrived", "Mechanic arrived at the destination.");
            _selectedRequest.Status = "arrived";
            if (_selectedMechanic is not null)
            {
                _selectedMechanic.StatusType = MechanicDispatchStatus.Arrived;
            }
            UpdateSelectionLabel();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Status Error", ex.Message, "OK");
        }
    }

    private async void SetAvailable_Clicked(object sender, EventArgs e)
    {
        if (_selectedMechanic is null)
        {
            await DisplayAlert("No Mechanic", "Select a mechanic before changing the local status view.", "OK");
            return;
        }

        await DisplayAlert("Mechanic Status", "Mechanic availability is controlled by the mechanic side/API. This local view will refresh from shop/mechanics.", "OK");
        await LoadDispatchDataAsync();
    }

    private void UpdateSelectionLabel()
    {
        string mechanic = _selectedMechanic?.Name ?? "No mechanic selected";
        string request = _selectedRequest?.CustomerName ?? "No request selected";
        SelectedDispatchLabel.Text = $"Mechanic: {mechanic}\nRequest: {request}";
    }
}

public enum MechanicDispatchStatus
{
    Unavailable,
    Available,
    Dispatched,
    Arrived


}

public sealed class DispatchMechanicItem : INotifyPropertyChanged
{
    private MechanicDispatchStatus _statusType;

    public DispatchMechanicItem(int mechanicId, string initials, string name, MechanicDispatchStatus statusType)
    {
        MechanicId = mechanicId;
        Initials = initials;
        Name = name;
        _statusType = statusType;
    }

    public int MechanicId { get; }
    public string Initials { get; }
    public string Name { get; }

    public MechanicDispatchStatus StatusType
    {
        get => _statusType;
        set
        {
            if (_statusType == value) return;
            _statusType = value;
            OnPropertyChanged(nameof(StatusType));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusColor));
        }
    }

    public string Status => StatusType switch
    {
        MechanicDispatchStatus.Unavailable => "Unavailable",
        MechanicDispatchStatus.Available => "Available",
        MechanicDispatchStatus.Dispatched => "Dispatched",
        MechanicDispatchStatus.Arrived => "Arrived",
        _ => "Unknown"
    };

    public Color StatusColor => StatusType switch
    {
        MechanicDispatchStatus.Unavailable => Color.FromArgb("#DC2626"),
        MechanicDispatchStatus.Available => Color.FromArgb("#16A34A"),
        MechanicDispatchStatus.Dispatched => Color.FromArgb("#FF7A2D"),
        MechanicDispatchStatus.Arrived => Color.FromArgb("#CA8A04"),
        _ => Color.FromArgb("#6B7280")
    };

    public static DispatchMechanicItem FromApi(AdminMechanic mechanic)
    {
        return new DispatchMechanicItem(
            mechanic.MechanicId,
            BuildInitials(mechanic.FullName),
            mechanic.FullName,
            StatusFromApi(mechanic.AvailabilityStatus));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static MechanicDispatchStatus StatusFromApi(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "available" or "online" => MechanicDispatchStatus.Available,
            "dispatched" or "on_job" or "accepted" or "en_route" or "in_progress" => MechanicDispatchStatus.Dispatched,
            "arrived" => MechanicDispatchStatus.Arrived,
            _ => MechanicDispatchStatus.Unavailable
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

public sealed class CustomerRequestItem : INotifyPropertyChanged
{
    private string _status;

    public CustomerRequestItem(AdminServiceRequest sourceRequest, string requestType, string address)
    {
        SourceRequest = sourceRequest;
        RequestId = sourceRequest.RequestId;
        CustomerName = sourceRequest.CustomerName;
        RequestType = requestType;
        Address = address;
        _status = sourceRequest.CurrentStatus;
    }

    public AdminServiceRequest SourceRequest { get; }
    public int RequestId { get; }
    public string CustomerName { get; }
    public string RequestType { get; }
    public string Address { get; }

    public string Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(Priority));
            OnPropertyChanged(nameof(PriorityColor));
        }
    }

    public string Priority => string.IsNullOrWhiteSpace(Status) ? "PENDING" : Status.ToUpperInvariant();
    public Color PriorityColor => Status.Trim().ToLowerInvariant() switch
    {
        "pending" => Color.FromArgb("#2563EB"),
        "accepted" or "en_route" or "in_progress" => Color.FromArgb("#FF7A2D"),
        "arrived" => Color.FromArgb("#CA8A04"),
        "completed" => Color.FromArgb("#16A34A"),
        "cancelled" or "rejected" => Color.FromArgb("#DC2626"),
        _ => Color.FromArgb("#6B7280")
    };

    public static CustomerRequestItem FromApi(AdminServiceRequest request)
    {
        return new CustomerRequestItem(
            request,
            request.ServiceName ?? request.IssueDescription,
            request.ServiceLocationAddress ?? "No service address");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}





