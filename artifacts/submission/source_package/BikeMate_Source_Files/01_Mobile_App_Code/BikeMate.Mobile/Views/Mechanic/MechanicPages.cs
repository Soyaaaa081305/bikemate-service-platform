using System.Diagnostics;
using System.Globalization;
using System.Net;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using BikeMate.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;

namespace BikeMate.Views.Mechanic;

public abstract class MechanicPageBase : ContentPage
{
    protected static readonly Color Orange = Color.FromArgb("#FF6B2C");
    protected static readonly Color Dark = Color.FromArgb("#242424");
    protected static readonly Color Muted = Color.FromArgb("#6E6E6E");
    protected static readonly Color BorderColor = Color.FromArgb("#E6E6E6");
    protected static readonly Color PageColor = Color.FromArgb("#F6F6F6");
    protected static readonly Color SoftOrange = Color.FromArgb("#FFF2EA");
    protected static readonly Color SoftGreen = Color.FromArgb("#EAF8EF");
    protected static readonly Color SoftBlue = Color.FromArgb("#EEF6FF");
    protected static readonly Color SoftRed = Color.FromArgb("#FFECEC");

    protected MechanicPageBase()
    {
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = PageColor;
    }

    protected void SetPage(string title, string subtitle, View content, bool isLoading, Func<Task> refresh, bool showBack = false)
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };

        root.Add(Header(title, subtitle, isLoading, refresh, showBack), 0, 0);
        root.Add(new ScrollView { Content = content }, 0, 1);
        Content = root;
    }

    protected View Header(string title, string subtitle, bool isLoading, Func<Task> refresh, bool showBack = false)
    {
        var grid = new Grid
        {
            BackgroundColor = Orange,
            Padding = new Thickness(20, 24, 16, 18),
            ColumnSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition(showBack ? GridLength.Auto : new GridLength(0)),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        if (showBack)
        {
            grid.Add(new Button
            {
                Text = "<",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.White,
                BorderColor = Color.FromArgb("#FFF2EA"),
                BorderWidth = 1,
                CornerRadius = 8,
                WidthRequest = 40,
                HeightRequest = 40,
                Padding = new Thickness(0),
                Command = new Command(async () => await Shell.Current.GoToAsync(".."))
            }, 0, 0);
        }

        var text = new VerticalStackLayout { Spacing = 4 };
        text.Add(Text(title, 20, Colors.White, FontAttributes.Bold));
        text.Add(Text(subtitle, 11, Color.FromArgb("#FFF2EA")));
        grid.Add(text, 1, 0);
        grid.Add(new Button
        {
            Text = isLoading ? "..." : "Refresh",
            IsEnabled = !isLoading,
            BackgroundColor = Colors.White,
            TextColor = Orange,
            CornerRadius = 8,
            HeightRequest = 40,
            Padding = new Thickness(14, 0),
            FontAttributes = FontAttributes.Bold,
            Command = new Command(async () => await refresh())
        }, 2, 0);

        return grid;
    }

    protected static Label Text(string text, double size, Color color, FontAttributes attributes = FontAttributes.None)
    {
        return new Label
        {
            Text = text,
            FontSize = AppTypography.SizeFor(size),
            TextColor = color,
            FontAttributes = attributes,
            FontFamily = FontFor(size, attributes),
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private static string FontFor(double size, FontAttributes attributes = FontAttributes.None)
    {
        return AppTypography.FontFor(size, attributes);
    }

    protected static Border Card(View content, Color? background = null, Thickness? padding = null)
    {
        return new Border
        {
            Stroke = BorderColor,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = background ?? Colors.White,
            Padding = padding ?? new Thickness(14),
            Shadow = new Shadow
            {
                Brush = Brush.Black,
                Opacity = 0.06f,
                Radius = 5,
                Offset = new Point(0, 2)
            },
            Content = content
        };
    }

    protected static Button PrimaryButton(string text, Func<Task> action, bool enabled = true)
    {
        return new Button
        {
            Text = text,
            IsEnabled = enabled,
            BackgroundColor = Orange,
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 48,
            MinimumHeightRequest = 48,
            Padding = new Thickness(18, 0),
            FontAttributes = FontAttributes.Bold,
            FontFamily = AppTypography.DisplayFont,
            FontSize = AppTypography.BodySize,
            Command = new Command(async () => await action())
        };
    }

    protected static Button SecondaryButton(string text, Func<Task> action, bool enabled = true)
    {
        return new Button
        {
            Text = text,
            IsEnabled = enabled,
            BackgroundColor = Colors.White,
            TextColor = Dark,
            BorderColor = BorderColor,
            BorderWidth = 1,
            CornerRadius = 8,
            HeightRequest = 46,
            MinimumHeightRequest = 46,
            Padding = new Thickness(16, 0),
            FontAttributes = FontAttributes.Bold,
            FontFamily = AppTypography.DisplayFont,
            FontSize = AppTypography.BodySize,
            Command = new Command(async () => await action())
        };
    }

    protected static View Banner(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? new BoxView { HeightRequest = 0, Opacity = 0 }
            : Card(Text(text, 11, Muted), Color.FromArgb("#FFF8F4"));
    }

    protected static View Stat(string label, string value, Color background)
    {
        var stack = new VerticalStackLayout { Spacing = 2 };
        stack.Add(Text(value, 17, Dark, FontAttributes.Bold));
        stack.Add(Text(label.ToUpperInvariant(), 9, Muted, FontAttributes.Bold));
        return Card(stack, background, new Thickness(12));
    }

    protected static View SectionTitle(string title, string? subtitle = null)
    {
        var stack = new VerticalStackLayout { Spacing = 2 };
        stack.Add(Text(title, 14, Dark, FontAttributes.Bold));
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            stack.Add(Text(subtitle, 10, Muted));
        }

        return stack;
    }

    protected static View EmptyState(string title, string message, string? actionText = null, Func<Task>? action = null)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(Text(title, 13, Dark, FontAttributes.Bold));
        stack.Add(Text(message, 11, Muted));
        if (actionText is not null && action is not null)
        {
            stack.Add(SecondaryButton(actionText, action));
        }

        return Card(stack, Colors.White);
    }

    protected static View DetailRow(string label, string value)
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
        grid.Add(Text(label, 11, Muted), 0, 0);
        var valueLabel = Text(value, 11, Dark, FontAttributes.Bold);
        valueLabel.HorizontalTextAlignment = TextAlignment.End;
        grid.Add(valueLabel, 1, 0);
        return grid;
    }

    protected static Button CompactButton(string text, Func<Task> action)
    {
        return new Button
        {
            Text = text,
            BackgroundColor = Colors.White,
            TextColor = Dark,
            BorderColor = BorderColor,
            BorderWidth = 1,
            CornerRadius = 8,
            HeightRequest = 40,
            MinimumHeightRequest = 40,
            Padding = new Thickness(12, 0),
            FontSize = AppTypography.CaptionSize,
            FontAttributes = FontAttributes.Bold,
            FontFamily = AppTypography.DisplayFont,
            Command = new Command(async () => await action())
        };
    }

    protected static View AttachmentPreview(string attachmentUrl)
    {
        var uriPath = Uri.TryCreate(attachmentUrl, UriKind.Absolute, out var uri) ? uri.LocalPath : attachmentUrl;
        var extension = System.IO.Path.GetExtension(uriPath);
        var isImage = extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);

        View preview = isImage
            ? new Image
            {
                Source = attachmentUrl,
                HeightRequest = 150,
                WidthRequest = 220,
                Aspect = Aspect.AspectFill,
                BackgroundColor = Color.FromArgb("#F1F1F1")
            }
            : Text(System.IO.Path.GetFileName(uriPath), 11, Orange, FontAttributes.Bold);

        preview.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                if (Uri.TryCreate(attachmentUrl, UriKind.Absolute, out var openUri))
                {
                    await Launcher.Default.OpenAsync(openUri);
                }
            })
        });
        return preview;
    }

    protected static View JobCard(ServiceRequestDto job, Func<int, Task>? details = null, Func<int, Task>? accept = null, Func<int, Task>? reject = null)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        var title = string.IsNullOrWhiteSpace(job.ServiceName) ? $"Request #{job.RequestId}" : job.ServiceName!;
        var top = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        top.Add(Text(title, 15, Dark, FontAttributes.Bold), 0, 0);
        top.Add(StatusPill(FormatStatus(job.CurrentStatus), StatusColor(job.CurrentStatus)), 1, 0);
        stack.Add(top);
        stack.Add(Text(job.CustomerName, 11, Muted));
        stack.Add(Text(string.IsNullOrWhiteSpace(job.ServiceLocationAddress) ? "No address provided" : job.ServiceLocationAddress!, 12, Dark));
        stack.Add(Text(CleanIssue(job.IssueDescription), 11, Muted));
        stack.Add(Text(JobMeta(job), 11, Orange, FontAttributes.Bold));

        var buttons = new Grid { ColumnSpacing = 8 };
        var buttonCount = 1 + (accept is null ? 0 : 1) + (reject is null ? 0 : 1);
        for (var index = 0; index < buttonCount; index++)
        {
            buttons.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        buttons.Add(SecondaryButton("Details", async () =>
        {
            if (details is not null)
            {
                await details(job.RequestId);
            }
            else
            {
                await Shell.Current.GoToAsync($"{nameof(MechanicJobDetailsPage)}?requestId={job.RequestId}");
            }
        }), 0, 0);

        var column = 1;
        if (accept is not null)
        {
            buttons.Add(PrimaryButton("Accept", async () => await accept(job.RequestId)), column++, 0);
        }

        if (reject is not null)
        {
            buttons.Add(SecondaryButton("Decline", async () => await reject(job.RequestId)), column, 0);
        }

        stack.Add(buttons);
        return Card(stack);
    }

    protected static View StatusPill(string text, Color background)
    {
        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = background,
            Padding = new Thickness(10, 5),
            VerticalOptions = LayoutOptions.Start,
            Content = Text(text, 10, Colors.White, FontAttributes.Bold)
        };
    }

    protected static View AvatarBadge(string text, Color background, double size = 46)
    {
        var initials = string.Concat(text
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0])));
        if (string.IsNullOrWhiteSpace(initials))
        {
            initials = "BM";
        }

        return new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = size / 2 },
            BackgroundColor = background,
            Content = new Label
            {
                Text = initials,
                TextColor = Dark,
                FontSize = AppTypography.BodySize,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
    }

    protected static View ProfileAvatar(string text, string? imageUrl, Color background, double size = 46)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return AvatarBadge(text, background, size);
        }

        return new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            Stroke = BorderColor,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = size / 2 },
            BackgroundColor = background,
            Content = new Image
            {
                Source = imageUrl,
                Aspect = Aspect.AspectFill
            }
        };
    }

    protected static string FormatStatus(string status)
    {
        return string.Join(" ", status.Replace("_", " ", StringComparison.Ordinal).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(x)));
    }

    protected static string Money(decimal amount)
    {
        return string.Create(CultureInfo.InvariantCulture, $"PHP {amount:N2}");
    }

    protected static string JobMeta(ServiceRequestDto job)
    {
        var total = job.FinalTotal > 0 ? job.FinalTotal : job.EstimatedTotal;
        var distance = DistanceFromYou(job);
        var schedule = job.ScheduledAt?.ToLocalTime().ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture) ?? "ASAP";
        return $"{distance} - {schedule} - {Money(total)}";
    }

    protected static string DistanceFromYou(ServiceRequestDto job)
    {
        return job.DistanceKm is null
            ? "Share location for distance"
            : $"{job.DistanceKm:0.##} km from you";
    }

    protected static string CleanIssue(string issue)
    {
        return issue
            .Replace("[EMERGENCY]", "Emergency:", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    protected static int StatusRank(string status)
    {
        return status switch
        {
            "payment_pending" or "pending" => 1,
            "paid" => 2,
            "accepted" => 3,
            "en_route" => 4,
            "arrived" => 5,
            "in_progress" => 6,
            "completed" => 7,
            "cancelled" or "rejected" => 8,
            _ => 0
        };
    }

    protected static bool IsTerminalStatus(string status)
    {
        return status is "completed" or "cancelled" or "rejected";
    }

    protected static Color StatusColor(string status)
    {
        return status switch
        {
            "completed" => Color.FromArgb("#147A3D"),
            "cancelled" or "rejected" => Color.FromArgb("#A23232"),
            "accepted" or "en_route" or "arrived" or "in_progress" => Orange,
            "paid" => Color.FromArgb("#347A52"),
            _ => Color.FromArgb("#6E6E6E")
        };
    }

    protected static HtmlWebViewSource MapSource(decimal latitude, decimal longitude)
    {
        var query = $"{latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}";
        var key = GoogleMapsEmbedKey();
        var frameUrl = string.IsNullOrWhiteSpace(key)
            ? $"https://maps.google.com/maps?q={Uri.EscapeDataString(query)}&z=15&output=embed"
            : $"https://www.google.com/maps/embed/v1/place?key={Uri.EscapeDataString(key)}&q={Uri.EscapeDataString(query)}&zoom=15";
        var encodedUrl = WebUtility.HtmlEncode(frameUrl);
        return new HtmlWebViewSource
        {
            BaseUrl = "https://www.google.com",
            Html = $$"""
<!doctype html>
<html>
<head>
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <style>
    html, body, iframe { width: 100%; height: 100%; margin: 0; padding: 0; border: 0; overflow: hidden; background: #eef1f4; }
  </style>
</head>
<body>
  <iframe src="{{encodedUrl}}" loading="lazy" referrerpolicy="no-referrer-when-downgrade" allowfullscreen></iframe>
</body>
</html>
"""
        };
    }

    private static string GoogleMapsEmbedKey()
    {
#if ANDROID
        return Android.App.Application.Context.GetString(Resource.String.google_maps_embed_key) ?? string.Empty;
#else
        return string.Empty;
#endif
    }

    protected static async Task<(decimal Latitude, decimal Longitude, decimal? Accuracy)?> ResolveAreaAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(12)))
                    ?? await Geolocation.Default.GetLastKnownLocationAsync();
                if (location is not null)
                {
                    return ((decimal)location.Latitude, (decimal)location.Longitude, (decimal?)location.Accuracy);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Location resolution failed: {ex}");
        }

        return null;
    }

    protected async Task UseMyAreaAsync(Func<string, Task> afterUpdate)
    {
        var area = await ResolveAreaAsync();
        if (area is null)
        {
            await afterUpdate("GPS location is unavailable. Turn on precise location and try again.");
            return;
        }

        await RiderApiClient.UpdateLocationAsync(null, area.Value.Latitude, area.Value.Longitude, area.Value.Accuracy);
        await afterUpdate("Your current area was saved. Incoming jobs are now filtered nearby first.");
    }
}
public sealed class MechanicDashboardPage : MechanicPageBase
{
    private RiderDashboardDto? _dashboard;
    private bool _isLoading;
    private string? _banner;

    public MechanicDashboardPage()
    {
        Title = "Mechanic Dashboard";
        Render("Loading mechanic dashboard...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing mechanic data...");
        try
        {
            _dashboard = await RiderApiClient.GetDashboardAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _banner = $"Could not load mechanic dashboard. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));

        if (_dashboard is null)
        {
            body.Add(Card(Text(_isLoading ? "Loading mechanic profile..." : "No mechanic data yet.", 12, Muted)));
            SetPage("Mechanic", "Jobs around your saved area.", body, _isLoading, async () => await LoadAsync());
            return;
        }

        var profileCard = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };
        profileCard.Add(ProfileAvatar(_dashboard.Profile.FullName, _dashboard.Profile.ProfileImageUrl, SoftOrange, 54), 0, 0);
        profileCard.Add(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                Text(_dashboard.Profile.FullName, 19, Dark, FontAttributes.Bold),
                Text($"{FormatStatus(_dashboard.AvailabilityStatus)} - {_dashboard.AverageRating:0.0} rating - {_dashboard.TotalCompletedJobs} completed jobs", 11, Muted),
                Text(IsOffline(_dashboard.AvailabilityStatus)
                    ? "You are offline. New bookings are hidden until you go online."
                    : "You are available for nearby bookings. Open Jobs from the bottom navigation.",
                    11,
                    Muted)
            }
        }, 1, 0);
        body.Add(Card(profileCard, Colors.White));

        body.Add(AvailabilityCard(_dashboard.AvailabilityStatus));

        var stats = new Grid { ColumnSpacing = 8 };
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.Add(Stat("Nearby jobs", _dashboard.IncomingRequests.ToString(CultureInfo.InvariantCulture), SoftBlue), 0, 0);
        stats.Add(Stat("Emergency", _dashboard.EmergencyRequests.ToString(CultureInfo.InvariantCulture), SoftOrange), 1, 0);
        body.Add(stats);

        body.Add(IsOffline(_dashboard.AvailabilityStatus)
            ? PrimaryButton("Go online", async () =>
            {
                await RiderApiClient.GoOnlineAsync();
                await LoadAsync("You are online and ready for nearby jobs.");
            })
            : SecondaryButton("Go offline", async () =>
            {
                await RiderApiClient.GoOfflineAsync();
                await LoadAsync("You are offline. New jobs are hidden until you go online.");
            }));

        body.Add(SectionTitle("Current job", "Your active booking and next action."));
        body.Add(_dashboard.ActiveJob is null
            ? EmptyState(
                "No active job",
                IsOffline(_dashboard.AvailabilityStatus)
                    ? "Go online when you are ready to receive bookings."
                    : "Open Jobs from the bottom navigation to accept a nearby request.")
            : JobCard(_dashboard.ActiveJob));

        SetPage("Mechanic", "Availability, matching area, and live job summary.", body, _isLoading, async () => await LoadAsync());
    }

    private static View AvailabilityCard(string status)
    {
        var offline = IsOffline(status);
        var stack = new VerticalStackLayout { Spacing = 5 };
        stack.Add(Text(offline ? "Offline mode" : "Online mode", 14, Dark, FontAttributes.Bold));
        stack.Add(Text(
            offline
                ? "You will not appear as available for new mechanic jobs."
                : "You can receive and accept nearby mechanic jobs from the Jobs tab.",
            11,
            Muted));
        return Card(stack, offline ? Color.FromArgb("#F1F1F1") : SoftGreen, new Thickness(14));
    }

    private static bool IsOffline(string status)
    {
        return status.Equals("offline", StringComparison.OrdinalIgnoreCase);
    }
}
public sealed class MechanicJobsPage : MechanicPageBase
{
    private IReadOnlyList<ServiceRequestDto> _jobs = [];
    private bool _isLoading;
    private string? _banner;

    public MechanicJobsPage()
    {
        Title = "Jobs";
        Render("Loading nearby jobs...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Checking jobs near your saved area...");
        try
        {
            _jobs = await RiderApiClient.GetIncomingAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _jobs = [];
            _banner = $"Could not load nearby jobs. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));
        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Text("Nearby matching", 16, Dark, FontAttributes.Bold),
                Text("Incoming requests use your saved mechanic location and show jobs within about 8 km first.", 11, Muted)
            }
        }, SoftBlue));

        var actionRow = new Grid { ColumnSpacing = 8 };
        actionRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actionRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actionRow.Add(SecondaryButton("Use my area", async () => await UseMyAreaAsync(LoadAsync)), 0, 0);
        actionRow.Add(PrimaryButton("Current job", async () => await Shell.Current.GoToAsync(nameof(MechanicJobDetailsPage))), 1, 0);
        body.Add(actionRow);

        if (_isLoading)
        {
            body.Add(new ActivityIndicator { IsRunning = true, Color = Orange, HeightRequest = 42 });
        }

        if (_jobs.Count == 0 && !_isLoading)
        {
            body.Add(EmptyState(
                "No nearby requests",
                "Ask the customer side to create a booking near your saved area, then refresh.",
                "Use my area",
                async () => await UseMyAreaAsync(LoadAsync)));
        }
        else
        {
            foreach (var job in _jobs)
            {
                body.Add(JobCard(
                    job,
                    requestId => Shell.Current.GoToAsync($"{nameof(MechanicJobDetailsPage)}?requestId={requestId}"),
                    AcceptAsync,
                    RejectAsync));
            }
        }

        SetPage("Jobs", "Incoming requests near your mechanic area.", body, _isLoading, async () => await LoadAsync());
    }

    private async Task AcceptAsync(int requestId)
    {
        await RiderApiClient.AcceptAsync(requestId);
        await Shell.Current.GoToAsync($"{nameof(MechanicJobDetailsPage)}?requestId={requestId}");
    }

    private async Task RejectAsync(int requestId)
    {
        var confirm = await DisplayAlertAsync("Decline booking", "Declining rejects this booking for the customer. Continue?", "Decline", "Cancel");
        if (!confirm)
        {
            return;
        }

        await RiderApiClient.RejectAsync(requestId);
        await LoadAsync($"Request #{requestId} was declined.");
    }
}

public sealed class MechanicEmergencyRequestsPage : MechanicPageBase
{
    private IReadOnlyList<ServiceRequestDto> _jobs = [];
    private bool _isLoading;
    private string? _banner;

    public MechanicEmergencyRequestsPage()
    {
        Title = "Emergency";
        Render("Loading emergency requests...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Checking emergency jobs near you...");
        try
        {
            _jobs = await RiderApiClient.GetEmergencyAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _jobs = [];
            _banner = $"Could not load emergency requests. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));
        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                Text("Emergency queue", 16, Dark, FontAttributes.Bold),
                Text("Emergency jobs use a wider nearby radius so responders can find high-priority roadside requests.", 11, Muted)
            }
        }, SoftOrange));
        body.Add(SecondaryButton("Use my area", async () => await UseMyAreaAsync(LoadAsync)));

        if (_jobs.Count == 0 && !_isLoading)
        {
            body.Add(EmptyState("No emergency requests", "No emergency requests are near your saved area."));
        }
        else
        {
            foreach (var job in _jobs)
            {
                body.Add(JobCard(job, requestId => Shell.Current.GoToAsync($"{nameof(MechanicJobDetailsPage)}?requestId={requestId}"), AcceptAsync));
            }
        }

        SetPage("Emergency", "Urgent nearby jobs for responders.", body, _isLoading, async () => await LoadAsync());
    }

    private async Task AcceptAsync(int requestId)
    {
        await RiderApiClient.AcceptAsync(requestId);
        await Shell.Current.GoToAsync($"{nameof(MechanicJobDetailsPage)}?requestId={requestId}");
    }
}

public sealed class MechanicJobDetailsPage : MechanicPageBase, IQueryAttributable
{
    private int _requestId;
    private ServiceRequestDto? _job;
    private readonly List<CompletionUpload> _completionProofs = [];
    private Editor? _completionCommentEditor;
    private string _completionComment = string.Empty;
    private bool _isLoading;
    private string? _banner;

    public MechanicJobDetailsPage()
    {
        Title = "Current Job";
        Render("Loading job...");
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
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing job details...");
        try
        {
            _job = _requestId > 0
                ? await RiderApiClient.GetJobAsync(_requestId)
                : await RiderApiClient.GetCurrentJobAsync();
            _requestId = _job?.RequestId ?? _requestId;
            _banner = banner;
        }
        catch (Exception ex)
        {
            _job = null;
            _banner = $"Could not load job details. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));

        if (_job is null)
        {
            body.Add(Card(Text(_isLoading ? "Loading current job..." : "No active job is assigned yet.", 12, Muted)));
            body.Add(PrimaryButton("Find nearby jobs", async () => await Shell.Current.GoToAsync("//MechanicJobsPage")));
            SetPage("Current Job", "Job details, status, and location.", body, _isLoading, async () => await LoadAsync(), true);
            return;
        }

        body.Add(JobOverview(_job));
        body.Add(JobProgress(_job.CurrentStatus));
        body.Add(JobDetails(_job));
        body.Add(JobActions(_job));

        SetPage("Current Job", "Move this booking through the mechanic workflow.", body, _isLoading, async () => await LoadAsync(), true);
    }

    private async Task UpdateStatusAsync(string status, string? notes = null)
    {
        if (_job is null)
        {
            return;
        }

        await ShareJobLocationIfAvailableAsync(_job.RequestId);
        _job = await RiderApiClient.UpdateStatusAsync(_job.RequestId, status, notes ?? $"Mechanic marked {FormatStatus(status)}.");
        await LoadAsync($"Request #{_job.RequestId} is now {FormatStatus(_job.CurrentStatus)}.");
    }

    private static View JobOverview(ServiceRequestDto job)
    {
        var title = string.IsNullOrWhiteSpace(job.ServiceName) ? $"Request #{job.RequestId}" : job.ServiceName!;
        var stack = new VerticalStackLayout { Spacing = 8 };
        var top = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        top.Add(Text(title, 17, Dark, FontAttributes.Bold), 0, 0);
        top.Add(StatusPill(FormatStatus(job.CurrentStatus), StatusColor(job.CurrentStatus)), 1, 0);
        stack.Add(top);
        stack.Add(Text($"Customer: {job.CustomerName}", 12, Muted));
        stack.Add(Text(CleanIssue(job.IssueDescription), 12, Muted));
        stack.Add(Text(JobMeta(job), 11, Orange, FontAttributes.Bold));
        return Card(stack, Colors.White);
    }

    private static View JobDetails(ServiceRequestDto job)
    {
        var total = job.FinalTotal > 0 ? job.FinalTotal : job.EstimatedTotal;
        var rows = new VerticalStackLayout { Spacing = 10 };
        rows.Add(Text("Booking details", 13, Dark, FontAttributes.Bold));
        rows.Add(DetailRow("Booking ID", $"BM-{job.RequestId:000000}"));
        rows.Add(DetailRow("Location", job.ServiceLocationAddress ?? "No address provided"));
        rows.Add(DetailRow("Distance from you", DistanceFromYou(job)));
        rows.Add(DetailRow("Schedule", job.ScheduledAt?.ToLocalTime().ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture) ?? "ASAP"));
        rows.Add(DetailRow("Shop", job.ShopName ?? "BikeMate partner"));
        rows.Add(DetailRow("Total", total > 0 ? Money(total) : "To be finalized"));
        return Card(rows, Colors.White);
    }

    private View JobActions(ServiceRequestDto job)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };

        var next = NextStep(job.CurrentStatus);
        if (next is null)
        {
            stack.Add(Text(
                job.CurrentStatus == "completed"
                    ? "This job is complete. It now belongs in History."
                    : "This booking is no longer active.",
                11,
                Muted));
            return Card(stack, Colors.White);
        }

        stack.Add(Text(next.Value.Description, 11, Muted));
        if (job.CurrentStatus is "arrived" or "in_progress")
        {
            stack.Add(CompletionProofCard());
        }

        stack.Add(PrimaryButton(
            next.Value.Label,
            next.Value.Status == "accepted"
                ? AcceptCurrentAsync
                : next.Value.Status == "completed"
                    ? CompleteWithProofAsync
                : async () => await UpdateStatusAsync(next.Value.Status),
            !_isLoading));

        stack.Add(SecondaryButton("Open map", async () => await Shell.Current.GoToAsync($"{nameof(MechanicMapPage)}?requestId={job.RequestId}"), !_isLoading));

        return Card(stack, Colors.White);
    }

    private static View JobProgress(string status)
    {
        var stack = new VerticalStackLayout { Spacing = 0 };
        stack.Add(Text("Mechanic flow", 13, Dark, FontAttributes.Bold));
        stack.Add(Text("Use the next action only after that step actually happens.", 10, Muted));
        stack.Add(new BoxView { HeightRequest = 12, Opacity = 0 });

        var steps = new[]
        {
            ("Accepted", "Mechanic accepted the customer booking.", "accepted"),
            ("On the way", "Travel started and live location can be shared.", "en_route"),
            ("Arrived", "Mechanic reached the service location.", "arrived"),
            ("Completed", "Repair proof and mechanic comment are submitted.", "completed")
        };

        for (var index = 0; index < steps.Length; index++)
        {
            var step = steps[index];
            stack.Add(ProgressRow(step.Item1, step.Item2, ProgressState(status, step.Item3), index < steps.Length - 1));
        }

        return Card(stack, Colors.White);
    }

    private static View ProgressRow(string title, string subtitle, string state, bool showConnector)
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

        var active = state is "done" or "current";
        var markerColumn = new Grid
        {
            WidthRequest = 22,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };
        markerColumn.Add(new Border
        {
            WidthRequest = 20,
            HeightRequest = 20,
            Stroke = active ? Orange : BorderColor,
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            BackgroundColor = state == "done" ? Orange : Colors.White,
            Content = new Label
            {
                Text = state == "done" ? "\u2713" : state == "current" ? "\u2022" : "",
                TextColor = state == "done" ? Colors.White : Orange,
                FontSize = state == "current" ? 18 : 10,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        }, 0, 0);

        if (showConnector)
        {
            markerColumn.Add(new BoxView
            {
                WidthRequest = 2,
                Color = state == "done" ? Orange : BorderColor,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 2)
            }, 0, 1);
        }

        grid.Add(markerColumn, 0, 0);
        var text = new VerticalStackLayout
        {
            Spacing = 2,
            Padding = new Thickness(0, 0, 0, showConnector ? 16 : 0)
        };
        text.Add(Text(title, 11, state == "upcoming" ? Muted : Dark, state == "current" ? FontAttributes.Bold : FontAttributes.None));
        text.Add(Text(subtitle, 9, Muted));
        grid.Add(text, 1, 0);
        return grid;
    }

    private static string ProgressState(string status, string target)
    {
        if (status is "cancelled" or "rejected")
        {
            return "upcoming";
        }

        var currentRank = StatusRank(status);
        var targetRank = StatusRank(target);
        return currentRank > targetRank ? "done" :
            currentRank == targetRank ? "current" : "upcoming";
    }

    private static (string Label, string Status, string Description)? NextStep(string status)
    {
        return status switch
        {
            "pending" or "paid" or "payment_pending" => ("Accept job", "accepted", "Accept this booking before travelling to the customer."),
            "accepted" => ("Mark on the way", "en_route", "Use this when you leave for the customer location."),
            "en_route" => ("Mark arrived", "arrived", "Use this when you reach the service location."),
            "arrived" or "in_progress" => ("Complete job", "completed", "Attach completion images or videos, add a mechanic comment, then close this booking."),
            _ => null
        };
    }

    private View CompletionProofCard()
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(Text("Completion proof", 12, Dark, FontAttributes.Bold));
        stack.Add(Text(
            _completionProofs.Count == 0
                ? "Attach one or more images or videos showing the finished repair before completing this job."
                : $"{_completionProofs.Count} proof file(s) attached.",
            10,
            Muted));

        foreach (var proof in _completionProofs)
        {
            stack.Add(DetailRow(proof.Kind, proof.FileName));
        }

        stack.Add(SecondaryButton(
            _completionProofs.Count == 0 ? "Add images/videos" : "Add more proof",
            PickCompletionProofsAsync,
            !_isLoading));

        _completionCommentEditor = new Editor
        {
            Text = _completionComment,
            Placeholder = "Mechanic comment for this job",
            AutoSize = EditorAutoSizeOption.TextChanges,
            MinimumHeightRequest = 90,
            TextColor = Dark,
            PlaceholderColor = Muted,
            BackgroundColor = Colors.Transparent
        };
        _completionCommentEditor.TextChanged += (_, e) => _completionComment = e.NewTextValue ?? string.Empty;
        stack.Add(Text("Mechanic comment", 12, Dark, FontAttributes.Bold));
        stack.Add(Card(_completionCommentEditor, Colors.White, new Thickness(10, 0)));

        return Card(stack, _completionProofs.Count == 0 ? Color.FromArgb("#FFF8F4") : SoftGreen, new Thickness(12));
    }

    private async Task PickCompletionProofsAsync()
    {
        if (_job is null || _isLoading)
        {
            return;
        }

        try
        {
            var files = await FilePicker.Default.PickMultipleAsync(new PickOptions { PickerTitle = "Select completion images or videos" });
            var selected = files.Where(file => file is not null && IsImageOrVideo(file)).Cast<FileResult>().ToArray();
            if (selected.Length == 0)
            {
                Render("Select image or video files for completion proof.");
                return;
            }

            _isLoading = true;
            Render("Uploading completion proof...");
            foreach (var file in selected)
            {
                var uploaded = await RiderApiClient.UploadFileAsync(file, "mechanic-completions");
                var mediaType = IsVideo(file) ? "completion_video" : "completion_image";
                await RiderApiClient.AttachAfterPhotoAsync(
                    _job.RequestId,
                    new UploadMediaDto(uploaded.Url, mediaType, $"Mechanic completion proof: {uploaded.FileName}"));
                _completionProofs.Add(new CompletionUpload(uploaded.FileName, uploaded.Url, IsVideo(file) ? "Video" : "Image"));
            }

            _isLoading = false;
            Render("Completion proof uploaded. Add a comment, then complete this repair.");
        }
        catch (Exception ex)
        {
            _isLoading = false;
            Render($"Completion proof was not uploaded. {ex.Message}");
        }
    }

    private async Task CompleteWithProofAsync()
    {
        if (_job is null)
        {
            return;
        }

        if (_completionProofs.Count == 0)
        {
            await PickCompletionProofsAsync();
            if (_completionProofs.Count == 0)
            {
                return;
            }
        }

        var mechanicComment = (_completionCommentEditor?.Text ?? _completionComment).Trim();
        if (string.IsNullOrWhiteSpace(mechanicComment))
        {
            await DisplayAlertAsync("Mechanic comment required", "Add a short comment about the work completed for this job.", "OK");
            return;
        }

        var confirm = await DisplayAlertAsync(
            "Complete repair",
            "Completion proof and your mechanic comment will be saved. Mark this customer repair as complete?",
            "Complete",
            "Cancel");
        if (!confirm)
        {
            return;
        }

        var proofList = string.Join(", ", _completionProofs.Select(x => x.FileName));
        await UpdateStatusAsync("completed", $"Mechanic completion comment: {mechanicComment}\nProof files: {proofList}");
    }

    private async Task AcceptCurrentAsync()
    {
        if (_job is null)
        {
            return;
        }

        _job = await RiderApiClient.AcceptAsync(_job.RequestId);
        await LoadAsync($"Request #{_job.RequestId} was accepted. Next step: travel to the customer.");
    }

    private static async Task ShareJobLocationIfAvailableAsync(int requestId)
    {
        var area = await ResolveAreaAsync();
        if (area is not null)
        {
            await RiderApiClient.UpdateLocationAsync(requestId, area.Value.Latitude, area.Value.Longitude, area.Value.Accuracy);
        }
    }

    private static bool IsImageOrVideo(FileResult file)
    {
        return IsImage(file) || IsVideo(file);
    }

    private static bool IsImage(FileResult file)
    {
        var contentType = file.ContentType ?? string.Empty;
        var extension = System.IO.Path.GetExtension(file.FileName);
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVideo(FileResult file)
    {
        var contentType = file.ContentType ?? string.Empty;
        var extension = System.IO.Path.GetExtension(file.FileName);
        return contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".mov", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".m4v", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".webm", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record CompletionUpload(string FileName, string Url, string Kind);
}

public sealed class MechanicMapPage : MechanicPageBase, IQueryAttributable
{
    private int _requestId;
    private ServiceRequestDto? _job;
    private bool _isLoading;
    private string? _banner;

    public MechanicMapPage()
    {
        Title = "Job Location";
        Render("Loading map...");
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
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing map...");
        try
        {
            _job = _requestId > 0
                ? await RiderApiClient.GetJobAsync(_requestId)
                : await RiderApiClient.GetCurrentJobAsync();
            _requestId = _job?.RequestId ?? _requestId;
            _banner = banner;
        }
        catch (Exception ex)
        {
            _job = null;
            _banner = $"Could not load job location. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));

        if (_job?.ServiceLatitude is null || _job.ServiceLongitude is null)
        {
            body.Add(Card(Text("This job has no saved coordinates yet. Ask the customer to use current location during booking.", 12, Muted)));
            SetPage("Job Location", "Live map for the selected request.", body, _isLoading, async () => await LoadAsync(), true);
            return;
        }

        body.Add(Card(new Grid
        {
            HeightRequest = 360,
            Children =
            {
                new WebView { Source = MapSource(_job.ServiceLatitude.Value, _job.ServiceLongitude.Value), HeightRequest = 360 }
            }
        }, Colors.White, new Thickness(0)));
        body.Add(Card(Text(_job.ServiceLocationAddress ?? "Customer location", 12, Dark)));
        body.Add(PrimaryButton("Open in Google Maps", async () =>
        {
            var query = Uri.EscapeDataString($"{_job.ServiceLatitude.Value.ToString(CultureInfo.InvariantCulture)},{_job.ServiceLongitude.Value.ToString(CultureInfo.InvariantCulture)}");
            await Launcher.Default.OpenAsync(new Uri($"https://www.google.com/maps/search/?api=1&query={query}"));
        }));

        SetPage("Job Location", "Customer request location from live booking data.", body, _isLoading, async () => await LoadAsync(), true);
    }
}

public sealed class MechanicMessagesPage : MechanicPageBase
{
    private IReadOnlyList<ConversationSummaryDto> _conversations = [];
    private bool _isLoading;
    private string? _banner;

    public MechanicMessagesPage()
    {
        Title = "Messages";
        Render("Loading conversations...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing messages...");
        try
        {
            _conversations = await RiderApiClient.GetConversationsAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _conversations = [];
            _banner = $"Could not load messages. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));

        if (_conversations.Count == 0 && !_isLoading)
        {
            body.Add(EmptyState("No conversations", "Customer chats will appear after a request is connected."));
        }
        else
        {
            foreach (var conversation in _conversations
                         .Where(x => x.ConversationId > 0)
                         .OrderByDescending(x => x.LastMessageAt ?? DateTime.MinValue))
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

                row.Add(ProfileAvatar(ConversationTitle(conversation), conversation.OtherProfileImageUrl, ConversationAccent(conversation)), 0, 0);

                var stack = new VerticalStackLayout { Spacing = 4 };
                var titleRow = new HorizontalStackLayout { Spacing = 6 };
                titleRow.Add(Text(ConversationTitle(conversation), 14, Dark, FontAttributes.Bold));
                titleRow.Add(SmallBadge(ConversationKind(conversation), ConversationAccent(conversation)));
                stack.Add(titleRow);
                stack.Add(Text(
                    conversation.RequestId is null
                        ? conversation.Subtitle ?? "BikeMate chat"
                        : $"BM-{conversation.RequestId:000000} - {FormatStatus(conversation.BookingStatus ?? "pending")}",
                    10,
                    Muted));
                var preview = Text(Preview(conversation.LastMessageText ?? conversation.Subtitle ?? "Tap to open conversation"), 11, Dark);
                preview.MaxLines = 2;
                preview.LineBreakMode = LineBreakMode.TailTruncation;
                stack.Add(preview);
                row.Add(stack, 1, 0);

                var meta = new VerticalStackLayout { Spacing = 7, HorizontalOptions = LayoutOptions.End };
                meta.Add(Text(FriendlyTime(conversation.LastMessageAt), 9, Muted));
                if (conversation.UnreadCount > 0)
                {
                    meta.Add(new Border
                    {
                        BackgroundColor = Orange,
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
                            FontSize = AppTypography.CaptionSize,
                            FontAttributes = FontAttributes.Bold,
                            HorizontalTextAlignment = TextAlignment.Center
                        }
                    });
                }
                row.Add(meta, 2, 0);

                var card = Card(row);
                card.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () => await OpenConversationAsync(conversation.ConversationId))
                });
                body.Add(card);
            }
        }

        SetPage("Messages", "Job and customer conversations.", body, _isLoading, async () => await LoadAsync());
    }

    private async Task OpenConversationAsync(int conversationId)
    {
        if (conversationId <= 0)
        {
            await DisplayAlertAsync("Message unavailable", "This conversation is missing its ID. Refresh messages and try again.", "OK");
            return;
        }

        try
        {
            await Shell.Current.Navigation.PushAsync(new MechanicChatPage(conversationId));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Message unavailable", ex.Message, "OK");
        }
    }

    private static string ConversationTitle(ConversationSummaryDto conversation)
    {
        return string.IsNullOrWhiteSpace(conversation.Title)
            ? $"Conversation #{conversation.ConversationId}"
            : conversation.Title;
    }

    private static string ConversationKind(ConversationSummaryDto conversation)
    {
        return conversation.ConversationType switch
        {
            "emergency_support" or "emergency_request" => "Emergency",
            "booking_shop" => "Shop",
            "booking_mechanic" => "Customer",
            _ => "Booking"
        };
    }

    private static Color ConversationAccent(ConversationSummaryDto conversation)
    {
        return conversation.ConversationType switch
        {
            "emergency_support" or "emergency_request" => SoftRed,
            "booking_shop" => SoftBlue,
            "booking_mechanic" => SoftOrange,
            _ => Color.FromArgb("#EEF1F4")
        };
    }

    private static View SmallBadge(string text, Color background)
    {
        return new Border
        {
            BackgroundColor = background,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(7, 2),
            Content = Text(text, 9, Dark, FontAttributes.Bold)
        };
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

public sealed class MechanicChatPage : MechanicPageBase, IQueryAttributable
{
    private int _conversationId;
    private int? _currentUserId;
    private ConversationSummaryDto? _conversation;
    private IReadOnlyList<MessageDto> _messages = [];
    private Entry? _messageEntry;
    private string _draftMessage = string.Empty;
    private bool _isSending;

    public MechanicChatPage()
    {
        Title = "Chat";
        Render("Loading chat...");
    }

    public MechanicChatPage(int conversationId)
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

    private async Task LoadAsync(string? banner = null)
    {
        if (_conversationId <= 0)
        {
            Render("Open a conversation from Messages first.");
            return;
        }

        try
        {
            _currentUserId = await CurrentUserIdAsync();
            try
            {
                _conversation = await RiderApiClient.GetConversationAsync(_conversationId);
            }
            catch
            {
                var conversations = await RiderApiClient.GetConversationsAsync();
                _conversation = conversations.FirstOrDefault(x => x.ConversationId == _conversationId);
            }

            _messages = await RiderApiClient.GetMessagesAsync(_conversationId);
            await RiderApiClient.MarkConversationReadAsync(_conversationId);
            Render(banner);
        }
        catch (Exception ex)
        {
            Render($"Could not load chat. {ex.Message}");
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
        root.Add(ChatHeader(), 0, 0);
        root.Add(new ScrollView { Content = MessageList(banner) }, 0, 1);
        root.Add(Composer(), 0, 2);
        Content = root;
    }

    private View ChatHeader()
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
            TextColor = Dark,
            WidthRequest = 44,
            HeightRequest = 40,
            Command = new Command(async () => await Shell.Current.GoToAsync(".."))
        }, 0, 0);

        grid.Add(ProfileAvatar(_conversation?.Title ?? "Conversation", _conversation?.OtherProfileImageUrl, SoftOrange, 42), 1, 0);

        var title = new VerticalStackLayout { Spacing = 2, Margin = new Thickness(8, 0, 0, 0) };
        title.Add(Text(string.IsNullOrWhiteSpace(_conversation?.Title) ? "Conversation" : _conversation.Title, 14, Dark, FontAttributes.Bold));
        title.Add(Text(_conversation?.RequestId is null ? "BikeMate chat" : $"BM-{_conversation.RequestId:000000} | {FormatStatus(_conversation.BookingStatus ?? "pending")}", 11, Muted));
        grid.Add(title, 2, 0);
        grid.Add(CompactButton("Job", async () =>
        {
            if (_conversation?.RequestId is not null)
            {
                await Shell.Current.GoToAsync($"{nameof(MechanicJobDetailsPage)}?requestId={_conversation.RequestId}");
            }
        }), 3, 0);

        return new Border { Stroke = BorderColor, StrokeThickness = 1, BackgroundColor = Colors.White, Content = grid };
    }

    private View MessageList(string? banner)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 10, BackgroundColor = PageColor };
        body.Add(Banner(banner));
        if (_conversation?.RequestId is not null)
        {
            body.Add(Card(new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    Text($"Booking BM-{_conversation.RequestId:000000}", 13, Dark, FontAttributes.Bold),
                    Text(_conversation.Subtitle ?? "Booking conversation", 11, Muted)
                }
            }, SoftOrange));
        }

        if (_messages.Count == 0)
        {
            body.Add(Card(Text("No messages yet. Send a message to begin the conversation.", 12, Muted)));
            return body;
        }

        foreach (var message in _messages.OrderBy(x => x.CreatedAt))
        {
            body.Add(Bubble(message, _currentUserId is not null && message.SenderUserId == _currentUserId.Value));
        }

        return body;
    }

    private static View Bubble(MessageDto message, bool mine)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        if (!string.IsNullOrWhiteSpace(message.MessageText))
        {
            stack.Add(Text(message.MessageText, 12, mine ? Colors.White : Dark));
        }

        if (!string.IsNullOrWhiteSpace(message.AttachmentUrl))
        {
            stack.Add(AttachmentPreview(message.AttachmentUrl));
        }

        stack.Add(Text(message.CreatedAt.ToLocalTime().ToString("h:mm tt", CultureInfo.InvariantCulture), 9, mine ? Color.FromArgb("#FFF0E9") : Muted));
        var bubble = Card(stack, mine ? Orange : Colors.White, new Thickness(12, 9));
        bubble.HorizontalOptions = mine ? LayoutOptions.End : LayoutOptions.Start;
        bubble.MaximumWidthRequest = 310;
        return bubble;
    }

    private View Composer()
    {
        _messageEntry = new Entry
        {
            Text = _draftMessage,
            Placeholder = "Write a message",
            BackgroundColor = Colors.Transparent,
            TextColor = Dark,
            PlaceholderColor = Muted
        };
        _messageEntry.TextChanged += (_, e) => _draftMessage = e.NewTextValue ?? string.Empty;

        var root = new VerticalStackLayout { Padding = new Thickness(14, 8, 14, 12), Spacing = 8, BackgroundColor = Colors.White };
        root.Add(new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                CompactButton("Attach file", async () => await PickAndSendAttachmentAsync(false)),
                CompactButton("Add photo", async () => await PickAndSendAttachmentAsync(true))
            }
        });

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        grid.Add(Card(_messageEntry, Color.FromArgb("#F8F8F8"), new Thickness(10, 0)), 0, 0);
        grid.Add(PrimaryButton(_isSending ? "Sending" : "Send", SendAsync), 1, 0);
        root.Add(grid);
        return new Border { Stroke = BorderColor, StrokeThickness = 1, BackgroundColor = Colors.White, Content = root };
    }

    private async Task SendAsync()
    {
        var text = (_messageEntry?.Text ?? _draftMessage).Trim();
        if (_conversationId <= 0 || string.IsNullOrWhiteSpace(text) || _isSending)
        {
            return;
        }

        try
        {
            _isSending = true;
            Render();
            await RiderApiClient.SendMessageAsync(_conversationId, text);
            _draftMessage = string.Empty;
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
            if (!string.IsNullOrWhiteSpace(_draftMessage))
            {
                Render();
            }
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
            var file = await FilePicker.Default.PickAsync(imageOnly ? PickOptions.Images : new PickOptions { PickerTitle = "Select an attachment" });
            if (file is null)
            {
                return;
            }

            _isSending = true;
            Render("Uploading attachment...");
            var uploaded = await RiderApiClient.UploadFileAsync(file, "mechanic-chat");
            var text = string.IsNullOrWhiteSpace(_draftMessage) ? uploaded.FileName : _draftMessage.Trim();
            await RiderApiClient.SendMessageAsync(_conversationId, text, uploaded.Url);
            _draftMessage = string.Empty;
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

    private static async Task<int?> CurrentUserIdAsync()
    {
        var raw = await SecureStorage.Default.GetAsync("user_id");
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
        {
            return userId;
        }

        var currentUser = await RiderApiClient.GetCurrentUserAsync();
        await SecureStorage.Default.SetAsync("user_id", currentUser.UserId.ToString(CultureInfo.InvariantCulture));
        return currentUser.UserId;
    }
}

public sealed class MechanicHistoryPage : MechanicPageBase
{
    private IReadOnlyList<ServiceRequestDto> _jobs = [];
    private bool _isLoading;
    private string? _banner;

    public MechanicHistoryPage()
    {
        Title = "History";
        Render("Loading history...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing completed jobs...");
        try
        {
            _jobs = await RiderApiClient.GetHistoryAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _jobs = [];
            _banner = $"Could not load history. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));
        if (_jobs.Count > 0)
        {
            var completed = _jobs.Count(x => x.CurrentStatus == "completed");
            var ended = _jobs.Count(x => x.CurrentStatus is "cancelled" or "rejected");
            var earned = _jobs
                .Where(x => x.CurrentStatus == "completed")
                .Sum(x => x.FinalTotal > 0 ? x.FinalTotal : x.EstimatedTotal);
            var stats = new Grid { ColumnSpacing = 8 };
            stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            stats.Add(Stat("Completed", completed.ToString(CultureInfo.InvariantCulture), SoftGreen), 0, 0);
            stats.Add(Stat("Ended", ended.ToString(CultureInfo.InvariantCulture), SoftOrange), 1, 0);
            stats.Add(Stat("Value", Money(earned), SoftBlue), 2, 0);
            body.Add(stats);
        }

        if (_jobs.Count == 0 && !_isLoading)
        {
            body.Add(Card(Text("Completed, cancelled, and rejected jobs will appear here.", 12, Muted)));
        }
        else
        {
            foreach (var job in _jobs)
            {
                body.Add(HistoryCard(job));
            }
        }

        SetPage("History", "Past mechanic jobs and outcomes.", body, _isLoading, async () => await LoadAsync());
    }

    private static View HistoryCard(ServiceRequestDto job)
    {
        var total = job.FinalTotal > 0 ? job.FinalTotal : job.EstimatedTotal;
        var stack = new VerticalStackLayout { Spacing = 8 };
        var top = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        top.Add(Text(string.IsNullOrWhiteSpace(job.ServiceName) ? $"BM-{job.RequestId:000000}" : job.ServiceName!, 14, Dark, FontAttributes.Bold), 0, 0);
        top.Add(StatusPill(FormatStatus(job.CurrentStatus), StatusColor(job.CurrentStatus)), 1, 0);
        stack.Add(top);
        stack.Add(DetailRow("Booking", $"BM-{job.RequestId:000000}"));
        stack.Add(DetailRow("Customer", job.CustomerName));
        stack.Add(DetailRow("Distance from you", DistanceFromYou(job)));
        stack.Add(DetailRow("Schedule", job.ScheduledAt?.ToLocalTime().ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture) ?? "ASAP"));
        stack.Add(DetailRow("Total", total > 0 ? Money(total) : "No charge"));
        stack.Add(SecondaryButton(
            "View details",
            async () => await Shell.Current.GoToAsync($"{nameof(MechanicHistoryDetailsPage)}?requestId={job.RequestId}")));
        return Card(stack);
    }
}

public sealed class MechanicHistoryDetailsPage : MechanicPageBase, IQueryAttributable
{
    private int _requestId;
    private ServiceRequestDto? _job;
    private bool _isLoading;
    private string? _banner;

    public MechanicHistoryDetailsPage()
    {
        Title = "History Details";
        Render("Loading history details...");
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
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing history details...");
        try
        {
            _job = _requestId > 0 ? await RiderApiClient.GetJobAsync(_requestId) : null;
            _banner = banner;
        }
        catch (Exception ex)
        {
            _job = null;
            _banner = $"Could not load this history record. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));

        if (_job is null)
        {
            body.Add(Card(Text(_isLoading ? "Loading history details..." : "This history record is unavailable.", 12, Muted)));
            SetPage("History Details", "Full record for this mechanic job.", body, _isLoading, async () => await LoadAsync(), true);
            return;
        }

        body.Add(HistoryHero(_job));
        body.Add(OutcomeCard(_job));
        body.Add(ServiceCard(_job));
        body.Add(LocationCard(_job));
        body.Add(CustomerCard(_job));
        body.Add(FinancialCard(_job));
        body.Add(IssueCard(_job));
        body.Add(SecondaryButton("Back to history", async () => await Shell.Current.GoToAsync("..")));

        SetPage("History Details", "Full record for this mechanic job.", body, _isLoading, async () => await LoadAsync(), true);
    }

    private static View HistoryHero(ServiceRequestDto job)
    {
        var title = string.IsNullOrWhiteSpace(job.ServiceName) ? $"BM-{job.RequestId:000000}" : job.ServiceName!;
        var stack = new VerticalStackLayout { Spacing = 8 };
        var top = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        top.Add(Text(title, 17, Dark, FontAttributes.Bold), 0, 0);
        top.Add(StatusPill(FormatStatus(job.CurrentStatus), StatusColor(job.CurrentStatus)), 1, 0);
        stack.Add(top);
        stack.Add(Text($"Booking BM-{job.RequestId:000000}", 11, Muted, FontAttributes.Bold));
        stack.Add(Text(HistorySummary(job), 11, Orange, FontAttributes.Bold));
        return Card(stack, Colors.White);
    }

    private static View OutcomeCard(ServiceRequestDto job)
    {
        var rows = SectionStack("Outcome");
        rows.Add(DetailRow("Status", FormatStatus(job.CurrentStatus)));
        rows.Add(DetailRow("Created", FormatDateTime(job.CreatedAt)));
        rows.Add(DetailRow("Ended", job.CompletedAt is null ? EndedFallback(job) : FormatDateTime(job.CompletedAt.Value)));
        rows.Add(DetailRow("Mechanic", job.MechanicName ?? "Assigned mechanic"));
        rows.Add(DetailRow("Shop", job.ShopName ?? "BikeMate partner"));
        return Card(rows, Colors.White);
    }

    private static View ServiceCard(ServiceRequestDto job)
    {
        var rows = SectionStack("Service");
        rows.Add(DetailRow("Service", string.IsNullOrWhiteSpace(job.ServiceName) ? "Service not specified" : job.ServiceName!));
        rows.Add(DetailRow("Schedule", job.ScheduledAt is null ? "ASAP" : FormatDateTime(job.ScheduledAt.Value)));
        rows.Add(DetailRow("Distance from you", DistanceFromYou(job)));
        return Card(rows, Colors.White);
    }

    private static View LocationCard(ServiceRequestDto job)
    {
        var rows = SectionStack("Location");
        rows.Add(DetailRow("Address", string.IsNullOrWhiteSpace(job.ServiceLocationAddress) ? "No address provided" : job.ServiceLocationAddress!));
        rows.Add(DetailRow("Coordinates", Coordinates(job)));
        if (job.ServiceLatitude is not null && job.ServiceLongitude is not null)
        {
            rows.Add(SecondaryButton(
                "Open location map",
                async () => await Shell.Current.GoToAsync($"{nameof(MechanicMapPage)}?requestId={job.RequestId}")));
        }

        return Card(rows, Colors.White);
    }

    private static View CustomerCard(ServiceRequestDto job)
    {
        var rows = SectionStack("Customer");
        rows.Add(DetailRow("Name", job.CustomerName));
        rows.Add(DetailRow("Booking", $"BM-{job.RequestId:000000}"));
        return Card(rows, Colors.White);
    }

    private static View FinancialCard(ServiceRequestDto job)
    {
        var rows = SectionStack("Payment");
        rows.Add(DetailRow("Estimated total", job.EstimatedTotal > 0 ? Money(job.EstimatedTotal) : "Not set"));
        rows.Add(DetailRow("Final total", job.FinalTotal > 0 ? Money(job.FinalTotal) : "Not finalized"));
        rows.Add(DetailRow("History value", HistoryValue(job)));
        return Card(rows, Colors.White);
    }

    private static View IssueCard(ServiceRequestDto job)
    {
        var stack = SectionStack("Customer concern");
        stack.Add(Text(CleanIssue(job.IssueDescription), 12, Dark));
        return Card(stack, Colors.White);
    }

    private static VerticalStackLayout SectionStack(string title)
    {
        var rows = new VerticalStackLayout { Spacing = 10 };
        rows.Add(Text(title, 13, Dark, FontAttributes.Bold));
        return rows;
    }

    private static string HistorySummary(ServiceRequestDto job)
    {
        var ended = job.CompletedAt is null ? EndedFallback(job) : FormatDateTime(job.CompletedAt.Value);
        return $"{FormatStatus(job.CurrentStatus)} - {ended} - {HistoryValue(job)}";
    }

    private static string HistoryValue(ServiceRequestDto job)
    {
        var total = job.FinalTotal > 0 ? job.FinalTotal : job.EstimatedTotal;
        return total > 0 ? Money(total) : "No charge";
    }

    private static string EndedFallback(ServiceRequestDto job)
    {
        return job.CurrentStatus is "completed" or "cancelled" or "rejected"
            ? FormatDateTime(job.CreatedAt)
            : "Not ended";
    }

    private static string FormatDateTime(DateTime value)
    {
        return value.ToLocalTime().ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);
    }

    private static string Coordinates(ServiceRequestDto job)
    {
        return job.ServiceLatitude is null || job.ServiceLongitude is null
            ? "Not available"
            : $"{job.ServiceLatitude:0.######}, {job.ServiceLongitude:0.######}";
    }
}

public sealed class MechanicProfilePage : MechanicPageBase
{
    private RiderDashboardDto? _dashboard;
    private bool _isLoading;
    private string? _banner;

    public MechanicProfilePage()
    {
        Title = "Profile";
        Render("Loading profile...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing profile...");
        try
        {
            _dashboard = await RiderApiClient.GetDashboardAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _dashboard = null;
            _banner = $"Could not load profile. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));
        if (_dashboard is null)
        {
            body.Add(Card(Text("No profile data yet.", 12, Muted)));
            SetPage("Profile", "Mechanic verification and performance.", body, _isLoading, async () => await LoadAsync());
            return;
        }

        var profile = _dashboard.Profile;
        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };
        header.Add(ProfileAvatar(profile.FullName, profile.ProfileImageUrl, Colors.White, 64), 0, 0);
        header.Add(new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                Text(profile.FullName, 20, Dark, FontAttributes.Bold),
                Text(MechanicApprovalLabel(profile), 12, Orange, FontAttributes.Bold),
                Text(StatusSummary(profile), 12, Muted)
            }
        }, 1, 0);
        body.Add(Card(header, SoftOrange));

        var stats = new Grid { ColumnSpacing = 8 };
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.Add(Stat("Rating", profile.AverageRating.ToString("0.0", CultureInfo.InvariantCulture), SoftBlue), 0, 0);
        stats.Add(Stat("Completed", profile.TotalCompletedJobs.ToString(CultureInfo.InvariantCulture), SoftGreen), 1, 0);
        body.Add(stats);

        var details = new VerticalStackLayout { Spacing = 10 };
        details.Add(Text("Mechanic profile", 13, Dark, FontAttributes.Bold));
        if (!string.IsNullOrWhiteSpace(profile.Email))
        {
            details.Add(DetailRow("Email", profile.Email!));
        }
        if (!string.IsNullOrWhiteSpace(profile.PhoneNumber))
        {
            details.Add(DetailRow("Mobile number", profile.PhoneNumber!));
        }
        details.Add(DetailRow("Availability", FormatStatus(profile.AvailabilityStatus)));
        details.Add(DetailRow("Experience", $"{profile.YearsExperience ?? 0} year(s)"));
        details.Add(DetailRow("Account status", FormatStatus(profile.AccountStatus ?? (profile.IsVerified ? "active" : "pending"))));
        details.Add(DetailRow("Verification", profile.IsVerified ? "Approved" : "Pending review"));
        details.Add(DetailRow("Service area", MechanicAddress(profile)));
        details.Add(Text(string.IsNullOrWhiteSpace(profile.Bio) ? "Add a bio, specialties, certifications, and service area so customers know what you can repair." : profile.Bio!, 11, Muted));
        body.Add(Card(details));

        var documents = new VerticalStackLayout { Spacing = 8 };
        documents.Add(Text("Submitted documents", 13, Dark, FontAttributes.Bold));
        documents.Add(DetailRow("Profile photo", string.IsNullOrWhiteSpace(profile.ProfileImageUrl) ? "Not submitted" : "Submitted"));
        documents.Add(DetailRow("Valid ID", string.IsNullOrWhiteSpace(profile.ValidIdImageUrl) ? "Not submitted" : "Submitted"));
        documents.Add(DetailRow("License or certificate", string.IsNullOrWhiteSpace(profile.CertificationImageUrl) ? "Not submitted" : "Submitted"));
        documents.Add(Text("Documents are view-only after submission. Contact BikeMate admin if something needs to be corrected.", 11, Muted));
        body.Add(Card(documents));

        var readiness = new VerticalStackLayout { Spacing = 8 };
        readiness.Add(Text("Service readiness", 13, Dark, FontAttributes.Bold));
        readiness.Add(Text(profile.IsVerified ? "Your account can receive mechanic jobs." : "Verification is still pending. You can prepare your profile while waiting.", 11, Muted));
        readiness.Add(Text(_dashboard.ActiveJob is null ? "No active job assigned." : $"Active job: BM-{_dashboard.ActiveJob.RequestId:000000}", 11, Orange, FontAttributes.Bold));
        body.Add(Card(readiness, Colors.White));

        body.Add(SectionTitle("Profile actions", "Update public details and review mechanic performance."));
        body.Add(PrimaryButton("Edit profile", async () => await Shell.Current.GoToAsync(nameof(MechanicEditProfilePage))));

        var actions = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        actions.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actions.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actions.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        actions.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        actions.Add(SecondaryButton("Use my area", async () => await UseMyAreaAsync(LoadAsync)), 0, 0);
        actions.Add(SecondaryButton("Earnings", async () => await Shell.Current.GoToAsync(nameof(MechanicEarningsPage))), 1, 0);
        actions.Add(SecondaryButton("Ratings", async () => await Shell.Current.GoToAsync(nameof(MechanicRatingsPage))), 0, 1);
        actions.Add(SecondaryButton("History", async () => await Shell.Current.GoToAsync(nameof(MechanicHistoryPage))), 1, 1);
        body.Add(actions);

        SetPage("Profile", "Mechanic verification, rating, and matching area.", body, _isLoading, async () => await LoadAsync());
    }

    private static string StatusSummary(MechanicProfileDto profile)
    {
        var experience = profile.YearsExperience is null ? "New mechanic" : $"{profile.YearsExperience} year(s) experience";
        return $"{FormatStatus(profile.AvailabilityStatus)} - {experience}";
    }

    private static string MechanicApprovalLabel(MechanicProfileDto profile)
    {
        if (profile.IsVerified && string.Equals(profile.AccountStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            return "Approved mechanic";
        }

        return profile.EmailVerified == false
            ? "Email verification required"
            : "Pending BikeMate admin review";
    }

    private static string MechanicAddress(MechanicProfileDto profile)
    {
        var parts = new[]
        {
            profile.AddressLine,
            profile.Barangay,
            profile.City,
            profile.Province,
            profile.ZipCode
        }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).ToArray();

        return parts.Length == 0 ? "Not submitted" : string.Join(", ", parts);
    }
}

public sealed class MechanicLogoutPage : ContentPage
{
    private bool _isPrompting;

    public MechanicLogoutPage()
    {
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Color.FromArgb("#F6F6F6");
        Content = new Grid
        {
            Padding = new Thickness(20),
            Children =
            {
                new ActivityIndicator
                {
                    IsRunning = true,
                    Color = Color.FromArgb("#FF6B2C"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isPrompting)
        {
            return;
        }

        _isPrompting = true;
        try
        {
            var signedOut = await AppNavigation.ConfirmAndSignOutAsync(this);
            if (!signedOut && Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("//MechanicProfilePage");
            }
        }
        finally
        {
            _isPrompting = false;
        }
    }
}

public sealed class MechanicEditProfilePage : MechanicPageBase
{
    private RiderDashboardDto? _dashboard;
    private Editor? _bio;
    private Entry? _phone;
    private Entry? _years;
    private Picker? _status;
    private bool _isLoading;
    private string? _banner;

    public MechanicEditProfilePage()
    {
        Title = "Edit Profile";
        Render("Loading profile...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing profile...");
        try
        {
            _dashboard = await RiderApiClient.GetDashboardAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _banner = $"Could not load profile. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var profile = _dashboard?.Profile;
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));
        body.Add(ProfileSummaryCard(profile));
        if (profile is not null)
        {
            body.Add(SubmittedDetailsCard(profile));
            body.Add(SubmittedDocumentsCard(profile));
        }

        _bio = new Editor
        {
            Text = profile?.Bio,
            Placeholder = "Bio, specialization, certifications, service area",
            AutoSize = EditorAutoSizeOption.TextChanges,
            MinimumHeightRequest = 120,
            TextColor = Dark,
            PlaceholderColor = Muted
        };
        _phone = new Entry
        {
            Text = ToPhilippineMobileDisplay(profile?.PhoneNumber),
            Placeholder = "+63 mobile number",
            Keyboard = Keyboard.Telephone,
            MaxLength = 13,
            TextColor = Dark,
            PlaceholderColor = Muted
        };
        ConfigurePhilippineMobileEntry(_phone);
        _years = new Entry
        {
            Text = profile?.YearsExperience?.ToString(CultureInfo.InvariantCulture),
            Placeholder = "Years of experience",
            Keyboard = Keyboard.Numeric,
            TextColor = Dark,
            PlaceholderColor = Muted
        };
        _status = new Picker { Title = "Availability status", TextColor = Dark };
        foreach (var item in new[] { "online", "busy", "offline" })
        {
            _status.Items.Add(item);
        }
        _status.SelectedItem = string.IsNullOrWhiteSpace(profile?.AvailabilityStatus) ? "offline" : profile.AvailabilityStatus;

        body.Add(SectionTitle("Editable public details", "These are the fields mechanics can update from the mobile app."));
        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Text("Bio", 12, Dark, FontAttributes.Bold),
                _bio,
                Text("Mobile number", 12, Dark, FontAttributes.Bold),
                _phone,
                Text("Experience", 12, Dark, FontAttributes.Bold),
                _years,
                Text("Availability", 12, Dark, FontAttributes.Bold),
                _status
            }
        }));

        body.Add(PrimaryButton("Save profile", SaveAsync, !_isLoading && profile is not null));
        body.Add(SecondaryButton("Back to profile", async () => await Shell.Current.GoToAsync("..")));
        SetPage("Edit Profile", "Update the details customers and shops see.", body, _isLoading, async () => await LoadAsync(), true);
    }

    private static View ProfileSummaryCard(MechanicProfileDto? profile)
    {
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };

        row.Add(ProfileAvatar(profile?.FullName ?? "Mechanic profile", profile?.ProfileImageUrl, Colors.White, 60), 0, 0);
        row.Add(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                Text(profile?.FullName ?? "Mechanic profile", 18, Dark, FontAttributes.Bold),
                Text(profile is null ? "Loading submitted profile details..." : MechanicApprovalLabel(profile), 11, Orange, FontAttributes.Bold),
                Text(profile is null ? "Please wait while BikeMate loads your mechanic account." : "Submitted shop-admin details are shown below for review.", 11, Muted)
            }
        }, 1, 0);

        return Card(row, SoftOrange);
    }

    private static View SubmittedDetailsCard(MechanicProfileDto profile)
    {
        var stack = new VerticalStackLayout { Spacing = 9 };
        stack.Add(Text("Submitted account details", 13, Dark, FontAttributes.Bold));
        stack.Add(Text("These details came from the Shop Admin mechanic account creation flow. Contact BikeMate admin or your shop admin if corrections are needed.", 11, Muted));
        stack.Add(DetailRow("First name", Display(profile.FirstName)));
        stack.Add(DetailRow("Middle name", Display(profile.MiddleName)));
        stack.Add(DetailRow("Last name", Display(profile.LastName)));
        stack.Add(DetailRow("Sex", Display(profile.Sex)));
        stack.Add(DetailRow("Birthdate", FormatDate(profile.Birthdate)));
        stack.Add(DetailRow("Email", Display(profile.Email)));
        stack.Add(DetailRow("Mobile number", Display(profile.PhoneNumber)));
        stack.Add(DetailRow("Account status", FormatStatus(profile.AccountStatus ?? (profile.IsVerified ? "active" : "pending"))));
        stack.Add(DetailRow("Email verification", profile.EmailVerified == true ? "Verified" : "Pending"));
        stack.Add(DetailRow("Service address", MechanicAddress(profile)));
        return Card(stack);
    }

    private static View SubmittedDocumentsCard(MechanicProfileDto profile)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Add(Text("Submitted photos and documents", 13, Dark, FontAttributes.Bold));
        stack.Add(Text("These files are view-only here so the mechanic can double-check what was submitted.", 11, Muted));
        stack.Add(DocumentPreview("Profile photo", profile.ProfileImageUrl));
        stack.Add(DocumentPreview("Valid ID", profile.ValidIdImageUrl));
        stack.Add(DocumentPreview("License or certification", profile.CertificationImageUrl));
        return Card(stack);
    }

    private static View DocumentPreview(string label, string? imageUrl)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Add(DetailRow(label, string.IsNullOrWhiteSpace(imageUrl) ? "Not submitted" : "Submitted"));
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            stack.Add(AttachmentPreview(imageUrl));
        }

        return stack;
    }

    private static string Display(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Not submitted" : value.Trim();
    }

    private static string FormatDate(DateTime? value)
    {
        return value is null
            ? "Not submitted"
            : value.Value.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
    }

    private static string MechanicApprovalLabel(MechanicProfileDto profile)
    {
        if (profile.IsVerified && string.Equals(profile.AccountStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            return "Approved mechanic";
        }

        return profile.EmailVerified == false
            ? "Email verification required"
            : "Pending BikeMate admin review";
    }

    private static string MechanicAddress(MechanicProfileDto profile)
    {
        var parts = new[]
        {
            profile.AddressLine,
            profile.Barangay,
            profile.City,
            profile.Province,
            profile.ZipCode
        }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).ToArray();

        return parts.Length == 0 ? "Not submitted" : string.Join(", ", parts);
    }

    private async Task SaveAsync()
    {
        var current = _dashboard?.Profile;
        if (current is null)
        {
            return;
        }

        int? years = null;
        if (!string.IsNullOrWhiteSpace(_years?.Text) &&
            int.TryParse(_years.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedYears))
        {
            years = Math.Max(0, parsedYears);
        }

        var dto = new UpdateMechanicProfileDto(
            _bio?.Text?.Trim(),
            years,
            _status?.SelectedItem?.ToString() ?? current.AvailabilityStatus,
            null,
            null,
            MobileNumberForSave(_phone?.Text));
        await RiderApiClient.UpdateProfileAsync(dto);
        await LoadAsync("Profile changes saved.");
    }

    private static void ConfigurePhilippineMobileEntry(Entry entry)
    {
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
            var normalized = ToPhilippineMobileDisplay(args.NewTextValue);
            if (!string.Equals(args.NewTextValue, normalized, StringComparison.Ordinal))
            {
                entry.Text = normalized;
                try
                {
                    entry.CursorPosition = normalized.Length;
                }
                catch (ArgumentOutOfRangeException)
                {
                    entry.CursorPosition = Math.Max(0, entry.Text?.Length ?? 0);
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
}

public sealed class MechanicEarningsPage : MechanicPageBase
{
    private RiderEarningsDto? _earnings;
    private bool _isLoading;
    private string? _banner;

    public MechanicEarningsPage()
    {
        Title = "Earnings";
        Render("Loading earnings...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing earnings...");
        try
        {
            _earnings = await RiderApiClient.GetEarningsAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _banner = $"Could not load earnings. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));
        body.Add(Stat("Total paid earnings", Money(_earnings?.Total ?? 0m), SoftGreen));
        if (_earnings?.Payments.Count > 0)
        {
            foreach (var payment in _earnings.Payments.OrderByDescending(x => x.PaidAt ?? x.CreatedAt))
            {
                body.Add(Card(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children =
                    {
                        Text($"BM-{payment.RequestId:000000}", 14, Dark, FontAttributes.Bold),
                        DetailRow("Amount", Money(payment.Amount)),
                        DetailRow("Status", FormatStatus(payment.Status)),
                        DetailRow("Paid", payment.PaidAt?.ToLocalTime().ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture) ?? "Pending")
                    }
                }));
            }
        }
        else if (!_isLoading)
        {
            body.Add(Card(Text("Paid jobs will appear here after customer checkout is confirmed.", 12, Muted)));
        }

        SetPage("Earnings", "Paid service jobs assigned to you.", body, _isLoading, async () => await LoadAsync(), true);
    }
}

public sealed class MechanicRatingsPage : MechanicPageBase
{
    private IReadOnlyList<ReviewDto> _reviews = [];
    private bool _isLoading;
    private string? _banner;

    public MechanicRatingsPage()
    {
        Title = "Ratings";
        Render("Loading ratings...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing ratings...");
        try
        {
            _reviews = await RiderApiClient.GetRatingsAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _reviews = [];
            _banner = $"Could not load ratings. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));
        var average = _reviews.Count == 0 ? 0m : _reviews.Average(x => (decimal)x.Rating);
        body.Add(Stat("Average from customer reviews", average.ToString("0.0", CultureInfo.InvariantCulture), SoftBlue));
        if (_reviews.Count == 0 && !_isLoading)
        {
            body.Add(Card(Text("Customer reviews will appear after completed jobs are rated.", 12, Muted)));
        }
        else
        {
            foreach (var review in _reviews)
            {
                body.Add(Card(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children =
                    {
                        Text($"{review.Rating}/5 stars", 15, Dark, FontAttributes.Bold),
                        Text(review.Comment ?? "No written comment.", 12, Muted),
                        DetailRow("Booking", $"BM-{review.RequestId:000000}"),
                        DetailRow("Reviewed", review.CreatedAt.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture))
                    }
                }));
            }
        }

        SetPage("Ratings", "Customer feedback for completed jobs.", body, _isLoading, async () => await LoadAsync(), true);
    }
}
