namespace BikeMate
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(Views.Auth.LoginPage), typeof(Views.Auth.LoginPage));
            Routing.RegisterRoute(nameof(Views.Auth.RegisterPage), typeof(Views.Auth.RegisterPage));
            Routing.RegisterRoute(nameof(Views.Auth.OtpVerificationPage), typeof(Views.Auth.OtpVerificationPage));
            Routing.RegisterRoute(nameof(Views.Auth.PasswordResetPage), typeof(Views.Auth.PasswordResetPage));
            Routing.RegisterRoute(nameof(Views.Auth.GoogleAccountSetupPage), typeof(Views.Auth.GoogleAccountSetupPage));
            Routing.RegisterRoute(nameof(Views.Customer.CustomerProfilePage), typeof(Views.Customer.CustomerProfilePage));
            Routing.RegisterRoute(nameof(Views.Customer.CustomerNotificationsPage), typeof(Views.Customer.CustomerNotificationsPage));
            Routing.RegisterRoute(nameof(Views.Customer.CustomerHelpDeskPage), typeof(Views.Customer.CustomerHelpDeskPage));
            Routing.RegisterRoute(nameof(Views.Customer.CustomerNearbyShopsPage), typeof(Views.Customer.CustomerNearbyShopsPage));
            Routing.RegisterRoute(nameof(Views.Customer.CustomerShopDetailsPage), typeof(Views.Customer.CustomerShopDetailsPage));
            Routing.RegisterRoute(nameof(Views.Customer.CustomerChatPage), typeof(Views.Customer.CustomerChatPage));
            Routing.RegisterRoute(nameof(Views.Customer.BookServicePage), typeof(Views.Customer.BookServicePage));
            Routing.RegisterRoute(nameof(Views.Customer.BookingFillUpPage), typeof(Views.Customer.BookingFillUpPage));
            Routing.RegisterRoute(nameof(Views.Customer.BookingServiceTypePage), typeof(Views.Customer.BookingServiceTypePage));
            Routing.RegisterRoute(nameof(Views.Customer.BookingSchedulePage), typeof(Views.Customer.BookingSchedulePage));
            Routing.RegisterRoute(nameof(Views.Customer.BookingUploadPage), typeof(Views.Customer.BookingUploadPage));
            Routing.RegisterRoute(nameof(Views.Customer.BookingConfirmationPage), typeof(Views.Customer.BookingConfirmationPage));
            Routing.RegisterRoute(nameof(Views.Customer.BookingSearchShopPage), typeof(Views.Customer.BookingSearchShopPage));
            Routing.RegisterRoute(nameof(Views.Customer.StoreSelectionPage), typeof(Views.Customer.StoreSelectionPage));
            Routing.RegisterRoute(nameof(Views.Customer.StoreDetailsPage), typeof(Views.Customer.StoreDetailsPage));
            Routing.RegisterRoute(nameof(Views.Customer.TaskerProfilePage), typeof(Views.Customer.TaskerProfilePage));
            Routing.RegisterRoute(nameof(Views.Customer.BookingTrackMapPage), typeof(Views.Customer.BookingTrackMapPage));
            Routing.RegisterRoute(nameof(Views.Customer.TrackOrderPage), typeof(Views.Customer.TrackOrderPage));
            Routing.RegisterRoute(nameof(Views.Customer.BookingRatingPage), typeof(Views.Customer.BookingRatingPage));
            Routing.RegisterRoute(nameof(Views.Customer.BookingDetailsPage), typeof(Views.Customer.BookingDetailsPage));
            Routing.RegisterRoute(nameof(Views.Customer.TrackMechanicPage), typeof(Views.Customer.TrackMechanicPage));
            Routing.RegisterRoute(nameof(Views.Customer.PaymentCheckoutPage), typeof(Views.Customer.PaymentCheckoutPage));
            Routing.RegisterRoute(nameof(Views.Customer.PaymentReceiptPage), typeof(Views.Customer.PaymentReceiptPage));
            Routing.RegisterRoute(nameof(Views.Customer.PaymentInvoicePage), typeof(Views.Customer.PaymentInvoicePage));
            Routing.RegisterRoute(nameof(Views.Customer.PaymentOptionsPage), typeof(Views.Customer.PaymentOptionsPage));
            Routing.RegisterRoute(nameof(Views.Customer.Emergency.EmergencySosPage), typeof(Views.Customer.Emergency.EmergencySosPage));
            Routing.RegisterRoute(nameof(Views.Customer.Emergency.CallingEmergencyPage), typeof(Views.Customer.Emergency.CallingEmergencyPage));
            Routing.RegisterRoute(nameof(Views.Customer.Emergency.EmergencyLiveCallPage), typeof(Views.Customer.Emergency.EmergencyLiveCallPage));
            Routing.RegisterRoute(nameof(Views.Customer.Emergency.EmergencyLocationPickerPage), typeof(Views.Customer.Emergency.EmergencyLocationPickerPage));
            Routing.RegisterRoute(nameof(Views.Customer.Emergency.ActiveEmergencyTrackingPage), typeof(Views.Customer.Emergency.ActiveEmergencyTrackingPage));
            Routing.RegisterRoute(nameof(Views.Mechanic.MechanicJobDetailsPage), typeof(Views.Mechanic.MechanicJobDetailsPage));
            Routing.RegisterRoute(nameof(Views.Mechanic.MechanicMapPage), typeof(Views.Mechanic.MechanicMapPage));
            Routing.RegisterRoute(nameof(Views.Mechanic.MechanicEmergencyRequestsPage), typeof(Views.Mechanic.MechanicEmergencyRequestsPage));
            Routing.RegisterRoute(nameof(Views.Mechanic.MechanicChatPage), typeof(Views.Mechanic.MechanicChatPage));
            Routing.RegisterRoute(nameof(Views.Mechanic.MechanicEditProfilePage), typeof(Views.Mechanic.MechanicEditProfilePage));
            Routing.RegisterRoute(nameof(Views.Mechanic.MechanicEarningsPage), typeof(Views.Mechanic.MechanicEarningsPage));
            Routing.RegisterRoute(nameof(Views.Mechanic.MechanicRatingsPage), typeof(Views.Mechanic.MechanicRatingsPage));
            Routing.RegisterRoute(nameof(Views.Mechanic.MechanicHistoryPage), typeof(Views.Mechanic.MechanicHistoryPage));
            Routing.RegisterRoute(nameof(Views.Mechanic.MechanicHistoryDetailsPage), typeof(Views.Mechanic.MechanicHistoryDetailsPage));
            Routing.RegisterRoute(nameof(Views.Admin.AdminUsersPage), typeof(Views.Admin.AdminUsersPage));
            Routing.RegisterRoute(nameof(Views.Admin.AdminMechanicsVerificationPage), typeof(Views.Admin.AdminMechanicsVerificationPage));
            Routing.RegisterRoute(nameof(Views.Admin.AdminShopsVerificationPage), typeof(Views.Admin.AdminShopsVerificationPage));
            Routing.RegisterRoute(nameof(Views.Admin.AdminPaymentsPage), typeof(Views.Admin.AdminPaymentsPage));
            Routing.RegisterRoute(nameof(Views.Admin.AdminRevenueReportPage), typeof(Views.Admin.AdminRevenueReportPage));
        }
    }
}
