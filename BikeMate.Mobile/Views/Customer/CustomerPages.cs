using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using BikeMate.Services;
using BikeMate.Views.Auth;
using BikeMate.Views.Customer.Emergency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;

namespace BikeMate.Views.Customer;

internal static class CustomerUi
{
    public static readonly Color Orange = Color.FromArgb("#FF6B00");
    public static readonly Color LightOrange = Color.FromArgb("#FFE1D2");
    public static readonly Color Dark = Color.FromArgb("#242424");
    public static readonly Color Muted = Color.FromArgb("#6E6E6E");
    public static readonly Color Border = Color.FromArgb("#E6E6E6");
    public static readonly Color Page = Color.FromArgb("#F6F6F6");

    public const double CaptionSize = AppTypography.CaptionSize;
    public const double BodySize = AppTypography.BodySize;
    public const double TitleSize = AppTypography.TitleSize;
    public const string FontBody = AppTypography.BodyFont;
    public const string FontDisplay = AppTypography.DisplayFont;
    public const string FontCaption = AppTypography.CaptionFont;
    public const string FontCaptionBold = AppTypography.CaptionBoldFont;

    public const string OnlineBikeRepairImage = "bike_wrench.png";
    public const string HomeIcon = "home_icon.svg";
    public const string ScheduleIcon = "calendar_icon.svg";
    public const string PaymentsIcon = "wallet_icon.svg";
    public const string MessagesIcon = "message_icon.svg";

    public static string FontFor(double size, FontAttributes attributes = FontAttributes.None)
    {
        return AppTypography.FontFor(size, attributes);
    }

    public static double SizeFor(double size)
    {
        return AppTypography.SizeFor(size);
    }

    public static ImageSource Image(string assetOrUrl)
    {
        if (Uri.TryCreate(assetOrUrl, UriKind.Absolute, out var uri))
        {
            return ImageSource.FromUri(uri);
        }

        var logicalAssetName = System.IO.Path.GetFileName(assetOrUrl);
        return string.IsNullOrWhiteSpace(logicalAssetName)
            ? ImageSource.FromFile(assetOrUrl)
            : ImageSource.FromFile(logicalAssetName);
    }

    public static ImageSource PublicOrAssetImage(string? url, string fallbackAsset)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Image(fallbackAsset);
        }

        var publicUrl = ApiConfig.ToPublicUrl(url, fallbackAsset);
        return Uri.TryCreate(publicUrl, UriKind.Absolute, out var uri)
            ? ImageSource.FromUri(uri)
            : Image(publicUrl);
    }
}

public abstract class CustomerPageBase : ContentPage
{
    protected CustomerPageBase()
    {
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetTabBarIsVisible(this, false);
        BackgroundColor = Colors.White;
    }

    protected void SetScaffold(View body, string activeTab, bool showBottomBar = true)
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(showBottomBar ? GridLength.Auto : new GridLength(0))
            }
        };

        root.Add(body, 0, 0);
        if (showBottomBar)
        {
            root.Add(BottomBar(activeTab), 0, 1);
        }

        AppVisualPolish.Apply(root);
        Content = root;
    }

    protected async Task<CustomerMeDto?> EnsureCustomerApprovedForBookingAsync(CustomerMeDto? customer = null)
    {
        try
        {
            customer ??= await CustomerApiClient.GetCustomerAsync();
            BookingDraft.Customer = customer;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Account check failed", $"BikeMate could not verify your account approval status. {ex.Message}", "OK");
            return null;
        }

        if (CustomerRequestRules.IsApproved(customer))
        {
            return customer;
        }

        await DisplayAlertAsync(
            "Account approval required",
            CustomerRequestRules.BookingBlockedMessage(customer),
            "OK");
        return null;
    }

    protected static Label Label(string text, double size, Color color, FontAttributes attributes = FontAttributes.None)
    {
        return new Label
        {
            Text = text,
            FontSize = AppTypography.SizeFor(size),
            TextColor = color,
            FontAttributes = attributes,
            FontFamily = CustomerUi.FontFor(size, attributes),
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    protected static Border Card(View content, Color? background = null, double radius = 8, Thickness? padding = null)
    {
        return new Border
        {
            BackgroundColor = background ?? Colors.White,
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = Math.Min(radius, 8) },
            Padding = padding ?? new Thickness(14),
            Content = content
        };
    }

    protected static Button OrangeButton(string text, ICommand? command = null)
    {
        return new Button
        {
            Text = text,
            Command = command,
            BackgroundColor = CustomerUi.Orange,
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 48,
            MinimumHeightRequest = 48,
            Padding = new Thickness(18, 0),
            FontAttributes = FontAttributes.Bold,
            FontFamily = CustomerUi.FontDisplay,
            FontSize = CustomerUi.BodySize
        };
    }

    protected static Button GhostButton(string text, ICommand? command = null)
    {
        return new Button
        {
            Text = text,
            Command = command,
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            BorderColor = CustomerUi.Border,
            BorderWidth = 1,
            CornerRadius = 8,
            HeightRequest = 46,
            MinimumHeightRequest = 46,
            Padding = new Thickness(16, 0),
            FontFamily = CustomerUi.FontDisplay,
            FontAttributes = FontAttributes.Bold,
            FontSize = CustomerUi.BodySize
        };
    }

    internal static async Task ScheduleBookingRemindersAsync(IEnumerable<ServiceRequestDto> requests)
    {
        try
        {
            var scheduler = Application.Current?.Handler?.MauiContext?.Services.GetService<IBookingReminderService>();
            if (scheduler is not null)
            {
                await scheduler.ScheduleUpcomingBookingRemindersAsync(requests);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Booking reminder scheduling failed: {ex}");
        }
    }

    protected static string FriendlyError(string message, Exception ex)
    {
        Debug.WriteLine($"{message}: {ex}");
        return message;
    }

    protected static View Header(string title, bool back = true, string? right = null, ICommand? rightCommand = null)
    {
        var grid = new Grid
        {
            Padding = new Thickness(16, 18, 16, 8),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Add(new Button
        {
            Text = back ? "<" : string.Empty,
            Command = back ? new Command(async () => await Shell.Current.GoToAsync("..")) : null,
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            FontSize = 13,
            WidthRequest = 44,
            HeightRequest = 40,
            CornerRadius = 8,
            Padding = new Thickness(0)
        }, 0, 0);

        grid.Add(new Label
        {
            Text = title,
            FontSize = 13,
            FontFamily = CustomerUi.FontDisplay,
            TextColor = CustomerUi.Dark,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        }, 1, 0);

        if (!string.IsNullOrWhiteSpace(right))
        {
            var rightWidth = right.Length > 5 ? 106 : 76;
            grid.Add(new Button
            {
                Text = right,
                Command = rightCommand,
                BackgroundColor = Colors.Transparent,
                TextColor = CustomerUi.Orange,
                FontAttributes = FontAttributes.Bold,
                WidthRequest = rightWidth,
                FontSize = 13,
                FontFamily = CustomerUi.FontDisplay
            }, 2, 0);
        }
        else
        {
            grid.Add(new BoxView
            {
                WidthRequest = 44,
                Opacity = 0,
                InputTransparent = true
            }, 2, 0);
        }

        return grid;
    }

    protected static View Avatar(string text, double size = 48, Color? background = null)
    {
        return new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            StrokeShape = new RoundRectangle { CornerRadius = size / 2 },
            BackgroundColor = background ?? Color.FromArgb("#F1F1F1"),
            Stroke = Colors.Transparent,
            Content = new Label
            {
                Text = text,
                TextColor = CustomerUi.Dark,
                FontSize = CustomerUi.TitleSize,
                FontFamily = CustomerUi.FontDisplay,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
    }

    protected static View Separator()
    {
        return new BoxView
        {
            HeightRequest = 1,
            BackgroundColor = CustomerUi.Border,
            HorizontalOptions = LayoutOptions.Fill
        };
    }

    protected static View NoticeCard(string message, Color? accent = null)
    {
        var color = accent ?? CustomerUi.Orange;
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        grid.Add(new BoxView
        {
            WidthRequest = 4,
            CornerRadius = 2,
            Color = color,
            VerticalOptions = LayoutOptions.Fill
        }, 0, 0);
        grid.Add(Label(message, 11, CustomerUi.Muted), 1, 0);
        return Card(grid, Colors.White, 8, new Thickness(12));
    }

    protected static View EmptyState(
        string title,
        string message,
        string? primaryText = null,
        ICommand? primaryCommand = null,
        string? secondaryText = null,
        ICommand? secondaryCommand = null)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 10,
            Padding = new Thickness(4, 6)
        };
        stack.Add(Label(title, 14, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(Label(message, 11, CustomerUi.Muted));

        if (!string.IsNullOrWhiteSpace(primaryText) && primaryCommand is not null)
        {
            stack.Add(OrangeButton(primaryText, primaryCommand));
        }

        if (!string.IsNullOrWhiteSpace(secondaryText) && secondaryCommand is not null)
        {
            stack.Add(GhostButton(secondaryText, secondaryCommand));
        }

        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    protected static View SectionHeader(string title, string? actionText = null, ICommand? actionCommand = null)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        grid.Add(Label(title, 14, CustomerUi.Dark, FontAttributes.Bold), 0, 0);

        if (!string.IsNullOrWhiteSpace(actionText) && actionCommand is not null)
        {
            grid.Add(new Button
            {
                Text = actionText,
                Command = actionCommand,
                BackgroundColor = Colors.Transparent,
                TextColor = CustomerUi.Orange,
                FontAttributes = FontAttributes.Bold,
                FontFamily = CustomerUi.FontDisplay,
                FontSize = 12,
                HeightRequest = 36,
                MinimumHeightRequest = 36,
                Padding = new Thickness(10, 0)
            }, 1, 0);
        }

        return grid;
    }

    protected static View StatusChip(string text, Color? background = null, Color? textColor = null)
    {
        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = background ?? CustomerUi.LightOrange,
            Padding = new Thickness(8, 4),
            Content = Label(text, 10, textColor ?? CustomerUi.Orange, FontAttributes.Bold)
        };
    }

    protected static View Row(string left, string right = "", ICommand? command = null)
    {
        var grid = new Grid
        {
            Padding = new Thickness(0, 10),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Add(Label(left, 13, CustomerUi.Dark), 0, 0);
        grid.Add(Label(right, 13, CustomerUi.Muted), 1, 0);

        if (command is not null)
        {
            grid.GestureRecognizers.Add(new TapGestureRecognizer { Command = command });
        }

        return grid;
    }

    private static View BottomBar(string activeTab)
    {
        var grid = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(8, 6, 8, 8),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };

        AddNav(grid, 0, "Home", CustomerUi.HomeIcon, "CustomerHomePage", activeTab == "Home");
        AddNav(grid, 1, "Schedule", CustomerUi.ScheduleIcon, "CustomerSchedulePage", activeTab == "Schedule");
        AddNav(grid, 2, "Messages", CustomerUi.MessagesIcon, "CustomerMessagesPage", activeTab == "Messages");
        AddNav(grid, 3, "Payments", CustomerUi.PaymentsIcon, "CustomerPaymentsPage", activeTab == "Payments");

        return new Border
        {
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            BackgroundColor = Colors.White,
            Content = grid
        };
    }

    private static void AddNav(Grid grid, int column, string text, string iconAsset, string route, bool active)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Image { Source = CustomerUi.Image(iconAsset), WidthRequest = 22, HeightRequest = 22, Opacity = active ? 1 : 0.70 },
                new Label
                {
                    Text = text,
                    TextColor = active ? CustomerUi.Orange : CustomerUi.Dark,
                    FontSize = 11,
                    FontAttributes = active ? FontAttributes.Bold : FontAttributes.None,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            }
        };
        stack.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                if (!active)
                {
                    await Shell.Current.GoToAsync($"//{route}");
                }
            })
        });

        grid.Add(stack, column, 0);
    }

    protected static string Money(decimal amount)
    {
        return string.Format(CultureInfo.GetCultureInfo("en-PH"), "PHP {0:N2}", amount);
    }

    protected static decimal RequestTotal(ServiceRequestDto? request)
    {
        return request is null ? 0m : request.FinalTotal > 0 ? request.FinalTotal : request.EstimatedTotal;
    }

    protected static decimal ReceiptAmount(PaymentDto? payment, ServiceRequestDto? request)
    {
        var requestTotal = RequestTotal(request);
        return requestTotal > 0 ? requestTotal : payment?.Amount ?? 0m;
    }

    protected static string RequestServiceTitle(ServiceRequestDto? request)
    {
        if (request?.LineItems is { Count: > 0 } items)
        {
            var services = items
                .Where(x => string.Equals(x.ItemType, "service", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.ItemName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
            if (services.Length > 0)
            {
                return string.Join(", ", services);
            }
        }

        return request?.ServiceName ?? "Bike repair";
    }

    protected static string RequestProductsTitle(ServiceRequestDto? request)
    {
        if (request?.LineItems is not { Count: > 0 } items)
        {
            return "None";
        }

        var products = items
            .Where(x => string.Equals(x.ItemType, "product", StringComparison.OrdinalIgnoreCase))
            .Select(x => $"{x.ItemName} ({Money(x.LineTotal)})")
            .ToArray();
        return products.Length == 0 ? "None" : string.Join(", ", products);
    }

    protected static string FormatStatus(string status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "Unknown"
            : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(status.Replace("_", " "));
    }

    protected static string Initials(string? first, string? last = null)
    {
        var text = $"{first} {last}".Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return "B";
        }

        return text[..1].ToUpperInvariant();
    }

    private protected static View CustomerAvatar(CustomerMeDto? customer, double size, Color? fallbackColor = null)
    {
        if (!string.IsNullOrWhiteSpace(customer?.ProfileImageUrl))
        {
            return new Border
            {
                WidthRequest = size,
                HeightRequest = size,
                StrokeShape = new RoundRectangle { CornerRadius = size / 2 },
                Stroke = CustomerUi.Border,
                StrokeThickness = 1,
                BackgroundColor = Color.FromArgb("#F2F2F2"),
                Content = new Image
                {
                    Source = VersionedImageSource(customer.ProfileImageUrl, customer.UpdatedAt),
                    Aspect = Aspect.AspectFill
                }
            };
        }

        return Avatar(
            Initials(customer?.FirstName, customer?.LastName),
            size,
            fallbackColor ?? CustomerUi.LightOrange);
    }

    protected static ImageSource VersionedImageSource(string value, DateTime? updatedAt = null)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return CustomerUi.Image(value);
        }

        var builder = new UriBuilder(uri);
        var cacheVersion = (updatedAt ?? DateTime.UnixEpoch).ToUniversalTime().Ticks;
        builder.Query = string.IsNullOrWhiteSpace(builder.Query)
            ? $"v={cacheVersion}"
            : $"{builder.Query.TrimStart('?')}&v={cacheVersion}";
        return ImageSource.FromUri(builder.Uri);
    }
}

internal static class CustomerRequestRules
{
    public static bool IsApproved(CustomerMeDto? customer)
    {
        return string.Equals(customer?.AccountStatus, "active", StringComparison.OrdinalIgnoreCase);
    }

    public static string BookingBlockedMessage(CustomerMeDto? customer)
    {
        var status = customer?.AccountStatus;
        return status?.Trim().ToLowerInvariant() switch
        {
            "rejected" => "Your BikeMate account was not approved yet. Update your profile or valid ID from Profile, then wait for admin review before booking.",
            "deleted" => "This BikeMate account is not active, so booking is disabled.",
            _ => "Your BikeMate account is still waiting for admin approval. You can browse the app and update your profile, but booking opens only after approval."
        };
    }

    public static bool IsEmergency(ServiceRequestDto? request)
    {
        return request is not null &&
               (request.IssueDescription.StartsWith("[EMERGENCY]", StringComparison.OrdinalIgnoreCase) ||
                request.CurrentStatus is "emergency_pending" or "searching_responder" or "call_connecting" or "call_connected");
    }

    public static bool IsClosed(ServiceRequestDto? request)
    {
        return IsClosedStatus(request?.CurrentStatus);
    }

    public static bool IsClosedStatus(string? status)
    {
        var normalized = status?.Trim();
        return normalized is not null &&
               (normalized.Equals("completed", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("cancelled", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("rejected", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("ServiceCompleted", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class CustomerHomePage : CustomerPageBase
{
    private CustomerMeDto? _customer;
    private IReadOnlyList<ServiceRequestDto> _requests = [];
    private bool _approvalPromptOpen;

    public CustomerHomePage()
    {
        Title = "Home";
        Render("Loading your customer dashboard...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            _requests = await CustomerApiClient.GetMyRequestsAsync();
            await ScheduleBookingRemindersAsync(_requests);
            Render();
            await PromptForAccountFixAsync();
        }
        catch (Exception ex)
        {
            Render(FriendlyError("BikeMate could not load your dashboard. Check your connection and try again.", ex));
        }
    }

    private async Task PromptForAccountFixAsync()
    {
        if (_approvalPromptOpen || CustomerRequestRules.IsApproved(_customer))
        {
            return;
        }

        _approvalPromptOpen = true;
        try
        {
            var message = _customer?.EmailVerified == false
                ? "Your email OTP is not verified yet. Send a new code and verify your email so BikeMate admin can approve your account."
                : CustomerRequestRules.BookingBlockedMessage(_customer);
            var fixNow = await DisplayAlertAsync(
                "Finish account setup",
                message,
                _customer?.EmailVerified == false ? "Send OTP" : "Fix Profile",
                "Later");
            if (!fixNow)
            {
                return;
            }

            if (_customer?.EmailVerified == false)
            {
                await SendEmailOtpAndOpenVerificationAsync(_customer);
                return;
            }

            await Shell.Current.GoToAsync(nameof(CustomerProfilePage));
        }
        finally
        {
            _approvalPromptOpen = false;
        }
    }

    private async Task SendEmailOtpAndOpenVerificationAsync(CustomerMeDto customer)
    {
        try
        {
            await CustomerApiClient.ResendEmailVerificationOtpAsync(customer.Email);
            await DisplayAlertAsync("Verification code sent", $"Check {customer.Email} for your BikeMate OTP.", "OK");
            await Shell.Current.GoToAsync($"{nameof(OtpVerificationPage)}?email={Uri.EscapeDataString(customer.Email)}&fromProfile=true");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("OTP failed", ex.Message, "OK");
        }
    }

    private void Render(string? banner = null)
    {
        var content = new VerticalStackLayout { Spacing = 0, BackgroundColor = CustomerUi.Page };
        content.Add(BuildHero(_customer));
        content.Add(BuildRepairCard());
        content.Add(BuildHomeBody(_requests, banner));

        SetScaffold(new ScrollView { Content = content }, "Home");
    }

    private View BuildHero(CustomerMeDto? customer)
    {
        var grid = new Grid
        {
            Padding = new Thickness(20, 28, 20, 62),
            BackgroundColor = CustomerUi.Orange,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var profile = DashboardProfilePhoto(customer);
        profile.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.GoToAsync(nameof(CustomerProfilePage)))
        });

        grid.Add(profile, 0, 0);
        var welcome = new VerticalStackLayout
        {
            Spacing = 2,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalOptions = LayoutOptions.Center
        };
        welcome.Add(Label($"Hello, {customer?.FirstName ?? "there"}", 18, Colors.White, FontAttributes.Bold));
        welcome.Add(Label("What can BikeMate help with today?", 11, Color.FromArgb("#FFF3EA")));
        grid.Add(welcome, 1, 0);

        grid.Add(new Button
        {
            Text = "Help",
            TextColor = Colors.White,
            BackgroundColor = Colors.Transparent,
            Command = new Command(async () => await Shell.Current.GoToAsync(nameof(CustomerHelpDeskPage)))
        }, 2, 0);

        return grid;
    }

    private View BuildRepairCard()
    {
        var card = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var left = new VerticalStackLayout { Spacing = 7 };
        left.Add(Label("Book motorcycle service", 14, CustomerUi.Dark, FontAttributes.Bold));
        left.Add(Label("Choose your concern, service flow, shop, and exact service.", 11, CustomerUi.Muted));
        left.Add(new Button
        {
            Text = "Book a repair",
            BackgroundColor = CustomerUi.Orange,
            TextColor = Colors.White,
            HeightRequest = 44,
            CornerRadius = 8,
            Command = new Command(async () => await OpenBookingAsync())
        });

        card.Add(left, 0, 0);
        card.Add(new Image
        {
            Source = CustomerUi.Image(CustomerUi.OnlineBikeRepairImage),
            HeightRequest = 74,
            WidthRequest = 74,
            Aspect = Aspect.AspectFill
        }, 1, 0);

        return new Border
        {
            Margin = new Thickness(18, -42, 18, 18),
            Padding = new Thickness(16),
            Stroke = CustomerUi.Border,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Colors.White,
            Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.10f, Radius = 6, Offset = new Point(0, 3) },
            Content = card
        };
    }

    private async Task OpenBookingAsync()
    {
        if (await EnsureCustomerApprovedForBookingAsync(_customer) is null)
        {
            return;
        }

        await Shell.Current.GoToAsync(nameof(BookServicePage));
    }

    private View BuildHomeBody(IReadOnlyList<ServiceRequestDto> requests, string? banner)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(18, 0, 18, 20), Spacing = 18 };
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(NoticeCard(banner));
        }

        body.Add(NextStepCard(requests));
        body.Add(SectionHeader("Your BikeMate"));
        body.Add(IdentityStatusCard(_customer));

        var scroller = new ScrollView { Orientation = ScrollOrientation.Horizontal };
        var cards = new HorizontalStackLayout { Spacing = 10 };
        cards.Add(HomeShortcut("Scheduled", CustomerUi.ScheduleIcon, new Command(async () => await Shell.Current.GoToAsync("//CustomerSchedulePage"))));
        cards.Add(HomeShortcut("Nearby shops", CustomerUi.HomeIcon, new Command(async () => await Shell.Current.GoToAsync(nameof(CustomerNearbyShopsPage)))));
        cards.Add(HomeShortcut("Messages", CustomerUi.MessagesIcon, new Command(async () => await Shell.Current.GoToAsync("//CustomerMessagesPage"))));
        cards.Add(HomeShortcut("Payments", CustomerUi.PaymentsIcon, new Command(async () => await Shell.Current.GoToAsync("//CustomerPaymentsPage"))));
        cards.Add(HomeShortcut("Alerts", CustomerUi.HomeIcon, new Command(async () => await Shell.Current.GoToAsync(nameof(CustomerNotificationsPage)))));
        cards.Add(HomeShortcut("Help Desk", CustomerUi.HomeIcon, new Command(async () => await Shell.Current.GoToAsync(nameof(CustomerHelpDeskPage)))));
        scroller.Content = cards;
        body.Add(scroller);

        body.Add(SectionHeader(
            "Booked Repairs",
            "View all",
            new Command(async () => await Shell.Current.GoToAsync("//CustomerSchedulePage"))));
        var visibleRequests = requests.OrderByDescending(x => x.CreatedAt).Take(3).ToArray();
        if (visibleRequests.Length == 0)
        {
            body.Add(EmptyState(
                "No bookings yet",
                "Start from the booking card above when you are ready to choose your concern, service flow, shop, and exact service."));
        }
        else
        {
            foreach (var request in visibleRequests)
            {
                body.Add(RepairRow(request));
            }
        }

        body.Add(new Button
        {
            Text = "Emergency roadside help",
            BackgroundColor = Color.FromArgb("#FF626A"),
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 48,
            FontAttributes = FontAttributes.Bold,
            Command = new Command(async () => await OpenEmergencyAsync())
        });

        return body;
    }

    private View NextStepCard(IReadOnlyList<ServiceRequestDto> requests)
    {
        if (!CustomerRequestRules.IsApproved(_customer))
        {
            return EmptyState(
                "Finish account setup",
                _customer?.EmailVerified == false
                    ? "Verify your email OTP so BikeMate can review and approve your customer account."
                    : CustomerRequestRules.BookingBlockedMessage(_customer),
                _customer?.EmailVerified == false ? "Send OTP" : "Open profile",
                new Command(async () =>
                {
                    if (_customer?.EmailVerified == false)
                    {
                        await SendEmailOtpAndOpenVerificationAsync(_customer);
                        return;
                    }

                    await Shell.Current.GoToAsync(nameof(CustomerProfilePage));
                }));
        }

        var active = requests
            .Where(request => !CustomerRequestRules.IsClosed(request))
            .OrderByDescending(request => request.CreatedAt)
            .FirstOrDefault();
        if (active is not null)
        {
            return EmptyState(
                "Continue your active booking",
                $"{active.ServiceName ?? "Bike repair"} is {FormatStatus(active.CurrentStatus).ToLowerInvariant()}. Open the booking to see payment, shop, and tracking next steps.",
                "View booking",
                new Command(async () => await Shell.Current.GoToAsync($"{nameof(BookingDetailsPage)}?requestId={active.RequestId}")),
                "Messages",
                new Command(async () => await Shell.Current.GoToAsync("//CustomerMessagesPage")));
        }

        return EmptyState(
            "Ready when your bike needs help",
            "Book a repair, request emergency roadside help, or review past work from your dashboard.",
            "Emergency help",
            new Command(async () => await OpenEmergencyAsync()));
    }

    private async Task OpenEmergencyAsync()
    {
        if (await EnsureCustomerApprovedForBookingAsync(_customer) is null)
        {
            return;
        }

        await Shell.Current.GoToAsync(nameof(EmergencySosPage));
    }

    private static View IdentityStatusCard(CustomerMeDto? customer)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        grid.Add(CustomerAvatar(customer, 44), 0, 0);
        var text = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        text.Add(Label(
            customer is null ? "Loading your account" : $"{customer.FirstName} {customer.LastName}".Trim(),
            12,
            CustomerUi.Dark,
            FontAttributes.Bold));
        text.Add(Label(
            string.IsNullOrWhiteSpace(customer?.ValidIdImageUrl)
                ? "Valid ID not uploaded"
                : "Valid ID on file",
            10,
            CustomerUi.Muted));
        grid.Add(text, 1, 0);
        grid.Add(IdentityBadge(
            string.IsNullOrWhiteSpace(customer?.ValidIdImageUrl) ? "Action needed" : "ID on file",
            string.IsNullOrWhiteSpace(customer?.ValidIdImageUrl) ? CustomerUi.LightOrange : Color.FromArgb("#E8F6EF"),
            string.IsNullOrWhiteSpace(customer?.ValidIdImageUrl) ? CustomerUi.Orange : Color.FromArgb("#147A3D")), 2, 0);
        return Card(grid, Colors.White, 8, new Thickness(12));
    }

    private static View IdentityBadge(string text, Color background, Color textColor)
    {
        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = background,
            Padding = new Thickness(9, 4),
            VerticalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = text,
                FontSize = AppTypography.CaptionSize,
                FontAttributes = FontAttributes.Bold,
                FontFamily = CustomerUi.FontDisplay,
                TextColor = textColor,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
    }

    private static View DashboardProfilePhoto(CustomerMeDto? customer)
    {
        return CustomerAvatar(customer, 46, Colors.White);
    }

    private static View HomeShortcut(string title, string iconAsset, ICommand command)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Image { Source = CustomerUi.Image(iconAsset), HeightRequest = 38, WidthRequest = 38, Aspect = Aspect.AspectFit },
                new Label { Text = title, FontSize = 11, TextColor = CustomerUi.Muted, HorizontalTextAlignment = TextAlignment.Center }
            }
        };

        var border = Card(stack, Colors.White, 8, new Thickness(14));
        border.WidthRequest = 108;
        border.HeightRequest = 112;
        border.GestureRecognizers.Add(new TapGestureRecognizer { Command = command });
        return border;
    }

    private static View RepairRow(ServiceRequestDto request)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Add(new Image { Source = CustomerUi.Image(CustomerUi.OnlineBikeRepairImage), WidthRequest = 42, HeightRequest = 42, Aspect = Aspect.AspectFill }, 0, 0);

        var text = new VerticalStackLayout { Spacing = 2, Margin = new Thickness(10, 0, 0, 0) };
        text.Add(Label(request.ServiceName ?? "Bike repair", 13, CustomerUi.Dark, FontAttributes.Bold));
        text.Add(Label(request.MechanicName ?? request.ShopName ?? "Waiting for assignment", 11, CustomerUi.Muted));
        text.Add(Label(Money(request.FinalTotal > 0 ? request.FinalTotal : request.EstimatedTotal), 11, CustomerUi.Dark));
        grid.Add(text, 1, 0);

        grid.Add(StatusChip(FormatStatus(request.CurrentStatus)), 2, 0);

        var border = Card(grid, Colors.White, 8, new Thickness(12));
        border.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.GoToAsync($"{nameof(BookingDetailsPage)}?requestId={request.RequestId}"))
        });
        return border;
    }
}

public sealed class CustomerNearbyShopsPage : CustomerPageBase
{
    private CustomerMeDto? _customer;
    private IReadOnlyList<ShopSummaryDto> _nearbyShops = [];
    private IReadOnlyList<ShopSummaryDto> _allShops = [];
    private IReadOnlyList<ShopServiceDto> _matchingServices = [];
    private IReadOnlyList<ProductDto> _matchingProducts = [];
    private decimal? _originLatitude;
    private decimal? _originLongitude;
    private string _searchText = string.Empty;
    private bool _isLoading;
    private int _searchVersion;
    private const decimal NearbyRadiusKm = 50m;

    public CustomerNearbyShopsPage()
    {
        Title = "Nearby shops";
        Render("Loading nearby shops...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            var address = DefaultAddress(_customer);
            _originLatitude = address?.Latitude;
            _originLongitude = address?.Longitude;
            await TryUseLastKnownLocationAsync();
            _allShops = await CustomerApiClient.GetShopsAsync();
            _nearbyShops = NearbyFromAllShops(_allShops);
            await LoadSearchResultsAsync();
            Render();
        }
        catch (Exception ex)
        {
            Render(FriendlyError("Nearby shops could not be loaded. Check your connection and try again.", ex));
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task TryUseLastKnownLocationAsync()
    {
        if (_originLatitude is not null && _originLongitude is not null)
        {
            return;
        }

        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync();
            if (location is not null)
            {
                _originLatitude = (decimal)location.Latitude;
                _originLongitude = (decimal)location.Longitude;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Last known location lookup failed: {ex}");
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout
        {
            Spacing = 14,
            Padding = new Thickness(18, 0, 18, 24),
            BackgroundColor = CustomerUi.Page
        };

        body.Add(Header("Nearby shops"));
        body.Add(SearchBox());

        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(NoticeCard(banner));
        }

        if (_originLatitude is null || _originLongitude is null)
        {
            body.Add(NoticeCard("Add a default address with map coordinates in your profile to show distance and routes."));
        }

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            RenderSearchResults(body);
        }
        else
        {
            RenderBrowseSections(body);
        }

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private View SearchBox()
    {
        var search = new SearchBar
        {
            Text = _searchText,
            Placeholder = "Search shops, services, or products",
            BackgroundColor = Colors.White,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            CancelButtonColor = CustomerUi.Orange,
            FontSize = CustomerUi.BodySize,
            HeightRequest = 48,
            MaxLength = 80,
            SearchCommand = new Command(async () => await LoadSearchResultsAsync())
        };
        search.TextChanged += async (_, args) =>
        {
            _searchText = args.NewTextValue ?? string.Empty;
            await DebouncedSearchAsync();
        };

        return Card(search, Colors.White, 8, new Thickness(0));
    }

    private async Task DebouncedSearchAsync()
    {
        var version = ++_searchVersion;
        await Task.Delay(350);
        if (version != _searchVersion)
        {
            return;
        }

        await LoadSearchResultsAsync();
        Render();
    }

    private async Task LoadSearchResultsAsync()
    {
        var query = _searchText.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            _matchingServices = [];
            _matchingProducts = [];
            return;
        }

        try
        {
            _matchingServices = await CustomerApiClient.SearchServicesAsync(query);
            _matchingProducts = await CustomerApiClient.SearchProductsAsync(query);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Shop search failed: {ex}");
            _matchingServices = [];
            _matchingProducts = [];
        }
    }

    private void RenderBrowseSections(VerticalStackLayout body)
    {
        body.Add(SectionHeader("Nearby shops"));
        if (_nearbyShops.Count == 0)
        {
            body.Add(EmptyState(
                _originLatitude is null || _originLongitude is null ? "Location needed" : "No nearby shops",
                _originLatitude is null || _originLongitude is null
                    ? "BikeMate can show nearby shops after your profile address has map coordinates."
                    : $"No verified repair shop is within {NearbyRadiusKm:N0} km right now.",
                "Refresh",
                new Command(async () => await LoadAsync())));
        }
        else
        {
            foreach (var shop in _nearbyShops)
            {
                body.Add(ShopCard(shop));
            }
        }

        var outsideShops = OutsideRangeShops().ToArray();
        body.Add(SectionHeader(_originLatitude is null || _originLongitude is null ? "All shops" : "All shops outside your range"));
        if (outsideShops.Length == 0)
        {
            body.Add(EmptyState("No more shops", "Every available verified shop is already shown above."));
            return;
        }

        foreach (var shop in outsideShops)
        {
            body.Add(ShopCard(shop));
        }
    }

    private void RenderSearchResults(VerticalStackLayout body)
    {
        var query = _searchText.Trim();
        var matchingShops = _allShops
            .Where(shop => Matches(query, shop.ShopName, shop.AddressLine, shop.City, shop.ContactNumber))
            .OrderBy(shop => SortDistance(shop) ?? decimal.MaxValue)
            .ThenBy(shop => shop.ShopName)
            .ToArray();

        body.Add(SectionHeader("Matching shops"));
        if (matchingShops.Length == 0)
        {
            body.Add(EmptyState("No shop matches", "Services and products can still match below."));
        }
        else
        {
            foreach (var shop in matchingShops)
            {
                body.Add(ShopCard(shop));
            }
        }

        body.Add(SectionHeader("Matching services"));
        if (_matchingServices.Count == 0)
        {
            body.Add(EmptyState("No service matches", "Try a concern like tire, oil, brakes, or battery."));
        }
        else
        {
            foreach (var service in _matchingServices.Take(20))
            {
                body.Add(SearchServiceCard(service));
            }
        }

        body.Add(SectionHeader("Matching products"));
        if (_matchingProducts.Count == 0)
        {
            body.Add(EmptyState("No product matches", "Try a product name or part type."));
        }
        else
        {
            foreach (var product in _matchingProducts.Take(20))
            {
                body.Add(SearchProductCard(product));
            }
        }
    }

    private View ShopCard(ShopSummaryDto shop)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };

        grid.Add(new Border
        {
            WidthRequest = 66,
            HeightRequest = 66,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb("#F0F0F0"),
            Content = new Image
            {
                Source = BookingVisuals.ShopImageSource(shop.ShopLogoUrl ?? shop.ShopImageUrl),
                WidthRequest = 66,
                HeightRequest = 66,
                Aspect = Aspect.AspectFill
            }
        }, 0, 0);

        var text = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        text.Add(Label(shop.ShopName, 14, CustomerUi.Dark, FontAttributes.Bold));
        text.Add(Label(LocationLine(shop), 11, CustomerUi.Muted));
        text.Add(Label($"{shop.ActiveServiceCount} services available", 11, CustomerUi.Muted));
        text.Add(Label(shop.StartingPrice is decimal price ? $"Starts at {Money(price)}" : "Pricing varies by service", 11, CustomerUi.Dark));
        grid.Add(text, 1, 0);

        var card = Card(grid, Colors.White, 8, new Thickness(12));
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.GoToAsync(DetailsRoute(shop.ShopId)))
        });
        return card;
    }

    private View SearchServiceCard(ShopServiceDto service)
    {
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        var copy = new VerticalStackLayout { Spacing = 3 };
        copy.Add(Label(service.ServiceName, 13, CustomerUi.Dark, FontAttributes.Bold));
        copy.Add(Label($"{ShopName(service.ShopId)} - {service.CategoryName} - {service.EstimatedMinutes} mins", 11, CustomerUi.Muted));
        row.Add(copy, 0, 0);
        row.Add(Label(Money(service.BasePrice), 12, CustomerUi.Dark, FontAttributes.Bold), 1, 0);

        var card = Card(row, Colors.White, 8, new Thickness(12));
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.GoToAsync(DetailsRoute(service.ShopId)))
        });
        return card;
    }

    private View SearchProductCard(ProductDto product)
    {
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };

        row.Add(new Image
        {
            Source = BookingVisuals.ProductImageSource(product.ProductImageUrl),
            WidthRequest = 46,
            HeightRequest = 46,
            Aspect = Aspect.AspectFill
        }, 0, 0);

        var copy = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
        copy.Add(Label(product.ProductName, 13, CustomerUi.Dark, FontAttributes.Bold));
        copy.Add(Label($"{ShopName(product.ShopId)} - {(product.StockQuantity > 0 ? $"{product.StockQuantity} in stock" : "Out of stock")}", 11, CustomerUi.Muted));
        row.Add(copy, 1, 0);
        row.Add(Label(Money(product.Price), 12, CustomerUi.Dark, FontAttributes.Bold), 2, 0);

        var card = Card(row, Colors.White, 8, new Thickness(12));
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.GoToAsync(DetailsRoute(product.ShopId)))
        });
        return card;
    }

    private string DetailsRoute(int shopId)
    {
        var route = $"{nameof(CustomerShopDetailsPage)}?shopId={shopId}";
        if (_originLatitude is decimal latitude && _originLongitude is decimal longitude)
        {
            route += $"&originLat={latitude.ToString(CultureInfo.InvariantCulture)}&originLng={longitude.ToString(CultureInfo.InvariantCulture)}";
        }

        return route;
    }

    private string LocationLine(ShopSummaryDto shop)
    {
        var address = string.Join(", ", new[] { shop.AddressLine, shop.City }.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (_originLatitude is decimal originLat &&
            _originLongitude is decimal originLng &&
            shop.Latitude is decimal shopLat &&
            shop.Longitude is decimal shopLng)
        {
            var km = DistanceKm(originLat, originLng, shopLat, shopLng);
            return string.IsNullOrWhiteSpace(address)
                ? $"{km:N1} km away"
                : $"{address} - {km:N1} km away";
        }

        return string.IsNullOrWhiteSpace(address) ? "Verified BikeMate shop" : address;
    }

    private IReadOnlyList<ShopSummaryDto> NearbyFromAllShops(IReadOnlyList<ShopSummaryDto> shops)
    {
        if (_originLatitude is not decimal originLat || _originLongitude is not decimal originLng)
        {
            return [];
        }

        return shops
            .Where(shop => shop.Latitude is decimal shopLat &&
                shop.Longitude is decimal shopLng &&
                DistanceKm(originLat, originLng, shopLat, shopLng) <= NearbyRadiusKm)
            .OrderBy(shop => SortDistance(shop) ?? decimal.MaxValue)
            .ThenBy(shop => shop.ShopName)
            .ToArray();
    }

    private IEnumerable<ShopSummaryDto> OutsideRangeShops()
    {
        if (_originLatitude is null || _originLongitude is null)
        {
            return _allShops.OrderBy(shop => shop.ShopName);
        }

        var nearbyIds = _nearbyShops.Select(shop => shop.ShopId).ToHashSet();
        return _allShops
            .Where(shop => !nearbyIds.Contains(shop.ShopId))
            .OrderBy(shop => SortDistance(shop) ?? decimal.MaxValue)
            .ThenBy(shop => shop.ShopName);
    }

    private decimal? SortDistance(ShopSummaryDto shop)
    {
        return _originLatitude is decimal originLat &&
            _originLongitude is decimal originLng &&
            shop.Latitude is decimal shopLat &&
            shop.Longitude is decimal shopLng
            ? DistanceKm(originLat, originLng, shopLat, shopLng)
            : null;
    }

    private string ShopName(int shopId)
    {
        return _allShops.FirstOrDefault(shop => shop.ShopId == shopId)?.ShopName ?? "BikeMate shop";
    }

    private static bool Matches(string query, params string?[] values)
    {
        return values.Any(value => !string.IsNullOrWhiteSpace(value) &&
            value.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static CustomerAddressDto? DefaultAddress(CustomerMeDto? customer)
    {
        return customer?.Addresses.FirstOrDefault(x => x.IsDefault) ?? customer?.Addresses.FirstOrDefault();
    }

    private static decimal DistanceKm(decimal latitudeA, decimal longitudeA, decimal latitudeB, decimal longitudeB)
    {
        const double radiusKm = 6371d;
        var dLat = DegreesToRadians((double)(latitudeB - latitudeA));
        var dLon = DegreesToRadians((double)(longitudeB - longitudeA));
        var lat1 = DegreesToRadians((double)latitudeA);
        var lat2 = DegreesToRadians((double)latitudeB);
        var a = Math.Pow(Math.Sin(dLat / 2d), 2d) +
            Math.Pow(Math.Sin(dLon / 2d), 2d) * Math.Cos(lat1) * Math.Cos(lat2);
        return (decimal)(radiusKm * 2d * Math.Asin(Math.Sqrt(a)));
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }
}

public sealed class CustomerShopDetailsPage : CustomerPageBase, IQueryAttributable
{
    private int _shopId;
    private decimal? _originLatitude;
    private decimal? _originLongitude;
    private ShopDetailsDto? _shop;
    private IReadOnlyList<ShopServiceDto> _services = [];
    private IReadOnlyList<ProductDto> _products = [];
    private string _detailSearchText = string.Empty;
    private bool _isLoading;

    public CustomerShopDetailsPage()
    {
        Title = "Shop details";
        Render("Loading shop details...");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("shopId", out var shopIdValue) &&
            int.TryParse(Uri.UnescapeDataString(shopIdValue?.ToString() ?? ""), out var shopId))
        {
            _shopId = shopId;
        }

        if (query.TryGetValue("originLat", out var latValue) &&
            decimal.TryParse(Uri.UnescapeDataString(latValue?.ToString() ?? ""), NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude))
        {
            _originLatitude = latitude;
        }

        if (query.TryGetValue("originLng", out var lngValue) &&
            decimal.TryParse(Uri.UnescapeDataString(lngValue?.ToString() ?? ""), NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
        {
            _originLongitude = longitude;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isLoading || _shopId <= 0)
        {
            return;
        }

        _isLoading = true;
        try
        {
            _shop = await CustomerApiClient.GetShopDetailsAsync(_shopId);
            _services = await CustomerApiClient.GetShopServicesAsync(_shopId);
            _products = await CustomerApiClient.GetShopProductsAsync(_shopId);
            await FillOriginFromProfileAsync();
            Render();
        }
        catch (Exception ex)
        {
            Render(FriendlyError("Shop details could not be loaded. Check your connection and try again.", ex));
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task FillOriginFromProfileAsync()
    {
        if (_originLatitude is not null && _originLongitude is not null)
        {
            return;
        }

        try
        {
            var customer = await CustomerApiClient.GetCustomerAsync();
            var address = customer.Addresses.FirstOrDefault(x => x.IsDefault) ?? customer.Addresses.FirstOrDefault();
            _originLatitude = address?.Latitude;
            _originLongitude = address?.Longitude;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Profile address lookup failed: {ex}");
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout
        {
            Spacing = 14,
            Padding = new Thickness(18, 0, 18, 24),
            BackgroundColor = CustomerUi.Page
        };

        body.Add(Header("Shop details"));

        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(NoticeCard(banner));
        }

        if (_shop is null)
        {
            body.Add(EmptyState("Shop unavailable", "BikeMate could not load this repair shop."));
            SetScaffold(new ScrollView { Content = body }, "Home", false);
            return;
        }

        var address = string.Join(", ", new[] { _shop.AddressLine, _shop.City, _shop.Province }.Where(x => !string.IsNullOrWhiteSpace(x)));
        body.Add(BookingVisuals.ShopProfileCard(_shop.ShopName, _shop.ShopImageUrl, _shop.ShopLogoUrl, address, "Verified repair shop"));
        if (!string.IsNullOrWhiteSpace(_shop.ShopDescription))
        {
            body.Add(Card(Label(_shop.ShopDescription, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(ActionButtons());
        body.Add(RouteCard());
        body.Add(DetailSearchBox());

        var visibleServices = VisibleServices().ToArray();
        body.Add(SectionHeader(string.IsNullOrWhiteSpace(_detailSearchText) ? "Services" : "Matching services"));
        if (visibleServices.Length == 0)
        {
            body.Add(EmptyState(
                string.IsNullOrWhiteSpace(_detailSearchText) ? "No services listed" : "No matching services",
                string.IsNullOrWhiteSpace(_detailSearchText)
                    ? "This shop has not published active services yet."
                    : "Try another service or concern."));
        }
        else
        {
            foreach (var service in visibleServices)
            {
                body.Add(ServiceCard(service));
            }
        }

        var visibleProducts = VisibleProducts().ToArray();
        body.Add(SectionHeader(string.IsNullOrWhiteSpace(_detailSearchText) ? "Products" : "Matching products"));
        if (visibleProducts.Length == 0)
        {
            body.Add(EmptyState(
                string.IsNullOrWhiteSpace(_detailSearchText) ? "No products listed" : "No matching products",
                string.IsNullOrWhiteSpace(_detailSearchText)
                    ? "This shop has not published products yet."
                    : "Try another product or part name."));
        }
        else
        {
            foreach (var product in visibleProducts)
            {
                body.Add(ProductCard(product));
            }
        }

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private View ActionButtons()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };

        grid.Add(OrangeButton("Message shop", new Command(async () => await MessageShopAsync())), 0, 0);
        grid.Add(GhostButton("Book repair", new Command(async () => await Shell.Current.GoToAsync(nameof(BookServicePage)))), 1, 0);
        return grid;
    }

    private View DetailSearchBox()
    {
        var search = new SearchBar
        {
            Text = _detailSearchText,
            Placeholder = "Search this shop's services or products",
            BackgroundColor = Colors.White,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            CancelButtonColor = CustomerUi.Orange,
            FontSize = CustomerUi.BodySize,
            HeightRequest = 48,
            MaxLength = 80
        };
        search.TextChanged += (_, args) =>
        {
            _detailSearchText = args.NewTextValue ?? string.Empty;
            Render();
        };

        return Card(search, Colors.White, 8, new Thickness(0));
    }

    private View RouteCard()
    {
        if (_shop?.Latitude is not decimal destinationLat ||
            _shop.Longitude is not decimal destinationLng)
        {
            return NoticeCard("This shop has no map coordinates yet.");
        }

        var stack = new VerticalStackLayout { Spacing = 10 };
        var hasRoute = _originLatitude is not null && _originLongitude is not null;
        var originLat = _originLatitude ?? 0m;
        var originLng = _originLongitude ?? 0m;
        var title = hasRoute
            ? $"{DistanceKm(originLat, originLng, destinationLat, destinationLng):N1} km from your location"
            : "Shop location";
        stack.Add(Label(title, 13, CustomerUi.Dark, FontAttributes.Bold));

        stack.Add(new WebView
        {
            HeightRequest = 180,
            Source = hasRoute
                ? BookingVisuals.GoogleDirectionsSource(originLat, originLng, destinationLat, destinationLng)
                : BookingVisuals.GoogleMapSource(destinationLat, destinationLng)
        });

        stack.Add(GhostButton(
            hasRoute ? "Open route in Maps" : "Open in Maps",
            new Command(async () =>
            {
                if (hasRoute)
                {
                    await BookingVisuals.OpenGoogleDirectionsAsync(originLat, originLng, destinationLat, destinationLng);
                }
                else
                {
                    await BookingVisuals.OpenGoogleMapsAsync($"{destinationLat.ToString(CultureInfo.InvariantCulture)},{destinationLng.ToString(CultureInfo.InvariantCulture)}");
                }
            })));

        return Card(stack, Colors.White, 8, new Thickness(12));
    }

    private View ServiceCard(ShopServiceDto service)
    {
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        var copy = new VerticalStackLayout { Spacing = 3 };
        copy.Add(Label(service.ServiceName, 13, CustomerUi.Dark, FontAttributes.Bold));
        copy.Add(Label($"{service.CategoryName} - {service.EstimatedMinutes} mins", 11, CustomerUi.Muted));
        if (!string.IsNullOrWhiteSpace(service.ServiceDescription))
        {
            copy.Add(Label(service.ServiceDescription, 10, CustomerUi.Muted));
        }

        row.Add(copy, 0, 0);
        row.Add(Label(Money(service.BasePrice), 12, CustomerUi.Dark, FontAttributes.Bold), 1, 0);
        return Card(row, Colors.White, 8, new Thickness(12));
    }

    private IEnumerable<ShopServiceDto> VisibleServices()
    {
        var query = _detailSearchText.Trim();
        var services = _services.Where(x => x.IsActive);
        return string.IsNullOrWhiteSpace(query)
            ? services
            : services.Where(service => Matches(query, service.ServiceName, service.CategoryName, service.ServiceDescription));
    }

    private IEnumerable<ProductDto> VisibleProducts()
    {
        var query = _detailSearchText.Trim();
        var products = _products.Where(x => x.IsActive);
        return string.IsNullOrWhiteSpace(query)
            ? products
            : products.Where(product => Matches(query, product.ProductName, product.ProductDescription));
    }

    private View ProductCard(ProductDto product)
    {
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };

        row.Add(new Image
        {
            Source = BookingVisuals.ProductImageSource(product.ProductImageUrl),
            WidthRequest = 46,
            HeightRequest = 46,
            Aspect = Aspect.AspectFill
        }, 0, 0);

        var copy = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
        copy.Add(Label(product.ProductName, 13, CustomerUi.Dark, FontAttributes.Bold));
        copy.Add(Label(product.StockQuantity > 0 ? $"{product.StockQuantity} in stock" : "Out of stock", 11, CustomerUi.Muted));
        row.Add(copy, 1, 0);
        row.Add(Label(Money(product.Price), 12, CustomerUi.Dark, FontAttributes.Bold), 2, 0);
        return Card(row, Colors.White, 8, new Thickness(12));
    }

    private async Task MessageShopAsync()
    {
        if (_shop is null)
        {
            return;
        }

        try
        {
            var conversation = await CustomerApiClient.StartShopInquiryAsync(_shop.ShopId);
            await Shell.Current.Navigation.PushAsync(new CustomerChatPage(conversation.ConversationId));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Message shop", FriendlyError("BikeMate could not open a shop chat. Check your connection and try again.", ex), "OK");
        }
    }

    private static decimal DistanceKm(decimal latitudeA, decimal longitudeA, decimal latitudeB, decimal longitudeB)
    {
        const double radiusKm = 6371d;
        var dLat = DegreesToRadians((double)(latitudeB - latitudeA));
        var dLon = DegreesToRadians((double)(longitudeB - longitudeA));
        var lat1 = DegreesToRadians((double)latitudeA);
        var lat2 = DegreesToRadians((double)latitudeB);
        var a = Math.Pow(Math.Sin(dLat / 2d), 2d) +
            Math.Pow(Math.Sin(dLon / 2d), 2d) * Math.Cos(lat1) * Math.Cos(lat2);
        return (decimal)(radiusKm * 2d * Math.Asin(Math.Sqrt(a)));
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }

    private static bool Matches(string query, params string?[] values)
    {
        return values.Any(value => !string.IsNullOrWhiteSpace(value) &&
            value.Contains(query, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class CustomerNotificationsPage : CustomerPageBase
{
    private IReadOnlyList<NotificationDto> _notifications = [];
    private NotificationPermissionState _notificationPermissionState = NotificationPermissionState.NotRequired;
    private string? _banner;
    private bool _isLoading;

    public CustomerNotificationsPage()
    {
        Title = "Notifications";
        Render("Loading notifications...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        Render("Refreshing notifications...");
        try
        {
            await RefreshNotificationPermissionStateAsync();
            _notifications = await CustomerApiClient.GetNotificationsAsync();
            _banner = null;
        }
        catch (Exception ex)
        {
            _banner = FriendlyError("Notifications could not be loaded right now. Try again in a moment.", ex);
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16, 8, 16, 20), Spacing = 12 };
        body.Add(Header("Notifications"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(NoticeCard(banner));
        }

        if (_notificationPermissionState == NotificationPermissionState.Disabled)
        {
            body.Add(NotificationPermissionCard());
        }

        body.Add(GhostButton(_isLoading ? "Refreshing..." : "Refresh status", new Command(async () => await LoadAsync())));

        if (_notifications.Count == 0 && !_isLoading)
        {
            body.Add(EmptyState(
                "No notifications yet",
                "Booking, payment, chat, and emergency alerts will appear here as soon as there is something new.",
                "View schedule",
                new Command(async () => await Shell.Current.GoToAsync("//CustomerSchedulePage"))));
        }
        else
        {
            foreach (var notification in _notifications.OrderByDescending(x => x.CreatedAt))
            {
                body.Add(NotificationRow(notification));
            }
        }

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private async Task RefreshNotificationPermissionStateAsync()
    {
        var reminderService = Application.Current?.Handler?.MauiContext?.Services.GetService<IBookingReminderService>();
        if (reminderService is null)
        {
            _notificationPermissionState = NotificationPermissionState.NotRequired;
            return;
        }

        _notificationPermissionState = await reminderService.GetNotificationPermissionStateAsync();
    }

    private View NotificationPermissionCard()
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Add(Label(
            "Phone notifications are off. BikeMate can still show alerts here, but Android will block reminder popups until permission is enabled.",
            11,
            CustomerUi.Muted));
        stack.Add(OrangeButton("Enable notifications", new Command(async () => await EnableNotificationsAsync())));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private async Task EnableNotificationsAsync()
    {
        var reminderService = Application.Current?.Handler?.MauiContext?.Services.GetService<IBookingReminderService>();
        if (reminderService is null)
        {
            return;
        }

        var enabled = await reminderService.EnsureNotificationsEnabledAsync();
        if (!enabled)
        {
            await reminderService.OpenNotificationSettingsAsync();
        }
        else
        {
            try
            {
                await ScheduleBookingRemindersAsync(await CustomerApiClient.GetMyRequestsAsync());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Booking reminder reschedule failed after enabling notifications: {ex}");
            }
        }

        await RefreshNotificationPermissionStateAsync();
        Render(_banner);
    }

    private View NotificationRow(NotificationDto notification)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Add(new Label
        {
            Text = notification.Title,
            TextColor = CustomerUi.Dark,
            FontAttributes = notification.IsRead ? FontAttributes.None : FontAttributes.Bold,
            FontSize = 13
        });
        stack.Add(Label(notification.Message, 12, CustomerUi.Muted));
        stack.Add(Label($"{FormatStatus(notification.NotificationType ?? "notification")} - {notification.CreatedAt.ToLocalTime():MMM d, h:mm tt}", 10, CustomerUi.Orange));

        var card = Card(stack, notification.IsRead ? Colors.White : Color.FromArgb("#FFF4EF"), 8, new Thickness(14));
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                if (!notification.IsRead)
                {
                    await CustomerApiClient.MarkNotificationReadAsync(notification.NotificationId);
                    await LoadAsync();
                }
            })
        });
        return card;
    }
}

public sealed class CustomerProfilePage : CustomerPageBase
{
    private CustomerMeDto? _customer;
    private Entry? _firstNameEntry;
    private Entry? _middleNameEntry;
    private Entry? _lastNameEntry;
    private Entry? _emailEntry;
    private Entry? _phoneEntry;
    private Picker? _sexPicker;
    private DatePicker? _birthdayPicker;
    private Picker? _regionPicker;
    private Picker? _localityPicker;
    private Picker? _barangayPicker;
    private Entry? _zipCodeEntry;
    private Editor? _addressEditor;
    private Entry? _bikeBrandEntry;
    private Entry? _bikeModelEntry;
    private Entry? _bikeYearEntry;
    private Entry? _bikePlateEntry;
    private Entry? _bikeEngineEntry;
    private Entry? _bikeColorEntry;
    private LocalIdCardScanResult? _localIdScanResult;
    private IReadOnlyList<PhilippineRegionDto> _regions = [];
    private IReadOnlyList<PhilippineLocalityDto> _localities = [];
    private IReadOnlyList<PhilippineBarangayDto> _barangays = [];
    private string? _selectedRegionCode;
    private string? _selectedLocalityCode;
    private string? _selectedBarangayName;
    private string? _selectedProvince;
    private string? _locationMessage;
    private bool _isLoadingLocations;
    private bool _isUpdatingLocationPickers;
    private bool _restoredAddressSelection;
    private bool _isSendingEmailOtp;
    private bool _isLoading;
    private bool _isSaving;

    public CustomerProfilePage()
    {
        Title = "Account Details";
        Render("Loading account details...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            Render();
            await EnsureAddressLocationsReadyAsync();
        }
        catch (ApiSessionExpiredException)
        {
        }
        catch (Exception ex)
        {
            Render(FriendlyError("Account details could not be loaded. Check your connection and try again.", ex));
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void Render(string? banner = null)
    {
        var customer = _customer;
        var defaultAddress = customer?.Addresses.FirstOrDefault(x => x.IsDefault) ?? customer?.Addresses.FirstOrDefault();
        var motorcycle = customer?.Motorcycles.FirstOrDefault();
        PrepareFields(customer, defaultAddress, motorcycle);

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 8, 16, 20),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };

        body.Add(Header("Account Details", true, _isSaving ? "Saving" : "Save", new Command(async () => await SaveProfileAsync())));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(NoticeCard(banner));
        }

        body.Add(ProfileHeaderCard(customer));
        body.Add(AccountApprovalCard(customer));
        body.Add(PersonalDetailsCard());
        body.Add(ContactDetailsCard(customer));
        body.Add(AddressCard());
        body.Add(MotorcycleCard());
        body.Add(IdentityVerificationCard(customer));
        body.Add(AccountSecurityCard(customer));
        body.Add(DangerZoneCard());
        body.Add(OrangeButton(_isSaving ? "Saving account details..." : "Save account details", new Command(async () => await SaveProfileAsync())));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private async Task RefreshProfileAsync()
    {
        _customer = await CustomerApiClient.GetCustomerAsync();
        _restoredAddressSelection = false;
        Render();
        await EnsureAddressLocationsReadyAsync();
    }

    private async Task EnsureAddressLocationsReadyAsync()
    {
        await EnsureRegionsLoadedAsync();
        var address = _customer?.Addresses.FirstOrDefault(x => x.IsDefault) ?? _customer?.Addresses.FirstOrDefault();
        await RestoreSavedAddressSelectionAsync(address);
        Render();
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
            _locationMessage = _regions.Count == 0 ? "No Philippine regions were returned. Please try again." : null;
        }
        catch (Exception ex)
        {
            _locationMessage = FriendlyError("Philippine locations could not be loaded. Check your connection and try again.", ex);
        }
        finally
        {
            _isLoadingLocations = false;
        }
    }

    private async Task RestoreSavedAddressSelectionAsync(CustomerAddressDto? address)
    {
        if (_restoredAddressSelection || _regions.Count == 0 || address is null)
        {
            return;
        }

        _restoredAddressSelection = true;
        _selectedBarangayName = address.Barangay;
        _selectedProvince = address.Province;

        var query = string.Join(", ", new[] { address.City, address.Province, "Philippines" }
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

            _selectedRegionCode = match.Region.Code;
            _selectedLocalityCode = match.Locality.Code;
            _selectedProvince = match.Locality.Province ?? address.Province;
            _localities = await CustomerApiClient.GetPhilippineLocalitiesAsync(match.Region.Code);
            _barangays = await CustomerApiClient.GetPhilippineBarangaysAsync(match.Locality.Code);
        }
        catch (Exception ex)
        {
            _locationMessage = FriendlyError("Saved address could not be matched automatically. Choose the address manually.", ex);
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

        _selectedRegionCode = region.Code;
        _selectedLocalityCode = null;
        _selectedBarangayName = null;
        _selectedProvince = null;
        _localities = [];
        _barangays = [];
        _locationMessage = "Loading cities and municipalities...";
        Render();
        try
        {
            _localities = await CustomerApiClient.GetPhilippineLocalitiesAsync(region.Code);
            _locationMessage = _localities.Count == 0 ? "No cities or municipalities were returned for this region." : null;
        }
        catch (Exception ex)
        {
            _locationMessage = FriendlyError("Cities and municipalities could not be loaded. Check your connection and try again.", ex);
        }
        finally
        {
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

        _selectedLocalityCode = locality.Code;
        _selectedProvince = locality.Province;
        _selectedBarangayName = null;
        _barangays = [];
        _locationMessage = "Loading barangays...";
        Render();
        try
        {
            _barangays = await CustomerApiClient.GetPhilippineBarangaysAsync(locality.Code);
            _locationMessage = _barangays.Count == 0 ? "No barangays were returned for this city or municipality." : null;
        }
        catch (Exception ex)
        {
            _locationMessage = FriendlyError("Barangays could not be loaded. Check your connection and try again.", ex);
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
            : _selectedRegionCode is null
                ? null
                : _regions.FirstOrDefault(region => string.Equals(region.Code, _selectedRegionCode, StringComparison.OrdinalIgnoreCase));
    }

    private PhilippineLocalityDto? SelectedLocality()
    {
        return _localityPicker is not null && _localityPicker.SelectedIndex >= 0 && _localityPicker.SelectedIndex < _localities.Count
            ? _localities[_localityPicker.SelectedIndex]
            : _selectedLocalityCode is null
                ? null
                : _localities.FirstOrDefault(locality => string.Equals(locality.Code, _selectedLocalityCode, StringComparison.OrdinalIgnoreCase));
    }

    private PhilippineBarangayDto? SelectedBarangay()
    {
        return _barangayPicker is not null && _barangayPicker.SelectedIndex >= 0 && _barangayPicker.SelectedIndex < _barangays.Count
            ? _barangays[_barangayPicker.SelectedIndex]
            : _selectedBarangayName is null
                ? null
                : _barangays.FirstOrDefault(barangay => string.Equals(barangay.Name, _selectedBarangayName, StringComparison.OrdinalIgnoreCase));
    }

    private void PrepareFields(CustomerMeDto? customer, CustomerAddressDto? address, MotorcycleDto? motorcycle)
    {
        _firstNameEntry = Field(customer?.FirstName, "First name");
        _middleNameEntry = Field(customer?.MiddleName, "Middle name");
        _lastNameEntry = Field(customer?.LastName, "Last name");
        _emailEntry = Field(customer?.Email, "Email address", Keyboard.Email);
        _emailEntry.IsReadOnly = true;
        _phoneEntry = Field(ToPhilippineMobileDisplay(customer?.PhoneNumber), "+63 mobile number", Keyboard.Telephone);
        _phoneEntry.MaxLength = 13;
        ConfigurePhilippineMobileEntry(_phoneEntry);

        _sexPicker = new Picker
        {
            Title = "Select sex",
            TextColor = CustomerUi.Dark,
            TitleColor = CustomerUi.Muted,
            FontSize = CustomerUi.BodySize,
            FontFamily = CustomerUi.FontBody
        };
        _sexPicker.Items.Add("Female");
        _sexPicker.Items.Add("Male");
        _sexPicker.Items.Add("Prefer not to say");
        if (!string.IsNullOrWhiteSpace(customer?.Sex))
        {
            var index = _sexPicker.Items.IndexOf(customer.Sex);
            _sexPicker.SelectedIndex = index >= 0 ? index : -1;
        }

        _birthdayPicker = new DatePicker
        {
            Date = customer?.Birthdate?.Date ?? DateTime.Today.AddYears(-18),
            MinimumDate = DateTime.Today.AddYears(-100),
            MaximumDate = DateTime.Today.AddYears(-18),
            TextColor = CustomerUi.Dark,
            FontSize = CustomerUi.BodySize,
            FontFamily = CustomerUi.FontBody
        };

        _zipCodeEntry = Field(address?.PostalCode, "Zip code", Keyboard.Numeric);
        _zipCodeEntry.MaxLength = 4;
        _addressEditor = EditorField(address?.AddressLine, "House number, street, subdivision, landmark");

        _regionPicker = PickerField("Select region");
        _regionPicker.SelectedIndexChanged += async (_, _) => await RegionChangedAsync();
        SetPickerItems(_regionPicker, _regions.Select(region => region.Name));
        SelectPickerIndex(_regionPicker, IndexOf(_regions, region => string.Equals(region.Code, _selectedRegionCode, StringComparison.OrdinalIgnoreCase)));
        _regionPicker.IsEnabled = !_isLoadingLocations && _regions.Count > 0;

        _localityPicker = PickerField("Select city or municipality");
        _localityPicker.SelectedIndexChanged += async (_, _) => await LocalityChangedAsync();
        SetPickerItems(_localityPicker, _localities.Select(LocalityDisplayName));
        SelectPickerIndex(_localityPicker, IndexOf(_localities, locality => string.Equals(locality.Code, _selectedLocalityCode, StringComparison.OrdinalIgnoreCase)));
        _localityPicker.IsEnabled = !_isLoadingLocations && _selectedRegionCode is not null && _localities.Count > 0;

        _barangayPicker = PickerField("Select barangay");
        _barangayPicker.SelectedIndexChanged += (_, _) =>
        {
            if (_isUpdatingLocationPickers)
            {
                return;
            }

            _selectedBarangayName = SelectedBarangay()?.Name;
        };
        SetPickerItems(_barangayPicker, _barangays.Select(barangay => barangay.Name));
        SelectPickerIndex(_barangayPicker, IndexOf(_barangays, barangay => string.Equals(barangay.Name, _selectedBarangayName, StringComparison.OrdinalIgnoreCase)));
        _barangayPicker.IsEnabled = !_isLoadingLocations && _selectedLocalityCode is not null && _barangays.Count > 0;

        _bikeBrandEntry = Field(motorcycle?.Brand, "Brand");
        _bikeModelEntry = Field(motorcycle?.Model, "Model");
        _bikeYearEntry = Field(motorcycle?.YearModel?.ToString(CultureInfo.InvariantCulture), "Year model", Keyboard.Numeric);
        _bikePlateEntry = Field(motorcycle?.PlateNumber, "Plate number");
        _bikeEngineEntry = Field(motorcycle?.EngineType, "Engine type");
        _bikeColorEntry = Field(motorcycle?.Color, "Color");
    }

    private View ProfileHeaderCard(CustomerMeDto? customer)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 14
        };

        grid.Add(ProfilePhoto(customer), 0, 0);

        var details = new VerticalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        details.Add(Label(customer is null ? "Loading account" : FullName(customer), 18, CustomerUi.Dark, FontAttributes.Bold));
        details.Add(Label(customer is null ? "Fetching your BikeMate profile." : $"CUST-{customer.ClientId:0000}", 11, CustomerUi.Muted));
        var badges = new HorizontalStackLayout { Spacing = 6 };
        badges.Add(Badge(FormatStatus(customer?.AccountStatus ?? "loading"), CustomerUi.LightOrange, CustomerUi.Orange));
        badges.Add(Badge(customer?.EmailVerified == true ? "Email verified" : "Email pending", Color.FromArgb("#EEF1F4"), CustomerUi.Dark));
        details.Add(badges);

        var actions = new Grid { ColumnSpacing = 8 };
        actions.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actions.Add(GhostButton("Change photo", new Command(async () => await ChangeProfilePhotoAsync())), 0, 0);
        details.Add(actions);

        grid.Add(details, 1, 0);
        return Card(grid, Colors.White, 8, new Thickness(14));
    }

    private View AccountApprovalCard(CustomerMeDto? customer)
    {
        if (customer is null || CustomerRequestRules.IsApproved(customer))
        {
            return new BoxView { HeightRequest = 0, Opacity = 0 };
        }

        var stack = Section("Account approval", CustomerRequestRules.BookingBlockedMessage(customer));
        stack.Add(BadgeRow("Approval status", FormatStatus(customer.AccountStatus)));
        stack.Add(BadgeRow("Email OTP", customer.EmailVerified ? "Verified" : "Needs verification"));
        stack.Add(BadgeRow("Valid ID", string.IsNullOrWhiteSpace(customer.ValidIdImageUrl) ? "Required" : "Uploaded"));
        if (!customer.EmailVerified)
        {
            stack.Add(OrangeButton(
                _isSendingEmailOtp ? "Sending verification code..." : "Send email OTP for approval",
                new Command(async () => await SendEmailVerificationOtpAsync())));
        }
        else if (string.IsNullOrWhiteSpace(customer.ValidIdImageUrl))
        {
            stack.Add(OrangeButton("Scan and upload valid ID", new Command(async () => await ReplaceValidIdAsync())));
        }
        else
        {
            stack.Add(GhostButton("Refresh approval status", new Command(async () => await RefreshProfileAsync())));
        }

        return Card(stack, Color.FromArgb("#FFF8F4"), 8, new Thickness(14));
    }

    private View PersonalDetailsCard()
    {
        var stack = Section("Personal details", "Shown to shops and mechanics assigned to your bookings.");
        stack.Add(TwoColumnInputs(("First name", _firstNameEntry!), ("Last name", _lastNameEntry!)));
        stack.Add(InputBlock("Middle name", _middleNameEntry!));
        stack.Add(InputBlock("Sex", _sexPicker!));
        stack.Add(InputBlock("Birthdate", _birthdayPicker!));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View ContactDetailsCard(CustomerMeDto? customer)
    {
        var stack = Section("Contact details", "Keep your mobile number updated so shops and mechanics can reach you.");
        stack.Add(InputBlock("Email address", _emailEntry!));
        stack.Add(BadgeRow("Email status", customer?.EmailVerified == true ? "Verified" : "Verification pending"));
        if (customer?.EmailVerified != true && !string.IsNullOrWhiteSpace(customer?.Email))
        {
            stack.Add(OrangeButton(
                _isSendingEmailOtp ? "Sending verification code..." : "Send email OTP for approval",
                new Command(async () => await SendEmailVerificationOtpAsync())));
        }

        stack.Add(InputBlock("Mobile number", _phoneEntry!));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private async Task SendEmailVerificationOtpAsync()
    {
        if (_customer is null || string.IsNullOrWhiteSpace(_customer.Email) || _isSendingEmailOtp)
        {
            return;
        }

        _isSendingEmailOtp = true;
        Render();
        try
        {
            await CustomerApiClient.ResendEmailVerificationOtpAsync(_customer.Email);
            await DisplayAlertAsync("Verification code sent", $"Check {_customer.Email} for your BikeMate OTP.", "OK");
            await Shell.Current.GoToAsync($"{nameof(OtpVerificationPage)}?email={Uri.EscapeDataString(_customer.Email)}&fromProfile=true");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("OTP failed", ex.Message, "OK");
        }
        finally
        {
            _isSendingEmailOtp = false;
            Render();
        }
    }

    private View AddressCard()
    {
        var stack = Section("Primary service address", "Used as your default pickup or repair location.");
        if (!string.IsNullOrWhiteSpace(_locationMessage))
        {
            stack.Add(Label(_locationMessage, 11, CustomerUi.Muted));
        }

        stack.Add(InputBlock("Region", _regionPicker!));
        stack.Add(InputBlock("City or municipality", _localityPicker!));
        stack.Add(TwoColumnInputs(("Barangay", _barangayPicker!), ("Zip code", _zipCodeEntry!)));
        stack.Add(InputBlock("Complete address", _addressEditor!));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View MotorcycleCard()
    {
        var stack = Section("Motorcycle details", "These details help shops prepare parts and tools before arrival.");
        stack.Add(TwoColumnInputs(("Brand", _bikeBrandEntry!), ("Model", _bikeModelEntry!)));
        stack.Add(TwoColumnInputs(("Year", _bikeYearEntry!), ("Plate", _bikePlateEntry!)));
        stack.Add(TwoColumnInputs(("Engine", _bikeEngineEntry!), ("Color", _bikeColorEntry!)));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View AccountSecurityCard(CustomerMeDto? customer)
    {
        var stack = Section("Security and account", "Manage access and account health.");
        stack.Add(DetailLine("Account ID", customer is null ? "Loading" : $"CUST-{customer.ClientId:0000}"));
        stack.Add(DetailLine("Member since", customer is null ? "Loading" : customer.CreatedAt.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture)));
        stack.Add(DetailLine("Valid ID", string.IsNullOrWhiteSpace(customer?.ValidIdImageUrl) ? "Not uploaded" : "Uploaded"));
        stack.Add(GhostButton("Change password", new Command(async () => await Shell.Current.GoToAsync(nameof(PasswordResetPage)))));
        stack.Add(GhostButton("Log out", new Command(async () => await AppNavigation.ConfirmAndSignOutAsync(this))));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View IdentityVerificationCard(CustomerMeDto? customer)
    {
        var stack = Section(
            "Identity verification",
            "Your valid ID is used for account verification. Contact BikeMate admin if a submitted ID needs correction.");
        if (string.IsNullOrWhiteSpace(customer?.ValidIdImageUrl))
        {
            stack.Add(Label("No valid ID uploaded yet.", 11, CustomerUi.Muted));
        }
        else
        {
            stack.Add(new Border
            {
                HeightRequest = 180,
                Stroke = CustomerUi.Border,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                BackgroundColor = Color.FromArgb("#F4F4F4"),
                Content = new Image
                {
                    Source = VersionedImageSource(customer.ValidIdImageUrl, customer.UpdatedAt),
                    Aspect = Aspect.AspectFit
                }
            });
            stack.Add(BadgeRow("Document status", "Uploaded"));
        }

        if (string.IsNullOrWhiteSpace(customer?.ValidIdImageUrl))
        {
            stack.Add(GhostButton("Scan and upload valid ID", new Command(async () => await ReplaceValidIdAsync())));
        }
        if (_localIdScanResult is not null)
        {
            stack.Add(BadgeRow("Last scan", FormatReadability(_localIdScanResult.ReadabilityStatus)));
            stack.Add(Label(
                "This scan was uploaded for admin review.",
                11,
                CustomerUi.Muted));
        }
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View DangerZoneCard()
    {
        var stack = Section("Account deletion", "Deleting your account disables sign-in but keeps booking and payment records required for support and compliance.");
        stack.Add(new Button
        {
            Text = "Delete account",
            BackgroundColor = Color.FromArgb("#FFF0F0"),
            TextColor = Color.FromArgb("#B42318"),
            BorderColor = Color.FromArgb("#F5B5B5"),
            BorderWidth = 1,
            CornerRadius = 8,
            HeightRequest = 46,
            FontAttributes = FontAttributes.Bold,
            FontFamily = CustomerUi.FontDisplay,
            Command = new Command(async () => await DeleteAccountAsync())
        });
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private async Task SaveProfileAsync()
    {
        if (_customer is null || _isSaving)
        {
            return;
        }

        var firstName = Clean(_firstNameEntry?.Text);
        var middleName = Clean(_middleNameEntry?.Text);
        var lastName = Clean(_lastNameEntry?.Text);
        var email = _customer.Email;
        var phoneNumber = MobileNumberForSave(_phoneEntry?.Text);
        var sex = _sexPicker?.SelectedItem?.ToString();
        var birthdate = _birthdayPicker?.Date?.Date;
        var selectedRegion = SelectedRegion();
        var selectedLocality = SelectedLocality();
        var selectedBarangay = SelectedBarangay();
        var province = Clean(_selectedProvince) ?? Clean(selectedLocality?.Province) ?? Clean(selectedRegion?.Name);
        var city = Clean(selectedLocality?.Name);
        var barangay = Clean(selectedBarangay?.Name);
        var zipCode = Clean(_zipCodeEntry?.Text);
        var addressLine = Clean(_addressEditor?.Text);
        var bikeBrand = Clean(_bikeBrandEntry?.Text);
        var bikeModel = Clean(_bikeModelEntry?.Text);
        var yearText = Clean(_bikeYearEntry?.Text);
        var bikePlate = Clean(_bikePlateEntry?.Text);
        var bikeEngine = Clean(_bikeEngineEntry?.Text);
        var bikeColor = Clean(_bikeColorEntry?.Text);

        if (firstName is null || lastName is null)
        {
            await DisplayAlertAsync("Missing details", "First name and last name are required.", "OK");
            return;
        }

        if (selectedRegion is null || selectedLocality is null || selectedBarangay is null || zipCode is null || addressLine is null)
        {
            await DisplayAlertAsync("Missing address", "Please select your Philippine region, city or municipality, barangay, zip code, and complete service address.", "OK");
            return;
        }

        if (zipCode.Length != 4 || !zipCode.All(char.IsDigit))
        {
            await DisplayAlertAsync("Invalid zip code", "Enter a valid 4-digit Philippine zip code.", "OK");
            return;
        }

        var hasBikeDetails = bikeBrand is not null || bikeModel is not null || yearText is not null ||
                             bikePlate is not null || bikeEngine is not null || bikeColor is not null;
        if (hasBikeDetails && (bikeBrand is null || bikeModel is null))
        {
            await DisplayAlertAsync("Motorcycle details", "Bike brand and model are required when saving motorcycle details.", "OK");
            return;
        }

        int? yearModel = null;
        if (yearText is not null)
        {
            if (!int.TryParse(yearText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedYear) ||
                parsedYear < 1950 ||
                parsedYear > DateTime.Today.Year + 1)
            {
                await DisplayAlertAsync("Motorcycle year", "Enter a valid motorcycle year model.", "OK");
                return;
            }

            yearModel = parsedYear;
        }

        _isSaving = true;
        Render("Saving your account details...");
        try
        {
            await CustomerApiClient.UpdateCustomerAsync(new UpsertCustomerProfileDto(
                firstName,
                lastName,
                email,
                phoneNumber,
                middleName,
                sex,
                birthdate));

            var existingAddress = _customer.Addresses.FirstOrDefault(x => x.IsDefault) ?? _customer.Addresses.FirstOrDefault();
            await CustomerApiClient.UpsertAddressAsync(existingAddress, new UpsertCustomerAddressDto(
                existingAddress?.Label ?? "Home",
                addressLine,
                barangay,
                city,
                province,
                zipCode,
                existingAddress?.Latitude,
                existingAddress?.Longitude,
                true));

            if (hasBikeDetails)
            {
                var existingMotorcycle = _customer.Motorcycles.FirstOrDefault();
                await CustomerApiClient.UpsertMotorcycleAsync(existingMotorcycle, new UpsertMotorcycleDto(
                    bikeBrand ?? string.Empty,
                    bikeModel ?? string.Empty,
                    yearModel,
                    bikePlate,
                    bikeEngine,
                    bikeColor,
                    existingMotorcycle?.MotorcycleImageUrl));
            }

            _customer = await CustomerApiClient.GetCustomerAsync();
            _isSaving = false;
            Render("Account details saved.");
        }
        catch (Exception ex)
        {
            _isSaving = false;
            Render(FriendlyError("Account details were not saved. Please review your entries and try again.", ex));
        }
    }

    private async Task ChangeProfilePhotoAsync()
    {
        if (_customer is null)
        {
            return;
        }

        var file = await PickImageAsync("Choose profile photo");
        if (file is null)
        {
            return;
        }

        try
        {
            Render("Uploading profile photo...");
            var upload = await CustomerApiClient.UploadFileAsync(file, "profile");
            await CustomerApiClient.UpdateCustomerProfileImageAsync(upload.Url);
            await RefreshProfileAsync();
        }
        catch (Exception ex)
        {
            Render(FriendlyError("Profile photo was not updated. Try another image or retry in a moment.", ex));
        }
    }

    private async Task ReplaceValidIdAsync()
    {
        if (_customer is null)
        {
            return;
        }

        LocalIdCardScanResult scan;
        try
        {
            scan = await new LocalIdCardScannerService().ScanAsync("Scan valid ID");
        }
        catch (Exception ex)
        {
            Render(FriendlyError("Valid ID scan did not start. Check camera permissions and try again.", ex));
            return;
        }

        if (!scan.IsSuccessful)
        {
            if (scan.WasCancelled)
            {
                return;
            }

            Render(scan.ErrorMessage ?? "Valid ID scan was cancelled.");
            return;
        }

        var processedImagePath = scan.LocalProcessedImagePath;
        if (string.IsNullOrWhiteSpace(processedImagePath))
        {
            Render("BikeMate could not prepare the scanned ID image. Please scan again.");
            return;
        }

        var file = new FileResult(processedImagePath)
        {
            FileName = System.IO.Path.GetFileName(processedImagePath),
            ContentType = "image/jpeg"
        };
        try
        {
            Render("Uploading valid ID...");
            var upload = await CustomerApiClient.UploadFileAsync(file, "customer-id");
            await CustomerApiClient.UpdateCustomerValidIdAsync(upload.Url);
            _localIdScanResult = scan;
            await RefreshProfileAsync();
        }
        catch (Exception ex)
        {
            Render(FriendlyError("Valid ID was not updated. Try another image or retry in a moment.", ex));
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

    private async Task DeleteAccountAsync()
    {
        if (_customer is null)
        {
            return;
        }

        var confirm = await DisplayAlertAsync(
            "Delete account",
            "This will disable your BikeMate account and sign you out. Booking, payment, and support records may be retained for service history and compliance.",
            "Continue",
            "Cancel");
        if (!confirm)
        {
            return;
        }

        var typed = await DisplayPromptAsync("Final confirmation", "Type DELETE to confirm account deletion.", "Delete", "Cancel");
        if (!string.Equals(typed, "DELETE", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            await CustomerApiClient.DeleteCustomerAccountAsync();
            await AppNavigation.SignOutAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Account not deleted", ex.Message, "OK");
        }
    }

    private static async Task<FileResult?> PickImageAsync(string title)
    {
        return await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = title,
            FileTypes = FilePickerFileType.Images
        });
    }

    private static string FullName(CustomerMeDto customer)
    {
        return string.Join(" ", new[] { customer.FirstName, customer.MiddleName, customer.LastName }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static View ProfilePhoto(CustomerMeDto? customer, double size = 94, Color? fallbackColor = null)
    {
        return CustomerAvatar(customer, size, fallbackColor);
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

    private static View TwoColumnInputs((string Label, View Input) left, (string Label, View Input) right)
    {
        var grid = new Grid { ColumnSpacing = 10 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.Add(InputBlock(left.Label, left.Input), 0, 0);
        grid.Add(InputBlock(right.Label, right.Input), 1, 0);
        return grid;
    }

    private static View InputBlock(string label, View input)
    {
        var stack = new VerticalStackLayout { Spacing = 5 };
        stack.Add(Label(label, 11, CustomerUi.Muted, FontAttributes.Bold));
        stack.Add(new Border
        {
            Stroke = CustomerUi.Border,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb("#FAFAFA"),
            Padding = new Thickness(10, 2),
            Content = input
        });
        return stack;
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

    private static Picker PickerField(string title)
    {
        return new Picker
        {
            Title = title,
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            TitleColor = CustomerUi.Muted,
            FontSize = CustomerUi.BodySize,
            FontFamily = CustomerUi.FontBody
        };
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

    private void SelectPickerIndex(Picker picker, int index)
    {
        if (index < 0 || index >= picker.Items.Count)
        {
            return;
        }

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

    private static string LocalityDisplayName(PhilippineLocalityDto locality)
    {
        return string.IsNullOrWhiteSpace(locality.Province)
            ? locality.Name
            : $"{locality.Name}, {locality.Province}";
    }

    private static void ConfigurePhilippineMobileEntry(Entry entry)
    {
        var isFormatting = false;
        entry.Text = ToPhilippineMobileDisplay(entry.Text);
        entry.Focused += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(entry.Text))
            {
                entry.Text = "+63";
            }
        };
        entry.TextChanged += (_, args) =>
        {
            if (isFormatting)
            {
                return;
            }

            var normalized = ToPhilippineMobileDisplay(args.NewTextValue);
            if (!string.Equals(args.NewTextValue, normalized, StringComparison.Ordinal))
            {
                isFormatting = true;
                try
                {
                    entry.Text = normalized;
                    TryMoveCursorToEnd(entry, normalized);
                }
                finally
                {
                    isFormatting = false;
                }
            }
        };
    }

    private static string ToPhilippineMobileDisplay(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "+63";
        }

        var compact = new string(value.Where(char.IsDigit).ToArray());
        if (compact.Length == 0 ||
            string.Equals(compact, "6", StringComparison.Ordinal) ||
            string.Equals(compact, "63", StringComparison.Ordinal))
        {
            return "+63";
        }

        var national = compact.StartsWith("63", StringComparison.Ordinal)
            ? compact[2..]
            : compact.StartsWith("0", StringComparison.Ordinal)
                ? compact[1..]
                : compact;
        if (national.Length == 0 || national[0] != '9')
        {
            return "+63";
        }

        if (national.Length > 10)
        {
            national = national[..10];
        }

        return $"+63{national}";
    }

    private static string? MobileNumberForSave(string? value)
    {
        var display = ToPhilippineMobileDisplay(value);
        return string.Equals(display, "+63", StringComparison.Ordinal) ? null : display;
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
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(Label(label, 12, CustomerUi.Dark), 0, 0);
        grid.Add(Label(value, 12, CustomerUi.Muted, FontAttributes.Bold), 1, 0);
        return grid;
    }

    private static View Badge(string text, Color background, Color textColor)
    {
        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = background,
            Padding = new Thickness(9, 4),
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

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class CustomerHelpDeskPage : CustomerPageBase
{
    public CustomerHelpDeskPage()
    {
        Title = "Help Desk";

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 8, 16, 24),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(Header("Help Desk"));
        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                Label("How can we help?", 18, CustomerUi.Dark, FontAttributes.Bold),
                Label(
                    "Find quick answers about bookings, payments, mechanics, and emergency roadside support.",
                    13,
                    CustomerUi.Muted)
            }
        }, Colors.White, 8, new Thickness(16)));

        body.Add(Label("Frequently asked questions", 14, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(FaqRow("How do I book a repair?", "Simply open the app, describe your issue, select a time, and confirm your booking."));
        body.Add(FaqRow("How much will the repair cost?", "Each shop lists its service price before payment. Any approved adjustment should appear in your booking and payment details."));
        body.Add(FaqRow("When can I track the mechanic?", "Live tracking becomes available after payment, mechanic assignment, and location sharing."));
        body.Add(FaqRow("What should I do in an emergency?", "Use Emergency roadside help for motorcycle assistance. Contact public emergency services first for accidents, injuries, fire, or immediate danger."));

        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Label("Still need help?", 14, CustomerUi.Dark, FontAttributes.Bold),
                Label("Open Messages to contact the repair shop or assigned mechanic connected to your booking.", 11, CustomerUi.Muted),
                OrangeButton("Open messages", new Command(async () => await Shell.Current.GoToAsync("//CustomerMessagesPage")))
            }
        }, Colors.White, 8, new Thickness(16)));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private static View FaqRow(string question, string answer)
    {
        return Card(new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                Label(question, 13, CustomerUi.Dark, FontAttributes.Bold),
                Label(answer, 11, CustomerUi.Muted)
            }
        }, Colors.White, 8, new Thickness(14));
    }
}

public sealed class CustomerSchedulePage : CustomerPageBase
{
    private IReadOnlyList<ServiceRequestDto> _requests = [];
    private bool _showHistory;

    public CustomerSchedulePage()
    {
        Title = "Schedule";
        Render("Loading booked repairs...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _requests = await CustomerApiClient.GetMyRequestsAsync();
            await ScheduleBookingRemindersAsync(_requests);
            Render();
        }
        catch (Exception ex)
        {
            Render(FriendlyError("Booked repairs could not be loaded. Check your connection and try again.", ex));
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 14 };
        body.Add(Header("Booked Repairs", false));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(NoticeCard(banner));
        }

        body.Add(Tabs());

        var visibleRequests = _requests
            .Where(request => IsHistoryRequest(request) == _showHistory)
            .OrderByDescending(request => request.CreatedAt)
            .ToArray();
        if (visibleRequests.Length == 0)
        {
            body.Add(EmptyState(
                _showHistory ? "No repair history yet" : "No active repairs right now",
                _showHistory
                    ? "Completed, cancelled, and rejected bookings will appear here after your first service cycle."
                    : "When you book a repair or request emergency help, the live status will appear here.",
                _showHistory ? "View active repairs" : "Go to home",
                _showHistory
                    ? new Command(() =>
                    {
                        _showHistory = false;
                        Render();
                    })
                    : new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage")),
                _showHistory ? null : "Emergency help",
                _showHistory ? null : new Command(async () => await Shell.Current.GoToAsync(nameof(EmergencySosPage)))));
        }
        else
        {
            foreach (var request in visibleRequests)
            {
                body.Add(ScheduleCard(request));
            }
        }

        SetScaffold(new ScrollView { Content = body }, "Schedule");
    }

    private View Tabs()
    {
        var grid = new Grid
        {
            HeightRequest = 46,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 0
        };

        grid.Add(TabButton("Upcoming", !_showHistory, () =>
        {
            _showHistory = false;
            Render();
        }), 0, 0);
        grid.Add(TabButton("History", _showHistory, () =>
        {
            _showHistory = true;
            Render();
        }), 1, 0);

        return new Border
        {
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Colors.White,
            Content = grid
        };
    }

    private static Button TabButton(string text, bool selected, Action select)
    {
        return new Button
        {
            Text = text,
            Command = new Command(select),
            BackgroundColor = selected ? CustomerUi.Orange : Colors.White,
            TextColor = selected ? Colors.White : CustomerUi.Dark,
            CornerRadius = 7,
            FontAttributes = FontAttributes.Bold,
            FontFamily = CustomerUi.FontDisplay,
            FontSize = CustomerUi.BodySize,
            HeightRequest = 42,
            MinimumHeightRequest = 42,
            Padding = new Thickness(12, 0)
        };
    }

    private static bool IsHistoryRequest(ServiceRequestDto request)
    {
        return request.CurrentStatus.Equals("completed", StringComparison.OrdinalIgnoreCase) ||
               request.CurrentStatus.Equals("cancelled", StringComparison.OrdinalIgnoreCase) ||
               request.CurrentStatus.Equals("rejected", StringComparison.OrdinalIgnoreCase);
    }

    private static View ScheduleCard(ServiceRequestDto request)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(new Image { Source = CustomerUi.Image(CustomerUi.OnlineBikeRepairImage), WidthRequest = 54, HeightRequest = 54, Aspect = Aspect.AspectFill }, 0, 0);

        var text = new VerticalStackLayout { Spacing = 2, Margin = new Thickness(10, 0, 0, 0) };
        text.Add(Label(request.ServiceName ?? "Bike repair", 13, CustomerUi.Dark, FontAttributes.Bold));
        text.Add(Label(request.MechanicName ?? request.ShopName ?? "Waiting for assignment", 11, CustomerUi.Muted));
        text.Add(Label(
            $"Timing: {request.ScheduledAt?.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture) ?? "Shop confirmation"}",
            11,
            CustomerUi.Muted));
        text.Add(Label(
            $"Booked: {request.CreatedAt.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture)}",
            10,
            CustomerUi.Muted));
        text.Add(Label(
            CustomerRequestRules.IsEmergency(request)
                ? "No upfront payment"
                : Money(request.FinalTotal > 0 ? request.FinalTotal : request.EstimatedTotal),
            11,
            CustomerUi.Dark));
        grid.Add(text, 1, 0);
        grid.Add(StatusChip(FormatStatus(request.CurrentStatus)), 2, 0);

        var card = Card(grid, Colors.White, 8, new Thickness(12));
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.GoToAsync($"{nameof(BookingDetailsPage)}?requestId={request.RequestId}"))
        });
        return card;
    }
}

public sealed class CustomerMessagesPage : CustomerPageBase
{
    private IReadOnlyList<ConversationSummaryDto> _conversations = [];
    private string _filter = "all";
    private string _searchText = string.Empty;
    private bool _isLoading;

    public CustomerMessagesPage()
    {
        Title = "Messages";
        Render("Loading conversations...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        try
        {
            _conversations = await CustomerApiClient.GetConversationsAsync();
            Render();
        }
        catch (ApiSessionExpiredException)
        {
        }
        catch (Exception ex)
        {
            Render(FriendlyError("Messages could not be loaded. Check your connection and try again.", ex));
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 8, 16, 18),
            Spacing = 12,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(Header("Messages", false));
        var subtitle = Label("Messages from BikeMate Emergency, repair shops, and assigned mechanics.", 11, CustomerUi.Muted);
        subtitle.HorizontalTextAlignment = TextAlignment.Center;
        body.Add(subtitle);

        var search = new Entry
        {
            Text = _searchText,
            Placeholder = "Search messages or booking ID",
            ReturnType = ReturnType.Search,
            BackgroundColor = Colors.White,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            FontFamily = CustomerUi.FontBody,
            FontSize = CustomerUi.BodySize,
            HeightRequest = 48
        };
        search.Completed += (_, _) =>
        {
            _searchText = search.Text?.Trim() ?? string.Empty;
            Render();
        };
        body.Add(Card(search, Colors.White, 8, new Thickness(10, 0)));
        body.Add(FilterTabs());

        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        var visibleConversations = _conversations
            .Where(MatchesFilter)
            .Where(MatchesSearch)
            .ToArray();
        if (visibleConversations.Length == 0)
        {
            body.Add(EmptyState(
                string.IsNullOrWhiteSpace(_searchText) ? "No conversations yet" : "No matching conversations",
                string.IsNullOrWhiteSpace(_searchText)
                    ? "Messages open automatically when a shop, mechanic, or BikeMate emergency responder is connected to your booking."
                    : "Try a booking ID, shop name, mechanic name, or clear the search to see all conversations.",
                string.IsNullOrWhiteSpace(_searchText) ? "Go to home" : "Clear search",
                string.IsNullOrWhiteSpace(_searchText)
                    ? new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))
                    : new Command(() =>
                    {
                        _searchText = string.Empty;
                        Render();
                    }),
                string.IsNullOrWhiteSpace(_searchText) ? "View schedule" : null,
                string.IsNullOrWhiteSpace(_searchText) ? new Command(async () => await Shell.Current.GoToAsync("//CustomerSchedulePage")) : null));
        }
        else
        {
            foreach (var conversation in visibleConversations)
            {
                body.Add(MessageRow(conversation));
            }
        }

        SetScaffold(new ScrollView { Content = body }, "Messages");
    }

    private View FilterTabs()
    {
        var grid = new Grid
        {
            HeightRequest = 44,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };
        grid.Add(FilterButton("All", "all"), 0, 0);
        grid.Add(FilterButton("Emergency", "emergency"), 1, 0);
        grid.Add(FilterButton("Shops", "shop"), 2, 0);
        grid.Add(FilterButton("Mechanics", "mechanic"), 3, 0);
        return new Border
        {
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Colors.White,
            Content = grid
        };
    }

    private Button FilterButton(string label, string value)
    {
        var selected = _filter == value;
        return new Button
        {
            Text = label,
            BackgroundColor = selected ? CustomerUi.Orange : Colors.White,
            TextColor = selected ? Colors.White : CustomerUi.Dark,
            FontFamily = CustomerUi.FontDisplay,
            FontAttributes = FontAttributes.Bold,
            FontSize = CustomerUi.CaptionSize,
            CornerRadius = 7,
            Padding = new Thickness(4, 0),
            Command = new Command(() =>
            {
                _filter = value;
                Render();
            })
        };
    }

    private bool MatchesFilter(ConversationSummaryDto conversation)
    {
        return _filter switch
        {
            "emergency" => IsEmergencyConversation(conversation),
            "shop" => IsShopConversation(conversation),
            "mechanic" => IsMechanicConversation(conversation),
            _ => true
        };
    }

    private bool MatchesSearch(ConversationSummaryDto conversation)
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            return true;
        }

        return conversation.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
               (conversation.Subtitle?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (conversation.LastMessageText?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (conversation.RequestId?.ToString(CultureInfo.InvariantCulture).Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static View MessageRow(ConversationSummaryDto conversation)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var avatar = ConversationAvatar(conversation);
        grid.Add(avatar, 0, 0);

        var text = new VerticalStackLayout { Spacing = 5, Margin = new Thickness(10, 0, 8, 0) };
        var titleRow = new HorizontalStackLayout { Spacing = 6 };
        titleRow.Add(Label(conversation.Title, 13, CustomerUi.Dark, FontAttributes.Bold));
        titleRow.Add(TypeBadge(conversation));
        text.Add(titleRow);
        var bookingLine = Label(
            conversation.RequestId is null
                ? conversation.Subtitle ?? "BikeMate conversation"
                : $"BM-{conversation.RequestId:000000} | {FormatStatus(conversation.BookingStatus ?? "pending")}",
            10,
            CustomerUi.Muted);
        text.Add(bookingLine);
        var preview = Label(Preview(conversation.LastMessageText ?? conversation.Subtitle ?? "Open conversation"), 11, CustomerUi.Dark);
        preview.LineBreakMode = LineBreakMode.TailTruncation;
        preview.MaxLines = 2;
        text.Add(preview);
        grid.Add(text, 1, 0);

        var meta = new VerticalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.End };
        meta.Add(Label(FriendlyTime(conversation.LastMessageAt), 9, CustomerUi.Muted));
        if (conversation.UnreadCount > 0)
        {
            meta.Add(new Border
            {
                BackgroundColor = CustomerUi.Orange,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                HeightRequest = 20,
                MinimumWidthRequest = 20,
                Padding = new Thickness(6, 1),
                HorizontalOptions = LayoutOptions.End,
                Content = new Label
                {
                    Text = conversation.UnreadCount > 99 ? "99+" : conversation.UnreadCount.ToString(CultureInfo.InvariantCulture),
                    TextColor = Colors.White,
                    FontSize = CustomerUi.CaptionSize,
                    FontFamily = CustomerUi.FontCaptionBold,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            });
        }
        grid.Add(meta, 2, 0);

        var row = Card(grid, Colors.White, 8, new Thickness(12));
        row.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.Navigation.PushAsync(new CustomerChatPage(conversation.ConversationId)))
        });
        return row;
    }

    private static View ConversationAvatar(ConversationSummaryDto conversation)
    {
        if (!string.IsNullOrWhiteSpace(conversation.OtherProfileImageUrl))
        {
            return new Border
            {
                WidthRequest = 50,
                HeightRequest = 50,
                Stroke = CustomerUi.Border,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 25 },
                Content = new Image
                {
                    Source = conversation.OtherProfileImageUrl,
                    Aspect = Aspect.AspectFill
                }
            };
        }

        var background = IsEmergencyConversation(conversation)
            ? Color.FromArgb("#FFE1E1")
            : IsShopConversation(conversation)
                ? CustomerUi.LightOrange
                : Color.FromArgb("#EEF1F4");
        return Avatar(Initials(conversation.Title), 50, background);
    }

    private static View TypeBadge(ConversationSummaryDto conversation)
    {
        var isEmergency = IsEmergencyConversation(conversation);
        var isShop = IsShopConversation(conversation);
        var background = isEmergency
            ? Color.FromArgb("#FFE1E1")
            : isShop
                ? CustomerUi.LightOrange
                : Color.FromArgb("#EEF1F4");
        var textColor = isEmergency
            ? Color.FromArgb("#B42318")
            : isShop
                ? CustomerUi.Orange
                : CustomerUi.Dark;
        return new Border
        {
            BackgroundColor = background,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(7, 2),
            Content = Label(isEmergency ? "EMERGENCY" : isShop ? "SHOP" : "MECHANIC", 9, textColor, FontAttributes.Bold)
        };
    }

    private static bool IsEmergencyConversation(ConversationSummaryDto conversation)
    {
        return conversation.ConversationType is "emergency_support" or "emergency_request";
    }

    private static bool IsShopConversation(ConversationSummaryDto conversation)
    {
        return conversation.ConversationType == "booking_shop";
    }

    private static bool IsMechanicConversation(ConversationSummaryDto conversation)
    {
        return conversation.ConversationType is "booking_mechanic" or "service_request";
    }

    private static string Preview(string text)
    {
        return string.Join(" ", text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string FriendlyTime(DateTime? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var local = value.Value.ToLocalTime();
        return local.Date == DateTime.Today
            ? local.ToString("h:mm tt", CultureInfo.InvariantCulture)
            : local.Year == DateTime.Today.Year
                ? local.ToString("MMM d", CultureInfo.InvariantCulture)
                : local.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
    }
}

public sealed class CustomerChatPage : CustomerPageBase, IQueryAttributable
{
    private int _conversationId;
    private CustomerMeDto? _customer;
    private ConversationSummaryDto? _conversation;
    private IReadOnlyList<MessageDto> _messages = [];
    private Entry? _messageEntry;
    private string _draftMessage = string.Empty;
    private bool _isSending;

    public CustomerChatPage()
    {
        Title = "Chat";
        Render("Loading chat...");
    }

    public CustomerChatPage(int conversationId)
        : this()
    {
        _conversationId = conversationId;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("conversationId", out var value) &&
            int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var conversationId))
        {
            _conversationId = conversationId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_conversationId <= 0)
        {
            Render("Open a conversation from Messages first.");
            return;
        }

        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            var conversations = await CustomerApiClient.GetConversationsAsync();
            _conversation = conversations.FirstOrDefault(x => x.ConversationId == _conversationId);
            _messages = await CustomerApiClient.GetMessagesAsync(_conversationId);
            await CustomerApiClient.MarkConversationReadAsync(_conversationId);
            Render();
        }
        catch (Exception ex)
        {
            Render(FriendlyError("This chat could not be loaded. Check your connection and try again.", ex));
        }
    }

    private void Render(string? banner = null)
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };

        root.Add(BuildHeader(), 0, 0);
        root.Add(new ScrollView { Content = BuildMessages(banner) }, 0, 1);
        root.Add(BuildComposer(), 0, 2);

        SetScaffold(root, "Messages", false);
    }

    private View BuildHeader()
    {
        var grid = new Grid
        {
            Padding = new Thickness(14, 10),
            BackgroundColor = Colors.White,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Add(new Button
        {
            Text = "<",
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            FontSize = 13,
            WidthRequest = 44,
            HeightRequest = 40,
            CornerRadius = 8,
            Padding = new Thickness(0),
            Command = new Command(async () =>
            {
                if (Shell.Current.Navigation.NavigationStack.Count > 1)
                {
                    await Shell.Current.Navigation.PopAsync();
                }
                else
                {
                    await Shell.Current.GoToAsync("..");
                }
            })
        }, 0, 0);
        grid.Add(Avatar(Initials(_conversation?.Title), 42, CustomerUi.LightOrange), 1, 0);

        var who = new VerticalStackLayout { Spacing = 1, Margin = new Thickness(8, 0, 0, 0) };
        who.Add(Label(_conversation?.Title ?? "Conversation", 13, CustomerUi.Dark, FontAttributes.Bold));
        who.Add(Label(PartnerCaption(), 10, CustomerUi.Muted));
        grid.Add(who, 2, 0);

        grid.Add(HeaderIcon("Info", BookingInfo()), 3, 0);

        return new Border
        {
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            BackgroundColor = Colors.White,
            Content = grid
        };
    }

    private static Button HeaderIcon(string text, string title)
    {
        return new Button
        {
            Text = text,
            FontSize = CustomerUi.CaptionSize,
            FontFamily = CustomerUi.FontCaptionBold,
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Orange,
            WidthRequest = text.Length > 2 ? 60 : 40,
            HeightRequest = 40,
            MinimumHeightRequest = 40,
            Padding = new Thickness(10, 0),
            FontAttributes = FontAttributes.Bold,
            Command = new Command(async () => await Shell.Current.DisplayAlertAsync("Contact", title, "OK"))
        };
    }

    private View BuildMessages(string? banner)
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 12, 16, 20),
            Spacing = 10,
            BackgroundColor = CustomerUi.Page
        };
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        if (_conversation?.RequestId is not null)
        {
            body.Add(BookingContextCard());
        }

        if (_messages.Count == 0)
        {
            body.Add(Card(Label("No messages yet. Send a message to begin the conversation.", 12, CustomerUi.Muted)));
            return body;
        }

        DateTime? currentDate = null;
        foreach (var message in _messages.OrderBy(x => x.CreatedAt))
        {
            var localDate = message.CreatedAt.ToLocalTime().Date;
            if (currentDate != localDate)
            {
                currentDate = localDate;
                body.Add(DateDivider(localDate));
            }

            body.Add(Bubble(message, message.SenderUserId == _customer?.UserId));
        }

        return body;
    }

    private static View Bubble(MessageDto message, bool mine)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        if (!mine && IsAutomatedMessage(message.MessageText))
        {
            stack.Add(Label("AUTOMATED BOOKING UPDATE", 9, CustomerUi.Orange, FontAttributes.Bold));
        }

        if (!string.IsNullOrWhiteSpace(message.MessageText))
        {
            stack.Add(Label(message.MessageText, 12, mine ? Colors.White : CustomerUi.Dark));
        }

        if (!string.IsNullOrWhiteSpace(message.AttachmentUrl))
        {
            stack.Add(AttachmentPreview(message.AttachmentUrl, mine));
        }

        var time = Label(
            message.CreatedAt.ToLocalTime().ToString("h:mm tt", CultureInfo.InvariantCulture),
            9,
            mine ? Color.FromArgb("#FFF0E9") : CustomerUi.Muted);
        time.HorizontalTextAlignment = mine ? TextAlignment.End : TextAlignment.Start;
        stack.Add(time);

        var bubble = Card(stack, mine ? CustomerUi.Orange : Colors.White, 8, new Thickness(12, 9));
        bubble.HorizontalOptions = mine ? LayoutOptions.End : LayoutOptions.Start;
        bubble.MaximumWidthRequest = 300;
        return bubble;
    }

    private static View AttachmentPreview(string attachmentUrl, bool mine)
    {
        var textColor = mine ? Colors.White : CustomerUi.Dark;
        var fileName = Uri.TryCreate(attachmentUrl, UriKind.Absolute, out var uri)
            ? System.IO.Path.GetFileName(uri.LocalPath)
            : System.IO.Path.GetFileName(attachmentUrl);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "Attachment";
        }

        View preview = IsImageUrl(attachmentUrl)
            ? new Image
            {
                Source = attachmentUrl,
                HeightRequest = 150,
                WidthRequest = 220,
                Aspect = Aspect.AspectFill,
                BackgroundColor = Color.FromArgb("#F1F1F1")
            }
            : new HorizontalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    Label("File", 11, textColor, FontAttributes.Bold),
                    Label(fileName, 11, textColor)
                }
            };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            if (Uri.TryCreate(attachmentUrl, UriKind.Absolute, out var openUri))
            {
                await Launcher.Default.OpenAsync(openUri);
            }
        };
        preview.GestureRecognizers.Add(tap);
        return preview;
    }

    private static bool IsImageUrl(string attachmentUrl)
    {
        var extension = System.IO.Path.GetExtension(Uri.TryCreate(attachmentUrl, UriKind.Absolute, out var uri) ? uri.LocalPath : attachmentUrl);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private View BuildComposer()
    {
        _messageEntry = new Entry
        {
            Text = _draftMessage,
            Placeholder = "Write a message",
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            FontFamily = CustomerUi.FontBody,
            FontSize = CustomerUi.BodySize
        };
        _messageEntry.TextChanged += (_, e) => _draftMessage = e.NewTextValue ?? string.Empty;

        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(14, 8, 14, 12),
            Spacing = 8,
            BackgroundColor = Colors.White
        };
        var attachments = new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                CompactAction("Attach file", new Command(async () => await PickAndSendAttachmentAsync(false))),
                CompactAction("Add photo", new Command(async () => await PickAndSendAttachmentAsync(true)))
            }
        };
        stack.Add(attachments);

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        grid.Add(new Border
        {
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb("#F8F8F8"),
            Padding = new Thickness(10, 0),
            Content = _messageEntry
        }, 0, 0);
        grid.Add(new Button
        {
            Text = _isSending ? "Sending" : "Send",
            BackgroundColor = CustomerUi.Orange,
            TextColor = Colors.White,
            CornerRadius = 8,
            WidthRequest = 76,
            FontFamily = CustomerUi.FontDisplay,
            FontAttributes = FontAttributes.Bold,
            Command = new Command(async () => await SendAsync())
        }, 1, 0);
        stack.Add(grid);
        return new Border
        {
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            BackgroundColor = Colors.White,
            Content = stack
        };
    }

    private View BookingContextCard()
    {
        var stack = new VerticalStackLayout { Spacing = 7 };
        var isEmergency = _conversation!.ConversationType is "emergency_support" or "emergency_request";
        var top = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        top.Add(Label(
            $"{(isEmergency ? "Emergency request" : "Booking")} BM-{_conversation.RequestId:000000}",
            12,
            CustomerUi.Dark,
            FontAttributes.Bold), 0, 0);
        top.Add(new Border
        {
            BackgroundColor = CustomerUi.LightOrange,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(8, 3),
            Content = Label(FormatStatus(_conversation.BookingStatus ?? "pending"), 9, CustomerUi.Orange, FontAttributes.Bold)
        }, 1, 0);
        stack.Add(top);
        stack.Add(Label(
            _conversation.Subtitle ?? (isEmergency ? "BikeMate emergency support" : "Booking conversation"),
            10,
            CustomerUi.Muted));
        if (_conversation.ScheduledAt is not null)
        {
            stack.Add(Label(
                $"Scheduled {_conversation.ScheduledAt.Value.ToLocalTime():MMM d, yyyy 'at' h:mm tt}",
                10,
                CustomerUi.Dark));
        }

        var emergencyClosed = isEmergency && CustomerRequestRules.IsClosedStatus(_conversation.BookingStatus);
        var openBooking = GhostButton(
            emergencyClosed ? "Emergency completed" : isEmergency ? "Return to emergency tracking" : "View booking details",
            new Command(async () =>
            {
                if (emergencyClosed)
                {
                    await DisplayAlertAsync("Emergency completed", "Responder tracking is no longer available after emergency roadside help is completed.", "OK");
                    return;
                }

                var route = isEmergency
                    ? $"{nameof(ActiveEmergencyTrackingPage)}?requestId={_conversation.RequestId}"
                    : $"{nameof(BookingDetailsPage)}?requestId={_conversation.RequestId}";
                await Shell.Current.GoToAsync(route);
            }));
        openBooking.HeightRequest = 38;
        stack.Add(openBooking);
        return Card(stack, Colors.White, 8, new Thickness(12));
    }

    private static View DateDivider(DateTime date)
    {
        var text = date == DateTime.Today
            ? "Today"
            : date == DateTime.Today.AddDays(-1)
                ? "Yesterday"
                : date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
        var label = Label(text, 9, CustomerUi.Muted, FontAttributes.Bold);
        label.HorizontalTextAlignment = TextAlignment.Center;
        label.HorizontalOptions = LayoutOptions.Fill;
        return label;
    }

    private static Button CompactAction(string text, ICommand command)
    {
        return new Button
        {
            Text = text,
            Command = command,
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            BorderColor = CustomerUi.Border,
            BorderWidth = 1,
            CornerRadius = 8,
            HeightRequest = 40,
            MinimumHeightRequest = 40,
            Padding = new Thickness(12, 0),
            FontFamily = CustomerUi.FontDisplay,
            FontAttributes = FontAttributes.Bold,
            FontSize = CustomerUi.CaptionSize
        };
    }

    private string PartnerCaption()
    {
        var partner = _conversation?.ConversationType switch
        {
            "emergency_support" or "emergency_request" => "BikeMate emergency support",
            "booking_shop" => "Repair shop",
            _ => "Assigned mechanic"
        };
        return _conversation?.RequestId is null
            ? partner
            : $"{partner} | BM-{_conversation.RequestId:000000}";
    }

    private string BookingInfo()
    {
        if (_conversation?.RequestId is null)
        {
            return _conversation?.Title ?? "BikeMate conversation";
        }

        var isEmergency = _conversation.ConversationType is "emergency_support" or "emergency_request";
        return
            $"{(isEmergency ? "Emergency request" : "Booking")} BM-{_conversation.RequestId:000000}\n" +
            $"Partner: {_conversation.Title}\n" +
            $"Status: {FormatStatus(_conversation.BookingStatus ?? "pending")}\n" +
            $"{_conversation.Subtitle}";
    }

    private static bool IsAutomatedMessage(string text)
    {
        return text.StartsWith("Booking BM-", StringComparison.Ordinal) ||
               text.StartsWith("BikeMate Emergency received", StringComparison.Ordinal) ||
               (text.StartsWith("Hi ", StringComparison.Ordinal) &&
                text.Contains("assigned to booking BM-", StringComparison.Ordinal));
    }

    private async Task SendAsync()
    {
        var text = (_messageEntry?.Text ?? _draftMessage).Trim();
        if (_conversationId <= 0 || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        try
        {
            _isSending = true;
            Render();
            await CustomerApiClient.SendMessageAsync(_conversationId, text);
            _draftMessage = string.Empty;
            if (_messageEntry is not null)
            {
                _messageEntry.Text = string.Empty;
            }
            _isSending = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Message not sent", ex.Message, "OK");
        }
        finally
        {
            _isSending = false;
        }
    }

    private async Task PickAndSendAttachmentAsync(bool imageOnly)
    {
        if (_conversationId <= 0 || _isSending)
        {
            return;
        }

        try
        {
            var file = await FilePicker.Default.PickAsync(imageOnly
                ? PickOptions.Images
                : new PickOptions
                {
                    PickerTitle = "Select an attachment",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        [DevicePlatform.Android] = new[]
                        {
                            "image/jpeg",
                            "image/png",
                            "image/webp",
                            "application/pdf",
                            "text/plain",
                            "application/msword",
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                        }
                    })
                });
            if (file is null)
            {
                return;
            }

            _isSending = true;
            Render("Uploading attachment...");
            var uploaded = await CustomerApiClient.UploadFileAsync(file);
            var text = string.IsNullOrWhiteSpace(_draftMessage)
                ? uploaded.FileName
                : _draftMessage.Trim();
            await CustomerApiClient.SendMessageAsync(_conversationId, text, uploaded.Url);
            _draftMessage = string.Empty;
            if (_messageEntry is not null)
            {
                _messageEntry.Text = string.Empty;
            }

            _isSending = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Attachment not sent", ex.Message, "OK");
        }
        finally
        {
            _isSending = false;
        }
    }
}

public sealed class CustomerPaymentsPage : CustomerPageBase
{
    private IReadOnlyList<PaymentDto> _payments = [];
    private IReadOnlyList<ServiceRequestDto> _requests = [];
    private bool _showHistory;

    public CustomerPaymentsPage()
    {
        Title = "Payments";
        Render("Loading payment history...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _payments = await CustomerApiClient.GetPaymentHistoryAsync();
            _requests = await CustomerApiClient.GetMyRequestsAsync();
            await ScheduleBookingRemindersAsync(_requests);
            Render();
        }
        catch (Exception ex)
        {
            Render(FriendlyError("Payment data could not be loaded. Check your connection and try again.", ex));
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(14), Spacing = 12 };
        body.Add(Header("Payments", false));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(NoticeCard(banner));
        }

        body.Add(PaymentTabs());

        var visiblePayments = _payments
            .Where(payment => IsHistoricalPayment(payment) == _showHistory)
            .OrderByDescending(payment => payment.PaidAt ?? payment.CreatedAt)
            .ToArray();
        if (visiblePayments.Length == 0)
        {
            body.Add(EmptyState(
                _showHistory ? "No payment history yet" : "No payment due",
                _showHistory
                    ? "Paid, cancelled, failed, and refunded payments will appear here once a checkout has been processed."
                    : "When a shop confirms pricing for a booking, the secure PayMongo checkout will appear here.",
                _showHistory ? "View ongoing" : "View schedule",
                _showHistory
                    ? new Command(() =>
                    {
                        _showHistory = false;
                        Render();
                    })
                    : new Command(async () => await Shell.Current.GoToAsync("//CustomerSchedulePage")),
                _showHistory ? null : "Go to home",
                _showHistory ? null : new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))));
        }
        else
        {
            foreach (var payment in visiblePayments)
            {
                var request = _requests.FirstOrDefault(x => x.RequestId == payment.RequestId);
                body.Add(PaymentRow(payment, request));
            }
        }

        SetScaffold(new ScrollView { Content = body }, "Payments");
    }

    private View PaymentTabs()
    {
        var grid = new Grid
        {
            HeightRequest = 46,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 0
        };

        grid.Add(PaymentTabButton("Ongoing", !_showHistory, () =>
        {
            _showHistory = false;
            Render();
        }), 0, 0);
        grid.Add(PaymentTabButton("History", _showHistory, () =>
        {
            _showHistory = true;
            Render();
        }), 1, 0);

        return new Border
        {
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Colors.White,
            Content = grid
        };
    }

    private static Button PaymentTabButton(string text, bool selected, Action select)
    {
        return new Button
        {
            Text = text,
            Command = new Command(select),
            BackgroundColor = selected ? CustomerUi.Orange : Colors.White,
            TextColor = selected ? Colors.White : CustomerUi.Dark,
            CornerRadius = 7,
            FontAttributes = FontAttributes.Bold,
            FontFamily = CustomerUi.FontDisplay,
            FontSize = CustomerUi.BodySize,
            HeightRequest = 42,
            MinimumHeightRequest = 42,
            Padding = new Thickness(12, 0)
        };
    }

    private static bool IsHistoricalPayment(PaymentDto payment)
    {
        return payment.Status.Equals("paid", StringComparison.OrdinalIgnoreCase) ||
               payment.Status.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
               payment.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase) ||
               payment.Status.Equals("refunded", StringComparison.OrdinalIgnoreCase);
    }

    private static View PaymentRow(PaymentDto payment, ServiceRequestDto? request)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(new Image { Source = CustomerUi.Image(CustomerUi.OnlineBikeRepairImage), WidthRequest = 64, HeightRequest = 64, Aspect = Aspect.AspectFill }, 0, 0);

        var text = new VerticalStackLayout { Spacing = 6, Margin = new Thickness(8, 0, 0, 0) };
        text.Add(Label(request?.ServiceName ?? $"Request #{payment.RequestId}", 12, CustomerUi.Dark, FontAttributes.Bold));
        text.Add(Label($"Amount: {Money(payment.Amount)}", 10, CustomerUi.Dark));
        text.Add(Label(
            string.Equals(payment.Status, "paid", StringComparison.OrdinalIgnoreCase)
                ? "Tap to view receipt and invoice."
                : "Tap to review or continue secure checkout.",
            10,
            CustomerUi.Muted));
        grid.Add(text, 1, 0);
        grid.Add(StatusChip(FormatStatus(payment.Status)), 2, 0);

        var row = Card(grid, Colors.White, 8, new Thickness(10));
        row.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                var route = string.Equals(payment.Status, "paid", StringComparison.OrdinalIgnoreCase)
                    ? $"{nameof(PaymentReceiptPage)}?paymentId={payment.PaymentId}"
                    : $"{nameof(PaymentCheckoutPage)}?paymentId={payment.PaymentId}";
                await Shell.Current.GoToAsync(route);
            })
        });
        return row;
    }
}

public sealed class PaymentOptionsPage : CustomerPageBase
{
    private ServiceRequestDto? _request;
    private bool _isLoading = true;

    public PaymentOptionsPage()
    {
        Title = "Review and Pay";
        Render();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BookingDraft.RequestId > 0)
        {
            try
            {
                _request = await CustomerApiClient.GetRequestAsync(BookingDraft.RequestId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Payment summary request refresh failed: {ex}");
            }
        }

        _isLoading = false;
        Render();
    }

    private void Render()
    {
        var selectedService = BookingDraft.SelectedService();
        var hasPricedBooking = BookingDraft.RequestId > 0 &&
            BookingDraft.SelectedShopId is not null &&
            BookingDraft.SelectedShopServiceId is not null;
        var amount = _request is null
            ? BookingDraft.SelectedItemsTotal()
            : RequestTotal(_request);
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 0, 14, 18),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(BookingVisuals.FlowHeader("Review and pay"));
        body.Add(BookingVisuals.Text("Confirm your payment details", 18, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(BookingVisuals.Text(
            "Review the selected shop, service, and total before opening secure checkout.",
            11,
            CustomerUi.Muted));
        if (hasPricedBooking)
        {
            body.Add(BookingVisuals.ShopProfileCard(
                BookingDraft.DisplayShopName(_request?.ShopName),
                _request?.ShopImageUrl ?? BookingDraft.SelectedShopImageUrl,
                _request?.ShopLogoUrl ?? BookingDraft.SelectedShopLogoUrl,
                RequestServiceTitle(_request) ?? selectedService?.ServiceName ?? "Selected service"));
        }

        if (_isLoading)
        {
            body.Add(new ActivityIndicator
            {
                IsRunning = true,
                Color = CustomerUi.Orange,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 20)
            });
        }
        else
        {
            body.Add(PaymentAmountCard(amount, hasPricedBooking));
            body.Add(BookingSummaryCard(selectedService));
            body.Add(SecureCheckoutCard());
        }

        var continueButton = BookingVisuals.PrimaryButton(
            hasPricedBooking ? "Continue to secure checkout" : "Choose a repair shop",
            new Command(async () =>
        {
            if (!hasPricedBooking)
            {
                await Shell.Current.GoToAsync(nameof(StoreSelectionPage));
                return;
            }

            var route = BookingDraft.RequestId > 0
                ? $"{nameof(PaymentCheckoutPage)}?requestId={BookingDraft.RequestId}"
                : nameof(PaymentCheckoutPage);
            await Shell.Current.GoToAsync(route);
        }));
        continueButton.IsEnabled = !_isLoading;
        continueButton.Opacity = continueButton.IsEnabled ? 1 : 0.55;
        body.Add(continueButton);
        body.Add(BookingVisuals.SecondaryButton("Back to shop details", new Command(async () => await Shell.Current.GoToAsync(".."))));

        SetScaffold(new ScrollView { Content = body }, "Payments", false);
    }

    private static View PaymentAmountCard(decimal amount, bool hasPricedBooking)
    {
        var stack = new VerticalStackLayout { Spacing = 5 };
        stack.Add(BookingVisuals.Text("AMOUNT DUE", 9, CustomerUi.Muted, FontAttributes.Bold));
        stack.Add(BookingVisuals.Text(
            hasPricedBooking ? Money(amount) : "To be finalized",
            22,
            CustomerUi.Dark,
            FontAttributes.Bold));
        stack.Add(BookingVisuals.Text(
            hasPricedBooking
                ? "Based on the service and verified shop you selected."
                : "Choose a shop and service to calculate the checkout amount.",
            10,
            CustomerUi.Muted));
        return BookingVisuals.WhiteCard(stack, 8, new Thickness(16));
    }

    private View BookingSummaryCard(ShopServiceDto? service)
    {
        var stack = new VerticalStackLayout { Spacing = 11 };
        stack.Add(BookingVisuals.Text("Booking summary", 13, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(PaymentDetailRow("Repair shop", BookingDraft.DisplayShopName(_request?.ShopName)));
        stack.Add(PaymentDetailRow("Services", _request is null ? BookingDraft.SelectedServicesSummary() : RequestServiceTitle(_request)));
        stack.Add(PaymentDetailRow("Products", _request is null ? BookingDraft.SelectedProductsSummary() : RequestProductsTitle(_request)));
        stack.Add(PaymentDetailRow("Vehicle", $"{BookingDraft.Brand} {BookingDraft.Model}".Trim()));
        stack.Add(PaymentDetailRow("Plate number", BookingDraft.NormalizePlate(BookingDraft.PlateNumber)));
        stack.Add(PaymentDetailRow("Assistance method", BookingDraft.AssistanceMethod));
        stack.Add(PaymentDetailRow("Timing", BookingDraft.FormatSchedule(_request?.ScheduledAt)));
        return BookingVisuals.WhiteCard(stack, 8, new Thickness(14));
    }

    private static View SecureCheckoutCard()
    {
        var stack = new VerticalStackLayout { Spacing = 7 };
        stack.Add(BookingVisuals.Text("Secure checkout", 13, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(BookingVisuals.Text(
            "PayMongo will display the payment methods available for this checkout, including supported cards and e-wallets.",
            10,
            CustomerUi.Muted));
        stack.Add(BookingVisuals.Text(
            "BikeMate does not store your card number, wallet PIN, or payment credentials.",
            10,
            CustomerUi.Muted));
        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb("#EEF7F1"),
            Padding = new Thickness(14),
            Content = stack
        };
    }

    private static View PaymentDetailRow(string label, string value)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };
        grid.Add(BookingVisuals.Text(label, 10, CustomerUi.Muted), 0, 0);
        var valueLabel = BookingVisuals.Text(
            string.IsNullOrWhiteSpace(value) ? "Not available" : value,
            10,
            CustomerUi.Dark,
            FontAttributes.Bold);
        valueLabel.HorizontalTextAlignment = TextAlignment.End;
        grid.Add(valueLabel, 1, 0);
        return grid;
    }
}

public sealed class PaymentCheckoutPage : CustomerPageBase, IQueryAttributable
{
    private int _paymentId;
    private int _requestId;
    private PaymentDto? _payment;
    private ServiceRequestDto? _request;
    private bool _fromBookingFlow;
    private bool _isOpeningCheckout;

    public PaymentCheckoutPage()
    {
        Title = "Payment Details";
        Render("Loading payment details...");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("paymentId", out var value) &&
            int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var paymentId))
        {
            _paymentId = paymentId;
        }

        if (query.TryGetValue("requestId", out var requestValue) &&
            int.TryParse(Uri.UnescapeDataString(requestValue?.ToString() ?? ""), out var requestId))
        {
            _requestId = requestId;
            _fromBookingFlow = true;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var paymentReturn = PaymentReturnService.ConsumeReturn();
        if (paymentReturn is not null)
        {
            if (paymentReturn.PaymentId > 0)
            {
                _paymentId = paymentReturn.PaymentId;
            }

            if (paymentReturn.RequestId > 0)
            {
                _requestId = paymentReturn.RequestId;
            }

            _fromBookingFlow = _fromBookingFlow || paymentReturn.FromBookingFlow;
            await LoadAsync(PaymentReturnService.FormatBanner(paymentReturn));
            return;
        }

        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        try
        {
            var payments = await CustomerApiClient.GetPaymentHistoryAsync();
            _requestId = _requestId > 0 ? _requestId : BookingDraft.RequestId;
            _fromBookingFlow = _fromBookingFlow || BookingDraft.RequestId > 0;

            if (_paymentId > 0)
            {
                _payment = payments.FirstOrDefault(x => x.PaymentId == _paymentId);
            }
            else if (_requestId > 0)
            {
                _payment = payments.OrderByDescending(x => x.CreatedAt).FirstOrDefault(x => x.RequestId == _requestId);
                _request = await CustomerApiClient.GetRequestAsync(_requestId);
                if (_payment is null)
                {
                    var amount = _request.FinalTotal > 0 ? _request.FinalTotal : _request.EstimatedTotal;
                    _payment = await CustomerApiClient.CreateCheckoutAsync(new CreateCheckoutSessionDto(_requestId, amount));
                }
            }
            else
            {
                _payment = payments.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            }

            if (_payment is not null)
            {
                _paymentId = _payment.PaymentId;
                _requestId = _payment.RequestId;
                BookingDraft.PaymentId = _payment.PaymentId;
                _request ??= await CustomerApiClient.GetRequestAsync(_payment.RequestId);
                if (!string.Equals(_payment.Status, "paid", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        _payment = await CustomerApiClient.RefreshPaymentAsync(_payment.PaymentId);
                        if (string.Equals(_payment.Status, "paid", StringComparison.OrdinalIgnoreCase))
                        {
                            _request = await CustomerApiClient.GetRequestAsync(_payment.RequestId);
                            banner = AppendBanner(banner, "Payment confirmed by BikeMate.");
                        }
                    }
                    catch (Exception refreshError)
                    {
                        banner = AppendBanner(banner, $"BikeMate could not verify PayMongo yet. {refreshError.Message}");
                    }
                }
            }

            Render(banner);
        }
        catch (Exception ex)
        {
            var message = FriendlyError("Payment details could not be loaded. Check your connection and try again.", ex);
            Render(string.IsNullOrWhiteSpace(banner) ? message : $"{banner}\n{message}");
        }
    }

    private static string? AppendBanner(string? current, string message)
    {
        return string.IsNullOrWhiteSpace(current) ? message : $"{current}\n{message}";
    }

    private void Render(string? banner = null)
    {
        var selectedService = BookingDraft.SelectedService();
        var amount = _payment?.Amount
            ?? (_request is null ? selectedService?.BasePrice ?? 0m : _request.FinalTotal > 0 ? _request.FinalTotal : _request.EstimatedTotal);
        var service = _request?.ServiceName
            ?? selectedService?.ServiceName
            ?? (_payment is null ? "No payment selected" : $"Request #{_payment.RequestId}");
        var issue = _request?.IssueDescription ?? $"{BookingDraft.ProblemCategory}: {BookingDraft.OtherDetails}";
        var location = _request?.ServiceLocationAddress ?? BookingDraft.ConfirmationAddress();
        var timing = BookingDraft.FormatSchedule(_request?.ScheduledAt);
        var bookingStatus = _request is null ? "pending" : FormatStatus(_request.CurrentStatus);
        var isPaid = IsPaidForDisplay();
        var paymentStatus = isPaid ? "Paid" : _payment is null ? "Unpaid" : FormatStatus(_payment.Status);

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 0, 14, 18),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(BookingVisuals.FlowHeader("Payment details"));
        body.Add(PaymentStatusHero(paymentStatus, amount, isPaid));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(PaymentNotice(banner, isPaid));
        }

        if (_request is not null || !string.IsNullOrWhiteSpace(BookingDraft.SelectedShopName))
        {
            body.Add(BookingVisuals.ShopProfileCard(
                BookingDraft.DisplayShopName(_request?.ShopName),
                _request?.ShopImageUrl ?? BookingDraft.SelectedShopImageUrl,
                _request?.ShopLogoUrl ?? BookingDraft.SelectedShopLogoUrl,
                service));
        }
        body.Add(PaymentBookingCard(
            service,
            issue,
            location,
            timing,
            bookingStatus));
        body.Add(PaymentBreakdownCard(amount));
        body.Add(PaymentProviderCard(paymentStatus));

        if (isPaid)
        {
            body.Add(PaidConfirmationCard());
        }

        if (!isPaid &&
            Uri.TryCreate(_payment?.CheckoutUrl, UriKind.Absolute, out var checkoutUri))
        {
            var checkoutButton = BookingVisuals.PrimaryButton(
                _isOpeningCheckout ? "Opening secure checkout..." : "Pay securely with PayMongo",
                new Command(async () => await OpenCheckoutAsync(checkoutUri)));
            checkoutButton.IsEnabled = !_isOpeningCheckout;
            checkoutButton.Opacity = checkoutButton.IsEnabled ? 1 : 0.55;
            body.Add(checkoutButton);
        }

        if (!isPaid)
        {
            body.Add(BookingVisuals.SecondaryButton(
                "Refresh payment status",
                new Command(async () => await LoadAsync())));
        }
        else
        {
            body.Add(BookingVisuals.PrimaryButton(
                "View booking status",
                new Command(async () => await Shell.Current.GoToAsync(nameof(TrackOrderPage)))));
            if (_paymentId > 0)
            {
                body.Add(BookingVisuals.SecondaryButton(
                    "View payment receipt",
                    new Command(async () => await Shell.Current.GoToAsync($"{nameof(PaymentReceiptPage)}?paymentId={_paymentId}"))));
            }
            body.Add(BookingVisuals.SecondaryButton(
                "Return home",
                new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))));
        }

        SetScaffold(new ScrollView { Content = body }, "Payments", false);
    }

    private static View PaymentStatusHero(string status, decimal amount, bool isPaid)
    {
        var color = isPaid ? Color.FromArgb("#147A3D") : CustomerUi.Orange;
        var background = isPaid ? Color.FromArgb("#E8F6EF") : Color.FromArgb("#FFF3EA");
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };
        var copy = new VerticalStackLayout { Spacing = 4 };
        copy.Add(BookingVisuals.Text(isPaid ? "Payment confirmed" : "Amount due", 10, CustomerUi.Muted, FontAttributes.Bold));
        copy.Add(BookingVisuals.Text(Money(amount), 22, CustomerUi.Dark, FontAttributes.Bold));
        copy.Add(BookingVisuals.Text(
            isPaid ? "BikeMate has verified this payment." : "Complete checkout to confirm and unlock tracking.",
            10,
            CustomerUi.Muted));
        grid.Add(copy, 0, 0);
        grid.Add(new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 13 },
            BackgroundColor = background,
            Padding = new Thickness(11, 6),
            Content = BookingVisuals.Text(status, 10, color, FontAttributes.Bold)
        }, 1, 0);
        return BookingVisuals.WhiteCard(grid, 8, new Thickness(16));
    }

    private static View PaymentNotice(string message, bool isPaid)
    {
        var isCancellation = message.Contains("cancel", StringComparison.OrdinalIgnoreCase);
        var color = isPaid
            ? Color.FromArgb("#147A3D")
            : isCancellation ? Color.FromArgb("#A65B00") : CustomerUi.Muted;
        var background = isPaid
            ? Color.FromArgb("#E8F6EF")
            : isCancellation ? Color.FromArgb("#FFF5E6") : Color.FromArgb("#F2F2F2");
        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = background,
            Padding = new Thickness(12, 10),
            Content = BookingVisuals.Text(message, 10, color)
        };
    }

    private View PaymentBookingCard(
        string service,
        string issue,
        string location,
        string timing,
        string bookingStatus)
    {
        var stack = new VerticalStackLayout { Spacing = 11 };
        stack.Add(BookingVisuals.Text("Booking details", 13, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(PaymentInfoRow("Repair shop", BookingDraft.DisplayShopName(_request?.ShopName)));
        stack.Add(PaymentInfoRow("Service", service));
        stack.Add(PaymentInfoRow("Vehicle", $"{BookingDraft.Brand} {BookingDraft.Model}".Trim()));
        if (!string.IsNullOrWhiteSpace(BookingDraft.PlateNumber))
        {
            stack.Add(PaymentInfoRow("Plate number", BookingDraft.NormalizePlate(BookingDraft.PlateNumber)));
        }
        stack.Add(PaymentInfoRow("Assistance method", BookingDraft.AssistanceMethod));
        stack.Add(PaymentInfoRow("Timing", timing));
        stack.Add(PaymentInfoRow("Location", location));
        stack.Add(PaymentInfoRow("Concern", CleanIssueForDisplay(issue)));
        stack.Add(PaymentInfoRow("Booking status", bookingStatus));
        return BookingVisuals.WhiteCard(stack, 8, new Thickness(14));
    }

    private static View PaymentBreakdownCard(decimal amount)
    {
        var stack = new VerticalStackLayout { Spacing = 11 };
        stack.Add(BookingVisuals.Text("Price breakdown", 13, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(PaymentInfoRow("Shop service", Money(amount)));
        stack.Add(PaymentInfoRow("PayMongo processing", "Included"));
        stack.Add(new BoxView { HeightRequest = 1, Color = CustomerUi.Border });
        stack.Add(PaymentInfoRow("Total", Money(amount), true));
        return BookingVisuals.WhiteCard(stack, 8, new Thickness(14));
    }

    private View PaymentProviderCard(string paymentStatus)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(BookingVisuals.Text("Secure payment", 13, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(BookingVisuals.Text(
            "Checkout is hosted by PayMongo. Available cards and e-wallets are shown on their secure payment page.",
            10,
            CustomerUi.Muted));
        stack.Add(PaymentInfoRow("Provider", FormatStatus(_payment?.ProviderName ?? "PayMongo")));
        stack.Add(PaymentInfoRow("Reference", _payment?.ReferenceNumber ?? $"BM-PAY-{_paymentId:0000}"));
        stack.Add(PaymentInfoRow("Status", paymentStatus));
        return BookingVisuals.WhiteCard(stack, 8, new Thickness(14));
    }

    private static View PaidConfirmationCard()
    {
        var stack = new VerticalStackLayout { Spacing = 5 };
        stack.Add(BookingVisuals.Text("Your booking is confirmed", 13, Color.FromArgb("#147A3D"), FontAttributes.Bold));
        stack.Add(BookingVisuals.Text(
            "The shop can now continue processing the booking. Tracking becomes available as the mechanic is assigned and begins sharing location.",
            10,
            CustomerUi.Muted));
        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb("#E8F6EF"),
            Padding = new Thickness(14),
            Content = stack
        };
    }

    private static View PaymentInfoRow(string label, string value, bool emphasized = false)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };
        grid.Add(BookingVisuals.Text(
            label,
            emphasized ? 11 : 10,
            emphasized ? CustomerUi.Dark : CustomerUi.Muted,
            emphasized ? FontAttributes.Bold : FontAttributes.None), 0, 0);
        var valueLabel = BookingVisuals.Text(
            string.IsNullOrWhiteSpace(value) ? "Not available" : value,
            emphasized ? 12 : 10,
            emphasized ? CustomerUi.Orange : CustomerUi.Dark,
            FontAttributes.Bold);
        valueLabel.HorizontalTextAlignment = TextAlignment.End;
        grid.Add(valueLabel, 1, 0);
        return grid;
    }

    private static string CleanIssueForDisplay(string issue)
    {
        var vehicleIndex = issue.IndexOf("\nVehicle:", StringComparison.OrdinalIgnoreCase);
        return (vehicleIndex >= 0 ? issue[..vehicleIndex] : issue).Trim();
    }

    private async Task OpenCheckoutAsync(Uri checkoutUri)
    {
        if (_isOpeningCheckout)
        {
            return;
        }

        _isOpeningCheckout = true;
        var openedCheckout = false;
        Render();
        try
        {
            PaymentReturnService.RememberCheckoutContext(_paymentId, _requestId, _fromBookingFlow);

            if (!await Launcher.Default.CanOpenAsync(checkoutUri))
            {
                await DisplayAlertAsync("Payment unavailable", "Android could not open the secure PayMongo checkout link. Enable a browser, then try again.", "OK");
                return;
            }

            var opened = await Launcher.Default.OpenAsync(checkoutUri);
            if (!opened)
            {
                await DisplayAlertAsync("Payment unavailable", "Android did not open the PayMongo checkout page. Please try again.", "OK");
                return;
            }

            openedCheckout = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PayMongo checkout open failed: {ex}");
            await DisplayAlertAsync("Payment unavailable", "Could not open secure PayMongo checkout. Please try again or contact support.", "OK");
        }
        finally
        {
            _isOpeningCheckout = false;
            if (openedCheckout)
            {
                Render("PayMongo checkout opened. BikeMate will refresh this page when you return.");
            }
            else
            {
                await LoadAsync();
            }
        }
    }

    private bool IsPaidForDisplay()
    {
        return string.Equals(_payment?.Status, "paid", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class PaymentReceiptPage : CustomerPageBase, IQueryAttributable
{
    private int _paymentId;
    private PaymentDto? _payment;
    private ServiceRequestDto? _request;
    private CustomerMeDto? _customer;

    public PaymentReceiptPage()
    {
        Title = "Payment Receipt";
        Render("Loading receipt...");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("paymentId", out var value) &&
            int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var paymentId))
        {
            _paymentId = paymentId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            var payments = await CustomerApiClient.GetPaymentHistoryAsync();
            _payment = payments.FirstOrDefault(x => x.PaymentId == _paymentId) ?? payments.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            if (_payment is not null)
            {
                _paymentId = _payment.PaymentId;
                _request = await CustomerApiClient.GetRequestAsync(_payment.RequestId);
            }

            Render();
        }
        catch (Exception ex)
        {
            Render(FriendlyError("Receipt data could not be loaded. Check your connection and try again.", ex));
        }
    }

    private void Render(string? banner = null)
    {
        var name = _customer is null ? "Customer" : $"{_customer.FirstName} {_customer.LastName}";
        var amount = ReceiptAmount(_payment, _request);
        var paidAt = (_payment?.PaidAt ?? _payment?.CreatedAt)?.ToLocalTime();

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(18, 8, 18, 24),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(Header("Payment Receipt"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        var receipt = new VerticalStackLayout { Spacing = 14 };
        receipt.Add(new Image { Source = "bikemate_logo", HeightRequest = 64, HorizontalOptions = LayoutOptions.Center });
        var title = Label("Payment confirmed", 18, CustomerUi.Dark, FontAttributes.Bold);
        title.HorizontalTextAlignment = TextAlignment.Center;
        receipt.Add(title);
        var reference = Label(
            _payment?.ReferenceNumber ?? $"BM-PAY-{_paymentId:000000}",
            11,
            CustomerUi.Muted,
            FontAttributes.Bold);
        reference.HorizontalTextAlignment = TextAlignment.Center;
        receipt.Add(reference);
        receipt.Add(Separator());
        receipt.Add(Row("Customer", name));
        receipt.Add(Row("Booking", $"BM-{(_payment?.RequestId ?? 0):000000}"));
        receipt.Add(Row("Services", RequestServiceTitle(_request)));
        receipt.Add(Row("Products", RequestProductsTitle(_request)));
        receipt.Add(Row("Provider", FormatStatus(_payment?.ProviderName ?? "PayMongo")));
        receipt.Add(Row("Paid on", paidAt?.ToString("MMM d, yyyy, h:mm tt", CultureInfo.InvariantCulture) ?? "Pending"));
        receipt.Add(Row("Status", FormatStatus(_payment?.Status ?? "pending")));
        receipt.Add(new Border
        {
            BackgroundColor = Color.FromArgb("#FFF3EA"),
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(14, 12),
            Content = new VerticalStackLayout
            {
                Spacing = 3,
                Children =
                {
                    Label("AMOUNT PAID", 11, CustomerUi.Muted, FontAttributes.Bold),
                    Label(Money(amount), 18, CustomerUi.Dark, FontAttributes.Bold)
                }
            }
        });
        body.Add(Card(receipt, Colors.White, 8, new Thickness(18)));
        body.Add(OrangeButton("View invoice", new Command(async () => await Shell.Current.GoToAsync($"{nameof(PaymentInvoicePage)}?paymentId={_paymentId}"))));
        body.Add(GhostButton("Return to payments", new Command(async () => await Shell.Current.GoToAsync("//CustomerPaymentsPage"))));

        SetScaffold(new ScrollView { Content = body }, "Payments", false);
    }
}

public sealed class PaymentInvoicePage : CustomerPageBase, IQueryAttributable
{
    private int _paymentId;
    private PaymentDto? _payment;
    private ServiceRequestDto? _request;
    private CustomerMeDto? _customer;

    public PaymentInvoicePage()
    {
        Title = "Payment Invoice";
        Render("Loading invoice...");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("paymentId", out var value) &&
            int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var paymentId))
        {
            _paymentId = paymentId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            var payments = await CustomerApiClient.GetPaymentHistoryAsync();
            _payment = payments.FirstOrDefault(x => x.PaymentId == _paymentId) ?? payments.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            if (_payment is not null)
            {
                _paymentId = _payment.PaymentId;
                _request = await CustomerApiClient.GetRequestAsync(_payment.RequestId);
            }

            Render();
        }
        catch (Exception ex)
        {
            Render(FriendlyError("Invoice data could not be loaded. Check your connection and try again.", ex));
        }
    }

    private void Render(string? banner = null)
    {
        var name = _customer is null ? "Customer" : $"{_customer.FirstName} {_customer.LastName}";
        var amount = ReceiptAmount(_payment, _request);
        var created = (_payment?.CreatedAt ?? DateTime.UtcNow).ToLocalTime();

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(18, 8, 18, 24),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(Header("Payment Invoice"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        var invoice = new VerticalStackLayout { Spacing = 12 };
        var heading = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };
        var brand = new VerticalStackLayout { Spacing = 2 };
        brand.Add(Label("BikeMate", 18, CustomerUi.Dark, FontAttributes.Bold));
        brand.Add(Label("Official payment invoice", 11, CustomerUi.Muted));
        heading.Add(brand, 0, 0);
        heading.Add(Label($"#{_paymentId:000000}", 13, CustomerUi.Orange, FontAttributes.Bold), 1, 0);
        invoice.Add(heading);
        invoice.Add(Separator());
        invoice.Add(Row("Issued to", name));
        invoice.Add(Row("Issued on", created.ToString("MMM d, yyyy, h:mm tt", CultureInfo.InvariantCulture)));
        invoice.Add(Row("Booking reference", $"BM-{(_payment?.RequestId ?? 0):000000}"));
        invoice.Add(Row("Services", RequestServiceTitle(_request)));
        invoice.Add(Row("Products", RequestProductsTitle(_request)));
        invoice.Add(Row("Payment provider", FormatStatus(_payment?.ProviderName ?? "PayMongo")));
        invoice.Add(Row("Provider reference", _payment?.ReferenceNumber ?? $"BM-PAY-{_paymentId:000000}"));
        invoice.Add(Row("Payment status", FormatStatus(_payment?.Status ?? "pending")));
        invoice.Add(Separator());
        if (_request?.LineItems is { Count: > 0 } items)
        {
            foreach (var item in items)
            {
                invoice.Add(Row($"{FormatStatus(item.ItemType)}: {item.ItemName}", Money(item.LineTotal)));
            }
        }
        else
        {
            invoice.Add(Row("Service payment", Money(amount)));
        }
        var totalGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        totalGrid.Add(Label("TOTAL", 11, Colors.White, FontAttributes.Bold), 0, 0);
        totalGrid.Add(Label(Money(amount), 18, Colors.White, FontAttributes.Bold), 1, 0);
        invoice.Add(new Border
        {
            BackgroundColor = CustomerUi.Dark,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(14, 12),
            Content = totalGrid
        });
        invoice.Add(Label("This electronic invoice was generated by BikeMate and does not require a signature.", 11, CustomerUi.Muted));
        body.Add(Card(invoice, Colors.White, 8, new Thickness(18)));
        body.Add(OrangeButton("Return to payments", new Command(async () => await Shell.Current.GoToAsync("//CustomerPaymentsPage"))));

        SetScaffold(new ScrollView { Content = body }, "Payments", false);
    }
}

public sealed class BookServicePage : CustomerPageBase
{
    private CustomerMeDto? _customer;
    private IReadOnlyList<ShopServiceDto> _services = [];
    private IReadOnlyList<PhilippineRegionDto> _regions = [];
    private IReadOnlyList<PhilippineLocalityDto> _localities = [];
    private Editor? _addressEditor;
    private bool _hasLoaded;
    private bool _isLoading;
    private bool _isLoadingLocalities;
    private bool _isResolvingLocation;
    private string? _bookingError;
    private string? _locationMessage;

    public BookServicePage()
    {
        Title = "Book Now";
        BookingDraft.Reset();
        Render();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_hasLoaded || _isLoading)
        {
            return;
        }

        var approvedCustomer = await EnsureCustomerApprovedForBookingAsync();
        if (approvedCustomer is null)
        {
            await Shell.Current.GoToAsync("//CustomerHomePage");
            return;
        }

        await LoadAsync(approvedCustomer);
    }

    private async Task LoadAsync(CustomerMeDto? approvedCustomer = null)
    {
        _isLoading = true;
        _bookingError = null;
        _locationMessage = "Loading Philippine locations...";
        Render();

        try
        {
            var servicesTask = CustomerApiClient.SearchServicesAsync();
            _customer = approvedCustomer ?? await CustomerApiClient.GetCustomerAsync();
            if (!CustomerRequestRules.IsApproved(_customer))
            {
                _bookingError = CustomerRequestRules.BookingBlockedMessage(_customer);
                await DisplayAlertAsync("Account approval required", _bookingError, "OK");
                await Shell.Current.GoToAsync("//CustomerHomePage");
                return;
            }

            _services = await servicesTask;
            BookingDraft.ApplyCustomer(_customer);
            BookingDraft.Services = _services;
        }
        catch (Exception ex)
        {
            _bookingError = FriendlyError("BikeMate could not prepare your booking. Check your connection and try again.", ex);
        }

        try
        {
            _regions = await CustomerApiClient.GetPhilippineRegionsAsync();
            await RestoreSavedLocationAsync();
            _locationMessage = null;
        }
        catch (Exception ex)
        {
            _locationMessage = FriendlyError("Philippine location data could not be loaded. Check your connection and try again.", ex);
        }
        finally
        {
            _isLoading = false;
            _hasLoaded = true;
            Render();
        }
    }

    private void Render()
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 0, 14, 18),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(BookingVisuals.FlowHeader("Book a service"));
        body.Add(LocationHeading());

        if (!string.IsNullOrWhiteSpace(_bookingError))
        {
            body.Add(Notice(_bookingError, true));
        }

        body.Add(BookingVisuals.MapPanel(190));
        body.Add(CurrentLocationButton());

        var regionEnabled = !_isLoading && _regions.Count > 0 && !_isResolvingLocation;
        body.Add(BookingVisuals.PickerRow(
            "Region / main area",
            _regions.Select(region => region.Name).ToArray(),
            BookingDraft.RegionCode is null ? null : BookingDraft.Region,
            RegionChangedAsync,
            regionEnabled,
            _isLoading ? "Loading regions..." : "Select a Philippine region"));

        var localityEnabled =
            regionEnabled &&
            BookingDraft.RegionCode is not null &&
            !_isLoadingLocalities &&
            _localities.Count > 0;
        body.Add(BookingVisuals.PickerRow(
            "City / municipality",
            _localities.Select(LocalityDisplayName).ToArray(),
            BookingDraft.LocalityCode is null
                ? null
                : _localities
                    .Where(item => item.Code == BookingDraft.LocalityCode)
                    .Select(LocalityDisplayName)
                    .FirstOrDefault(),
            LocalityChangedAsync,
            localityEnabled,
            BookingDraft.RegionCode is null
                ? "Select a region first"
                : _isLoadingLocalities ? "Loading cities..." : "Select a city or municipality"));

        if (_isLoading || _isLoadingLocalities || _isResolvingLocation)
        {
            body.Add(new ActivityIndicator
            {
                IsRunning = true,
                Color = CustomerUi.Orange,
                HorizontalOptions = LayoutOptions.Center
            });
        }

        if (!string.IsNullOrWhiteSpace(_locationMessage))
        {
            body.Add(Notice(_locationMessage, false));
        }

        body.Add(BookingVisuals.Text("Exact service address", 12, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(BookingVisuals.Text(
            "Add the house number, street, subdivision, building, or nearest landmark.",
            10,
            CustomerUi.Muted));

        _addressEditor = new Editor
        {
            Text = BookingDraft.AddressLine,
            Placeholder = "House number, street, subdivision, landmark",
            MinimumHeightRequest = 88,
            AutoSize = EditorAutoSizeOption.TextChanges,
            FontSize = 13,
            FontFamily = CustomerUi.FontBody,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            BackgroundColor = Colors.Transparent
        };
        _addressEditor.TextChanged += (_, e) => BookingDraft.AddressLine = e.NewTextValue ?? string.Empty;
        body.Add(BookingVisuals.WhiteCard(_addressEditor, 8, new Thickness(12, 8)));

        if (!string.IsNullOrWhiteSpace(BookingDraft.LocationName))
        {
            body.Add(LocationSummary());
        }

        var continueButton = BookingVisuals.PrimaryButton(
            "Continue to bike details",
            new Command(async () => await ContinueAsync()));
        continueButton.IsEnabled = !_isLoading && !_isResolvingLocation && _customer is not null;
        continueButton.Opacity = continueButton.IsEnabled ? 1 : 0.55;
        body.Add(continueButton);

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    public void RefreshLocationUi()
    {
        Render();
    }

    private View LocationHeading()
    {
        return BookingVisuals.StepIntro(
            "Where do you need help?",
            "Choose the service area first. BikeMate will use it to find nearby repair shops and available mechanics.",
            1,
            "Service location",
            7);
    }

    private View CurrentLocationButton()
    {
        return new Button
        {
            Text = _isResolvingLocation ? "Finding your current location..." : "Use current location",
            Command = new Command(async () => await UseCurrentLocationAsync()),
            IsEnabled = !_isResolvingLocation && !_isLoading,
            HeightRequest = 46,
            CornerRadius = 8,
            BackgroundColor = Colors.White,
            BorderColor = CustomerUi.Orange,
            BorderWidth = 1,
            TextColor = CustomerUi.Orange,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            FontFamily = CustomerUi.FontDisplay
        };
    }

    private static View Notice(string message, bool isError)
    {
        var color = isError ? Color.FromArgb("#A23232") : CustomerUi.Muted;
        var background = isError ? Color.FromArgb("#FFF0F0") : Color.FromArgb("#FFF7F2");
        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = background,
            Padding = new Thickness(12, 10),
            Content = BookingVisuals.Text(message, 10, color)
        };
    }

    private static View LocationSummary()
    {
        var stack = new VerticalStackLayout { Spacing = 4 };
        stack.Add(BookingVisuals.Text("Selected service area", 10, CustomerUi.Muted));
        stack.Add(BookingVisuals.Text(BookingDraft.LocationName, 13, CustomerUi.Dark, FontAttributes.Bold));
        if (!string.IsNullOrWhiteSpace(BookingDraft.Province))
        {
            stack.Add(BookingVisuals.Text(BookingDraft.Province, 10, CustomerUi.Muted));
        }
        if (!string.IsNullOrWhiteSpace(BookingDraft.Region))
        {
            stack.Add(BookingVisuals.Text(BookingDraft.Region, 10, CustomerUi.Muted));
        }

        return BookingVisuals.WhiteCard(stack, 8, new Thickness(12));
    }

    private async Task RestoreSavedLocationAsync()
    {
        var address = _customer?.Addresses.FirstOrDefault(item => item.IsDefault)
            ?? _customer?.Addresses.FirstOrDefault();
        if (address is null || string.IsNullOrWhiteSpace(address.City))
        {
            return;
        }

        var query = string.Join(", ", new[] { address.City, address.Province, "Philippines" }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var match = await CustomerApiClient.ResolvePhilippineLocationAsync(query);
        if (match is null)
        {
            return;
        }

        BookingDraft.SetAdministrativeLocation(match.Region, match.Locality, true);
        _localities = await CustomerApiClient.GetPhilippineLocalitiesAsync(match.Region.Code);
    }

    private async Task RegionChangedAsync(string selected)
    {
        var region = _regions.FirstOrDefault(item =>
            string.Equals(item.Name, selected, StringComparison.OrdinalIgnoreCase));
        if (region is null)
        {
            return;
        }

        BookingDraft.SelectRegion(region);
        _localities = [];
        _isLoadingLocalities = true;
        _locationMessage = "Loading cities and municipalities...";
        Render();

        try
        {
            _localities = await CustomerApiClient.GetPhilippineLocalitiesAsync(region.Code);
            _locationMessage = _localities.Count == 0
                ? "No cities or municipalities were returned for this region."
                : null;
        }
        catch (Exception ex)
        {
            _locationMessage = FriendlyError("Cities and municipalities could not be loaded. Check your connection and try again.", ex);
        }
        finally
        {
            _isLoadingLocalities = false;
            Render();
        }
    }

    private async Task LocalityChangedAsync(string selected)
    {
        var region = _regions.FirstOrDefault(item => item.Code == BookingDraft.RegionCode);
        var locality = _localities.FirstOrDefault(item =>
            string.Equals(LocalityDisplayName(item), selected, StringComparison.OrdinalIgnoreCase));
        if (region is null || locality is null)
        {
            return;
        }

        BookingDraft.SetAdministrativeLocation(region, locality);
        _isResolvingLocation = true;
        _locationMessage = $"Locating {locality.Name} on the map...";
        Render();

        try
        {
            await ResolveCoordinatesAsync(string.Join(", ", new[]
            {
                locality.Name,
                locality.Province,
                region.Name,
                "Philippines"
            }.Where(value => !string.IsNullOrWhiteSpace(value))));
            _locationMessage = "City selected. Add the exact street or landmark below.";
        }
        finally
        {
            _isResolvingLocation = false;
            Render();
        }
    }

    private async Task UseCurrentLocationAsync()
    {
        _isResolvingLocation = true;
        _locationMessage = "Getting your phone's current location...";
        Render();

        try
        {
            if (!await BookingVisuals.UpdateCurrentLocationAsync(this))
            {
                _locationMessage = "Current location was not selected.";
                return;
            }

            if (BookingDraft.RegionCode is not null)
            {
                _localities = await CustomerApiClient.GetPhilippineLocalitiesAsync(BookingDraft.RegionCode);
            }

            _locationMessage = BookingDraft.LocalityCode is null
                ? "GPS location found. Review the address before continuing."
                : "Current location selected and matched to its service area.";
        }
        catch (Exception ex)
        {
            _locationMessage = FriendlyError("Current location could not be completed. Check location permission and try again.", ex);
        }
        finally
        {
            _isResolvingLocation = false;
            Render();
        }
    }

    private static async Task<bool> ResolveCoordinatesAsync(string address)
    {
        try
        {
            var point = await CustomerApiClient.GeocodeAsync(address);
            BookingDraft.ApplyCoordinates(point.Latitude, point.Longitude);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"BikeMate geocoding failed: {ex}");
        }

        try
        {
            var location = (await Geocoding.Default.GetLocationsAsync(address)).FirstOrDefault();
            if (location is null)
            {
                return false;
            }

            BookingDraft.ApplyCoordinates((decimal)location.Latitude, (decimal)location.Longitude);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Device geocoding failed: {ex}");
            return false;
        }
    }

    private static string LocalityDisplayName(PhilippineLocalityDto locality)
    {
        return string.IsNullOrWhiteSpace(locality.Province)
            ? locality.Name
            : $"{locality.Name}, {locality.Province}";
    }

    private async Task ContinueAsync()
    {
        BookingDraft.Customer = _customer ?? BookingDraft.Customer;
        BookingDraft.Services = _services.Count == 0 ? BookingDraft.Services : _services;
        BookingDraft.AddressLine = _addressEditor?.Text?.Trim() ?? BookingDraft.AddressLine;

        if (!BookingDraft.IsGpsLocation &&
            (string.IsNullOrWhiteSpace(BookingDraft.RegionCode) ||
             string.IsNullOrWhiteSpace(BookingDraft.LocalityCode)))
        {
            await DisplayAlertAsync(
                "Service area required",
                "Select a Philippine region and city or municipality before continuing.",
                "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(BookingDraft.AddressLine))
        {
            await DisplayAlertAsync(
                "Exact address required",
                "Enter the house number, street, subdivision, building, or nearest landmark.",
                "OK");
            return;
        }

        if (BookingDraft.Customer is null)
        {
            await DisplayAlertAsync("Profile required", "Connect the API or login as a customer before booking.", "OK");
            return;
        }

        _isResolvingLocation = true;
        _locationMessage = "Confirming the exact service point...";
        Render();
        var hasCoordinates = await ResolveCoordinatesAsync(BookingDraft.ServiceAddress());
        _isResolvingLocation = false;
        if (!hasCoordinates && (BookingDraft.Latitude is null || BookingDraft.Longitude is null))
        {
            _locationMessage = "BikeMate could not map that address. Check the address or use your current location.";
            Render();
            await DisplayAlertAsync(
                "Address not found",
                "BikeMate could not place this address on the map. Check the details or use your current location.",
                "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(BookingFillUpPage));
    }
}

public sealed class BookingDetailsPage : CustomerPageBase, IQueryAttributable
{
    private int _requestId;
    private ServiceRequestDto? _request;

    public BookingDetailsPage()
    {
        Title = "Booking Details";
        Render("Loading booking details...");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("requestId", out var value) &&
            int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var requestId))
        {
            _requestId = requestId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            if (_requestId > 0)
            {
                _request = await CustomerApiClient.GetRequestAsync(_requestId);
            }
            else
            {
                _request = (await CustomerApiClient.GetMyRequestsAsync()).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
                _requestId = _request?.RequestId ?? 0;
            }

            if (_request is not null)
            {
                await ScheduleBookingRemindersAsync(new[] { _request });
            }

            Render();
        }
        catch (Exception ex)
        {
            Render(FriendlyError("Booking details could not be loaded. Check your connection and try again.", ex));
        }
    }

    private void Render(string? banner = null)
    {
        var scheduledAt = _request?.ScheduledAt?.ToLocalTime();
        var bookedAt = _request?.CreatedAt.ToLocalTime();
        var isEmergency = CustomerRequestRules.IsEmergency(_request);
        var body = new VerticalStackLayout { Padding = new Thickness(18), Spacing = 14 };
        body.Add(Header(isEmergency ? "Emergency Request" : "Booking Details"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Label(isEmergency ? "Emergency Roadside Help" : _request?.ServiceName ?? "Bike repair", 18, CustomerUi.Dark, FontAttributes.Bold),
                Label($"Status: {(_request is null ? "" : FormatStatus(_request.CurrentStatus))}", 13, CustomerUi.Orange, FontAttributes.Bold),
                Label($"{(isEmergency ? "Responder" : "Mechanic")}: {_request?.MechanicName ?? "Not assigned"}", 13, CustomerUi.Dark),
                Label(isEmergency ? "Coordinator: BikeMate Emergency" : $"Shop: {_request?.ShopName ?? "Not assigned"}", 13, CustomerUi.Dark),
                Label($"{BookingDraft.AssistanceLocationLabel()}: {_request?.ServiceLocationAddress ?? "No address"}", 13, CustomerUi.Dark),
                Label(
                    $"Timing: {(scheduledAt is null ? BookingDraft.FormatSchedule(null) : scheduledAt.Value.ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture))}",
                    13,
                    CustomerUi.Dark),
                Label(
                    $"Booked on: {(bookedAt is null ? "Not available" : bookedAt.Value.ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture))}",
                    13,
                    CustomerUi.Dark),
                Label(
                    isEmergency
                        ? "Payment: No upfront payment required"
                        : $"Estimated total: {Money(_request?.EstimatedTotal ?? 0m)}",
                    13,
                    CustomerUi.Dark)
            }
        }));
        body.Add(OrangeButton(
            isEmergency && CustomerRequestRules.IsClosed(_request)
                ? "Emergency Completed"
                : isEmergency ? "Track Emergency Responder" : "Track Mechanic",
            new Command(async () => await OpenTrackMechanicAsync())));

        SetScaffold(new ScrollView { Content = body }, "Schedule", false);
    }

    private async Task OpenTrackMechanicAsync()
    {
        if (_requestId <= 0)
        {
            return;
        }

        if (CustomerRequestRules.IsEmergency(_request))
        {
            if (CustomerRequestRules.IsClosed(_request))
            {
                await DisplayAlertAsync("Emergency completed", "Responder tracking is no longer available after emergency roadside help is completed.", "OK");
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(ActiveEmergencyTrackingPage)}?requestId={_requestId}");
            return;
        }

        if (!await CustomerPaymentGate.EnsurePaidOrRedirectAsync(this, _requestId))
        {
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(TrackMechanicPage)}?requestId={_requestId}");
    }
}

public sealed class TrackMechanicPage : CustomerPageBase, IQueryAttributable
{
    private int _requestId;
    private ServiceRequestDto? _request;
    private LiveLocationDto? _location;
    private PaymentDto? _payment;
    private IDispatcherTimer? _timer;
    private bool _isLoading;

    public TrackMechanicPage()
    {
        Title = "Track Mechanic";
        Render("Loading tracking details...");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("requestId", out var value) &&
            int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var requestId))
        {
            _requestId = requestId;
            BookingDraft.RequestId = requestId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        StartLiveRefresh();
        await LoadAsync();
    }

    protected override void OnDisappearing()
    {
        _timer?.Stop();
        base.OnDisappearing();
    }

    private void StartLiveRefresh()
    {
        if (_timer is null)
        {
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(8);
            _timer.Tick += async (_, _) => await LoadAsync(true);
        }

        if (!_timer.IsRunning)
        {
            _timer.Start();
        }
    }

    private async Task LoadAsync(bool silent = false)
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        try
        {
            _request = _requestId > 0
                ? await CustomerApiClient.GetRequestAsync(_requestId)
                : (await CustomerApiClient.GetMyRequestsAsync()).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            _requestId = _request?.RequestId ?? _requestId;
            if (_request is not null)
            {
                await ScheduleBookingRemindersAsync(new[] { _request });
            }

            BookingDraft.RequestId = _requestId;
            if (_requestId > 0 && !await CustomerPaymentGate.EnsurePaidOrRedirectAsync(this, _requestId))
            {
                return;
            }

            if (_requestId > 0)
            {
                _payment = await CustomerApiClient.GetLatestPaymentForRequestAsync(_requestId);
                _location = await CustomerApiClient.GetLatestRequestLocationAsync(_requestId);
            }

            Render();
        }
        catch (Exception ex)
        {
            if (!silent)
            {
                Render(FriendlyError("Tracking data could not be loaded. Check your connection and try again.", ex));
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void Render(string? banner = null)
    {
        var request = _request;
        var status = request?.CurrentStatus ?? "pending";
        var trackingStatus = string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase) &&
                             !string.IsNullOrWhiteSpace(request?.MechanicName)
            ? "accepted"
            : status;
        var destinationLat = request?.ServiceLatitude ?? BookingDraft.Latitude;
        var destinationLng = request?.ServiceLongitude ?? BookingDraft.Longitude;
        var destination = request?.ServiceLocationAddress ?? BookingDraft.ConfirmationAddress();
        var hasLiveLocation = _location is not null;
        var route = BuildRouteSummary(
            _location?.Latitude,
            _location?.Longitude,
            destinationLat,
            destinationLng,
            destination);
        var amount = RequestTotal(request);

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 0, 14, 16),
            Spacing = 12,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(Header("Track Mechanic", true, "Refresh", new Command(async () => await LoadAsync())));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(TrackingMapCard(_location, destinationLat, destinationLng, route));
        body.Add(StatusCard(trackingStatus, request, hasLiveLocation));
        body.Add(RouteStats(route));
        body.Add(SectionCard("Repair details",
        [
            DetailRow("Services", RequestServiceTitle(request)),
            DetailRow("Products", RequestProductsTitle(request)),
            DetailRow("Repair concern", request?.IssueDescription ?? "Repair request"),
            DetailRow("Shop", request?.ShopName ?? "Waiting for shop assignment"),
            DetailRow("Mechanic", request?.MechanicName ?? "Waiting for mechanic assignment")
        ]));
        body.Add(SectionCard("Timing and payment",
        [
            DetailRow("Booking ID", _requestId > 0 ? $"BM-{_requestId:000000}" : "Pending"),
            DetailRow("Timing", BookingDraft.FormatSchedule(request?.ScheduledAt)),
            DetailRow("Booked on", request is null ? "Not available" : request.CreatedAt.ToLocalTime().ToString("MMM d, yyyy, h:mm tt", CultureInfo.InvariantCulture)),
            DetailRow("Payment", _payment is null ? "Checking" : FormatStatus(_payment.Status)),
            DetailRow("Total", amount > 0 ? Money(amount) : "To be finalized")
        ]));
        body.Add(Timeline(trackingStatus));
        body.Add(ActionCard(trackingStatus, destination));

        SetScaffold(new ScrollView { Content = body }, "Schedule", false);
    }

    private static View TrackingMapCard(
        LiveLocationDto? liveLocation,
        decimal? destinationLatitude,
        decimal? destinationLongitude,
        (string Origin, string Destination, string Distance, string Time) route)
    {
        var map = new Grid { HeightRequest = 280, BackgroundColor = Color.FromArgb("#EEF1F4") };
        if (liveLocation is null)
        {
            var title = Label("Waiting for live mechanic location", 15, CustomerUi.Dark, FontAttributes.Bold);
            title.HorizontalTextAlignment = TextAlignment.Center;
            var message = Label(
                "The route will appear after the assigned mechanic starts sharing their location.",
                11,
                CustomerUi.Muted);
            message.HorizontalTextAlignment = TextAlignment.Center;
            map.Add(new VerticalStackLayout
            {
                Spacing = 8,
                Padding = new Thickness(28),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Children = { title, message }
            });
            return Card(map, Colors.White, 8, new Thickness(0));
        }

        var source = destinationLatitude is not null && destinationLongitude is not null
            ? BookingVisuals.GoogleDirectionsSource(liveLocation.Latitude, liveLocation.Longitude, destinationLatitude.Value, destinationLongitude.Value)
            : BookingVisuals.GoogleMapSource(liveLocation.Latitude, liveLocation.Longitude);
        map.Add(new WebView
        {
            Source = source,
            HeightRequest = 280
        });

        var badge = new VerticalStackLayout
        {
            Spacing = 2,
            Padding = new Thickness(12, 8),
            BackgroundColor = Color.FromRgba(255, 255, 255, 0.92),
            Children =
            {
                Label("Live mechanic route", 12, CustomerUi.Dark, FontAttributes.Bold),
                Label($"{route.Distance} | {route.Time}", 10, CustomerUi.Orange)
            }
        };
        map.Add(new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = badge,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(12)
        });

        return Card(map, Colors.White, 8, new Thickness(0));
    }

    private static View StatusCard(string status, ServiceRequestDto? request, bool hasLiveLocation)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };
        grid.Add(Avatar(Initials(request?.MechanicName ?? "BM"), 48, CustomerUi.LightOrange), 0, 0);

        var text = new VerticalStackLayout { Spacing = 3 };
        text.Add(Label(request?.MechanicName ?? "Mechanic not assigned yet", 14, CustomerUi.Dark, FontAttributes.Bold));
        text.Add(Label(StatusMessage(status, hasLiveLocation), 11, CustomerUi.Muted));
        grid.Add(text, 1, 0);
        grid.Add(Pill(FormatStatus(status), StatusColor(status), Colors.White), 2, 0);
        return Card(grid, Colors.White, 8, new Thickness(14));
    }

    private static View RouteStats((string Origin, string Destination, string Distance, string Time) route)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Add(RouteLine("From", route.Origin));
        stack.Add(RouteLine("To", route.Destination));

        var stats = new Grid { ColumnSpacing = 8 };
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.Add(Stat("Distance", route.Distance), 0, 0);
        stats.Add(Stat("ETA", route.Time), 1, 0);
        stack.Add(stats);

        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private static View SectionCard(string title, IReadOnlyList<View> rows)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Add(Label(title, 13, CustomerUi.Dark, FontAttributes.Bold));
        foreach (var row in rows)
        {
            stack.Add(row);
        }

        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private static View DetailRow(string label, string value)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };
        grid.Add(Label(label, 11, CustomerUi.Muted), 0, 0);
        var valueLabel = Label(value, 11, CustomerUi.Dark, FontAttributes.Bold);
        valueLabel.HorizontalTextAlignment = TextAlignment.End;
        grid.Add(valueLabel, 1, 0);
        return grid;
    }

    private static View Timeline(string status)
    {
        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Add(Label("Progress", 13, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(TimelineRow("Payment confirmed", "Tracking is available after secure payment.", IsAtLeast(status, "paid")));
        stack.Add(TimelineRow("Mechanic assigned", "A shop/mechanic accepted the booking.", IsAtLeast(status, "accepted")));
        stack.Add(TimelineRow("Mechanic en route", "Live route updates appear when location is shared.", IsAtLeast(status, "en_route")));
        stack.Add(TimelineRow("Arrived", "Mechanic reached the service location.", IsAtLeast(status, "arrived")));
        stack.Add(TimelineRow("Repair in progress", "The repair work has started.", IsAtLeast(status, "in_progress")));
        stack.Add(TimelineRow("Completed", "Review your repair and keep the receipt.", IsAtLeast(status, "completed")));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View ActionCard(string status, string destination)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        if (IsAtLeast(status, "completed"))
        {
            stack.Add(Label("Repair completed", 13, CustomerUi.Dark, FontAttributes.Bold));
            stack.Add(Label("Please review your repair experience. Your feedback helps BikeMate improve shop and mechanic quality.", 11, CustomerUi.Muted));
            stack.Add(OrangeButton("Review repair", new Command(async () =>
            {
                BookingDraft.RequestId = _requestId;
                await Shell.Current.GoToAsync(nameof(BookingRatingPage));
            })));
        }
        else if (string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase))
        {
            stack.Add(Label("Booking ended", 13, CustomerUi.Dark, FontAttributes.Bold));
            stack.Add(Label("This request is no longer active. You can return home and book another service.", 11, CustomerUi.Muted));
            stack.Add(OrangeButton("Return home", new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))));
        }
        else
        {
            var buttons = new Grid { ColumnSpacing = 8 };
            buttons.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            buttons.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            buttons.Add(OrangeButton("Refresh status", new Command(async () => await LoadAsync())), 0, 0);
            buttons.Add(GhostButton("Open map", new Command(async () => await BookingVisuals.OpenGoogleMapsAsync(destination))), 1, 0);
            stack.Add(buttons);
            stack.Add(Label("The page refreshes automatically while open when the mechanic app shares live location.", 10, CustomerUi.Muted));
        }

        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private static View RouteLine(string label, string value)
    {
        var stack = new VerticalStackLayout { Spacing = 2 };
        stack.Add(Label(label.ToUpperInvariant(), 9, CustomerUi.Muted, FontAttributes.Bold));
        stack.Add(Label(value, 12, CustomerUi.Dark));
        return stack;
    }

    private static View Stat(string label, string value)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#F7F7F7"),
            Stroke = CustomerUi.Border,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12, 8),
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    Label(label.ToUpperInvariant(), 9, CustomerUi.Muted, FontAttributes.Bold),
                    Label(value, 15, CustomerUi.Dark, FontAttributes.Bold)
                }
            }
        };
    }

    private static View TimelineRow(string title, string subtitle, bool done)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        grid.Add(new Border
        {
            WidthRequest = 18,
            HeightRequest = 18,
            Stroke = done ? CustomerUi.Orange : CustomerUi.Border,
            BackgroundColor = done ? CustomerUi.Orange : Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 9 },
            Content = new Label
            {
                Text = done ? "\u2713" : "",
                TextColor = Colors.White,
                FontSize = AppTypography.CaptionSize,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        }, 0, 0);

        var text = new VerticalStackLayout { Spacing = 2 };
        text.Add(Label(title, 11, CustomerUi.Dark, FontAttributes.Bold));
        text.Add(Label(subtitle, 9, CustomerUi.Muted));
        grid.Add(text, 1, 0);
        return grid;
    }

    private static View Pill(string text, Color background, Color textColor)
    {
        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = background,
            Padding = new Thickness(10, 5),
            Content = new Label
            {
                Text = text,
                FontSize = AppTypography.CaptionSize,
                FontAttributes = FontAttributes.Bold,
                TextColor = textColor,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
    }

    private static (string Origin, string Destination, string Distance, string Time) BuildRouteSummary(
        decimal? mechanicLatitude,
        decimal? mechanicLongitude,
        decimal? destinationLatitude,
        decimal? destinationLongitude,
        string destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            destination = "Service location";
        }

        if (mechanicLatitude is null ||
            mechanicLongitude is null ||
            destinationLatitude is null ||
            destinationLongitude is null)
        {
            return ("Mechanic live location", destination, "--", "--");
        }

        var km = DistanceKm(mechanicLatitude.Value, mechanicLongitude.Value, destinationLatitude.Value, destinationLongitude.Value);
        var minutes = Math.Max(1, (int)Math.Ceiling((double)km / 18d * 60d));
        return ("Mechanic live location", destination, $"{km:0.##} km", $"{minutes} min");
    }

    private static decimal DistanceKm(decimal latitudeA, decimal longitudeA, decimal latitudeB, decimal longitudeB)
    {
        const double earthRadiusKm = 6371d;
        var lat1 = ToRadians((double)latitudeA);
        var lat2 = ToRadians((double)latitudeB);
        var deltaLat = ToRadians((double)(latitudeB - latitudeA));
        var deltaLng = ToRadians((double)(longitudeB - longitudeA));
        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round((decimal)(earthRadiusKm * c), 2);
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }

    private static bool IsAtLeast(string status, string target)
    {
        return StatusRank(status) >= StatusRank(target);
    }

    private static int StatusRank(string status)
    {
        return status switch
        {
            "payment_pending" => 1,
            "paid" => 2,
            "pending" => 2,
            "accepted" => 3,
            "en_route" => 4,
            "arrived" => 5,
            "in_progress" => 6,
            "completed" => 7,
            "cancelled" => 7,
            "rejected" => 7,
            _ => 0
        };
    }

    private static Color StatusColor(string status)
    {
        return status switch
        {
            "completed" => Color.FromArgb("#1D7D46"),
            "cancelled" or "rejected" => Color.FromArgb("#9F2A2A"),
            "en_route" or "arrived" or "in_progress" => CustomerUi.Orange,
            _ => Color.FromArgb("#6E6E6E")
        };
    }

    private static string StatusMessage(string status, bool hasLiveLocation)
    {
        return status switch
        {
            "payment_pending" => "Waiting for secure payment before tracking starts.",
            "paid" or "pending" => "Waiting for shop or mechanic confirmation.",
            "accepted" => hasLiveLocation ? "Mechanic accepted and location is active." : "Mechanic accepted. Waiting for live location.",
            "en_route" => hasLiveLocation ? "Mechanic is on the way to you." : "Mechanic is on the way. Waiting for location update.",
            "arrived" => "Mechanic has arrived at the service location.",
            "in_progress" => "Repair is currently in progress.",
            "completed" => "Repair is complete and ready for review.",
            "cancelled" => "This booking was cancelled.",
            "rejected" => "This booking was rejected.",
            _ => "Tracking details are being updated."
        };
    }

    private static string Initials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "BM";
        }

        return string.Concat(parts.Take(2).Select(x => char.ToUpperInvariant(x[0])));
    }
}

internal static class CustomerPaymentGate
{
    public static async Task<bool> EnsurePaidOrRedirectAsync(Page page, int requestId)
    {
        var request = await CustomerApiClient.GetRequestAsync(requestId);
        if (CustomerRequestRules.IsEmergency(request))
        {
            await Shell.Current.GoToAsync($"{nameof(ActiveEmergencyTrackingPage)}?requestId={requestId}");
            return false;
        }

        var payment = await CustomerApiClient.GetLatestPaymentForRequestAsync(requestId);
        if (payment is not null && !string.Equals(payment.Status, "paid", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                payment = await CustomerApiClient.RefreshPaymentAsync(payment.PaymentId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Payment refresh failed for payment {payment.PaymentId}: {ex}");
            }
        }

        if (payment is not null && string.Equals(payment.Status, "paid", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        await page.DisplayAlertAsync("Payment required", "Complete secure payment before tracking your mechanic.", "OK");
        await Shell.Current.GoToAsync($"{nameof(PaymentCheckoutPage)}?requestId={requestId}");
        return false;
    }
}
