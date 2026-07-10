using BIKEMATES_ADMIN.Services;
using System.Text.RegularExpressions;

namespace BIKEMATES_ADMIN.Pages.Account;

public partial class AccCreate0 : ContentPage
{
    private const string PhonePrefix = "63";
    private const int PhoneLocalDigitCount = 10;

    private readonly AccountCreationDraft _draft;
    private bool _formattingPhone;

    public AccCreate0()
        : this(new AccountCreationDraft())
    {
    }

    public AccCreate0(AccountCreationDraft draft)
    {
        _draft = draft;
        InitializeComponent();
        PhoneEntry.TextChanged += OnPhoneTextChanged;
        LoadDraft();
    }

    protected override void OnDisappearing()
    {
        SaveDraft();
        base.OnDisappearing();
    }

    private async void OnContinueClicked(object? sender, EventArgs e)
    {
        SaveDraft();
        var password = PasswordEntry.Text ?? string.Empty;
        var confirmPassword = ConfirmPasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_draft.PhoneNumber) ||
            string.IsNullOrWhiteSpace(_draft.Email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            await DisplayAlertAsync("Missing Information", "Please complete all credential fields.", "OK");
            return;
        }

        var normalizedPhone = NormalizePhoneForEntry(_draft.PhoneNumber);
        if (!Regex.IsMatch(normalizedPhone, @"^639\d{9}$"))
        {
            await DisplayAlertAsync("Invalid Phone Number", "Enter a Philippine mobile number in 63 format: 639XXXXXXXXX.", "OK");
            return;
        }

        if (!Regex.IsMatch(_draft.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            await DisplayAlertAsync("Invalid Email", "Enter a valid email address with @ and a domain such as .com.", "OK");
            return;
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            await DisplayAlertAsync("Password Mismatch", "Password and confirm password must match.", "OK");
            return;
        }

        if (password.Length <= 8)
        {
            await DisplayAlertAsync("Password Too Short", "Password must be more than 8 characters.", "OK");
            return;
        }

        _draft.PhoneNumber = normalizedPhone;
        _draft.Email = _draft.Email.Trim();
        _draft.Password = password;
        await Navigation.PushAsync(new AccCreate1(_draft));
    }

    private void LoadDraft()
    {
        PhoneEntry.Text = NormalizePhoneForEntry(_draft.PhoneNumber);
        EmailEntry.Text = _draft.Email;
        PasswordEntry.Text = _draft.Password;
        ConfirmPasswordEntry.Text = _draft.Password;
    }

    private void SaveDraft()
    {
        _draft.PhoneNumber = NormalizePhoneForEntry(PhoneEntry.Text ?? string.Empty);
        _draft.Email = EmailEntry.Text ?? string.Empty;
        _draft.Password = PasswordEntry.Text ?? _draft.Password;
    }

    private void OnPhoneTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_formattingPhone)
        {
            return;
        }

        var formatted = NormalizePhoneForEntry(e.NewTextValue ?? string.Empty);
        if (string.Equals(e.NewTextValue, formatted, StringComparison.Ordinal))
        {
            return;
        }

        _formattingPhone = true;
        PhoneEntry.Text = formatted;
        PhoneEntry.CursorPosition = formatted.Length;
        _formattingPhone = false;
    }

    private static string NormalizePhoneForEntry(string phoneNumber)
    {
        var digits = NormalizePhone(phoneNumber);
        var localDigits = digits switch
        {
            "" => string.Empty,
            "6" => string.Empty,
            "3" => string.Empty,
            _ when digits.StartsWith(PhonePrefix, StringComparison.Ordinal) => digits[PhonePrefix.Length..],
            _ when digits.StartsWith("09", StringComparison.Ordinal) => digits[1..],
            _ when digits.StartsWith("9", StringComparison.Ordinal) => digits,
            _ when digits.StartsWith("0", StringComparison.Ordinal) => digits[1..],
            _ => digits
        };

        return PhonePrefix + new string(localDigits.Take(PhoneLocalDigitCount).ToArray());
    }

    private static string NormalizePhone(string phoneNumber)
        => new(phoneNumber.Where(char.IsDigit).ToArray());
}
