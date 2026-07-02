using System.Collections.ObjectModel;
using BIKEMATES_ADMIN.Services;
using Microsoft.Maui.Media;
using Microsoft.Maui.Graphics;

namespace BIKEMATES_ADMIN.Pages;

public partial class Admins : ContentPage
{
    public ObservableCollection<MechanicApplicationRow> Applications { get; } = new();

    private string? _profileImageUrl;
    private string? _validIdImageUrl;
    private string? _certificationImageUrl;
    private IReadOnlyList<PhilippineRegion> _regions = [];
    private IReadOnlyList<PhilippineLocality> _localities = [];
    private IReadOnlyList<PhilippineBarangay> _barangays = [];
    private bool _loadingLocations;
    private bool _updatingPickers;
    private bool _formattingPhoneNumber;

    public Admins()
    {
        InitializeComponent();
        BindingContext = this;

        SexPicker.ItemsSource = new List<string> { "Female", "Male", "Prefer not to say" };
        BirthdatePicker.MaximumDate = DateTime.Today.AddYears(-18);
        BirthdatePicker.MinimumDate = DateTime.Today.AddYears(-80);
        BirthdatePicker.Date = BirthdatePicker.MaximumDate;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRegionsAsync();
        await LoadApplicationsAsync();
    }

    private async Task LoadRegionsAsync()
    {
        if (_regions.Count > 0 || _loadingLocations)
        {
            return;
        }

        _loadingLocations = true;
        LocationStatusLabel.Text = "Loading Philippine regions...";
        try
        {
            _regions = await BikeMateDatabaseService.GetPhilippineRegionsAsync();
            SetPickerItems(RegionPicker, _regions.Select(region => region.Name));
            LocationStatusLabel.Text = _regions.Count == 0 ? "No regions were returned. Please try again." : "";
        }
        catch (Exception ex)
        {
            LocationStatusLabel.Text = $"Unable to load Philippine locations. {ex.Message}";
        }
        finally
        {
            _loadingLocations = false;
        }
    }

    private async void OnRegionChanged(object sender, EventArgs e)
    {
        if (_updatingPickers)
        {
            return;
        }

        var region = SelectedRegion();
        if (region is null)
        {
            return;
        }

        _loadingLocations = true;
        LocationStatusLabel.Text = "Loading cities and municipalities...";
        ProvinceLabel.Text = "";
        _localities = [];
        _barangays = [];
        ResetPicker(CityPicker, "Select city or municipality");
        ResetPicker(BarangayPicker, "Select barangay");

        try
        {
            _localities = await BikeMateDatabaseService.GetPhilippineLocalitiesAsync(region.Code);
            SetPickerItems(CityPicker, _localities.Select(LocalityDisplayName));
            LocationStatusLabel.Text = _localities.Count == 0 ? "No cities or municipalities were returned." : "";
        }
        catch (Exception ex)
        {
            LocationStatusLabel.Text = $"Unable to load cities or municipalities. {ex.Message}";
        }
        finally
        {
            _loadingLocations = false;
        }
    }

    private async void OnCityChanged(object sender, EventArgs e)
    {
        if (_updatingPickers)
        {
            return;
        }

        var locality = SelectedLocality();
        if (locality is null)
        {
            return;
        }

        ProvinceLabel.Text = string.IsNullOrWhiteSpace(locality.Province) ? "" : $"Province: {locality.Province}";
        _loadingLocations = true;
        LocationStatusLabel.Text = "Loading barangays...";
        _barangays = [];
        ResetPicker(BarangayPicker, "Select barangay");

        try
        {
            _barangays = await BikeMateDatabaseService.GetPhilippineBarangaysAsync(locality.Code);
            SetPickerItems(BarangayPicker, _barangays.Select(barangay => barangay.Name));
            LocationStatusLabel.Text = _barangays.Count == 0 ? "No barangays were returned." : "";
        }
        catch (Exception ex)
        {
            LocationStatusLabel.Text = $"Unable to load barangays. {ex.Message}";
        }
        finally
        {
            _loadingLocations = false;
        }
    }

    private async Task LoadApplicationsAsync()
    {
        try
        {
            Applications.Clear();
            foreach (var application in await BikeMateDatabaseService.GetMechanicApplicationsAsync())
            {
                Applications.Add(MechanicApplicationRow.FromApi(application));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Mechanic Applications", $"Unable to load mechanic applications: {ex.Message}", "OK");
        }
    }

    private async void Submit_Clicked(object sender, EventArgs e)
    {
        if (!TryBuildRequest(out var request, out var message))
        {
            await DisplayAlert("Check Mechanic Details", message, "OK");
            return;
        }

        SubmitButton.IsEnabled = false;
        SubmitButton.Text = "Creating...";

        try
        {
            var created = await BikeMateDatabaseService.CreateMechanicApplicationAsync(request);
            OtpEmailEntry.Text = created.Email;
            OtpEntry.Text = string.Empty;
            OtpStatusLabel.Text = $"OTP sent to {created.Email}. Verify it before BikeMate admin approval.";
            await LoadApplicationsAsync();
            ClearForm(keepOtpEmail: created.Email);
            OtpEntry.Focus();
            await DisplayAlert(
                "OTP Sent",
                "The mechanic account details were saved, but it is not approval-ready yet. Enter the email OTP to send it for BikeMate admin review.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Submission Failed", ex.Message, "OK");
        }
        finally
        {
            SubmitButton.Text = "Create Account and Send OTP";
            SubmitButton.IsEnabled = true;
        }
    }

    private async void PickProfile_Clicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Choose mechanic profile photo"
            });

            if (photo is null)
            {
                return;
            }

            var uploaded = await BikeMateDatabaseService.UploadShopFileAsync(photo, "mechanic-profile");
            _profileImageUrl = uploaded.Url;
            UpdateFileStatus();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Profile Upload", ex.Message, "OK");
        }
    }

    private async void PickValidId_Clicked(object sender, EventArgs e)
    {
        await PickAndUploadDocumentAsync("Select mechanic valid ID", "mechanic-valid-ids", url => _validIdImageUrl = url);
    }

    private async void PickCertification_Clicked(object sender, EventArgs e)
    {
        await PickAndUploadDocumentAsync("Select mechanic license or certification", "mechanic-certifications", url => _certificationImageUrl = url);
    }

    private async Task PickAndUploadDocumentAsync(string title, string folder, Action<string> saveUrl)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = title
            });

            if (file is null)
            {
                return;
            }

            FileStatusLabel.Text = $"Uploading {file.FileName}...";
            var uploaded = await BikeMateDatabaseService.UploadShopFileAsync(file, folder);
            saveUrl(uploaded.Url);
            UpdateFileStatus();
        }
        catch (Exception ex)
        {
            UpdateFileStatus();
            await DisplayAlert("Document Upload", ex.Message, "OK");
        }
    }

    private async void VerifyOtp_Clicked(object sender, EventArgs e)
    {
        var email = OtpEmailEntry.Text?.Trim() ?? string.Empty;
        var otp = OtpEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
        {
            await DisplayAlert("OTP Required", "Enter the mechanic email and OTP code.", "OK");
            return;
        }

        try
        {
            await BikeMateDatabaseService.VerifyEmailOtpAsync(email, otp);
            OtpEntry.Text = string.Empty;
            OtpStatusLabel.Text = $"{email} is verified and ready for BikeMate admin approval.";
            await LoadApplicationsAsync();
            await DisplayAlert("Email Verified", "The mechanic can now appear in the BikeMate web-admin approval queue for document review.", "OK");
        }
        catch (Exception ex)
        {
            OtpStatusLabel.Text = "OTP verification failed. Check the latest code or resend a new one.";
            await DisplayAlert("OTP Failed", ex.Message, "OK");
        }
    }

    private async void ResendOtp_Clicked(object sender, EventArgs e)
    {
        var email = OtpEmailEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Email Required", "Enter the mechanic email address first.", "OK");
            return;
        }

        try
        {
            await BikeMateDatabaseService.ResendEmailOtpAsync(email);
            OtpEntry.Text = string.Empty;
            OtpStatusLabel.Text = $"A new OTP was sent to {email}. Use the latest code.";
            await DisplayAlert("OTP Sent", "A new verification code was sent to the mechanic email.", "OK");
        }
        catch (Exception ex)
        {
            OtpStatusLabel.Text = "Unable to resend OTP. Check the email and API connection.";
            await DisplayAlert("OTP Failed", ex.Message, "OK");
        }
    }

    private async void Refresh_Clicked(object sender, EventArgs e)
    {
        await LoadApplicationsAsync();
    }

    private void PhoneEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_formattingPhoneNumber)
        {
            return;
        }

        var digits = new string((e.NewTextValue ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.StartsWith("63", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        if (digits.StartsWith("0", StringComparison.Ordinal))
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
            PhoneEntry.Text = digits;
            _formattingPhoneNumber = false;
        }
    }

    private bool TryBuildRequest(out CreateAdminMechanicApplication request, out string message)
    {
        request = new CreateAdminMechanicApplication(string.Empty, null, string.Empty, null, null, string.Empty, string.Empty, string.Empty, null, null, null, null, null, string.Empty, string.Empty, null, null, null);
        message = string.Empty;

        var firstName = FirstNameEntry.Text?.Trim() ?? string.Empty;
        var middleName = MiddleNameEntry.Text?.Trim();
        var lastName = LastNameEntry.Text?.Trim() ?? string.Empty;
        var sex = SexPicker.SelectedItem?.ToString();
        var email = EmailEntry.Text?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;
        var confirmPassword = ConfirmPasswordEntry.Text ?? string.Empty;
        var address = AddressEntry.Text?.Trim() ?? string.Empty;
        var region = SelectedRegion();
        var locality = SelectedLocality();
        var barangay = SelectedBarangay();
        var zipCode = ZipCodeEntry.Text?.Trim() ?? string.Empty;
        var bio = BioEditor.Text?.Trim();
        var birthdate = (BirthdatePicker.Date ?? DateTime.Today).Date;

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(sex))
        {
            message = "Enter first name, last name, and sex.";
            return false;
        }

        if (CalculateAge(birthdate, DateTime.Today) < 18)
        {
            message = "Mechanic accounts require an owner/technician who is at least 18 years old.";
            return false;
        }

        if (!email.Contains('@') || !email.Contains('.'))
        {
            message = "Enter a valid email address with @ and a domain.";
            return false;
        }

        if (!TryNormalizePhilippineMobile(PhoneEntry.Text, out var phoneNumber))
        {
            message = "Enter the 10 Philippine mobile digits after +63, for example 9171234567.";
            return false;
        }

        if (password.Length <= 8)
        {
            message = "Password must be more than 8 characters.";
            return false;
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            message = "Password and confirm password do not match.";
            return false;
        }

        if (region is null ||
            locality is null ||
            barangay is null ||
            string.IsNullOrWhiteSpace(address) ||
            zipCode.Length != 4 ||
            !zipCode.All(char.IsDigit))
        {
            message = "Complete the mechanic address using region, city or municipality, barangay, address, and 4-digit ZIP code.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_validIdImageUrl) || string.IsNullOrWhiteSpace(_certificationImageUrl))
        {
            message = "Upload the mechanic valid ID and license/certification file.";
            return false;
        }

        if (!TermsCheckBox.IsChecked)
        {
            message = "Confirm the mechanic application terms before submitting.";
            return false;
        }

        int? yearsExperience = null;
        if (!string.IsNullOrWhiteSpace(YearsEntry.Text))
        {
            if (!int.TryParse(YearsEntry.Text, out var parsedYears) || parsedYears < 0 || parsedYears > 80)
            {
                message = "Years of experience must be between 0 and 80.";
                return false;
            }

            yearsExperience = parsedYears;
        }

        PhoneEntry.Text = phoneNumber;
        var province = string.IsNullOrWhiteSpace(locality.Province) ? region.Name : locality.Province;
        request = new CreateAdminMechanicApplication(
            firstName,
            string.IsNullOrWhiteSpace(middleName) ? null : middleName,
            lastName,
            sex,
            birthdate.ToString("yyyy-MM-dd"),
            email,
            phoneNumber,
            password,
            address,
            barangay.Name,
            locality.Name,
            province,
            zipCode,
            _validIdImageUrl,
            _certificationImageUrl,
            _profileImageUrl,
            bio,
            yearsExperience);
        return true;
    }

    private void ClearForm(string keepOtpEmail)
    {
        FirstNameEntry.Text = string.Empty;
        MiddleNameEntry.Text = string.Empty;
        LastNameEntry.Text = string.Empty;
        SexPicker.SelectedItem = null;
        BirthdatePicker.Date = BirthdatePicker.MaximumDate;
        EmailEntry.Text = string.Empty;
        PhoneEntry.Text = string.Empty;
        PasswordEntry.Text = string.Empty;
        ConfirmPasswordEntry.Text = string.Empty;
        AddressEntry.Text = string.Empty;
        ProvinceLabel.Text = string.Empty;
        ClearPickerSelection(RegionPicker);
        ResetPicker(CityPicker, "Select city or municipality");
        ResetPicker(BarangayPicker, "Select barangay");
        _localities = [];
        _barangays = [];
        ZipCodeEntry.Text = string.Empty;
        BioEditor.Text = string.Empty;
        YearsEntry.Text = string.Empty;
        TermsCheckBox.IsChecked = false;
        _profileImageUrl = null;
        _validIdImageUrl = null;
        _certificationImageUrl = null;
        OtpEmailEntry.Text = keepOtpEmail;
        OtpStatusLabel.Text = string.IsNullOrWhiteSpace(keepOtpEmail)
            ? "No mechanic OTP is pending."
            : $"OTP sent to {keepOtpEmail}. Verify it before BikeMate admin approval.";
        UpdateFileStatus();
    }

    private PhilippineRegion? SelectedRegion()
    {
        return RegionPicker.SelectedIndex >= 0 && RegionPicker.SelectedIndex < _regions.Count
            ? _regions[RegionPicker.SelectedIndex]
            : null;
    }

    private PhilippineLocality? SelectedLocality()
    {
        return CityPicker.SelectedIndex >= 0 && CityPicker.SelectedIndex < _localities.Count
            ? _localities[CityPicker.SelectedIndex]
            : null;
    }

    private PhilippineBarangay? SelectedBarangay()
    {
        return BarangayPicker.SelectedIndex >= 0 && BarangayPicker.SelectedIndex < _barangays.Count
            ? _barangays[BarangayPicker.SelectedIndex]
            : null;
    }

    private static string LocalityDisplayName(PhilippineLocality locality)
    {
        return string.IsNullOrWhiteSpace(locality.Province)
            ? locality.Name
            : $"{locality.Name}, {locality.Province}";
    }

    private void SetPickerItems(Picker picker, IEnumerable<string> items)
    {
        _updatingPickers = true;
        picker.Items.Clear();
        foreach (var item in items)
        {
            picker.Items.Add(item);
        }
        picker.SelectedIndex = -1;
        _updatingPickers = false;
    }

    private void ResetPicker(Picker picker, string title)
    {
        _updatingPickers = true;
        picker.Items.Clear();
        picker.Title = title;
        picker.SelectedIndex = -1;
        _updatingPickers = false;
    }

    private void ClearPickerSelection(Picker picker)
    {
        _updatingPickers = true;
        picker.SelectedIndex = -1;
        _updatingPickers = false;
    }

    private void UpdateFileStatus()
    {
        var files = new List<string>();
        if (!string.IsNullOrWhiteSpace(_profileImageUrl))
        {
            files.Add("profile photo");
        }

        if (!string.IsNullOrWhiteSpace(_validIdImageUrl))
        {
            files.Add("valid ID");
        }

        if (!string.IsNullOrWhiteSpace(_certificationImageUrl))
        {
            files.Add("license/certification");
        }

        FileStatusLabel.Text = files.Count == 0
            ? "No files uploaded yet."
            : $"Uploaded: {string.Join(", ", files)}.";
    }

    private static bool TryNormalizePhilippineMobile(string? value, out string phoneNumber)
    {
        var clean = (value ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty);

        if (clean.StartsWith("9", StringComparison.Ordinal) &&
            clean.Length == 10 &&
            clean.All(char.IsDigit))
        {
            phoneNumber = "+63" + clean;
            return true;
        }

        if (clean.StartsWith("09", StringComparison.Ordinal) &&
            clean.Length == 11 &&
            clean.All(char.IsDigit))
        {
            phoneNumber = "+63" + clean[1..];
            return true;
        }

        if (clean.StartsWith("639", StringComparison.Ordinal) &&
            clean.Length == 12 &&
            clean.All(char.IsDigit))
        {
            phoneNumber = "+" + clean;
            return true;
        }

        if (clean.StartsWith("+639", StringComparison.Ordinal) &&
            clean.Length == 13 &&
            clean.Skip(1).All(char.IsDigit))
        {
            phoneNumber = clean;
            return true;
        }

        phoneNumber = string.Empty;
        return false;
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

public sealed record MechanicApplicationRow(
    string FullName,
    string Email,
    string ShopName,
    string StatusText,
    Color StatusColor,
    string OtpText,
    string ApprovalText,
    string SubmittedText)
{
    public static MechanicApplicationRow FromApi(AdminMechanicApplication application)
    {
        var approved = application.IsVerified &&
            application.IsAssignedToShop &&
            string.Equals(application.AccountStatus, "active", StringComparison.OrdinalIgnoreCase);

        var waiting = application.EmailVerified
            ? "Visible in web-admin approval queue"
            : "Verify OTP before admin can review";

        return new MechanicApplicationRow(
            application.FullName,
            application.Email,
            string.IsNullOrWhiteSpace(application.ShopName) ? "Current shop" : application.ShopName!,
            approved ? "APPROVED" : application.EmailVerified ? "READY FOR REVIEW" : "OTP REQUIRED",
            approved ? Color.FromArgb("#16A34A") : application.EmailVerified ? Color.FromArgb("#F97316") : Color.FromArgb("#DC2626"),
            application.EmailVerified ? "OTP verified" : "OTP pending",
            approved ? "Can receive jobs" : waiting,
            $"Submitted {application.CreatedAt.ToLocalTime():MMM dd, yyyy h:mm tt}");
    }
}
