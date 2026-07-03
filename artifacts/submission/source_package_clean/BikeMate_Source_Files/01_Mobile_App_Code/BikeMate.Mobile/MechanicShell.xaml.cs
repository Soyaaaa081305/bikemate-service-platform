namespace BikeMate;

public partial class MechanicShell : Shell
{
    public MechanicShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(Views.Mechanic.MechanicJobDetailsPage), typeof(Views.Mechanic.MechanicJobDetailsPage));
        Routing.RegisterRoute(nameof(Views.Mechanic.MechanicMapPage), typeof(Views.Mechanic.MechanicMapPage));
        Routing.RegisterRoute(nameof(Views.Mechanic.MechanicEmergencyRequestsPage), typeof(Views.Mechanic.MechanicEmergencyRequestsPage));
        Routing.RegisterRoute(nameof(Views.Mechanic.MechanicChatPage), typeof(Views.Mechanic.MechanicChatPage));
        Routing.RegisterRoute(nameof(Views.Mechanic.MechanicEditProfilePage), typeof(Views.Mechanic.MechanicEditProfilePage));
        Routing.RegisterRoute(nameof(Views.Mechanic.MechanicEarningsPage), typeof(Views.Mechanic.MechanicEarningsPage));
        Routing.RegisterRoute(nameof(Views.Mechanic.MechanicRatingsPage), typeof(Views.Mechanic.MechanicRatingsPage));
        Routing.RegisterRoute(nameof(Views.Mechanic.MechanicHistoryPage), typeof(Views.Mechanic.MechanicHistoryPage));
        Routing.RegisterRoute(nameof(Views.Mechanic.MechanicHistoryDetailsPage), typeof(Views.Mechanic.MechanicHistoryDetailsPage));
    }
}
