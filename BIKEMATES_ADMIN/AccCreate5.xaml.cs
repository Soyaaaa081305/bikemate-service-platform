namespace BIKEMATES_ADMIN.Pages.Account;

public partial class AccCreate5 : ContentPage
{
    public AccCreate5()
    {
        InitializeComponent();
    }

    private void OnProceedToLoginClicked(object sender, EventArgs e)
    {
        Application.Current!.MainPage = new NavigationPage(new Login());
    }
}
