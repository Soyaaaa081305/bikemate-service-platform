using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages.Account;

public partial class Login : ContentPage
{
    public Login() => InitializeComponent();

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        LoginButton.IsEnabled = false;

        try
        {
            await BikeMateDatabaseService.LoginAsync(
                EmailEntry.Text ?? string.Empty,
                PasswordEntry.Text ?? string.Empty);

            Application.Current!.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Login Failed", ex.Message, "OK");
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private async void OnCreateAccountClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new AccCreate0());

    private async void OnForgotPasswordClicked(object sender, EventArgs e)
    {
        // Dummy: just show alert
        await DisplayAlert("Forgot Password", "A reset link will be sent to your email.", "OK");
    }
}
