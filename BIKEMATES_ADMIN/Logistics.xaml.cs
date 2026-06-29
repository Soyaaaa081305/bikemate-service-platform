namespace BIKEMATES_ADMIN;

public partial class Logistics : ContentPage
{
	public Logistics()
	{
		InitializeComponent();
        RefreshMetrics();
	}

    private void RefreshMetrics()
    {
        RevenueLabel.Text = "PHP 86,420";
        OrdersLabel.Text = "37";
        PaymentStatusLabel.Text = $"PayMongo status: ready - last sync {DateTime.Now:t}";
    }

    private void OnRefreshClicked(object sender, EventArgs e) => RefreshMetrics();

    private async void OnExportClicked(object sender, EventArgs e)
        => await DisplayAlert("Export queued", "Sales report export is prepared for the reports module.", "OK");

    private async void OnMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MenuPage());
}
