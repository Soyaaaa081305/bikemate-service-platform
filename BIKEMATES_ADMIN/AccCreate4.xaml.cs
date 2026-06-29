using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages.Account;

public partial class AccCreate4 : ContentPage
{
    private readonly AccountCreationDraft _draft;

    public AccCreate4()
        : this(new AccountCreationDraft())
    {
    }

    public AccCreate4(AccountCreationDraft draft)
    {
        _draft = draft;
        InitializeComponent();
    }

    private async void OnCreateAccountClicked(object sender, EventArgs e)
    {
        _draft.AccessCode = AccessCodeEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_draft.AccessCode))
        {
            await DisplayAlert("Missing Access Code", "Please enter the account creation code.", "OK");
            return;
        }

        CreateAccountButton.IsEnabled = false;

        try
        {
            await BikeMateDatabaseService.CreateShopAdminAccountAsync(_draft);
            await Navigation.PushAsync(new AccCreate5());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Account Creation Failed", ex.Message, "OK");
        }
        finally
        {
            CreateAccountButton.IsEnabled = true;
        }
    }
}
