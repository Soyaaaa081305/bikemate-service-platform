using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages;

public partial class DispatchDetails : ContentPage
{
    public static AdminServiceRequest? SelectedRequest { get; set; }
    public static DispatchMechanicItem? SelectedMechanic { get; set; }

    public DispatchDetails()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplySelection();
    }

    private void ApplySelection()
    {
        var request = SelectedRequest;
        var mechanic = SelectedMechanic;

        if (mechanic is not null)
        {
            MechanicInitialsLabel.Text = mechanic.Initials;
            MechanicNameLabel.Text = mechanic.Name;
            MechanicStatusLabel.Text = $"Status: {mechanic.Status}";
        }

        if (request is null)
        {
            return;
        }

        CustomerLabel.Text = $"Customer: {request.CustomerName}";
        RequestLabel.Text = $"Request: {request.ServiceName ?? request.IssueDescription}";
        AddressLabel.Text = $"Address: {request.ServiceLocationAddress ?? "No service address"}";
        ProblemLabel.Text = request.IssueDescription;
        CreatedAtLabel.Text = $"Created: {request.CreatedAt:g}";
        ScheduledAtLabel.Text = request.ScheduledAt is null ? "Scheduled: not set" : $"Scheduled: {request.ScheduledAt:g}";
        CurrentStatusLabel.Text = $"Current status: {request.CurrentStatus}";
        CoordinateLabel.Text = request.ServiceLatitude is null || request.ServiceLongitude is null
            ? "No customer coordinates loaded"
            : $"{request.ServiceLatitude:0.000000}, {request.ServiceLongitude:0.000000}";
    }

    private async void CallMechanic_Clicked(object sender, EventArgs e)
    {
        await DisplayAlert("Call Mechanic", "Phone calling can be connected once mechanic contact data is exposed by the API.", "OK");
    }

    private async void MarkComplete_Clicked(object sender, EventArgs e)
    {
        if (SelectedRequest is null)
        {
            await DisplayAlert("No Request", "Select a dispatch request first.", "OK");
            return;
        }

        try
        {
            await BikeMateDatabaseService.UpdateRequestStatusAsync(SelectedRequest.RequestId, "completed", "Marked complete by shop admin.");
            CurrentStatusLabel.Text = "Current status: completed";
            await DisplayAlert("Complete", "The service request was marked complete through the API.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Status Error", ex.Message, "OK");
        }
    }
}



