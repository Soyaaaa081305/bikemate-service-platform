using System.Globalization;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using BikeMate.Services;
using BikeMate.Views.Customer;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Storage;

namespace BikeMate.Views.Auth;

public sealed class GoogleAccountSetupPage : CustomerPageBase
{
    private CustomerMeDto? _customer;
    private CustomerAddressDto? _address;
    private Entry? _phoneEntry;
    private Picker? _sexPicker;
    private DatePicker? _birthdatePicker;
    private Entry? _provinceEntry;
    private Entry? _cityEntry;
    private Entry? _barangayEntry;
    private Editor? _addressEditor;
    private string? _validIdUrl;
    private string? _validIdFileName;
    private bool _isBusy;

    public GoogleAccountSetupPage()
    {
        Title = "Google Account Setup";
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

    private async Task LoadAsync()
    {
        _isBusy = true;
        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            _address = _customer.Addresses.FirstOrDefault(item => item.IsDefault) ?? _customer.Addresses.FirstOrDefault();
            _validIdUrl = _customer.ValidIdImageUrl;
            Render();
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
        PrepareFields();

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 8, 16, 24),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };

        body.Add(Header("Account Setup", false));
        body.Add(HeroCard());

        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(ProfileCard());
        body.Add(AddressCard());
        body.Add(IdentityCard());

        body.Add(OrangeButton(_isBusy ? "Submitting..." : "Submit for review", new Command(async () => await SubmitAsync())));
        body.Add(GhostButton("Skip for now", new Command(async () => await SkipAsync())));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private void PrepareFields()
    {
        _phoneEntry ??= Field(_customer?.PhoneNumber, "+63 mobile number", Keyboard.Telephone);
        _phoneEntry.MaxLength = 13;

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

        _provinceEntry ??= Field(_address?.Province, "Province");
        _cityEntry ??= Field(_address?.City, "City or municipality");
        _barangayEntry ??= Field(_address?.Barangay, "Barangay");
        _addressEditor ??= EditorField(_address?.AddressLine, "House number, street, subdivision, landmark");
    }

    private View HeroCard()
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(Label("Finish your BikeMate profile", 18, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(Label(
            "Your Google email is already verified. Add a few details now so BikeMate admin can review and approve your customer account.",
            11,
            CustomerUi.Muted));
        stack.Add(BadgeRow("Approval status", FormatApprovalStatus(_customer?.AccountStatus ?? "pending")));
        return Card(stack, Colors.White, 8, new Thickness(14));
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
        stack.Add(TwoColumnInputs(("Province", _provinceEntry!), ("City", _cityEntry!)));
        stack.Add(InputBlock("Barangay", _barangayEntry!));
        stack.Add(InputBlock("Complete address", _addressEditor!));
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
        stack.Add(GhostButton("Upload valid ID", new Command(async () => await UploadValidIdAsync())));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private async Task UploadValidIdAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Upload valid ID",
                FileTypes = FilePickerFileType.Images
            });
            if (file is null)
            {
                return;
            }

            _isBusy = true;
            Render("Uploading valid ID...");
            var upload = await CustomerApiClient.UploadFileAsync(file, "customer-id");
            await CustomerApiClient.UpdateCustomerValidIdAsync(upload.Url);
            _validIdUrl = upload.Url;
            _validIdFileName = file.FileName;
            Render("Valid ID uploaded. Finish the remaining details and submit for review.");
        }
        catch (Exception ex)
        {
            Render($"Valid ID was not uploaded. {ex.Message}");
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task SubmitAsync()
    {
        if (_customer is null || _isBusy)
        {
            return;
        }

        var phone = Clean(_phoneEntry?.Text);
        var sex = _sexPicker?.SelectedItem?.ToString();
        DateTime? birthdate = _birthdatePicker?.Date;
        birthdate = birthdate?.Date;
        var province = Clean(_provinceEntry?.Text);
        var city = Clean(_cityEntry?.Text);
        var barangay = Clean(_barangayEntry?.Text);
        var addressLine = Clean(_addressEditor?.Text);

        if (phone is null || addressLine is null || city is null || province is null)
        {
            await DisplayAlertAsync(
                "Setup details needed",
                "Please add your mobile number, complete address, city, and province before submitting for admin review.",
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
                barangay,
                city,
                province,
                _address?.PostalCode,
                _address?.Latitude,
                _address?.Longitude,
                true));

            await DisplayAlertAsync(
                "Submitted for review",
                string.IsNullOrWhiteSpace(_validIdUrl)
                    ? "Your Google account setup was saved. Upload a valid ID from Profile so BikeMate admin can approve the account."
                    : "Your Google account setup was saved. BikeMate admin can now review your customer account.",
                "OK");

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
        await DisplayAlertAsync(
            "Setup skipped",
            "You can continue browsing BikeMate, but booking approval requires profile setup and BikeMate admin approval. Finish it anytime from Profile.",
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

    private static string FullName(CustomerMeDto customer)
    {
        return string.Join(" ", new[] { customer.FirstName, customer.MiddleName, customer.LastName }
            .Where(item => !string.IsNullOrWhiteSpace(item)));
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
