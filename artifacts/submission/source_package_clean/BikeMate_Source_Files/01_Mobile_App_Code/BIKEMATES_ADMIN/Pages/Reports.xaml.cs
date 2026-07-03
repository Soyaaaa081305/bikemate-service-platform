using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace BIKEMATES_ADMIN.Pages;

public partial class Reports : ContentPage
{
    private Services.AdminDashboard? _dashboard;
    private bool _loaded;

    public Reports()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded)
        {
            _loaded = true;
            await LoadReportAsync();
        }
    }

    private async Task LoadReportAsync()
    {
        try
        {
            _dashboard = await Services.BikeMateDatabaseService.GetAdminDashboardAsync();
            RevenueLabel.Text = $"PHP {_dashboard.MonthlyRevenue:N2}";
            BookingsLabel.Text = _dashboard.ActiveBookings.ToString();
            ServicesSummaryLabel.Text = $"{_dashboard.Services} active service(s), {_dashboard.InventoryAlerts} low-stock inventory alert(s), {_dashboard.Mechanics} mechanic(s).";
            RatingsSummaryLabel.Text = _dashboard.AverageRating <= 0
                ? "Ratings will appear after completed jobs receive customer reviews."
                : $"Average mechanic rating: {_dashboard.AverageRating:N1}";
            ExportStatusLabel.Text = $"Report loaded for {_dashboard.Profile.ShopName}.";
        }
        catch (Exception ex)
        {
            ExportStatusLabel.Text = $"Unable to load report data: {ex.Message}";
        }
    }

    private async void ExportReport_Clicked(object sender, EventArgs e)
    {
        try
        {
            var dashboard = _dashboard ?? await Services.BikeMateDatabaseService.GetAdminDashboardAsync();
            var fileName = $"bikemate-shop-report-{DateTime.Now:yyyyMMdd-HHmm}.csv";
            var path = Path.Combine(FileSystem.AppDataDirectory, fileName);
            var lines = new[]
            {
                "Metric,Value",
                $"Shop,{EscapeCsv(dashboard.Profile.ShopName)}",
                $"Generated,{DateTime.Now:yyyy-MM-dd HH:mm}",
                $"Monthly Revenue,{dashboard.MonthlyRevenue:N2}",
                $"Active Bookings,{dashboard.ActiveBookings}",
                $"Bookings Today,{dashboard.TodaysBookings}",
                $"Services,{dashboard.Services}",
                $"Inventory Alerts,{dashboard.InventoryAlerts}",
                $"Mechanics,{dashboard.Mechanics}",
                $"Average Rating,{dashboard.AverageRating:N1}"
            };

            await File.WriteAllLinesAsync(path, lines);
            ExportStatusLabel.Text = $"Exported: {path}";
            await DisplayAlert("Report Exported", $"CSV report saved as {fileName}. BikeMate will open Android sharing next.", "OK");
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "BikeMate shop report",
                File = new ShareFile(path)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Export Failed", ex.Message, "OK");
        }
    }

    private static string EscapeCsv(string? value)
    {
        var text = value ?? string.Empty;
        return text.Contains(',') || text.Contains('"') || text.Contains('\n')
            ? $"\"{text.Replace("\"", "\"\"")}\""
            : text;
    }
}
