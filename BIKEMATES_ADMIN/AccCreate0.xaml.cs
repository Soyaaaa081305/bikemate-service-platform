using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages.Account;

public partial class AccCreate0 : ContentPage
{
    private readonly AccountCreationDraft _draft;

    public AccCreate0()
        : this(new AccountCreationDraft())
    {
    }

    public AccCreate0(AccountCreationDraft draft)
    {
        _draft = draft;
        InitializeComponent();
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        var password = PasswordEntry.Text ?? string.Empty;
        var confirmPassword = ConfirmPasswordEntry.Text ?? string.Empty;

        _draft.PhoneNumber = PhoneEntry.Text ?? string.Empty;
        _draft.Email = EmailEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_draft.PhoneNumber) ||
            string.IsNullOrWhiteSpace(_draft.Email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            await DisplayAlert("Missing Information", "Please complete all credential fields.", "OK");
            return;
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            await DisplayAlert("Password Mismatch", "Password and confirm password must match.", "OK");
            return;
        }

        if (password.Length <= 8)
        {
            await DisplayAlert("Password Too Short", "Password must be more than 8 characters.", "OK");
            return;
        }

        _draft.Password = password;
        await Navigation.PushAsync(new AccCreate1(_draft));
    }
}
