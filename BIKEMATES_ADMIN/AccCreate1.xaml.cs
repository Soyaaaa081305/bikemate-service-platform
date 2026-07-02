using BIKEMATES_ADMIN.Services;

namespace BIKEMATES_ADMIN.Pages.Account;

public partial class AccCreate1 : ContentPage
{
    private readonly AccountCreationDraft _draft;
    private bool _loadingDraft;

    public AccCreate1()
        : this(new AccountCreationDraft())
    {
    }

    public AccCreate1(AccountCreationDraft draft)
    {
        _draft = draft;
        InitializeComponent();
        BirthdatePicker.MaximumDate = DateTime.Today.AddYears(-18);
        if (BirthdatePicker.Date > BirthdatePicker.MaximumDate)
        {
            BirthdatePicker.Date = BirthdatePicker.MaximumDate;
        }

        BirthdatePicker.DateSelected += (_, _) =>
        {
            if (!_loadingDraft)
            {
                _draft.BirthdateSelected = true;
            }
        };

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
        var birthdate = BirthdatePicker.Date ?? DateTime.Today;

        if (string.IsNullOrWhiteSpace(_draft.FirstName) ||
            string.IsNullOrWhiteSpace(_draft.LastName) ||
            string.IsNullOrWhiteSpace(_draft.Sex))
        {
            await DisplayAlert("Missing Information", "Please complete your first name, last name, and sex.", "OK");
            return;
        }

        if (!_draft.BirthdateSelected || string.IsNullOrWhiteSpace(_draft.Birthdate))
        {
            await DisplayAlert("Birthdate Required", "Please choose the owner's birthdate so BikeMate can confirm the account owner is at least 18 years old.", "OK");
            return;
        }

        if (CalculateAge(birthdate, DateTime.Today) < 18)
        {
            await DisplayAlert("Age Requirement", "Shop-admin account owners must be at least 18 years old.", "OK");
            return;
        }

        _draft.FullName = string.Join(" ", new[] { _draft.FirstName, _draft.MiddleName, _draft.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        await Navigation.PushAsync(new AccCreate2(_draft));
    }

    private void LoadDraft()
    {
        _loadingDraft = true;
        FirstNameEntry.Text = _draft.FirstName;
        MiddleNameEntry.Text = _draft.MiddleName;
        LastNameEntry.Text = _draft.LastName;
        if (!string.IsNullOrWhiteSpace(_draft.Sex))
        {
            SexPicker.SelectedItem = _draft.Sex;
        }

        if (DateTime.TryParse(_draft.Birthdate, out var birthdate))
        {
            BirthdatePicker.Date = birthdate.Date > BirthdatePicker.MaximumDate
                ? BirthdatePicker.MaximumDate
                : birthdate.Date;
            _draft.BirthdateSelected = true;
        }
        _loadingDraft = false;
    }

    private void SaveDraft()
    {
        _draft.FirstName = FirstNameEntry.Text ?? string.Empty;
        _draft.MiddleName = MiddleNameEntry.Text ?? string.Empty;
        _draft.LastName = LastNameEntry.Text ?? string.Empty;
        _draft.Sex = SexPicker.SelectedItem as string ?? string.Empty;
        if (_draft.BirthdateSelected)
        {
            var birthdate = BirthdatePicker.Date ?? DateTime.Today;
            _draft.Birthdate = $"{birthdate.Year:D4}-{birthdate.Month:D2}-{birthdate.Day:D2}";
        }
        else
        {
            _draft.Birthdate = string.Empty;
        }
    }

    private static int CalculateAge(DateTime birthdate, DateTime today)
    {
        var age = today.Year - birthdate.Year;
        if (birthdate.Date > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
