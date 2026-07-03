using BIKEMATES_ADMIN.Services;
using System.Text.RegularExpressions;

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
        LoadDraft();
    }

    protected override void OnDisappearing()
    {
        SaveDraft();
        base.OnDisappearing();
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        SaveDraft();
        var password = PasswordEntry.Text ?? string.Empty;
        var confirmPassword = ConfirmPasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_draft.PhoneNumber) ||
            string.IsNullOrWhiteSpace(_draft.Email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            await DisplayAlert("Missing Information", "Please complete all credential fields.", "OK");
            return;
        }

        var normalizedPhone = NormalizePhone(_draft.PhoneNumber);
        if (!Regex.IsMatch(normalizedPhone, @"^09\d{9}$"))
        {
            await DisplayAlert("Invalid Phone Number", "Enter an 11-digit Philippine mobile number that starts with 09.", "OK");
            return;
        }

        if (!Regex.IsMatch(_draft.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            await DisplayAlert("Invalid Email", "Enter a valid email address with @ and a domain such as .com.", "OK");
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

        _draft.PhoneNumber = normalizedPhone;
        _draft.Email = _draft.Email.Trim();
        _draft.Password = password;
        await Navigation.PushAsync(new AccCreate1(_draft));
    }

    private void LoadDraft()
    {
        PhoneEntry.Text = _draft.PhoneNumber;
        EmailEntry.Text = _draft.Email;
        PasswordEntry.Text = _draft.Password;
        ConfirmPasswordEntry.Text = _draft.Password;
    }

    private void SaveDraft()
    {
        _draft.PhoneNumber = PhoneEntry.Text ?? string.Empty;
        _draft.Email = EmailEntry.Text ?? string.Empty;
        _draft.Password = PasswordEntry.Text ?? _draft.Password;
    }

    private static string NormalizePhone(string phoneNumber)
        => new(phoneNumber.Where(char.IsDigit).ToArray());
}
