using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages.Account;

public partial class AccCreate1 : ContentPage
{
    private readonly AccountCreationDraft _draft;

    public AccCreate1()
        : this(new AccountCreationDraft())
    {
    }

    public AccCreate1(AccountCreationDraft draft)
    {
        _draft = draft;
        InitializeComponent();
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        _draft.FirstName = FirstNameEntry.Text ?? string.Empty;
        _draft.MiddleName = MiddleNameEntry.Text ?? string.Empty;
        _draft.LastName = LastNameEntry.Text ?? string.Empty;
        _draft.Sex = SexPicker.SelectedItem as string ?? string.Empty;
        var birthdate = BirthdatePicker.Date ?? DateTime.Today;
        _draft.Birthdate = $"{birthdate.Year:D4}-{birthdate.Month:D2}-{birthdate.Day:D2}";

        if (string.IsNullOrWhiteSpace(_draft.FirstName) ||
            string.IsNullOrWhiteSpace(_draft.LastName) ||
            string.IsNullOrWhiteSpace(_draft.Sex))
        {
            await DisplayAlert("Missing Information", "Please complete your first name, last name, and sex.", "OK");
            return;
        }

        _draft.FullName = string.Join(" ", new[] { _draft.FirstName, _draft.MiddleName, _draft.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        await Navigation.PushAsync(new AccCreate2(_draft));
    }
}
