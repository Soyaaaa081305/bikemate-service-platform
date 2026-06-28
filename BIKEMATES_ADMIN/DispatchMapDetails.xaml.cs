namespace BIKEMATES_ADMIN;

public partial class DispatchMapDetails : ContentPage
{
    public DispatchMapDetails()
    {
        InitializeComponent();
    }

    private async void OnCallMechanicClicked(object sender, EventArgs e)
        => await DisplayAlert("Call Mechanic", "This will call the mechanic once contact data is connected.", "OK");

    private async void OnOpenRequestClicked(object sender, EventArgs e)
        => await DisplayAlert("Request Details", "This will open the full customer request once API details are connected.", "OK");

    private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();
    private async void OnMessagesClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Messages());
    private async void OnMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MenuPage());
}
