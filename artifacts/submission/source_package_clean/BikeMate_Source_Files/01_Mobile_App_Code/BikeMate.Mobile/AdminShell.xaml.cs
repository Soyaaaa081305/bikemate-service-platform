namespace BikeMate;

public partial class AdminShell : Shell
{
    public AdminShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(Views.Admin.AdminMechanicsVerificationPage), typeof(Views.Admin.AdminMechanicsVerificationPage));
        Routing.RegisterRoute(nameof(Views.Admin.AdminShopsVerificationPage), typeof(Views.Admin.AdminShopsVerificationPage));
        Routing.RegisterRoute(nameof(Views.Admin.AdminAuditLogsPage), typeof(Views.Admin.AdminAuditLogsPage));
        Routing.RegisterRoute(nameof(Views.Admin.AdminEmergencyRequestsPage), typeof(Views.Admin.AdminEmergencyRequestsPage));
        Routing.RegisterRoute(nameof(Views.Admin.AdminRevenueReportPage), typeof(Views.Admin.AdminRevenueReportPage));
    }
}
