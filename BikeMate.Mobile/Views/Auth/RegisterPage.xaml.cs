using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using BikeMate.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Storage;

namespace BikeMate.Views.Auth;

internal static class CustomerRegistrationDraft
{
    public static int Step { get; set; } = 1;
    public static bool AcceptedTerms { get; set; }
    public static string PhoneNumber { get; set; } = string.Empty;
    public static string Email { get; set; } = string.Empty;
    public static string Password { get; set; } = string.Empty;
    public static string FirstName { get; set; } = string.Empty;
    public static string MiddleName { get; set; } = string.Empty;
    public static string LastName { get; set; } = string.Empty;
    public static string Sex { get; set; } = string.Empty;
    public static DateTime Birthdate { get; set; } = DateTime.Today.AddYears(-18);
    public static bool BirthdateSelected { get; set; }
    public static string RegionCode { get; set; } = string.Empty;
    public static string LocalityCode { get; set; } = string.Empty;
    public static string BarangayName { get; set; } = string.Empty;
    public static string SelectedProvince { get; set; } = string.Empty;
    public static string ZipCode { get; set; } = string.Empty;
    public static string Address { get; set; } = string.Empty;
    public static string MotorcycleBrand { get; set; } = string.Empty;
    public static string MotorcycleModel { get; set; } = string.Empty;
    public static string MotorcycleYear { get; set; } = string.Empty;
    public static string MotorcyclePlate { get; set; } = string.Empty;
    public static string MotorcycleEngineType { get; set; } = string.Empty;
    public static string MotorcycleColor { get; set; } = string.Empty;
    public static FileResult? ValidIdFile { get; set; }
    public static string? ValidIdPreviewPath { get; set; }

    public static void Reset()
    {
        Step = 1;
        AcceptedTerms = false;
        PhoneNumber = string.Empty;
        Email = string.Empty;
        Password = string.Empty;
        FirstName = string.Empty;
        MiddleName = string.Empty;
        LastName = string.Empty;
        Sex = string.Empty;
        Birthdate = DateTime.Today.AddYears(-18);
        BirthdateSelected = false;
        RegionCode = string.Empty;
        LocalityCode = string.Empty;
        BarangayName = string.Empty;
        SelectedProvince = string.Empty;
        ZipCode = string.Empty;
        Address = string.Empty;
        MotorcycleBrand = string.Empty;
        MotorcycleModel = string.Empty;
        MotorcycleYear = string.Empty;
        MotorcyclePlate = string.Empty;
        MotorcycleEngineType = string.Empty;
        MotorcycleColor = string.Empty;
        ValidIdFile = null;
        ValidIdPreviewPath = null;
    }
}

public partial class RegisterPage : ContentPage
{
    private const string Orange = "#FF6B00";
    private const string Dark = "#222222";
    private const string Muted = "#777777";
    private const string BorderColor = "#DCDCDC";

    private int _step = 1;
    private bool _acceptedTerms;
    private bool _birthdateSelected;
    private bool _restoringDraft;
    private FileResult? _validIdFile;
    private string? _validIdPreviewPath;
    private LocalIdCardScanResult? _localIdScanResult;

    private bool _formattingPhoneNumber;

    private readonly Entry _phoneEntry = Entry("9XX XXX XXXX", Keyboard.Telephone);
    private readonly Entry _emailEntry = Entry("Enter email", Keyboard.Email);
    private readonly Entry _passwordEntry = Entry("Enter password", null, true);
    private readonly Entry _confirmPasswordEntry = Entry("Confirm password", null, true);
    private readonly Entry _firstNameEntry = Entry("Enter first name");
    private readonly Entry _middleNameEntry = Entry("Enter middle name");
    private readonly Entry _lastNameEntry = Entry("Enter last name");
    private readonly Picker _sexPicker = new() { Title = "Select sex", TextColor = Color.FromArgb(Dark), TitleColor = Color.FromArgb(Muted) };
    private readonly DatePicker _birthdayPicker = new() { TextColor = Color.FromArgb(Dark), MaximumDate = DateTime.Today.AddYears(-18) };
    private readonly Picker _regionPicker = Picker("Select region");
    private readonly Picker _localityPicker = Picker("Select city or municipality");
    private readonly Picker _barangayPicker = Picker("Select barangay");
    private readonly Entry _zipCodeEntry = Entry("Zip code", Keyboard.Numeric);
    private readonly Entry _motorcycleBrandEntry = Entry("Brand");
    private readonly Entry _motorcycleModelEntry = Entry("Model");
    private readonly Entry _motorcycleYearEntry = Entry("Year model", Keyboard.Numeric);
    private readonly Entry _motorcyclePlateEntry = Entry("Plate number");
    private readonly Entry _motorcycleEngineTypeEntry = Entry("Engine type");
    private readonly Entry _motorcycleColorEntry = Entry("Color");
    private readonly Editor _addressEditor = new()
    {
        Placeholder = "House number, street, landmark",
        PlaceholderColor = Color.FromArgb(Muted),
        TextColor = Color.FromArgb(Dark),
        HeightRequest = 72,
        BackgroundColor = Colors.Transparent,
        FontSize = 13
    };
    private readonly ActivityIndicator _busy = new() { Color = Color.FromArgb(Orange), IsVisible = false, IsRunning = false };
    private IReadOnlyList<PhilippineRegionDto> _regions = [];
    private IReadOnlyList<PhilippineLocalityDto> _localities = [];
    private IReadOnlyList<PhilippineBarangayDto> _barangays = [];
    private bool _loadingLocations;
    private bool _updatingLocationPickers;
    private string? _selectedProvince;
    private string? _locationMessage;

    public RegisterPage()
    {
        InitializeComponent();
        _phoneEntry.MaxLength = 14;
        _zipCodeEntry.MaxLength = 4;
        _motorcycleYearEntry.MaxLength = 4;
        _motorcyclePlateEntry.MaxLength = 15;
        _phoneEntry.TextChanged += PhoneEntry_TextChanged;
        _sexPicker.Items.Add("Female");
        _sexPicker.Items.Add("Male");
        _sexPicker.Items.Add("Prefer not to say");
        _birthdayPicker.DateSelected += (_, _) =>
        {
            if (_restoringDraft)
            {
                return;
            }

            _birthdateSelected = true;
            CustomerRegistrationDraft.BirthdateSelected = true;
        };
        _regionPicker.SelectedIndexChanged += async (_, _) => await RegionChangedAsync();
        _localityPicker.SelectedIndexChanged += async (_, _) => await LocalityChangedAsync();
        LoadDraft();
        RenderStep();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = GoBackAsync();
        return true;
    }

    protected override void OnDisappearing()
    {
        SaveDraft();
        base.OnDisappearing();
    }

    private void PhoneEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_formattingPhoneNumber)
        {
            return;
        }

        var digits = Regex.Replace(e.NewTextValue ?? string.Empty, @"\D", "");
        if (digits.StartsWith("63", StringComparison.Ordinal) && digits.Length > 10)
        {
            digits = digits[2..];
        }
        else if (digits.StartsWith("0", StringComparison.Ordinal) && digits.Length > 10)
        {
            digits = digits[1..];
        }

        if (digits.Length > 10)
        {
            digits = digits[..10];
        }

        if (!string.Equals(digits, e.NewTextValue, StringComparison.Ordinal))
        {
            _formattingPhoneNumber = true;
            _phoneEntry.Text = digits;
            _formattingPhoneNumber = false;
        }
    }

    private void RenderStep()
    {
        DetachReusableControls();
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(18, 18, 18, 28),
            Spacing = 14,
            BackgroundColor = Color.FromArgb("#F6F6F6")
        };

        body.Add(Header("Create your account"));
        body.Add(StepIndicator(_step));
        var section = new VerticalStackLayout { Spacing = 12 };
        section.Add(Label(StepTitle(_step), 18, Dark, TextAlignment.Start, FontAttributes.Bold));
        section.Add(Label(StepDescription(_step), 13, Muted, TextAlignment.Start));

        switch (_step)
        {
            case 1:
                section.Add(PhoneNumberField());
                section.Add(LabeledField("Email address *", _emailEntry));
                section.Add(LabeledField("Password *", _passwordEntry));
                section.Add(LabeledField("Confirm password *", _confirmPasswordEntry));
                section.Add(Footnote("Use at least 9 characters. A mix of letters, numbers, and symbols is recommended."));
                section.Add(PrimaryButton("Continue", () => ContinueFromStep1Async()));
                break;
            case 2:
                section.Add(LabeledField("First name *", _firstNameEntry));
                section.Add(LabeledField("Middle name", _middleNameEntry));
                section.Add(LabeledField("Last name *", _lastNameEntry));
                section.Add(LabeledField("Sex *", _sexPicker));
                section.Add(LabeledField("Birthdate *", _birthdayPicker));
                section.Add(Footnote());
                section.Add(PrimaryButton("Continue", () => ContinueFromStep2Async()));
                break;
            case 3:
                section.Add(AddressGrid());
                section.Add(LabeledField("Complete address *", _addressEditor));
                section.Add(UploadRow());
                section.Add(IdPreview());
                section.Add(Footnote("Use a clear, readable photo of a valid government-issued ID."));
                section.Add(PrimaryButton("Continue", () => ContinueFromStep3Async()));
                break;
            case 4:
                section.Add(MotorcycleView());
                section.Add(Footnote("Plate number is required so shops can identify the bike or motorcycle during service."));
                section.Add(PrimaryButton("Review terms", () => ContinueFromStep4Async()));
                break;
            case 5:
                section.Add(TermsView());
                break;
        }

        body.Add(Card(section, new Thickness(16), 8));
        body.Add(_busy);
        AppVisualPolish.Apply(body);
        Content = new ScrollView { Content = body };
    }

    private View Header(string title)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };

        grid.Add(new Button
        {
            Text = "<",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb(Dark),
            WidthRequest = 38,
            HeightRequest = 38,
            FontSize = 18,
            Padding = new Thickness(0),
            Command = new Command(async () => await GoBackAsync())
        }, 0, 0);

        if (!string.IsNullOrWhiteSpace(title))
        {
            var copy = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            copy.Add(Label(title, 14, Dark, TextAlignment.Start, FontAttributes.Bold));
            copy.Add(Label("Secure customer registration", 11, Muted, TextAlignment.Start));
            grid.Add(copy, 1, 0);
        }

        return grid;
    }

    private static View StepIndicator(int step)
    {
        var stack = new VerticalStackLayout { Spacing = 7 };
        stack.Add(Label($"Step {step} of 5", 11, Muted, TextAlignment.Start, FontAttributes.Bold));
        var progress = new Grid { ColumnSpacing = 5 };
        for (var index = 0; index < 5; index++)
        {
            progress.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            progress.Add(new BoxView
            {
                HeightRequest = 4,
                CornerRadius = 2,
                Color = index < step ? Color.FromArgb(Orange) : Color.FromArgb("#DCDCDC")
            }, index, 0);
        }
        stack.Add(progress);
        return stack;
    }

    private View MotorcycleView()
    {
        Detach(_motorcycleBrandEntry);
        Detach(_motorcycleModelEntry);
        Detach(_motorcycleYearEntry);
        Detach(_motorcyclePlateEntry);
        Detach(_motorcycleEngineTypeEntry);
        Detach(_motorcycleColorEntry);

        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Add(LabeledField("Brand *", _motorcycleBrandEntry));
        stack.Add(LabeledField("Model *", _motorcycleModelEntry));
        stack.Add(LabeledField("Year model", _motorcycleYearEntry));
        stack.Add(LabeledField("Plate number *", _motorcyclePlateEntry));
        stack.Add(LabeledField("Engine type", _motorcycleEngineTypeEntry));
        stack.Add(LabeledField("Color", _motorcycleColorEntry));
        return stack;
    }

    private View AddressGrid()
    {
        Detach(_regionPicker);
        Detach(_localityPicker);
        Detach(_barangayPicker);
        Detach(_zipCodeEntry);

        var stack = new VerticalStackLayout { Spacing = 10 };
        if (!string.IsNullOrWhiteSpace(_locationMessage))
        {
            stack.Add(Footnote(_locationMessage));
        }

        stack.Add(LabeledField("Region *", _regionPicker));
        stack.Add(LabeledField("City / Municipality *", _localityPicker));
        if (!string.IsNullOrWhiteSpace(_selectedProvince))
        {
            stack.Add(Footnote($"Province: {_selectedProvince}"));
        }

        stack.Add(LabeledField("Barangay *", _barangayPicker));
        stack.Add(LabeledField("Zip code *", _zipCodeEntry));
        return stack;
    }

    private async Task EnsureRegionsLoadedAsync()
    {
        if (_regions.Count > 0 || _loadingLocations)
        {
            return;
        }

        _loadingLocations = true;
        _locationMessage = "Loading Philippine regions...";
        RenderStep();
        try
        {
            _regions = await CustomerApiClient.GetPhilippineRegionsAsync();
            SetPickerItems(_regionPicker, _regions.Select(region => region.Name));
            _locationMessage = _regions.Count == 0
                ? "No regions were returned. Please try again."
                : null;
        }
        catch (Exception ex)
        {
            _locationMessage = $"BikeMate could not load Philippine locations. {ex.Message}";
        }
        finally
        {
            _loadingLocations = false;
            RenderStep();
        }

        await RestoreLocationDraftAsync();
    }

    private async Task RegionChangedAsync()
    {
        if (_updatingLocationPickers)
        {
            return;
        }

        var region = SelectedRegion();
        if (region is null)
        {
            return;
        }

        _loadingLocations = true;
        _locationMessage = "Loading cities and municipalities...";
        _localities = [];
        _barangays = [];
        _selectedProvince = null;
        ResetPicker(_localityPicker, "Select city or municipality");
        ResetPicker(_barangayPicker, "Select barangay");
        RenderStep();

        try
        {
            _localities = await CustomerApiClient.GetPhilippineLocalitiesAsync(region.Code);
            SetPickerItems(_localityPicker, _localities.Select(LocalityDisplayName));
            _locationMessage = _localities.Count == 0
                ? "No cities or municipalities were returned for this region."
                : null;
            SaveDraft();
        }
        catch (Exception ex)
        {
            _locationMessage = $"BikeMate could not load cities or municipalities. {ex.Message}";
        }
        finally
        {
            _loadingLocations = false;
            RenderStep();
        }
    }

    private async Task LocalityChangedAsync()
    {
        if (_updatingLocationPickers)
        {
            return;
        }

        var locality = SelectedLocality();
        if (locality is null)
        {
            return;
        }

        _selectedProvince = locality.Province;
        _loadingLocations = true;
        _locationMessage = "Loading barangays...";
        _barangays = [];
        ResetPicker(_barangayPicker, "Select barangay");
        RenderStep();

        try
        {
            _barangays = await CustomerApiClient.GetPhilippineBarangaysAsync(locality.Code);
            SetPickerItems(_barangayPicker, _barangays.Select(barangay => barangay.Name));
            _locationMessage = _barangays.Count == 0
                ? "No barangays were returned for this city or municipality."
                : null;
            SaveDraft();
        }
        catch (Exception ex)
        {
            _locationMessage = $"BikeMate could not load barangays. {ex.Message}";
        }
        finally
        {
            _loadingLocations = false;
            RenderStep();
        }
    }

    private PhilippineRegionDto? SelectedRegion()
    {
        return _regionPicker.SelectedIndex >= 0 && _regionPicker.SelectedIndex < _regions.Count
            ? _regions[_regionPicker.SelectedIndex]
            : null;
    }

    private PhilippineLocalityDto? SelectedLocality()
    {
        return _localityPicker.SelectedIndex >= 0 && _localityPicker.SelectedIndex < _localities.Count
            ? _localities[_localityPicker.SelectedIndex]
            : null;
    }

    private PhilippineBarangayDto? SelectedBarangay()
    {
        return _barangayPicker.SelectedIndex >= 0 && _barangayPicker.SelectedIndex < _barangays.Count
            ? _barangays[_barangayPicker.SelectedIndex]
            : null;
    }

    private static string LocalityDisplayName(PhilippineLocalityDto locality)
    {
        return string.IsNullOrWhiteSpace(locality.Province)
            ? locality.Name
            : $"{locality.Name}, {locality.Province}";
    }

    private void SetPickerItems(Picker picker, IEnumerable<string> items)
    {
        _updatingLocationPickers = true;
        picker.Items.Clear();
        foreach (var item in items)
        {
            picker.Items.Add(item);
        }
        picker.SelectedIndex = -1;
        _updatingLocationPickers = false;
    }

    private void ResetPicker(Picker picker, string title)
    {
        _updatingLocationPickers = true;
        picker.Items.Clear();
        picker.Title = title;
        picker.SelectedIndex = -1;
        _updatingLocationPickers = false;
    }

    private View UploadRow()
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 8,
            Padding = new Thickness(0, 4)
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            Padding = new Thickness(0, 4)
        };

        grid.Add(Label(
            _validIdFile is null ? "Valid government ID *" : _validIdFile.FileName,
            11,
            Dark,
            TextAlignment.Start,
            FontAttributes.Bold), 0, 0);
        grid.Add(new Button
        {
            Text = _validIdFile is null ? "Scan ID" : "Rescan",
            BackgroundColor = Colors.White,
            TextColor = Color.FromArgb(Orange),
            BorderColor = Color.FromArgb(Orange),
            BorderWidth = 1,
            HeightRequest = 44,
            CornerRadius = 8,
            Padding = new Thickness(14, 0),
            Command = new Command(async () => await UploadValidIdAsync())
        }, 1, 0);

        stack.Add(grid);
        stack.Add(Label(
            _localIdScanResult is null
                ? "ID uploads require a fresh camera scan. Review the image before continuing."
                : $"Scan result: {FormatReadability(_localIdScanResult.ReadabilityStatus)}. This ID will be submitted for admin review.",
            11,
            Muted,
            TextAlignment.Start,
            FontAttributes.None));
        return stack;
    }

    private View IdPreview()
    {
        var grid = new Grid
        {
            HeightRequest = 126,
            BackgroundColor = Color.FromArgb("#FAFAFA")
        };

        if (!string.IsNullOrWhiteSpace(_validIdPreviewPath) && File.Exists(_validIdPreviewPath))
        {
            grid.Add(new Image
            {
                Source = ImageSource.FromFile(_validIdPreviewPath),
                Aspect = Aspect.AspectFit
            });
        }
        else
        {
            grid.Add(new Label
            {
                Text = "Your selected ID preview will appear here.",
                TextColor = Color.FromArgb(Muted),
                FontSize = AppTypography.CaptionSize,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                Padding = new Thickness(20)
            });
        }

        return Card(grid, new Thickness(0), 8);
    }

    private View TermsView()
    {
        var root = new VerticalStackLayout { Spacing = 14 };
        root.Add(TermsSection(
            "Using BikeMate",
            "BikeMate connects customers with repair shops, mechanics, emergency roadside support, booking tools, live location, chat, and secure payment. You agree to provide accurate booking, contact, bike, address, and location information so the assigned shop or mechanic can serve you safely.",
            Dark));
        root.Add(TermsSection(
            "Account responsibility",
            "Keep your login details private, use your real email and Philippine mobile number, and do not create duplicate, misleading, abusive, or fraudulent accounts. BikeMate may suspend or remove accounts that misuse bookings, payments, emergency tools, reviews, or chat.",
            Dark));
        root.Add(TermsSection(
            "Payments and repairs",
            "Prices, availability, pickup, arrival time, parts, and repair outcomes may vary by shop, service, location, and inspection. PayMongo processes secure payments; BikeMate does not store card or wallet credentials. Refunds, cancellations, and disputes may require review of booking records and payment status.",
            Dark));
        root.Add(TermsSection(
            "Privacy and safety",
            "BikeMate collects and uses account details, uploaded IDs, contact details, GPS/location data, booking media, chat records, and payment references only to operate, protect, and improve the service. You may request correction or deletion where allowed by law. Emergency and location features are support tools and do not replace public emergency services.",
            Dark));

        var check = new CheckBox { IsChecked = _acceptedTerms, Color = Color.FromArgb(Orange) };
        check.CheckedChanged += (_, e) => _acceptedTerms = e.Value;
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        row.Add(check, 0, 0);
        row.Add(Label("I have read and agree to BikeMate's Terms and Conditions and Privacy Policy.", 11, Muted, TextAlignment.Start), 1, 0);
        root.Add(row);
        root.Add(PrimaryButton("Create my account", RegisterAsync));
        return root;
    }

    private async Task ContinueFromStep1Async()
    {
        SaveDraft();
        if (string.IsNullOrWhiteSpace(_phoneEntry.Text) ||
            string.IsNullOrWhiteSpace(_emailEntry.Text) ||
            string.IsNullOrWhiteSpace(_passwordEntry.Text) ||
            string.IsNullOrWhiteSpace(_confirmPasswordEntry.Text))
        {
            await DisplayAlertAsync("Missing details", "Please complete all required fields.", "OK");
            return;
        }

        string normalizedEmail;
        string normalizedPhone;
        try
        {
            normalizedEmail = NormalizeEmail(_emailEntry.Text);
            normalizedPhone = NormalizePhilippineMobile(_phoneEntry.Text);
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlertAsync("Check your details", ex.Message, "OK");
            return;
        }

        if ((_passwordEntry.Text ?? string.Empty).Length <= 8)
        {
            await DisplayAlertAsync("Weak password", "Password must be more than 8 characters.", "OK");
            return;
        }

        if (!string.Equals(_passwordEntry.Text, _confirmPasswordEntry.Text, StringComparison.Ordinal))
        {
            await DisplayAlertAsync("Password mismatch", "Confirm password must match password.", "OK");
            return;
        }

        if (!await CheckAvailabilityAsync(normalizedEmail, normalizedPhone))
        {
            return;
        }

        _emailEntry.Text = normalizedEmail;
        _phoneEntry.Text = normalizedPhone;
        _step = 2;
        SaveDraft();
        RenderStep();
    }

    private async Task<bool> CheckAvailabilityAsync(string email, string phone)
    {
        try
        {
            SetBusy(true);
            using var http = ApiConfig.CreateHttpClient();
            var route = $"auth/availability?email={Uri.EscapeDataString(email)}&phone={Uri.EscapeDataString(phone)}";
            using var response = await http.GetAsync(route);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return true;
            }

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("Validation failed", await ReadErrorAsync(response), "OK");
                return false;
            }

            var availability = await response.Content.ReadFromJsonAsync<AuthAvailabilityDto>();
            if (availability is null)
            {
                await DisplayAlertAsync("Validation failed", "BikeMate could not check account availability. Please try again.", "OK");
                return false;
            }

            if (!availability.EmailAvailable)
            {
                await DisplayAlertAsync("Email already used", "This email address is already registered. Use a different email or sign in instead.", "OK");
                return false;
            }

            if (!availability.PhoneAvailable)
            {
                await DisplayAlertAsync("Phone already used", "This Philippine mobile number is already registered. Use a different number or sign in instead.", "OK");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("API offline", $"BikeMate could not check duplicate email or phone yet. Start the API, then try again. {ex.Message}", "OK");
            return false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ContinueFromStep2Async()
    {
        SaveDraft();
        if (string.IsNullOrWhiteSpace(_firstNameEntry.Text) ||
            string.IsNullOrWhiteSpace(_lastNameEntry.Text) ||
            _sexPicker.SelectedIndex < 0)
        {
            await DisplayAlertAsync("Missing details", "Please complete your basic information.", "OK");
            return;
        }

        if (!_birthdateSelected)
        {
            await DisplayAlertAsync("Birthdate required", "Please choose your birthdate so BikeMate can confirm you are at least 18 years old.", "OK");
            return;
        }

        if (CalculateAge(_birthdayPicker.Date ?? DateTime.Today, DateTime.Today) < 18)
        {
            await DisplayAlertAsync("Age requirement", "You must be at least 18 years old to create a BikeMate account.", "OK");
            return;
        }

        _step = 3;
        SaveDraft();
        RenderStep();
        await EnsureRegionsLoadedAsync();
    }

    private async Task ContinueFromStep3Async()
    {
        SaveDraft();
        if (SelectedRegion() is null ||
            SelectedLocality() is null ||
            SelectedBarangay() is null ||
            string.IsNullOrWhiteSpace(_zipCodeEntry.Text) ||
            string.IsNullOrWhiteSpace(_addressEditor.Text) ||
            _validIdFile is null)
        {
            await DisplayAlertAsync("Missing details", "Please complete your Philippine address, zip code, and valid ID.", "OK");
            return;
        }

        if (!Regex.IsMatch(_zipCodeEntry.Text.Trim(), @"^\d{4}$"))
        {
            await DisplayAlertAsync("Invalid zip code", "Enter a 4-digit Philippine zip code.", "OK");
            return;
        }

        _step = 4;
        SaveDraft();
        RenderStep();
    }

    private async Task ContinueFromStep4Async()
    {
        SaveDraft();
        var brand = Clean(_motorcycleBrandEntry.Text);
        var model = Clean(_motorcycleModelEntry.Text);
        var plate = Clean(_motorcyclePlateEntry.Text);

        if (brand is null || model is null || plate is null)
        {
            await DisplayAlertAsync(
                "Vehicle details needed",
                "Please add your bike or motorcycle brand, model, and plate number.",
                "OK");
            return;
        }

        if (!string.IsNullOrWhiteSpace(_motorcycleYearEntry.Text) &&
            (!int.TryParse(_motorcycleYearEntry.Text.Trim(), out var year) || year < 1950 || year > DateTime.Today.Year + 1))
        {
            await DisplayAlertAsync("Invalid year", "Enter a valid year model.", "OK");
            return;
        }

        _motorcyclePlateEntry.Text = plate.ToUpperInvariant();
        _step = 5;
        SaveDraft();
        RenderStep();
    }

    private async Task UploadValidIdAsync()
    {
        try
        {
            var scan = await new LocalIdCardScannerService().ScanAsync("Scan valid ID");
            if (!scan.IsSuccessful)
            {
                if (scan.WasCancelled)
                {
                    return;
                }

                await DisplayAlertAsync("ID scan", scan.ErrorMessage ?? "The ID scan could not be completed.", "OK");
                return;
            }

            var processedImagePath = scan.LocalProcessedImagePath;
            if (string.IsNullOrWhiteSpace(processedImagePath))
            {
                await DisplayAlertAsync("ID scan", "BikeMate could not prepare the scanned ID image. Please scan again.", "OK");
                return;
            }

            _validIdFile = new FileResult(processedImagePath)
            {
                FileName = System.IO.Path.GetFileName(processedImagePath),
                ContentType = "image/jpeg"
            };
            _validIdPreviewPath = processedImagePath;
            _localIdScanResult = scan;
            SaveDraft();
            await DisplayAlertAsync("ID scan ready", "Review the preview, then continue when the ID is clear and readable.", "Done");
            RenderStep();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("ID scan failed", ex.Message, "OK");
        }
    }

    private async Task RegisterAsync()
    {
        if (!_acceptedTerms)
        {
            await DisplayAlertAsync("Terms required", "Please read and accept the terms and conditions.", "OK");
            return;
        }

        if (Clean(_motorcycleBrandEntry.Text) is null ||
            Clean(_motorcycleModelEntry.Text) is null ||
            Clean(_motorcyclePlateEntry.Text) is null)
        {
            await DisplayAlertAsync(
                "Vehicle details needed",
                "Please add your bike or motorcycle brand, model, and plate number before creating the account.",
                "OK");
            _step = 4;
            RenderStep();
            return;
        }

        SetBusy(true);
        SaveDraft();
        try
        {
            var dto = new RegisterRequestDto(
                _firstNameEntry.Text?.Trim() ?? "",
                _lastNameEntry.Text?.Trim() ?? "",
                NormalizeEmail(_emailEntry.Text),
                _passwordEntry.Text ?? "",
                _confirmPasswordEntry.Text ?? "",
                NormalizePhilippineMobile(_phoneEntry.Text),
                AppRoles.Customer,
                _birthdayPicker.Date?.Date);

            using var http = ApiConfig.CreateHttpClient();
            using var response = await http.PostAsJsonAsync("auth/register", dto);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("Registration failed", await ReadErrorAsync(response), "OK");
                return;
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (auth is not null)
            {
                await SecureStorage.Default.SetAsync("access_token", auth.AccessToken);
                await SecureStorage.Default.SetAsync("primary_role", AppRoles.Customer);
                await SecureStorage.Default.SetAsync("user_id", auth.User.UserId.ToString());
                try
                {
                    await PersistCustomerSignupDetailsAsync(dto);
                }
                catch (Exception profileException)
                {
                    System.Diagnostics.Debug.WriteLine($"Registration profile setup failed: {profileException}");
                    await DisplayAlertAsync(
                        "Account created",
                        "Your account was created, but some profile details could not be saved. You can complete them later in Account Details.",
                        "Continue");
                }
            }

            await Shell.Current.GoToAsync($"{nameof(OtpVerificationPage)}?email={Uri.EscapeDataString(dto.Email)}&fromRegister=true");
            CustomerRegistrationDraft.Reset();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("API offline", $"Start the BikeMate API, then try registration again. {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task PersistCustomerSignupDetailsAsync(RegisterRequestDto dto)
    {
        if (_validIdFile is not null)
        {
            var upload = await CustomerApiClient.UploadFileAsync(_validIdFile, "customer-id");
            await CustomerApiClient.UpdateCustomerValidIdAsync(upload.Url);
        }

        await CustomerApiClient.UpdateCustomerAsync(new UpsertCustomerProfileDto(
            dto.FirstName,
            dto.LastName,
            dto.Email,
            dto.PhoneNumber,
            Clean(_middleNameEntry.Text),
            _sexPicker.SelectedItem?.ToString(),
            _birthdayPicker.Date?.Date));

        await CustomerApiClient.UpsertAddressAsync(null, new UpsertCustomerAddressDto(
            "Home",
            Clean(_addressEditor.Text) ?? string.Empty,
            SelectedBarangay()?.Name,
            SelectedLocality()?.Name,
            _selectedProvince ?? SelectedRegion()?.Name,
            Clean(_zipCodeEntry.Text),
            null,
            null,
            true));

        var motorcycleYear = ParseYear(_motorcycleYearEntry.Text);
        await CustomerApiClient.UpsertMotorcycleAsync(null, new UpsertMotorcycleDto(
            Clean(_motorcycleBrandEntry.Text) ?? string.Empty,
            Clean(_motorcycleModelEntry.Text) ?? string.Empty,
            motorcycleYear,
            Clean(_motorcyclePlateEntry.Text)?.ToUpperInvariant(),
            Clean(_motorcycleEngineTypeEntry.Text),
            Clean(_motorcycleColorEntry.Text),
            null));
    }

    private async Task GoBackAsync()
    {
        SaveDraft();
        if (_step > 1)
        {
            _step--;
            SaveDraft();
            RenderStep();
            return;
        }

        await Shell.Current.GoToAsync("..");
    }

    private void LoadDraft()
    {
        _step = Math.Clamp(CustomerRegistrationDraft.Step, 1, 5);
        _acceptedTerms = CustomerRegistrationDraft.AcceptedTerms;
        _birthdateSelected = CustomerRegistrationDraft.BirthdateSelected;
        _restoringDraft = true;
        _phoneEntry.Text = CustomerRegistrationDraft.PhoneNumber;
        _emailEntry.Text = CustomerRegistrationDraft.Email;
        _passwordEntry.Text = CustomerRegistrationDraft.Password;
        _confirmPasswordEntry.Text = CustomerRegistrationDraft.Password;
        _firstNameEntry.Text = CustomerRegistrationDraft.FirstName;
        _middleNameEntry.Text = CustomerRegistrationDraft.MiddleName;
        _lastNameEntry.Text = CustomerRegistrationDraft.LastName;
        if (!string.IsNullOrWhiteSpace(CustomerRegistrationDraft.Sex))
        {
            _sexPicker.SelectedItem = CustomerRegistrationDraft.Sex;
        }

        _birthdayPicker.Date = CustomerRegistrationDraft.Birthdate.Date > _birthdayPicker.MaximumDate
            ? _birthdayPicker.MaximumDate
            : CustomerRegistrationDraft.Birthdate.Date;
        _restoringDraft = false;
        _selectedProvince = CustomerRegistrationDraft.SelectedProvince;
        _zipCodeEntry.Text = CustomerRegistrationDraft.ZipCode;
        _addressEditor.Text = CustomerRegistrationDraft.Address;
        _motorcycleBrandEntry.Text = CustomerRegistrationDraft.MotorcycleBrand;
        _motorcycleModelEntry.Text = CustomerRegistrationDraft.MotorcycleModel;
        _motorcycleYearEntry.Text = CustomerRegistrationDraft.MotorcycleYear;
        _motorcyclePlateEntry.Text = CustomerRegistrationDraft.MotorcyclePlate;
        _motorcycleEngineTypeEntry.Text = CustomerRegistrationDraft.MotorcycleEngineType;
        _motorcycleColorEntry.Text = CustomerRegistrationDraft.MotorcycleColor;
        _validIdFile = CustomerRegistrationDraft.ValidIdFile;
        _validIdPreviewPath = CustomerRegistrationDraft.ValidIdPreviewPath;
    }

    private void SaveDraft()
    {
        CustomerRegistrationDraft.Step = _step;
        CustomerRegistrationDraft.AcceptedTerms = _acceptedTerms;
        CustomerRegistrationDraft.PhoneNumber = _phoneEntry.Text ?? string.Empty;
        CustomerRegistrationDraft.Email = _emailEntry.Text ?? string.Empty;
        CustomerRegistrationDraft.Password = _passwordEntry.Text ?? CustomerRegistrationDraft.Password;
        CustomerRegistrationDraft.FirstName = _firstNameEntry.Text ?? string.Empty;
        CustomerRegistrationDraft.MiddleName = _middleNameEntry.Text ?? string.Empty;
        CustomerRegistrationDraft.LastName = _lastNameEntry.Text ?? string.Empty;
        CustomerRegistrationDraft.Sex = _sexPicker.SelectedItem?.ToString() ?? string.Empty;
        CustomerRegistrationDraft.BirthdateSelected = _birthdateSelected;
        if (_birthdateSelected)
        {
            CustomerRegistrationDraft.Birthdate = (_birthdayPicker.Date ?? DateTime.Today).Date;
        }
        CustomerRegistrationDraft.RegionCode = SelectedRegion()?.Code ?? CustomerRegistrationDraft.RegionCode;
        CustomerRegistrationDraft.LocalityCode = SelectedLocality()?.Code ?? CustomerRegistrationDraft.LocalityCode;
        CustomerRegistrationDraft.BarangayName = SelectedBarangay()?.Name ?? CustomerRegistrationDraft.BarangayName;
        CustomerRegistrationDraft.SelectedProvince = _selectedProvince ?? string.Empty;
        CustomerRegistrationDraft.ZipCode = _zipCodeEntry.Text ?? string.Empty;
        CustomerRegistrationDraft.Address = _addressEditor.Text ?? string.Empty;
        CustomerRegistrationDraft.MotorcycleBrand = _motorcycleBrandEntry.Text ?? string.Empty;
        CustomerRegistrationDraft.MotorcycleModel = _motorcycleModelEntry.Text ?? string.Empty;
        CustomerRegistrationDraft.MotorcycleYear = _motorcycleYearEntry.Text ?? string.Empty;
        CustomerRegistrationDraft.MotorcyclePlate = _motorcyclePlateEntry.Text ?? string.Empty;
        CustomerRegistrationDraft.MotorcycleEngineType = _motorcycleEngineTypeEntry.Text ?? string.Empty;
        CustomerRegistrationDraft.MotorcycleColor = _motorcycleColorEntry.Text ?? string.Empty;
        CustomerRegistrationDraft.ValidIdFile = _validIdFile;
        CustomerRegistrationDraft.ValidIdPreviewPath = _validIdPreviewPath;
    }

    private async Task RestoreLocationDraftAsync()
    {
        if (_regions.Count == 0 || string.IsNullOrWhiteSpace(CustomerRegistrationDraft.RegionCode))
        {
            return;
        }

        var regionIndex = IndexOf(_regions, region => string.Equals(region.Code, CustomerRegistrationDraft.RegionCode, StringComparison.OrdinalIgnoreCase));
        if (regionIndex < 0)
        {
            return;
        }

        SelectPickerIndex(_regionPicker, regionIndex);
        var region = _regions[regionIndex];
        _localities = await CustomerApiClient.GetPhilippineLocalitiesAsync(region.Code);
        SetPickerItems(_localityPicker, _localities.Select(LocalityDisplayName));

        var localityIndex = IndexOf(_localities, locality => string.Equals(locality.Code, CustomerRegistrationDraft.LocalityCode, StringComparison.OrdinalIgnoreCase));
        if (localityIndex < 0)
        {
            return;
        }

        SelectPickerIndex(_localityPicker, localityIndex);
        var locality = _localities[localityIndex];
        _selectedProvince = locality.Province;
        _barangays = await CustomerApiClient.GetPhilippineBarangaysAsync(locality.Code);
        SetPickerItems(_barangayPicker, _barangays.Select(barangay => barangay.Name));

        var barangayIndex = IndexOf(_barangays, barangay => string.Equals(barangay.Name, CustomerRegistrationDraft.BarangayName, StringComparison.OrdinalIgnoreCase));
        if (barangayIndex >= 0)
        {
            SelectPickerIndex(_barangayPicker, barangayIndex);
        }

        RenderStep();
    }

    private void SetBusy(bool value)
    {
        _busy.IsVisible = value;
        _busy.IsRunning = value;
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int? ParseYear(string? value)
    {
        return int.TryParse(value?.Trim(), out var year) ? year : null;
    }

    private static Entry Entry(string placeholder, Keyboard? keyboard = null, bool isPassword = false)
    {
        return new Entry
        {
            Placeholder = placeholder,
            Keyboard = keyboard ?? Keyboard.Default,
            IsPassword = isPassword,
            TextColor = Color.FromArgb(Dark),
            PlaceholderColor = Color.FromArgb(Muted),
            FontSize = 13,
            FontFamily = AppTypography.BodyFont,
            BackgroundColor = Colors.Transparent
        };
    }

    private static Picker Picker(string title)
    {
        return new Picker
        {
            Title = title,
            TextColor = Color.FromArgb(Dark),
            TitleColor = Color.FromArgb(Muted),
            FontSize = 13,
            FontFamily = AppTypography.BodyFont,
            BackgroundColor = Colors.Transparent
        };
    }

    private static Border FieldCard(View content)
    {
        Detach(content);
        var card = Card(content, new Thickness(10, 1), 8);
        card.HorizontalOptions = LayoutOptions.Fill;
        return card;
    }

    private static View LabeledField(string label, View content)
    {
        return new VerticalStackLayout
        {
            Spacing = 5,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                Label(label, 10, Dark, TextAlignment.Start, FontAttributes.Bold),
                FieldCard(content)
            }
        };
    }

    private View PhoneNumberField()
    {
        Detach(_phoneEntry);

        var prefix = Label("+63", 13, Dark, TextAlignment.Start, FontAttributes.Bold);
        prefix.VerticalOptions = LayoutOptions.Center;

        var divider = new BoxView
        {
            WidthRequest = 1,
            HeightRequest = 24,
            Color = Color.FromArgb(BorderColor),
            VerticalOptions = LayoutOptions.Center
        };

        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10,
            HorizontalOptions = LayoutOptions.Fill
        };
        row.Add(prefix, 0, 0);
        row.Add(divider, 1, 0);
        row.Add(_phoneEntry, 2, 0);

        return new VerticalStackLayout
        {
            Spacing = 5,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                Label("Phone Number *", 10, Dark, TextAlignment.Start, FontAttributes.Bold),
                Card(row, new Thickness(10, 1), 8)
            }
        };
    }

    private static Border Card(View content, Thickness padding, double radius)
    {
        return new Border
        {
            Stroke = Color.FromArgb(BorderColor),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = radius },
            BackgroundColor = Colors.White,
            Padding = padding,
            HorizontalOptions = LayoutOptions.Fill,
            Content = content
        };
    }

    private static Button PrimaryButton(string text, Func<Task> action)
    {
        return new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb(Orange),
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 48,
            MinimumHeightRequest = 48,
            Padding = new Thickness(18, 0),
            FontSize = AppTypography.BodySize,
            FontAttributes = FontAttributes.Bold,
            FontFamily = AppTypography.DisplayFont,
            HorizontalOptions = LayoutOptions.Fill,
            Command = new Command(async () => await action())
        };
    }

    private static Label Label(string text, double size, string color, TextAlignment alignment, FontAttributes attributes = FontAttributes.None)
    {
        return new Label
        {
            Text = text,
            FontSize = AppTypography.SizeFor(size),
            TextColor = Color.FromArgb(color),
            FontAttributes = attributes,
            FontFamily = FontFor(size, attributes),
            HorizontalTextAlignment = alignment,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private static string FontFor(double size, FontAttributes attributes = FontAttributes.None)
    {
        return AppTypography.FontFor(size, attributes);
    }

    private static View Footnote(string text = "Fields with asterisk (*) are required.")
    {
        return Label(text, 11, Muted, TextAlignment.Start);
    }

    private static string StepTitle(int step)
    {
        return step switch
        {
            1 => "Account credentials",
            2 => "Personal details",
            3 => "Address and identity",
            4 => "Bike or motorcycle",
            _ => "Terms and privacy"
        };
    }

    private static string StepDescription(int step)
    {
        return step switch
        {
            1 => "Enter the contact details you will use to sign in.",
            2 => "Tell repair partners who they will be assisting.",
            3 => "Add your service address and an ID for account verification.",
            4 => "Add the default vehicle that shops and mechanics will service.",
            _ => "Review how BikeMate handles bookings, payments, safety, and personal data."
        };
    }

    private static View TermsSection(string title, string body, string color)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                Label(title, 13, color, TextAlignment.Start, FontAttributes.Bold),
                Label(body, 13, color, TextAlignment.Start)
            }
        };
    }

    private static string NormalizeEmail(string? email)
    {
        var normalized = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Email is required.");
        }

        try
        {
            var address = new MailAddress(normalized);
            if (!string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }
        }
        catch
        {
            throw new InvalidOperationException("Enter a valid email address.");
        }

        return normalized;
    }

    private static string NormalizePhilippineMobile(string? phoneNumber)
    {
        var digits = Regex.Replace(phoneNumber?.Trim() ?? string.Empty, @"\D", "");
        var localNumber = digits;

        if (digits.StartsWith("63", StringComparison.Ordinal) && digits.Length == 12)
        {
            localNumber = digits[2..];
        }
        else if (digits.StartsWith("0", StringComparison.Ordinal) && digits.Length == 11)
        {
            localNumber = digits[1..];
        }

        if (!Regex.IsMatch(localNumber, @"^9\d{9}$"))
        {
            throw new InvalidOperationException("Enter the 10 digits after +63, for example 9171234567.");
        }

        return $"+63{localNumber}";
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

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"Request failed with HTTP {(int)response.StatusCode}.";
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.ValueKind == JsonValueKind.String)
            {
                return error.GetString() ?? content;
            }
        }
        catch
        {
            // Fall back to raw content below.
        }

        return content;
    }

    private static View Spacer(double height)
    {
        return new BoxView { HeightRequest = height, Opacity = 0 };
    }

    private static async Task<string> CopyToCacheAsync(FileResult result)
    {
        var extension = System.IO.Path.GetExtension(result.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var targetPath = System.IO.Path.Combine(FileSystem.CacheDirectory, $"valid-id-{DateTime.UtcNow:yyyyMMddHHmmss}{extension}");
        await using var input = await result.OpenReadAsync();
        await using var output = File.Create(targetPath);
        await input.CopyToAsync(output);
        return targetPath;
    }

    private static string FormatReadability(LocalIdCardReadabilityStatus status)
    {
        return status switch
        {
            LocalIdCardReadabilityStatus.Readable => "Readable",
            LocalIdCardReadabilityStatus.Unreadable => "Could not read ID clearly",
            _ => "Needs manual review"
        };
    }

    private void DetachReusableControls()
    {
        foreach (var view in new View[]
        {
            _phoneEntry,
            _emailEntry,
            _passwordEntry,
            _confirmPasswordEntry,
            _firstNameEntry,
            _middleNameEntry,
            _lastNameEntry,
            _sexPicker,
            _birthdayPicker,
            _regionPicker,
            _localityPicker,
            _barangayPicker,
            _zipCodeEntry,
            _addressEditor,
            _motorcycleBrandEntry,
            _motorcycleModelEntry,
            _motorcycleYearEntry,
            _motorcyclePlateEntry,
            _motorcycleEngineTypeEntry,
            _motorcycleColorEntry,
            _busy
        })
        {
            Detach(view);
        }
    }

    private static void Detach(View view)
    {
        switch (view.Parent)
        {
            case Border border when ReferenceEquals(border.Content, view):
                border.Content = null;
                break;
            case Layout layout:
                layout.Remove(view);
                break;
            case ContentView contentView when ReferenceEquals(contentView.Content, view):
                contentView.Content = null;
                break;
            case ScrollView scrollView when ReferenceEquals(scrollView.Content, view):
                scrollView.Content = null;
                break;
        }
    }

    private void SelectPickerIndex(Picker picker, int index)
    {
        _updatingLocationPickers = true;
        picker.SelectedIndex = index;
        _updatingLocationPickers = false;
    }

    private static int IndexOf<T>(IReadOnlyList<T> values, Func<T, bool> predicate)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (predicate(values[index]))
            {
                return index;
            }
        }

        return -1;
    }
}
