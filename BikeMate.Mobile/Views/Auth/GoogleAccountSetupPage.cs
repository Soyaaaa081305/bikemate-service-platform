using System.Globalization;
using System.Text.RegularExpressions;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using BikeMate.Services;
using BikeMate.Views.Customer;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Storage;

namespace BikeMate.Views.Auth;

internal static class GoogleAccountSetupDraft
{
    private const string Prefix = "GoogleAccountSetup.";

    public static int CustomerId
    {
        get => Preferences.Get($"{Prefix}CustomerId", 0);
        set => Preferences.Set($"{Prefix}CustomerId", value);
    }

    public static int Step
    {
        get => Preferences.Get($"{Prefix}Step", 1);
        set => Preferences.Set($"{Prefix}Step", value);
    }

    public static string PhoneNumber
    {
        get => Preferences.Get($"{Prefix}PhoneNumber", string.Empty);
        set => Preferences.Set($"{Prefix}PhoneNumber", value);
    }

    public static string Sex
    {
        get => Preferences.Get($"{Prefix}Sex", string.Empty);
        set => Preferences.Set($"{Prefix}Sex", value);
    }

    public static string Birthdate
    {
        get => Preferences.Get($"{Prefix}Birthdate", string.Empty);
        set => Preferences.Set($"{Prefix}Birthdate", value);
    }

    public static string RegionCode
    {
        get => Preferences.Get($"{Prefix}RegionCode", string.Empty);
        set => Preferences.Set($"{Prefix}RegionCode", value);
    }

    public static string LocalityCode
    {
        get => Preferences.Get($"{Prefix}LocalityCode", string.Empty);
        set => Preferences.Set($"{Prefix}LocalityCode", value);
    }

    public static string BarangayName
    {
        get => Preferences.Get($"{Prefix}BarangayName", string.Empty);
        set => Preferences.Set($"{Prefix}BarangayName", value);
    }

    public static string SelectedProvince
    {
        get => Preferences.Get($"{Prefix}SelectedProvince", string.Empty);
        set => Preferences.Set($"{Prefix}SelectedProvince", value);
    }

    public static string ZipCode
    {
        get => Preferences.Get($"{Prefix}ZipCode", string.Empty);
        set => Preferences.Set($"{Prefix}ZipCode", value);
    }

    public static string Address
    {
        get => Preferences.Get($"{Prefix}Address", string.Empty);
        set => Preferences.Set($"{Prefix}Address", value);
    }

    public static string MotorcycleBrand
    {
        get => Preferences.Get($"{Prefix}MotorcycleBrand", string.Empty);
        set => Preferences.Set($"{Prefix}MotorcycleBrand", value);
    }

    public static string MotorcycleModel
    {
        get => Preferences.Get($"{Prefix}MotorcycleModel", string.Empty);
        set => Preferences.Set($"{Prefix}MotorcycleModel", value);
    }

    public static string MotorcycleYear
    {
        get => Preferences.Get($"{Prefix}MotorcycleYear", string.Empty);
        set => Preferences.Set($"{Prefix}MotorcycleYear", value);
    }

    public static string MotorcyclePlate
    {
        get => Preferences.Get($"{Prefix}MotorcyclePlate", string.Empty);
        set => Preferences.Set($"{Prefix}MotorcyclePlate", value);
    }

    public static string MotorcycleEngineType
    {
        get => Preferences.Get($"{Prefix}MotorcycleEngineType", string.Empty);
        set => Preferences.Set($"{Prefix}MotorcycleEngineType", value);
    }

    public static string MotorcycleColor
    {
        get => Preferences.Get($"{Prefix}MotorcycleColor", string.Empty);
        set => Preferences.Set($"{Prefix}MotorcycleColor", value);
    }

    public static string ValidIdUrl
    {
        get => Preferences.Get($"{Prefix}ValidIdUrl", string.Empty);
        set => Preferences.Set($"{Prefix}ValidIdUrl", value);
    }

    public static string ValidIdFileName
    {
        get => Preferences.Get($"{Prefix}ValidIdFileName", string.Empty);
        set => Preferences.Set($"{Prefix}ValidIdFileName", value);
    }

    public static void Reset()
    {
        foreach (var key in new[]
        {
            "CustomerId",
            "Step",
            "PhoneNumber",
            "Sex",
            "Birthdate",
            "RegionCode",
            "LocalityCode",
            "BarangayName",
            "SelectedProvince",
            "ZipCode",
            "Address",
            "MotorcycleBrand",
            "MotorcycleModel",
            "MotorcycleYear",
            "MotorcyclePlate",
            "MotorcycleEngineType",
            "MotorcycleColor",
            "ValidIdUrl",
            "ValidIdFileName"
        })
        {
            Preferences.Remove($"{Prefix}{key}");
        }
    }
}

public sealed class GoogleAccountSetupPage : CustomerPageBase
{
    private const int TotalSteps = 4;

    private CustomerMeDto? _customer;
    private CustomerAddressDto? _address;
    private MotorcycleDto? _motorcycle;
    private int _step = 1;
    private Entry? _phoneEntry;
    private Picker? _sexPicker;
    private DatePicker? _birthdatePicker;
    private Picker? _regionPicker;
    private Picker? _localityPicker;
    private Picker? _barangayPicker;
    private Entry? _zipCodeEntry;
    private Editor? _addressEditor;
    private Entry? _motorcycleBrandEntry;
    private Entry? _motorcycleModelEntry;
    private Entry? _motorcycleYearEntry;
    private Entry? _motorcyclePlateEntry;
    private Entry? _motorcycleEngineTypeEntry;
    private Entry? _motorcycleColorEntry;
    private string? _validIdUrl;
    private string? _validIdFileName;
    private LocalIdCardScanResult? _localIdScanResult;
    private bool _isBusy;
    private IReadOnlyList<PhilippineRegionDto> _regions = [];
    private IReadOnlyList<PhilippineLocalityDto> _localities = [];
    private IReadOnlyList<PhilippineBarangayDto> _barangays = [];
    private bool _isLoadingLocations;
    private bool _isUpdatingLocationPickers;
    private bool _restoredAddressSelection;
    private bool _draftLoaded;
    private bool _restoringDraft;
    private bool _formattingPhoneNumber;
    private bool _fieldChangeTrackingAttached;
    private bool _suppressDraftSave;
    private string? _selectedProvince;
    private string? _locationMessage;

    public GoogleAccountSetupPage()
    {
        Title = "Google Account Setup";
        _regionPicker = Picker("Select region");
        _localityPicker = Picker("Select city or municipality");
        _barangayPicker = Picker("Select barangay");
        _regionPicker!.SelectedIndexChanged += async (_, _) => await RegionChangedAsync();
        _localityPicker!.SelectedIndexChanged += async (_, _) => await LocalityChangedAsync();
        Render("Preparing your account setup...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_customer is not null || _isBusy)
        {
            return;
        }

        await LoadAsync();
    }

    protected override void OnDisappearing()
    {
        SaveDraft();
        base.OnDisappearing();
    }

    private async Task LoadAsync()
    {
        _isBusy = true;
        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            _address = _customer.Addresses.FirstOrDefault(item => item.IsDefault) ?? _customer.Addresses.FirstOrDefault();
            _motorcycle = _customer.Motorcycles.FirstOrDefault();
            _validIdUrl = _customer.ValidIdImageUrl;
            PrepareFields();
            LoadDraftOrDefaults();
            Render();
            await EnsureRegionsLoadedAsync();
        }
        catch (Exception ex)
        {
            Render($"Google sign-in finished, but setup could not be loaded. You can continue and finish setup from Profile. {ex.Message}");
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void Render(string? banner = null)
    {
        DetachReusableControls();
        PrepareFields();

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 8, 16, 24),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };

        body.Add(Header("Account Setup", false));
        body.Add(HeroCard());
        body.Add(StepIndicator());

        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(_step switch
        {
            1 => ProfileCard(),
            2 => AddressCard(),
            3 => MotorcycleCard(),
            _ => IdentityCard()
        });

        body.Add(NavigationCard());

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private void PrepareFields()
    {
        _phoneEntry ??= Field(FormatPhilippinePhoneForInput(_customer?.PhoneNumber), "+63 mobile number", Keyboard.Telephone);
        _phoneEntry.MaxLength = 13;
        _phoneEntry.TextChanged -= PhoneEntry_TextChanged;
        _phoneEntry.TextChanged += PhoneEntry_TextChanged;

        _sexPicker ??= new Picker
        {
            Title = "Select sex",
            TextColor = CustomerUi.Dark,
            TitleColor = CustomerUi.Muted,
            FontSize = CustomerUi.BodySize,
            FontFamily = CustomerUi.FontBody
        };
        if (_sexPicker.Items.Count == 0)
        {
            _sexPicker.Items.Add("Female");
            _sexPicker.Items.Add("Male");
            _sexPicker.Items.Add("Prefer not to say");
        }
        if (!string.IsNullOrWhiteSpace(_customer?.Sex) && _sexPicker.SelectedIndex < 0)
        {
            var index = _sexPicker.Items.IndexOf(_customer.Sex);
            _sexPicker.SelectedIndex = index >= 0 ? index : -1;
        }

        _birthdatePicker ??= new DatePicker
        {
            Date = _customer?.Birthdate?.Date ?? DateTime.Today.AddYears(-18),
            MinimumDate = DateTime.Today.AddYears(-100),
            MaximumDate = DateTime.Today.AddYears(-18),
            TextColor = CustomerUi.Dark,
            FontSize = CustomerUi.BodySize,
            FontFamily = CustomerUi.FontBody
        };

        _regionPicker ??= Picker("Select region");
        _localityPicker ??= Picker("Select city or municipality");
        _barangayPicker ??= Picker("Select barangay");
        _zipCodeEntry ??= Field(_address?.PostalCode, "Zip code", Keyboard.Numeric);
        _zipCodeEntry.MaxLength = 4;
        _addressEditor ??= EditorField(_address?.AddressLine, "House number, street, subdivision, landmark");

        _motorcycleBrandEntry ??= Field(_motorcycle?.Brand, "Brand");
        _motorcycleModelEntry ??= Field(_motorcycle?.Model, "Model");
        _motorcycleYearEntry ??= Field(_motorcycle?.YearModel?.ToString(CultureInfo.InvariantCulture), "Year model", Keyboard.Numeric);
        _motorcycleYearEntry.MaxLength = 4;
        _motorcyclePlateEntry ??= Field(_motorcycle?.PlateNumber, "Plate number");
        _motorcyclePlateEntry.MaxLength = 15;
        _motorcycleEngineTypeEntry ??= Field(_motorcycle?.EngineType, "Engine type");
        _motorcycleColorEntry ??= Field(_motorcycle?.Color, "Color");

        AttachDraftChangeTracking();
    }

    private void PhoneEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_formattingPhoneNumber)
        {
            return;
        }

        var formatted = FormatPhilippinePhoneForInput(e.NewTextValue);
        if (!string.Equals(formatted, e.NewTextValue, StringComparison.Ordinal))
        {
            _formattingPhoneNumber = true;
            try
            {
                _phoneEntry!.Text = formatted;
                TryMoveCursorToEnd(_phoneEntry, formatted);
            }
            finally
            {
                _formattingPhoneNumber = false;
            }
        }

        SaveDraft();
    }

    private void AttachDraftChangeTracking()
    {
        if (_fieldChangeTrackingAttached)
        {
            return;
        }

        foreach (var entry in new Entry?[]
        {
            _zipCodeEntry,
            _motorcycleBrandEntry,
            _motorcycleModelEntry,
            _motorcycleYearEntry,
            _motorcyclePlateEntry,
            _motorcycleEngineTypeEntry,
            _motorcycleColorEntry
        })
        {
            if (entry is not null)
            {
                entry.TextChanged += (_, _) => SaveDraft();
            }
        }

        if (_addressEditor is not null)
        {
            _addressEditor.TextChanged += (_, _) => SaveDraft();
        }

        if (_sexPicker is not null)
        {
            _sexPicker.SelectedIndexChanged += (_, _) => SaveDraft();
        }

        if (_barangayPicker is not null)
        {
            _barangayPicker.SelectedIndexChanged += (_, _) => SaveDraft();
        }

        if (_birthdatePicker is not null)
        {
            _birthdatePicker.DateSelected += (_, _) => SaveDraft();
        }

        _fieldChangeTrackingAttached = true;
    }

    private View HeroCard()
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(Label("Finish your BikeMate profile", 18, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(Label(
            "Your Google email is verified. Complete these setup steps so BikeMate admin can review your account. You can open the app, but bookings stay limited until approval.",
            11,
            CustomerUi.Muted));
        stack.Add(BadgeRow("Approval status", FormatApprovalStatus(_customer?.AccountStatus ?? "pending")));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View StepIndicator()
    {
        var stack = new VerticalStackLayout { Spacing = 7 };
        stack.Add(Label($"Step {_step} of {TotalSteps}", 11, CustomerUi.Muted, FontAttributes.Bold));

        var progress = new Grid { ColumnSpacing = 5 };
        for (var index = 0; index < TotalSteps; index++)
        {
            progress.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            progress.Add(new BoxView
            {
                HeightRequest = 4,
                CornerRadius = 2,
                Color = index < _step ? CustomerUi.Orange : Color.FromArgb("#DCDCDC")
            }, index, 0);
        }

        stack.Add(progress);
        stack.Add(Label(StepDescription(_step), 11, CustomerUi.Muted));
        return stack;
    }

    private View NavigationCard()
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        var primaryText = _step == TotalSteps
            ? (_isBusy ? "Submitting..." : "Submit for review")
            : "Continue";

        if (_step == 1)
        {
            stack.Add(OrangeButton(primaryText, new Command(async () => await ContinueAsync())));
            stack.Add(GhostButton("Open app, finish from Profile", new Command(async () => await SkipAsync())));
            return stack;
        }

        var grid = new Grid { ColumnSpacing = 10 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        grid.Add(GhostButton("Back", new Command(async () => await BackAsync())), 0, 0);
        grid.Add(OrangeButton(primaryText, new Command(async () => await ContinueAsync())), 1, 0);
        stack.Add(grid);
        return stack;
    }

    private View ProfileCard()
    {
        var stack = Section("Quick personal details", "No password or email OTP needed for Google accounts.");
        stack.Add(DetailLine("Name", _customer is null ? "Google account" : FullName(_customer)));
        stack.Add(DetailLine("Email", _customer?.Email ?? "Google email"));
        stack.Add(InputBlock("Mobile number", _phoneEntry!));
        stack.Add(TwoColumnInputs(("Sex", _sexPicker!), ("Birthdate", _birthdatePicker!)));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View AddressCard()
    {
        var stack = Section("Default service address", "Used by shops and mechanics when you book a repair.");
        if (!string.IsNullOrWhiteSpace(_locationMessage))
        {
            stack.Add(Label(_locationMessage, 11, CustomerUi.Muted));
        }

        stack.Add(InputBlock("Region", _regionPicker!));
        stack.Add(InputBlock("City / Municipality", _localityPicker!));
        if (!string.IsNullOrWhiteSpace(_selectedProvince))
        {
            stack.Add(BadgeRow("Province", _selectedProvince));
        }

        stack.Add(TwoColumnInputs(("Barangay", _barangayPicker!), ("Zip code", _zipCodeEntry!)));
        stack.Add(InputBlock("Complete address", _addressEditor!));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View MotorcycleCard()
    {
        var stack = Section("Primary bike or motorcycle", "Used as the default vehicle when booking repair services.");
        stack.Add(TwoColumnInputs(("Brand", _motorcycleBrandEntry!), ("Model", _motorcycleModelEntry!)));
        stack.Add(TwoColumnInputs(("Year", _motorcycleYearEntry!), ("Plate number", _motorcyclePlateEntry!)));
        stack.Add(TwoColumnInputs(("Engine type", _motorcycleEngineTypeEntry!), ("Color", _motorcycleColorEntry!)));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View IdentityCard()
    {
        var stack = Section(
            "Identity verification",
            "Admin approval requires a valid ID. You can upload it now or later from Profile.");

        var status = !string.IsNullOrWhiteSpace(_validIdUrl)
            ? "Uploaded"
            : !string.IsNullOrWhiteSpace(_validIdFileName)
                ? _validIdFileName
                : "Not uploaded";
        stack.Add(BadgeRow("Valid ID", status));
        if (_localIdScanResult is not null)
        {
            stack.Add(BadgeRow("Last scan", FormatReadability(_localIdScanResult.ReadabilityStatus)));
            stack.Add(Label(
                "This scan was uploaded for admin review.",
                11,
                CustomerUi.Muted));
        }

        var scanButtonText = string.IsNullOrWhiteSpace(_validIdUrl)
            ? "Scan and upload valid ID"
            : "Rescan and upload valid ID";
        stack.Add(GhostButton(scanButtonText, new Command(async () => await UploadValidIdAsync())));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private async Task EnsureRegionsLoadedAsync()
    {
        if (_regions.Count > 0 || _isLoadingLocations)
        {
            return;
        }

        _isLoadingLocations = true;
        _locationMessage = "Loading Philippine locations...";
        Render();
        try
        {
            _regions = await CustomerApiClient.GetPhilippineRegionsAsync();
            SetPickerItems(_regionPicker!, _regions.Select(region => region.Name));
            _locationMessage = _regions.Count == 0 ? "No Philippine regions were returned. Please try again." : null;
        }
        catch (Exception ex)
        {
            _locationMessage = $"BikeMate could not load Philippine locations. {ex.Message}";
        }
        finally
        {
            _isLoadingLocations = false;
            Render();
        }

        if (!await RestoreDraftAddressSelectionAsync())
        {
            await RestoreSavedAddressSelectionAsync();
        }
    }

    private async Task RegionChangedAsync()
    {
        if (_isUpdatingLocationPickers)
        {
            return;
        }

        var region = SelectedRegion();
        if (region is null)
        {
            return;
        }

        _isLoadingLocations = true;
        _locationMessage = "Loading cities and municipalities...";
        _localities = [];
        _barangays = [];
        _selectedProvince = null;
        GoogleAccountSetupDraft.RegionCode = region.Code;
        GoogleAccountSetupDraft.LocalityCode = string.Empty;
        GoogleAccountSetupDraft.BarangayName = string.Empty;
        GoogleAccountSetupDraft.SelectedProvince = string.Empty;
        ResetPicker(_localityPicker!, "Select city or municipality");
        ResetPicker(_barangayPicker!, "Select barangay");
        Render();

        try
        {
            _localities = await CustomerApiClient.GetPhilippineLocalitiesAsync(region.Code);
            SetPickerItems(_localityPicker!, _localities.Select(LocalityDisplayName));
            _locationMessage = _localities.Count == 0 ? "No cities or municipalities were returned for this region." : null;
        }
        catch (Exception ex)
        {
            _locationMessage = $"BikeMate could not load cities or municipalities. {ex.Message}";
        }
        finally
        {
            _isLoadingLocations = false;
            SaveDraft();
            Render();
        }
    }

    private async Task LocalityChangedAsync()
    {
        if (_isUpdatingLocationPickers)
        {
            return;
        }

        var locality = SelectedLocality();
        if (locality is null)
        {
            return;
        }

        _selectedProvince = locality.Province;
        GoogleAccountSetupDraft.LocalityCode = locality.Code;
        GoogleAccountSetupDraft.BarangayName = string.Empty;
        GoogleAccountSetupDraft.SelectedProvince = locality.Province ?? string.Empty;
        _isLoadingLocations = true;
        _locationMessage = "Loading barangays...";
        _barangays = [];
        ResetPicker(_barangayPicker!, "Select barangay");
        Render();

        try
        {
            _barangays = await CustomerApiClient.GetPhilippineBarangaysAsync(locality.Code);
            SetPickerItems(_barangayPicker!, _barangays.Select(barangay => barangay.Name));
            _locationMessage = _barangays.Count == 0 ? "No barangays were returned for this city or municipality." : null;
        }
        catch (Exception ex)
        {
            _locationMessage = $"BikeMate could not load barangays. {ex.Message}";
        }
        finally
        {
            _isLoadingLocations = false;
            SaveDraft();
            Render();
        }
    }

    private void LoadDraftOrDefaults()
    {
        if (_customer is null || _draftLoaded)
        {
            return;
        }

        if (GoogleAccountSetupDraft.CustomerId != 0 && GoogleAccountSetupDraft.CustomerId != _customer.ClientId)
        {
            GoogleAccountSetupDraft.Reset();
        }

        var hasDraft = GoogleAccountSetupDraft.CustomerId == _customer.ClientId;
        _restoringDraft = true;
        _step = hasDraft ? Math.Clamp(GoogleAccountSetupDraft.Step, 1, TotalSteps) : 1;

        _phoneEntry!.Text = FormatPhilippinePhoneForInput(hasDraft && !string.IsNullOrWhiteSpace(GoogleAccountSetupDraft.PhoneNumber)
            ? GoogleAccountSetupDraft.PhoneNumber
            : _customer.PhoneNumber);

        var sex = hasDraft && !string.IsNullOrWhiteSpace(GoogleAccountSetupDraft.Sex)
            ? GoogleAccountSetupDraft.Sex
            : _customer.Sex;
        if (!string.IsNullOrWhiteSpace(sex))
        {
            var index = _sexPicker!.Items.IndexOf(sex);
            _sexPicker.SelectedIndex = index >= 0 ? index : -1;
        }

        if (hasDraft &&
            DateTime.TryParse(GoogleAccountSetupDraft.Birthdate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var draftBirthdate))
        {
            _birthdatePicker!.Date = draftBirthdate.Date;
        }
        else if (_customer.Birthdate is not null)
        {
            _birthdatePicker!.Date = _customer.Birthdate.Value.Date;
        }

        _selectedProvince = hasDraft && !string.IsNullOrWhiteSpace(GoogleAccountSetupDraft.SelectedProvince)
            ? GoogleAccountSetupDraft.SelectedProvince
            : _address?.Province;
        _zipCodeEntry!.Text = hasDraft ? GoogleAccountSetupDraft.ZipCode : _address?.PostalCode ?? string.Empty;
        _addressEditor!.Text = hasDraft ? GoogleAccountSetupDraft.Address : _address?.AddressLine ?? string.Empty;
        _motorcycleBrandEntry!.Text = hasDraft ? GoogleAccountSetupDraft.MotorcycleBrand : _motorcycle?.Brand ?? string.Empty;
        _motorcycleModelEntry!.Text = hasDraft ? GoogleAccountSetupDraft.MotorcycleModel : _motorcycle?.Model ?? string.Empty;
        _motorcycleYearEntry!.Text = hasDraft ? GoogleAccountSetupDraft.MotorcycleYear : _motorcycle?.YearModel?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        _motorcyclePlateEntry!.Text = hasDraft ? GoogleAccountSetupDraft.MotorcyclePlate : _motorcycle?.PlateNumber ?? string.Empty;
        _motorcycleEngineTypeEntry!.Text = hasDraft ? GoogleAccountSetupDraft.MotorcycleEngineType : _motorcycle?.EngineType ?? string.Empty;
        _motorcycleColorEntry!.Text = hasDraft ? GoogleAccountSetupDraft.MotorcycleColor : _motorcycle?.Color ?? string.Empty;

        if (hasDraft && !string.IsNullOrWhiteSpace(GoogleAccountSetupDraft.ValidIdUrl))
        {
            _validIdUrl = GoogleAccountSetupDraft.ValidIdUrl;
            _validIdFileName = GoogleAccountSetupDraft.ValidIdFileName;
        }

        _restoringDraft = false;
        _draftLoaded = true;
    }

    private void SaveDraft()
    {
        if (_suppressDraftSave || _restoringDraft || _customer is null)
        {
            return;
        }

        GoogleAccountSetupDraft.CustomerId = _customer.ClientId;
        GoogleAccountSetupDraft.Step = _step;
        GoogleAccountSetupDraft.PhoneNumber = _phoneEntry?.Text ?? string.Empty;
        GoogleAccountSetupDraft.Sex = _sexPicker?.SelectedItem?.ToString() ?? string.Empty;
        GoogleAccountSetupDraft.Birthdate = (_birthdatePicker?.Date ?? DateTime.Today).Date.ToString("O", CultureInfo.InvariantCulture);
        GoogleAccountSetupDraft.RegionCode = SelectedRegion()?.Code ?? GoogleAccountSetupDraft.RegionCode;
        GoogleAccountSetupDraft.LocalityCode = SelectedLocality()?.Code ?? GoogleAccountSetupDraft.LocalityCode;
        GoogleAccountSetupDraft.BarangayName = SelectedBarangay()?.Name ?? GoogleAccountSetupDraft.BarangayName;
        GoogleAccountSetupDraft.SelectedProvince = _selectedProvince ?? string.Empty;
        GoogleAccountSetupDraft.ZipCode = _zipCodeEntry?.Text ?? string.Empty;
        GoogleAccountSetupDraft.Address = _addressEditor?.Text ?? string.Empty;
        GoogleAccountSetupDraft.MotorcycleBrand = _motorcycleBrandEntry?.Text ?? string.Empty;
        GoogleAccountSetupDraft.MotorcycleModel = _motorcycleModelEntry?.Text ?? string.Empty;
        GoogleAccountSetupDraft.MotorcycleYear = _motorcycleYearEntry?.Text ?? string.Empty;
        GoogleAccountSetupDraft.MotorcyclePlate = _motorcyclePlateEntry?.Text ?? string.Empty;
        GoogleAccountSetupDraft.MotorcycleEngineType = _motorcycleEngineTypeEntry?.Text ?? string.Empty;
        GoogleAccountSetupDraft.MotorcycleColor = _motorcycleColorEntry?.Text ?? string.Empty;
        GoogleAccountSetupDraft.ValidIdUrl = _validIdUrl ?? string.Empty;
        GoogleAccountSetupDraft.ValidIdFileName = _validIdFileName ?? string.Empty;
    }

    private async Task<bool> RestoreDraftAddressSelectionAsync()
    {
        if (_restoredAddressSelection ||
            _customer is null ||
            GoogleAccountSetupDraft.CustomerId != _customer.ClientId ||
            _regions.Count == 0 ||
            string.IsNullOrWhiteSpace(GoogleAccountSetupDraft.RegionCode))
        {
            return false;
        }

        var regionIndex = IndexOf(_regions, region => string.Equals(region.Code, GoogleAccountSetupDraft.RegionCode, StringComparison.OrdinalIgnoreCase));
        if (regionIndex < 0)
        {
            return false;
        }

        _restoredAddressSelection = true;
        SelectPickerIndex(_regionPicker!, regionIndex);
        var region = _regions[regionIndex];
        _localities = await CustomerApiClient.GetPhilippineLocalitiesAsync(region.Code);
        SetPickerItems(_localityPicker!, _localities.Select(LocalityDisplayName));

        var localityIndex = IndexOf(_localities, locality => string.Equals(locality.Code, GoogleAccountSetupDraft.LocalityCode, StringComparison.OrdinalIgnoreCase));
        if (localityIndex >= 0)
        {
            SelectPickerIndex(_localityPicker!, localityIndex);
            var locality = _localities[localityIndex];
            _selectedProvince = locality.Province;
            _barangays = await CustomerApiClient.GetPhilippineBarangaysAsync(locality.Code);
            SetPickerItems(_barangayPicker!, _barangays.Select(barangay => barangay.Name));

            var barangayIndex = IndexOf(_barangays, barangay => string.Equals(barangay.Name, GoogleAccountSetupDraft.BarangayName, StringComparison.OrdinalIgnoreCase));
            if (barangayIndex >= 0)
            {
                SelectPickerIndex(_barangayPicker!, barangayIndex);
            }
        }

        Render();
        return true;
    }

    private async Task RestoreSavedAddressSelectionAsync()
    {
        if (_restoredAddressSelection || _regions.Count == 0 || _address is null)
        {
            return;
        }

        var query = string.Join(", ", new[] { _address.City, _address.Province, "Philippines" }
            .Where(item => !string.IsNullOrWhiteSpace(item)));
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        try
        {
            var match = await CustomerApiClient.ResolvePhilippineLocationAsync(query);
            if (match is null)
            {
                _locationMessage = "Saved address loaded. Please reselect the region, city, and barangay from BikeMate location data.";
                return;
            }

            _restoredAddressSelection = true;
            var regionIndex = IndexOf(_regions, region => string.Equals(region.Code, match.Region.Code, StringComparison.OrdinalIgnoreCase));
            if (regionIndex < 0)
            {
                return;
            }

            SelectPickerIndex(_regionPicker!, regionIndex);
            _localities = await CustomerApiClient.GetPhilippineLocalitiesAsync(match.Region.Code);
            SetPickerItems(_localityPicker!, _localities.Select(LocalityDisplayName));
            var localityIndex = IndexOf(_localities, locality => string.Equals(locality.Code, match.Locality.Code, StringComparison.OrdinalIgnoreCase));
            if (localityIndex >= 0)
            {
                SelectPickerIndex(_localityPicker!, localityIndex);
                _selectedProvince = _localities[localityIndex].Province;
                _barangays = await CustomerApiClient.GetPhilippineBarangaysAsync(match.Locality.Code);
                SetPickerItems(_barangayPicker!, _barangays.Select(barangay => barangay.Name));
                var barangayIndex = IndexOf(_barangays, barangay => string.Equals(barangay.Name, _address.Barangay, StringComparison.OrdinalIgnoreCase));
                if (barangayIndex >= 0)
                {
                    SelectPickerIndex(_barangayPicker!, barangayIndex);
                }
            }
        }
        catch (Exception ex)
        {
            _locationMessage = $"Saved address could not be matched automatically. {ex.Message}";
        }
        finally
        {
            Render();
        }
    }

    private PhilippineRegionDto? SelectedRegion()
    {
        return _regionPicker is not null && _regionPicker.SelectedIndex >= 0 && _regionPicker.SelectedIndex < _regions.Count
            ? _regions[_regionPicker.SelectedIndex]
            : null;
    }

    private PhilippineLocalityDto? SelectedLocality()
    {
        return _localityPicker is not null && _localityPicker.SelectedIndex >= 0 && _localityPicker.SelectedIndex < _localities.Count
            ? _localities[_localityPicker.SelectedIndex]
            : null;
    }

    private PhilippineBarangayDto? SelectedBarangay()
    {
        return _barangayPicker is not null && _barangayPicker.SelectedIndex >= 0 && _barangayPicker.SelectedIndex < _barangays.Count
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
        _isUpdatingLocationPickers = true;
        picker.Items.Clear();
        foreach (var item in items)
        {
            picker.Items.Add(item);
        }

        picker.SelectedIndex = -1;
        _isUpdatingLocationPickers = false;
    }

    private void ResetPicker(Picker picker, string title)
    {
        _isUpdatingLocationPickers = true;
        picker.Items.Clear();
        picker.Title = title;
        picker.SelectedIndex = -1;
        _isUpdatingLocationPickers = false;
    }

    private void SelectPickerIndex(Picker picker, int index)
    {
        _isUpdatingLocationPickers = true;
        picker.SelectedIndex = index;
        _isUpdatingLocationPickers = false;
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

            var file = new FileResult(processedImagePath)
            {
                FileName = System.IO.Path.GetFileName(processedImagePath),
                ContentType = "image/jpeg"
            };
            _isBusy = true;
            Render("Uploading valid ID...");
            var upload = await CustomerApiClient.UploadFileAsync(file, "customer-id");
            await CustomerApiClient.UpdateCustomerValidIdAsync(upload.Url);
            _validIdUrl = upload.Url;
            _validIdFileName = file.FileName;
            _localIdScanResult = scan;
            SaveDraft();
            Render("Scanned valid ID uploaded. Finish the remaining details and submit for review.");
        }
        catch (Exception ex)
        {
            Render($"Valid ID scan was not uploaded. {ex.Message}");
        }
        finally
        {
            _isBusy = false;
        }
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

    private async Task ContinueAsync()
    {
        if (_isBusy)
        {
            return;
        }

        if (_step == TotalSteps)
        {
            await SubmitAsync();
            return;
        }

        if (!ValidateStep(_step, out var message))
        {
            await DisplayAlertAsync("Setup details needed", message, "OK");
            return;
        }

        SaveDraft();
        _step++;
        SaveDraft();
        Render();
        if (_step == 2)
        {
            await EnsureRegionsLoadedAsync();
        }
    }

    private async Task BackAsync()
    {
        if (_step > 1)
        {
            SaveDraft();
            _step--;
            SaveDraft();
            Render();
        }
    }

    private bool ValidateStep(int step, out string message)
    {
        message = string.Empty;
        switch (step)
        {
            case 1:
                try
                {
                    _phoneEntry!.Text = NormalizePhilippineMobile(_phoneEntry.Text);
                }
                catch (InvalidOperationException ex)
                {
                    message = ex.Message;
                    return false;
                }
                return true;
            case 2:
                if (SelectedRegion() is null ||
                    SelectedLocality() is null ||
                    SelectedBarangay() is null ||
                    Clean(_zipCodeEntry?.Text) is null ||
                    Clean(_addressEditor?.Text) is null)
                {
                    message = "Please select your Philippine region, city or municipality, barangay, zip code, and complete service address.";
                    return false;
                }

                if (!Regex.IsMatch(_zipCodeEntry!.Text.Trim(), @"^\d{4}$"))
                {
                    message = "Enter a valid 4-digit Philippine zip code.";
                    return false;
                }
                return true;
            case 3:
                if (Clean(_motorcycleBrandEntry?.Text) is null ||
                    Clean(_motorcycleModelEntry?.Text) is null ||
                    Clean(_motorcyclePlateEntry?.Text) is null)
                {
                    message = "Please add your bike or motorcycle brand, model, and plate number.";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(_motorcycleYearEntry?.Text) &&
                    (!int.TryParse(_motorcycleYearEntry.Text.Trim(), out var year) || year < 1950 || year > DateTime.Today.Year + 1))
                {
                    message = "Enter a valid year model.";
                    return false;
                }

                return true;
            default:
                return true;
        }
    }

    private async Task SubmitAsync()
    {
        if (_customer is null || _isBusy)
        {
            return;
        }

        string phone;
        try
        {
            phone = NormalizePhilippineMobile(_phoneEntry?.Text);
            _phoneEntry!.Text = phone;
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlertAsync("Setup details needed", ex.Message, "OK");
            return;
        }

        var sex = _sexPicker?.SelectedItem?.ToString();
        DateTime? birthdate = _birthdatePicker?.Date;
        birthdate = birthdate?.Date;
        var locality = SelectedLocality();
        var barangay = SelectedBarangay();
        var province = Clean(_selectedProvince) ?? Clean(locality?.Province) ?? SelectedRegion()?.Name;
        var city = locality?.Name;
        var barangayName = barangay?.Name;
        var zipCode = Clean(_zipCodeEntry?.Text);
        var addressLine = Clean(_addressEditor?.Text);
        var motorcycleBrand = Clean(_motorcycleBrandEntry?.Text);
        var motorcycleModel = Clean(_motorcycleModelEntry?.Text);
        var motorcyclePlate = Clean(_motorcyclePlateEntry?.Text)?.ToUpperInvariant();
        var motorcycleEngineType = Clean(_motorcycleEngineTypeEntry?.Text);
        var motorcycleColor = Clean(_motorcycleColorEntry?.Text);
        int? motorcycleYear = null;
        if (!string.IsNullOrWhiteSpace(_motorcycleYearEntry?.Text) &&
            int.TryParse(_motorcycleYearEntry.Text.Trim(), out var parsedYear))
        {
            motorcycleYear = parsedYear;
        }

        if (addressLine is null || city is null || province is null || barangayName is null || zipCode is null)
        {
            await DisplayAlertAsync(
                "Setup details needed",
                "Please add your mobile number and complete Philippine address before submitting for admin review.",
                "OK");
            return;
        }

        if (motorcycleBrand is null || motorcycleModel is null || motorcyclePlate is null)
        {
            await DisplayAlertAsync(
                "Vehicle details needed",
                "Please add your primary bike or motorcycle brand, model, and plate number before submitting for admin review.",
                "OK");
            return;
        }

        _isBusy = true;
        Render("Submitting account setup...");
        try
        {
            await CustomerApiClient.UpdateCustomerAsync(new UpsertCustomerProfileDto(
                _customer.FirstName,
                _customer.LastName,
                _customer.Email,
                phone,
                _customer.MiddleName,
                sex,
                birthdate));

            await CustomerApiClient.UpsertAddressAsync(_address, new UpsertCustomerAddressDto(
                _address?.Label ?? "Home",
                addressLine,
                barangayName,
                city,
                province,
                zipCode,
                _address?.Latitude,
                _address?.Longitude,
                true));

            await CustomerApiClient.UpsertMotorcycleAsync(_motorcycle, new UpsertMotorcycleDto(
                motorcycleBrand,
                motorcycleModel,
                motorcycleYear,
                motorcyclePlate,
                motorcycleEngineType,
                motorcycleColor,
                _motorcycle?.MotorcycleImageUrl));

            await DisplayAlertAsync(
                "Submitted for review",
                string.IsNullOrWhiteSpace(_validIdUrl)
                    ? "Your Google account setup was saved. Upload a valid ID from Profile so BikeMate admin can approve the account."
                    : "Your Google account setup was saved. BikeMate admin can now review your customer account.",
                "OK");

            GoogleAccountSetupDraft.Reset();
            _suppressDraftSave = true;
            await AppNavigation.NavigateByRoleAsync(AppRoles.Customer);
        }
        catch (Exception ex)
        {
            Render($"Account setup was not submitted. {ex.Message}");
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task SkipAsync()
    {
        var proceed = await DisplayAlertAsync(
            "Open BikeMate without finishing setup?",
            "You can browse your profile, but booking and approval-related actions stay limited until you finish your profile details and BikeMate admin approves the account.",
            "Open app",
            "Stay here");
        if (!proceed)
        {
            return;
        }

        SaveDraft();
        await DisplayAlertAsync(
            "Setup skipped",
            "You can open BikeMate and view your profile, but booking approval requires completed profile details and BikeMate admin approval. Finish setup anytime from Profile.",
            "OK");
        await AppNavigation.NavigateByRoleAsync(AppRoles.Customer);
    }

    private static Entry Field(string? value, string placeholder, Keyboard? keyboard = null)
    {
        return new Entry
        {
            Text = value ?? string.Empty,
            Placeholder = placeholder,
            Keyboard = keyboard ?? Keyboard.Text,
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            FontSize = CustomerUi.BodySize,
            FontFamily = CustomerUi.FontBody
        };
    }

    private static Picker Picker(string title)
    {
        return new Picker
        {
            Title = title,
            TextColor = CustomerUi.Dark,
            TitleColor = CustomerUi.Muted,
            FontSize = CustomerUi.BodySize,
            FontFamily = CustomerUi.FontBody,
            BackgroundColor = Colors.Transparent
        };
    }

    private static Editor EditorField(string? value, string placeholder)
    {
        return new Editor
        {
            Text = value ?? string.Empty,
            Placeholder = placeholder,
            HeightRequest = 78,
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            FontSize = CustomerUi.BodySize,
            FontFamily = CustomerUi.FontBody,
            AutoSize = EditorAutoSizeOption.TextChanges
        };
    }

    private static VerticalStackLayout Section(string title, string subtitle)
    {
        return new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Label(title, 14, CustomerUi.Dark, FontAttributes.Bold),
                Label(subtitle, 11, CustomerUi.Muted)
            }
        };
    }

    private static View InputBlock(string label, View input)
    {
        var stack = new VerticalStackLayout { Spacing = 5 };
        stack.Add(Label(label, 11, CustomerUi.Muted, FontAttributes.Bold));
        stack.Add(Card(input, Color.FromArgb("#FAFAFA"), 8, new Thickness(10, 2)));
        return stack;
    }

    private static View TwoColumnInputs((string Label, View Input) left, (string Label, View Input) right)
    {
        var grid = new Grid { ColumnSpacing = 10 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.Add(InputBlock(left.Label, left.Input), 0, 0);
        grid.Add(InputBlock(right.Label, right.Input), 1, 0);
        return grid;
    }

    private static View BadgeRow(string label, string value)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(Label(label, 11, CustomerUi.Muted), 0, 0);
        grid.Add(Badge(value, Color.FromArgb("#EEF1F4"), CustomerUi.Dark), 1, 0);
        return grid;
    }

    private static View DetailLine(string label, string value)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };
        grid.Add(Label(label, 11, CustomerUi.Muted), 0, 0);
        grid.Add(Label(value, 11, CustomerUi.Dark, FontAttributes.Bold), 1, 0);
        return grid;
    }

    private static Border Badge(string text, Color background, Color textColor)
    {
        return new Border
        {
            BackgroundColor = background,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(10, 4),
            Content = new Label
            {
                Text = text,
                FontSize = CustomerUi.CaptionSize,
                FontFamily = CustomerUi.FontCaptionBold,
                TextColor = textColor,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
    }

    private static string FormatApprovalStatus(string status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "Pending"
            : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(status.Replace("_", " ").ToLowerInvariant());
    }

    private static string StepDescription(int step)
    {
        return step switch
        {
            1 => "Confirm the contact and personal details connected to your Google account.",
            2 => "Add the service address shops and mechanics will use for bookings.",
            3 => "Add the bike or motorcycle that will be used as your default vehicle.",
            _ => "Upload a valid ID now or later; admin approval is still required before booking."
        };
    }

    private static string FullName(CustomerMeDto customer)
    {
        return string.Join(" ", new[] { customer.FirstName, customer.MiddleName, customer.LastName }
            .Where(item => !string.IsNullOrWhiteSpace(item)));
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string FormatPhilippinePhoneForInput(string? phoneNumber)
    {
        var digits = Regex.Replace(phoneNumber?.Trim() ?? string.Empty, @"\D", "");
        if (string.Equals(digits, "63", StringComparison.Ordinal))
        {
            digits = string.Empty;
        }
        else if (digits.StartsWith("63", StringComparison.Ordinal) && digits.Length > 2)
        {
            digits = digits[2..];
        }
        else if (digits.StartsWith("0", StringComparison.Ordinal) && digits.Length > 1)
        {
            digits = digits[1..];
        }

        if (digits.Length > 10)
        {
            digits = digits[..10];
        }

        return $"+63{digits}";
    }

    private static string NormalizePhilippineMobile(string? phoneNumber)
    {
        var formatted = FormatPhilippinePhoneForInput(phoneNumber);
        var localNumber = formatted.Length > 3 ? formatted[3..] : string.Empty;
        if (!Regex.IsMatch(localNumber, @"^9\d{9}$"))
        {
            throw new InvalidOperationException("Enter a valid Philippine mobile number starting with +639, for example +639171234567.");
        }

        return formatted;
    }

    private static void TryMoveCursorToEnd(Entry entry, string text)
    {
        try
        {
            entry.CursorPosition = Math.Min(text.Length, entry.Text?.Length ?? text.Length);
        }
        catch (Exception)
        {
            // Android can reject cursor changes while the native text box is still applying backspace.
        }
    }

    private void DetachReusableControls()
    {
        foreach (var view in new View?[]
        {
            _phoneEntry,
            _sexPicker,
            _birthdatePicker,
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
            _motorcycleColorEntry
        })
        {
            if (view is not null)
            {
                Detach(view);
            }
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
}
