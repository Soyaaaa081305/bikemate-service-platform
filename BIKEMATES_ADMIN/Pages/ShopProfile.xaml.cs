using BIKEMATES_ADMIN.Pages.Account;
using BIKEMATES_ADMIN.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;

namespace BIKEMATES_ADMIN.Pages;

public partial class ShopProfile : ContentPage
{
    private AdminShopProfile? _profile;
    private AccountCreationDraft? _applicationDraft;
    private bool _loaded;

    public ShopProfile()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded)
        {
            _loaded = true;
            await LoadProfileAsync();
        }
    }

    private async Task LoadProfileAsync()
    {
        try
        {
            _profile = await BikeMateDatabaseService.GetShopProfileAsync();
            ApplyProfile(_profile);
            _applicationDraft = await LoadApplicationDraftAsync();
            ApplyApplicationSummary(_applicationDraft);
            ApplyApplicationSnapshot(_applicationDraft);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Shop Profile", $"Unable to load profile from API: {ex.Message}", "OK");
        }
    }

    private void ApplyProfile(AdminShopProfile profile)
    {
        ShopTitleLabel.Text = profile.ShopName;
        ShopStatusLabel.Text = $"Status: {profile.ShopStatus}";
        ShopLocationLabel.Text = string.Join(", ", new[] { profile.AddressLine, profile.City, profile.Province }.Where(x => !string.IsNullOrWhiteSpace(x)));
        ShopNameEntry.Text = profile.ShopName;
        ShopDescriptionEditor.Text = profile.ShopDescription;
        ShopAddressEntry.Text = profile.AddressLine;
        ShopCityEntry.Text = profile.City;
        ShopProvinceEntry.Text = profile.Province;
        ContactNumberEntry.Text = profile.ContactNumber;

        if (Uri.TryCreate(profile.ShopImageUrl, UriKind.Absolute, out var imageUri))
        {
            ShopCoverImage.Source = ImageSource.FromUri(imageUri);
            ShopCoverPlaceholder.IsVisible = false;
        }
        else
        {
            ShopCoverImage.Source = null;
            ShopCoverPlaceholder.IsVisible = true;
        }

        if (Uri.TryCreate(profile.ShopLogoUrl, UriKind.Absolute, out var logoUri))
        {
            ShopLogoImage.Source = ImageSource.FromUri(logoUri);
            ShopLogoPlaceholder.IsVisible = false;
        }
        else
        {
            ShopLogoImage.Source = null;
            ShopLogoPlaceholder.IsVisible = true;
        }
    }

    private void ApplyApplicationSummary(AccountCreationDraft? draft)
    {
        if (draft is null)
        {
            ApplicationSummaryLabel.Text = "Open the locked application record submitted to BikeMate admin review.";
            return;
        }

        var status = string.IsNullOrWhiteSpace(draft.ApplicationStatus) ? "under review" : draft.ApplicationStatus;
        var otp = draft.EmailVerified ? "email verified" : "email OTP pending";
        ApplicationSummaryLabel.Text = $"{draft.ShopName} is {status}. Documents and owner details are view-only; {otp}.";
    }

    private async Task<AccountCreationDraft?> LoadApplicationDraftAsync()
    {
        try
        {
            return await BikeMateDatabaseService.GetSubmittedShopApplicationFromApiAsync();
        }
        catch
        {
            return BikeMateDatabaseService.TryGetSubmittedShopApplication(AppSession.CurrentUser?.Email);
        }
    }

    private void ApplyApplicationSnapshot(AccountCreationDraft? draft)
    {
        ApplicationSnapshotStack.Clear();

        if (draft is null)
        {
            ApplicationSnapshotStack.Add(MutedLabel("No submitted application details are available on this device yet. Reload after logging in with the shop account."));
            return;
        }

        ApplicationSnapshotStack.Add(SnapshotSection(
            "Review status",
            DetailRow("Status", Fallback(draft.ApplicationStatus)),
            DetailRow("Submitted", FormatDateTime(draft.SubmittedAt)),
            DetailRow("Last updated", FormatDateTime(draft.UpdatedAt)),
            DetailRow("Email OTP", draft.EmailVerified ? "Verified" : "Pending")));

        ApplicationSnapshotStack.Add(SnapshotSection(
            "Owner details",
            DetailRow("Full name", Fallback(FullName(draft))),
            DetailRow("First name", Fallback(draft.FirstName)),
            DetailRow("Middle name", Fallback(draft.MiddleName)),
            DetailRow("Last name", Fallback(draft.LastName)),
            DetailRow("Sex", Fallback(draft.Sex)),
            DetailRow("Birthdate", FormatDate(draft.Birthdate)),
            DetailRow("Email", Fallback(draft.Email)),
            DetailRow("Mobile", Fallback(draft.PhoneNumber))));

        ApplicationSnapshotStack.Add(SnapshotSection(
            "Owner address",
            DetailRow("Complete address", Fallback(draft.Address)),
            DetailRow("Barangay", Fallback(draft.Barangay)),
            DetailRow("City / Municipality", Fallback(draft.City)),
            DetailRow("Province / Region", Fallback(draft.Province)),
            DetailRow("Zip code", Fallback(draft.ZipCode))));

        ApplicationSnapshotStack.Add(SnapshotSection(
            "Shop details",
            DetailRow("Official shop name", Fallback(draft.ShopName)),
            DetailRow("Description", Fallback(draft.ShopDescription)),
            DetailRow("DTI registration", Fallback(draft.DtiRegistrationNumber)),
            DetailRow("Complete address", Fallback(draft.ShopAddress)),
            DetailRow("Barangay", Fallback(draft.ShopBarangay)),
            DetailRow("City / Municipality", Fallback(draft.ShopCity)),
            DetailRow("Province / Region", Fallback(draft.ShopProvince)),
            DetailRow("Zip code", Fallback(draft.ShopZipCode))));

        ApplicationSnapshotStack.Add(SnapshotSection(
            "Submitted files",
            FileRow("Owner valid ID", draft.ValidIdPath),
            FileRow("Business permit", draft.BusinessPermitPath),
            FileRow("Cover photo / shop image", draft.ShopImagePath)));
    }

    private async void ApplicationDetails_Clicked(object? sender, EventArgs e)
    {
        try
        {
            var draft = await LoadApplicationDraftAsync();
            if (draft is null)
            {
                await DisplayAlertAsync("Application Details", "No submitted application details were found for this account.", "OK");
                return;
            }

            _applicationDraft = draft;
            ApplyApplicationSummary(draft);
            ApplyApplicationSnapshot(draft);
            await Navigation.PushAsync(new ShopApplicationReviewPage(draft));
        }
        catch (Exception ex)
        {
            var fallback = BikeMateDatabaseService.TryGetSubmittedShopApplication(AppSession.CurrentUser?.Email);
            if (fallback is not null)
            {
                ApplyApplicationSummary(fallback);
                await Navigation.PushAsync(new ShopApplicationReviewPage(fallback));
                return;
            }

            await DisplayAlertAsync("Application Details", $"Unable to load the submitted application: {ex.Message}", "OK");
        }
    }

    private async void SaveProfile_Clicked(object? sender, EventArgs e)
    {
        var openEmail = await DisplayAlertAsync(
            "Request Changes",
            "Registered shop details and submitted documents are locked after submission. Email BikeMate admin to request corrections or submit updated documents.",
            "Open Email",
            "Cancel");

        if (!openEmail)
        {
            return;
        }

        await OpenChangeRequestEmailAsync();
    }

    private async void Reload_Clicked(object? sender, EventArgs e) => await LoadProfileAsync();

    private static View SnapshotSection(string title, params View[] rows)
    {
        var stack = new VerticalStackLayout { Spacing = 7 };
        stack.Add(new Label
        {
            Text = title,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            FontFamily = "PTSansCaptionBold",
            TextColor = Color.FromArgb("#242424")
        });

        foreach (var row in rows)
        {
            stack.Add(row);
        }

        return new Border
        {
            BackgroundColor = Color.FromArgb("#FAFAFA"),
            Stroke = Color.FromArgb("#E5E7EB"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = 12,
            Content = stack
        };
    }

    private static View DetailRow(string label, string value)
    {
        return new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(0.42, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(0.58, GridUnitType.Star))
            },
            ColumnSpacing = 10,
            Children =
            {
                new Label
                {
                    Text = label,
                    FontSize = 11,
                    FontFamily = "PublicSans",
                    TextColor = Color.FromArgb("#6B7280")
                },
                new Label
                {
                    Text = value,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    FontFamily = "PublicSans",
                    TextColor = Color.FromArgb("#242424"),
                    HorizontalTextAlignment = TextAlignment.End,
                    LineBreakMode = LineBreakMode.WordWrap
                }.Column(1)
            }
        };
    }

    private static View FileRow(string label, string? url)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(DetailRow(label, FileNameOrValue(url)));

        if (IsImage(url))
        {
            stack.Add(new Image
            {
                Source = ImageSource.FromUri(new Uri(url!)),
                HeightRequest = 120,
                Aspect = Aspect.AspectFill,
                BackgroundColor = Color.FromArgb("#F3F4F6")
            });
        }

        var button = new Button
        {
            Text = string.IsNullOrWhiteSpace(url) ? "No file submitted" : "Open file",
            Style = (Style)Application.Current!.Resources[string.IsNullOrWhiteSpace(url) ? "OutlineButton" : "PrimaryButton"],
            IsEnabled = !string.IsNullOrWhiteSpace(url)
        };
        button.Clicked += async (_, _) => await OpenFileAsync(url);
        stack.Add(button);

        return stack;
    }

    private static Label MutedLabel(string text)
    {
        return new Label
        {
            Text = text,
            FontSize = 13,
            FontFamily = "PublicSans",
            TextColor = Color.FromArgb("#6B7280"),
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private static async Task OpenFileAsync(string? url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            await Launcher.Default.OpenAsync(uri);
        }
    }

    private async Task OpenChangeRequestEmailAsync()
    {
        const string supportEmail = "bikemate@gmail.com";
        var subject = "BikeMate Shop Change Request";
        var body = string.Join(Environment.NewLine, new[]
        {
            "Hello BikeMate Admin,",
            string.Empty,
            "I would like to request changes to my submitted shop application or registered shop details.",
            string.Empty,
            $"Shop: {_profile?.ShopName ?? _applicationDraft?.ShopName ?? "Not loaded"}",
            $"Account email: {AppSession.CurrentUser?.Email ?? _applicationDraft?.Email ?? "Not loaded"}",
            $"Current status: {_profile?.ShopStatus ?? _applicationDraft?.ApplicationStatus ?? "Not loaded"}",
            string.Empty,
            "Requested changes:",
            "- ",
            string.Empty,
            "Thank you."
        });

        var mailto = new Uri($"mailto:{supportEmail}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}");

        try
        {
            if (await Launcher.Default.CanOpenAsync(mailto))
            {
                await Launcher.Default.OpenAsync(mailto);
                return;
            }
        }
        catch
        {
            // Fall through to the manual fallback message below.
        }

        await DisplayAlertAsync(
            "Email BikeMate Admin",
            $"Your device could not open an email app. Please email {supportEmail} and include your shop name, account email, and requested changes.",
            "OK");
    }

    private static string FullName(AccountCreationDraft draft)
    {
        return string.Join(" ", new[] { draft.FirstName, draft.MiddleName, draft.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static bool IsImage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            return false;
        }

        var clean = value.Split('?', '#')[0].ToLowerInvariant();
        return clean.EndsWith(".jpg") ||
            clean.EndsWith(".jpeg") ||
            clean.EndsWith(".png") ||
            clean.EndsWith(".webp");
    }

    private static string FileNameOrValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Not submitted";
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? System.IO.Path.GetFileName(uri.LocalPath)
            : System.IO.Path.GetFileName(value);
    }

    private static string FormatDate(string? value)
    {
        return DateTime.TryParse(value, out var date)
            ? date.ToString("MMM dd, yyyy")
            : Fallback(value);
    }

    private static string FormatDateTime(string? value)
    {
        return DateTime.TryParse(value, out var date)
            ? date.ToLocalTime().ToString("MMM dd, yyyy h:mm tt")
            : Fallback(value);
    }

    private static string Fallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Not submitted" : value.Trim();
    }
}
