using BIKEMATES_ADMIN.Pages.Main;

namespace BIKEMATES_ADMIN;

public partial class Reports : ContentPage
{
    public Reports()
    {
        InitializeComponent();
        ReportTypePicker.ItemsSource = new[] { "Sales", "Orders", "Dispatch", "Inventory", "Admin Activity" };
        ReportTypePicker.SelectedIndex = 0;
        FromDatePicker.Date = DateTime.Today.AddDays(-7);
        ToDatePicker.Date = DateTime.Today;
        GeneratePreview();
    }

    private void OnGenerateClicked(object sender, EventArgs e) => GeneratePreview();

    private void GeneratePreview()
    {
        var type = ReportTypePicker.SelectedItem?.ToString() ?? "Sales";
        ReportPreviewLabel.Text =
            $"{type} report\n" +
            $"{FromDatePicker.Date:MMM d, yyyy} to {ToDatePicker.Date:MMM d, yyyy}\n\n" +
            "Orders processed: 18\n" +
            "Completed services: 7\n" +
            "Estimated revenue: PHP 24,850.00\n" +
            "Low stock items: 4";
    }

    private async void OnExportClicked(object sender, EventArgs e)
        => await DisplayAlert("Export Report", "Report export will connect to downloadable files in the next API phase.", "OK");

    private async void OnHomeClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MainPage());
    private async void OnLogisticsClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Logistics());
    private async void OnAdminsClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Admins());
    private async void OnMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MenuPage());
}
