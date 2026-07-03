using BIKEMATES_ADMIN.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;

namespace BIKEMATES_ADMIN.Pages.Account;

public sealed class ShopApplicationReviewPage : ContentPage
{
    private readonly AccountCreationDraft _draft;

    public ShopApplicationReviewPage(AccountCreationDraft draft)
    {
        _draft = draft;
        Title = "Application Details";
        BackgroundColor = Colors.White;
        Content = BuildContent();
    }

    private View BuildContent()
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };

        root.Add(Header(), 0, 0);

        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(20, 20, 20, 28),
            Spacing = 14
        };

        stack.Add(NoticeCard());
        stack.Add(Section(
            "Owner account",
            Detail("Full name", FullName()),
            Detail("First name", _draft.FirstName),
            Detail("Middle name", _draft.MiddleName),
            Detail("Last name", _draft.LastName),
            Detail("Sex", _draft.Sex),
            Detail("Birthdate", FormatDate(_draft.Birthdate)),
            Detail("Email address", _draft.Email),
            Detail("Mobile number", _draft.PhoneNumber)));

        stack.Add(Section(
            "Owner address",
            Detail("Region / Province", _draft.Province),
            Detail("City / Municipality", _draft.City),
            Detail("Barangay", _draft.Barangay),
            Detail("Complete address", _draft.Address),
            Detail("Zip code", _draft.ZipCode)));

        stack.Add(Section(
            "Shop information",
            Detail("Official shop name", _draft.ShopName),
            Detail("Description", _draft.ShopDescription),
            Detail("DTI registration number", _draft.DtiRegistrationNumber),
            Detail("Region / Province", _draft.ShopProvince),
            Detail("City / Municipality", _draft.ShopCity),
            Detail("Barangay", _draft.ShopBarangay),
            Detail("Complete address", _draft.ShopAddress),
            Detail("Zip code", _draft.ShopZipCode)));

        stack.Add(Section(
            "Submitted files",
            FileDetail("Owner valid ID", _draft.ValidIdPath),
            FileDetail("Business permit", _draft.BusinessPermitPath),
            FileDetail("Cover photo / shop image", _draft.ShopImagePath)));

        stack.Add(Section(
            "Terms and review",
            Detail("Application status", _draft.ApplicationStatus),
            Detail("Email OTP", _draft.EmailVerified ? "Verified" : "Not verified yet"),
            Detail("Submitted", FormatDateTime(_draft.SubmittedAt)),
            Detail("Last updated", FormatDateTime(_draft.UpdatedAt)),
            Detail("Terms accepted", _draft.ShopTermsAccepted ? "Yes" : "No"),
            Detail("Editing status", "Locked for admin review"),
            Detail("How to change details", "Request BikeMate admin permission or resubmit if the admin asks for corrections.")));

        root.Add(new ScrollView { Content = stack }, 0, 1);
        return root;
    }

    private View Header()
    {
        var backButton = HeaderButton("<", true);
        backButton.Clicked += async (_, _) => await GoBackAsync();

        var homeButton = HeaderButton("Home");
        homeButton.Clicked += async (_, _) => await GoHomeAsync();

        return new Border
        {
            BackgroundColor = Color.FromArgb("#242424"),
            StrokeThickness = 0,
            Padding = new Thickness(12, 18, 12, 16),
            Content = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10,
                Children =
                {
                    backButton,
                    new VerticalStackLayout
                    {
                        Spacing = 2,
                        HorizontalOptions = LayoutOptions.Center,
                        Children =
                        {
                            new Label
                            {
                                Text = "Submitted Application",
                                FontSize = 18,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Colors.White,
                                HorizontalTextAlignment = TextAlignment.Center
                            },
                            new Label
                            {
                                Text = "Read-only details under BikeMate admin review",
                                FontSize = 13,
                                TextColor = Color.FromArgb("#E6E6E6"),
                                HorizontalTextAlignment = TextAlignment.Center
                            }
                        }
                    }.Column(1),
                    homeButton.Column(2)
                }
            }
        };
    }

    private static Button HeaderButton(string text, bool compact = false)
    {
        return new Button
        {
            Text = text,
            BackgroundColor = compact ? Color.FromArgb("#FFF3E8") : Color.FromArgb("#333333"),
            TextColor = compact ? Color.FromArgb("#1F2937") : Colors.White,
            FontSize = compact ? 18 : 13,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            Padding = compact ? new Thickness(0) : new Thickness(12, 0),
            HeightRequest = compact ? 40 : 38,
            WidthRequest = compact ? 40 : 64,
            MinimumWidthRequest = compact ? 40 : 64
        };
    }

    private async Task GoBackAsync()
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
            return;
        }

        await GoHomeAsync();
    }

    private async Task GoHomeAsync()
    {
        if (!string.IsNullOrWhiteSpace(AppSession.AccessToken) && AppSession.CurrentUser is not null)
        {
            if (Application.Current?.Windows.FirstOrDefault()?.Page is Shell shell)
            {
                await shell.GoToAsync("//AdminTabs/Home");
                return;
            }

            BIKEMATES_ADMIN.App.SetRootPage(new AppShell());
            return;
        }

        BIKEMATES_ADMIN.App.SetRootPage(new NavigationPage(new Login()));
    }

    private static View NoticeCard()
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#FFF7F2"),
            Stroke = Color.FromArgb("#FFD1BD"),
            Padding = 14,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = new Label
            {
                Text = "These details are view-only after submission. To correct shop, owner, address, or document information, request permission from BikeMate admin or submit a corrected application if asked.",
                FontSize = 13,
                TextColor = Color.FromArgb("#7A3A12"),
                LineBreakMode = LineBreakMode.WordWrap
            }
        };
    }

    private static View Section(string title, params View[] rows)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Add(new Label
        {
            Text = title,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#242424")
        });

        foreach (var row in rows)
        {
            stack.Add(row);
        }

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E6E6E6"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = 16,
            Content = stack
        };
    }

    private static View Detail(string label, string? value)
    {
        return new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(0.42, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(0.58, GridUnitType.Star))
            },
            ColumnSpacing = 12,
            Children =
            {
                new Label
                {
                    Text = label,
                    FontSize = 11,
                    TextColor = Color.FromArgb("#6E6E6E")
                },
                new Label
                {
                    Text = Fallback(value),
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#242424"),
                    HorizontalTextAlignment = TextAlignment.End,
                    LineBreakMode = LineBreakMode.WordWrap
                }.Column(1)
            }
        };
    }

    private static View FileDetail(string label, string? url)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(Detail(label, FileNameOrValue(url)));

        if (IsImage(url))
        {
            stack.Add(new Image
            {
                Source = ImageSource.FromUri(new Uri(url!)),
                HeightRequest = 150,
                Aspect = Aspect.AspectFill,
                BackgroundColor = Color.FromArgb("#F3F3F3")
            });
        }

        var button = new Button
        {
            Text = string.IsNullOrWhiteSpace(url) ? "No file submitted" : "Open file",
            BackgroundColor = string.IsNullOrWhiteSpace(url) ? Color.FromArgb("#DCDCDC") : Color.FromArgb("#FF6B2C"),
            TextColor = string.IsNullOrWhiteSpace(url) ? Color.FromArgb("#6E6E6E") : Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 42,
            IsEnabled = !string.IsNullOrWhiteSpace(url)
        };
        button.Clicked += async (_, _) => await OpenFileAsync(url);
        stack.Add(button);

        return new Border
        {
            BackgroundColor = Color.FromArgb("#FAFAFA"),
            Stroke = Color.FromArgb("#DCDCDC"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = 12,
            Content = stack
        };
    }

    private string FullName()
    {
        return string.Join(" ", new[] { _draft.FirstName, _draft.MiddleName, _draft.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static async Task OpenFileAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            await Launcher.Default.OpenAsync(uri);
        }
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
            return string.Empty;
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

internal static class ViewGridExtensions
{
    public static T Column<T>(this T view, int column) where T : BindableObject
    {
        Grid.SetColumn(view, column);
        return view;
    }
}
