using System.Globalization;
using System.Text.Json;
using System.Net.Http.Json;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using Microsoft.Maui.Controls.Shapes;
using static BikeMate.Helpers.JsonDisplayHelper;

namespace BikeMate.Views.Shared
{

public sealed record ModuleAction(string Text, string Route);

public abstract class ConnectedModulePage : ContentPage
{
    private static readonly Color Orange = Color.FromArgb("#FF6B2C");
    private static readonly Color Dark = Color.FromArgb("#1F2933");
    private static readonly Color Muted = Color.FromArgb("#6E6E6E");
    private static readonly Color BorderColor = Color.FromArgb("#E6E6E6");
    private static readonly Color PageColor = Color.FromArgb("#F6F6F6");

    private readonly string _pageTitle;
    private readonly string _subtitle;
    private readonly string _endpoint;
    private readonly string _emptyMessage;
    private readonly IReadOnlyList<ModuleAction> _actions;
    private bool _isLoading;
    private string? _banner;
    private string? _searchText;
    private IReadOnlyList<ModuleCard> _cards = [];

    protected ConnectedModulePage(
        string title,
        string subtitle,
        string endpoint,
        string emptyMessage,
        params ModuleAction[] actions)
    {
        _pageTitle = title;
        _subtitle = subtitle;
        _endpoint = endpoint;
        _emptyMessage = emptyMessage;
        _actions = actions;
        Title = title;
        BackgroundColor = PageColor;
        Render("Loading live data...");
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
        Render("Refreshing live data...");
        try
        {
            using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
            using var response = await http.GetAsync(_endpoint);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _cards = [];
                _banner = string.IsNullOrWhiteSpace(error)
                    ? $"Could not load /api/{_endpoint}. Status {(int)response.StatusCode}."
                    : error;
                return;
            }

            var payload = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
            _cards = ToCards(document.RootElement).Take(40).ToArray();
            _banner = null;
        }
        catch (Exception ex)
        {
            _cards = [];
            _banner = $"Could not load /api/{_endpoint}. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var visibleCards = FilterCards();

        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };

        root.Add(Header(), 0, 0);

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 14, 16, 24),
            Spacing = 12
        };

        if (_actions.Count > 0)
        {
            body.Add(ActionRow());
        }

        if (_cards.Count > 0 || !string.IsNullOrWhiteSpace(_searchText))
        {
            body.Add(SearchRow());
        }

        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(StateCard(banner, Colors.White));
        }

        if (_isLoading)
        {
            body.Add(new ActivityIndicator { IsVisible = true, IsRunning = true, Color = Orange, HeightRequest = 42 });
        }

        if (_cards.Count == 0 && !_isLoading)
        {
            body.Add(EmptyState(_emptyMessage));
        }
        else if (visibleCards.Count == 0 && !_isLoading)
        {
            body.Add(EmptyState($"No records match \"{_searchText}\". Clear the search to return to the full list."));
        }
        else
        {
            foreach (var card in visibleCards)
            {
                body.Add(DataCard(card));
            }
        }

        root.Add(new ScrollView { Content = body }, 0, 1);
        Content = root;
    }

    private View Header()
    {
        var grid = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 18, 16, 12),
            ColumnSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var text = new VerticalStackLayout { Spacing = 3 };
        text.Add(new Label
        {
            Text = _pageTitle,
            TextColor = Dark,
            FontAttributes = FontAttributes.Bold,
            FontSize = 18
        });
        text.Add(new Label
        {
            Text = _subtitle,
            TextColor = Muted,
            FontSize = 13,
            LineBreakMode = LineBreakMode.WordWrap
        });
        grid.Add(text, 0, 0);
        grid.Add(new Button
        {
            Text = "Dashboard",
            BackgroundColor = Colors.White,
            TextColor = Dark,
            BorderColor = BorderColor,
            BorderWidth = 1,
            CornerRadius = 8,
            HeightRequest = 40,
            Padding = new Thickness(12, 0),
            FontSize = 13,
            Command = new Command(async () => await Shell.Current.GoToAsync("//AdminDashboardPage"))
        }, 1, 0);
        grid.Add(new Button
        {
            Text = _isLoading ? "..." : "Refresh",
            IsEnabled = !_isLoading,
            BackgroundColor = Orange,
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 40,
            Padding = new Thickness(14, 0),
            Command = new Command(async () => await LoadAsync())
        }, 2, 0);
        grid.Add(new Button
        {
            Text = "Log out",
            BackgroundColor = Colors.Transparent,
            TextColor = Orange,
            BorderColor = BorderColor,
            BorderWidth = 1,
            CornerRadius = 8,
            HeightRequest = 40,
            Padding = new Thickness(12, 0),
            FontSize = 13,
            Command = new Command(async () => await AppNavigation.ConfirmAndSignOutAsync(this))
        }, 3, 0);

        return grid;
    }

    private View ActionRow()
    {
        var row = new HorizontalStackLayout { Spacing = 8 };
        foreach (var action in _actions)
        {
            row.Add(new Button
            {
                Text = action.Text,
                BackgroundColor = Colors.White,
                TextColor = Dark,
                BorderColor = BorderColor,
                BorderWidth = 1,
                CornerRadius = 8,
                HeightRequest = 38,
                Padding = new Thickness(12, 0),
                FontSize = 13,
                Command = new Command(async () => await Shell.Current.GoToAsync(action.Route))
            });
        }

        return new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = row };
    }

    private View SearchRow()
    {
        var search = new SearchBar
        {
            Text = _searchText ?? string.Empty,
            Placeholder = $"Search {_pageTitle.ToLowerInvariant()}",
            BackgroundColor = Colors.White,
            TextColor = Dark,
            PlaceholderColor = Muted,
            FontSize = 13,
            HeightRequest = 44
        };
        search.TextChanged += (_, args) =>
        {
            var nextSearch = string.IsNullOrWhiteSpace(args.NewTextValue) ? null : args.NewTextValue;
            if (string.Equals(_searchText, nextSearch, StringComparison.Ordinal))
            {
                return;
            }

            _searchText = nextSearch;
            Render(_banner);
        };

        var grid = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(search, 0, 0);
        grid.Add(new Button
        {
            Text = "Clear",
            IsEnabled = !string.IsNullOrWhiteSpace(_searchText),
            BackgroundColor = Colors.White,
            TextColor = Muted,
            BorderColor = BorderColor,
            BorderWidth = 1,
            CornerRadius = 8,
            HeightRequest = 40,
            Padding = new Thickness(12, 0),
            FontSize = 13,
            Command = new Command(() =>
            {
                _searchText = null;
                Render(_banner);
            })
        }, 1, 0);

        return grid;
    }

    private static View DataCard(ModuleCard card)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(new Label
        {
            Text = card.Title,
            TextColor = Dark,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.WordWrap
        });
        stack.Add(new Label
        {
            Text = card.Body,
            TextColor = Muted,
            FontSize = 13,
            LineBreakMode = LineBreakMode.WordWrap
        });
        return new Border
        {
            Stroke = BorderColor,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Colors.White,
            Padding = new Thickness(14),
            Content = stack
        };
    }

    private static View StateCard(string text, Color background)
    {
        return new Border
        {
            Stroke = BorderColor,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = background,
            Padding = new Thickness(14),
            Content = new Label
            {
                Text = text,
                TextColor = Muted,
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap
            }
        };
    }

    private static View EmptyState(string text)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(new Label
        {
            Text = text,
            TextColor = Dark,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.WordWrap
        });
        stack.Add(new Label
        {
            Text = "Use the tabs, quick actions, or refresh once backend data changes.",
            TextColor = Muted,
            FontSize = 13,
            LineBreakMode = LineBreakMode.WordWrap
        });

        return new Border
        {
            Stroke = BorderColor,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Colors.White,
            Padding = new Thickness(14),
            Content = stack
        };
    }

    private IReadOnlyList<ModuleCard> FilterCards()
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            return _cards;
        }

        var query = _searchText.Trim();
        return _cards
            .Where(card =>
                card.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                card.Body.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static IEnumerable<ModuleCard> ToCards(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                yield return ToCard(item);
            }

            yield break;
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            var scalarLines = root.EnumerateObject()
                .Where(x => x.Value.ValueKind is not JsonValueKind.Array and not JsonValueKind.Object)
                .Select(x => $"{Humanize(x.Name)}: {Scalar(x.Value)}")
                .ToArray();
            if (scalarLines.Length > 0)
            {
                yield return new ModuleCard("Summary", string.Join("\n", scalarLines));
            }

            foreach (var property in root.EnumerateObject().Where(x => x.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object))
            {
                yield return new ModuleCard(Humanize(property.Name), Describe(property.Value));
            }
        }
    }

    private static ModuleCard ToCard(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return new ModuleCard("Record", Scalar(element));
        }

        var title = FirstScalar(element, "title", "serviceName", "shopName", "fullName", "customerName", "email", "requestId", "paymentId", "productName")
            ?? "Record";
        return new ModuleCard(title, Describe(element));
    }

    private static string Describe(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Object => string.Join("\n", value.EnumerateObject()
                .Where(x => x.Value.ValueKind is not JsonValueKind.Object and not JsonValueKind.Array)
                .Take(10)
                .Select(x => $"{Humanize(x.Name)}: {Scalar(x.Value)}")),
            JsonValueKind.Array => value.GetArrayLength() == 0
                ? "No records yet."
                : string.Join("\n\n", value.EnumerateArray().Take(5).Select(x => ToCard(x).Body)),
            _ => Scalar(value)
        };
    }

    private sealed record ModuleCard(string Title, string Body);
}

}

namespace BikeMate.Views.Admin
{

using BikeMate.Views.Shared;

public sealed class AdminDashboardPage : ContentPage
{
    private static readonly Color PageBackground = Color.FromArgb("#F4F5F7");
    private static readonly Color SidebarBackground = Color.FromArgb("#121418");
    private static readonly Color SidebarMuted = Color.FromArgb("#9CA3AF");
    private static readonly Color SidebarActive = Color.FromArgb("#262A31");
    private static readonly Color CardBorder = Color.FromArgb("#E7E7E7");
    private static readonly Color DarkText = Color.FromArgb("#1F2933");
    private static readonly Color MutedText = Color.FromArgb("#6B7280");
    private static readonly Color Orange = Color.FromArgb("#D97706");
    private static readonly Color OrangeSoft = Color.FromArgb("#FFF4E5");
    private static readonly Color Green = Color.FromArgb("#1C8C5A");
    private static readonly Color Blue = Color.FromArgb("#1EA7D8");
    private static readonly Color Red = Color.FromArgb("#D63C4E");

    private bool _isLoading;
    private bool _isCompact;
    private string? _banner;
    private DashboardState _state = DashboardState.Empty;

    public AdminDashboardPage()
    {
        Title = "Admin Dashboard";
        BackgroundColor = PageBackground;
        Render();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        var compact = width > 0 && width < 980;
        if (compact != _isCompact)
        {
            _isCompact = compact;
            Render();
        }
    }

    private async Task LoadAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        Render();

        try
        {
            using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
            var dashboardTask = http.GetFromJsonAsync<AdminDashboardDto>("admin/dashboard");
            var meTask = http.GetFromJsonAsync<UserProfileDto>("auth/me");
            var customersTask = http.GetFromJsonAsync<CustomerRow[]>("admin/customers");
            var mechanicsTask = http.GetFromJsonAsync<MechanicRow[]>("admin/mechanics");
            var shopsTask = http.GetFromJsonAsync<ShopRow[]>("admin/shops");
            var requestsTask = http.GetFromJsonAsync<ServiceRequestDto[]>("admin/service-requests");
            var emergencyTask = http.GetFromJsonAsync<ServiceRequestDto[]>("admin/emergency-requests");

            await Task.WhenAll(dashboardTask, meTask, customersTask, mechanicsTask, shopsTask, requestsTask, emergencyTask);

            var dashboard = await dashboardTask ?? new AdminDashboardDto(0, 0, 0, 0, 0, 0, 0m, 0);
            var me = await meTask;
            var customers = (await customersTask ?? []).ToArray();
            var mechanics = (await mechanicsTask ?? []).ToArray();
            var shops = (await shopsTask ?? []).ToArray();
            var requests = (await requestsTask ?? []).ToArray();
            var emergencies = (await emergencyTask ?? []).ToArray();

            _state = new DashboardState(
                dashboard,
                me,
                customers,
                mechanics,
                shops,
                requests,
                emergencies,
                BuildWeeklyCustomerCounts(customers));

            var pendingMechanics = mechanics.Count(x => !x.IsVerified);
            var pendingShops = shops.Count(x => !string.Equals(x.ShopStatus, "verified", StringComparison.OrdinalIgnoreCase));

            _banner = emergencies.Length > 0
                ? $"{emergencies.Length} active emergency request{(emergencies.Length == 1 ? string.Empty : "s")} need monitoring."
                : pendingMechanics + pendingShops > 0
                    ? $"{pendingMechanics + pendingShops} verification item{(pendingMechanics + pendingShops == 1 ? string.Empty : "s")} are waiting for admin review."
                : null;
        }
        catch (Exception ex)
        {
            _banner = ex.Message;
        }
        finally
        {
            _isLoading = false;
            Render();
        }
    }

    private void Render()
    {
        var content = new Grid
        {
            RowSpacing = 14,
            Padding = new Thickness(18, 14, 18, 18),
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            }
        };

        content.Add(BuildTopBar(), 0, 0);
        content.Add(BuildOverviewHeader(), 0, 1);
        content.Add(BuildBanner(), 0, 2);
        content.Add(BuildMetricsGrid(), 0, 3);
        content.Add(BuildRequestsSection(), 0, 4);

        var root = new Grid
        {
            ColumnSpacing = 0,
            RowSpacing = 0
        };

        if (_isCompact)
        {
            root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            root.Add(BuildCompactNav(), 0, 0);
            root.Add(new ScrollView { Content = content }, 0, 1);
        }
        else
        {
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(224) });
            root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            root.Add(BuildSidebar(), 0, 0);
            root.Add(new ScrollView { Content = content }, 1, 0);
        }

        Content = root;
    }

    private View BuildSidebar()
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 18,
            Padding = new Thickness(16, 18, 16, 18)
        };

        stack.Add(new HorizontalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Border
                {
                    BackgroundColor = Colors.White,
                    Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 8 },
                    WidthRequest = 36,
                    HeightRequest = 36,
                    Content = new Image
                    {
                        Source = "bikemate_logo",
                        Aspect = Aspect.AspectFit,
                        Margin = new Thickness(5)
                    }
                },
                new VerticalStackLayout
                {
                    Spacing = 0,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label
                        {
                            Text = "BIKEMATE",
                            TextColor = Colors.White,
                            FontFamily = "Inter",
                            FontSize = AppTypography.BodySize,
                            FontAttributes = FontAttributes.Bold
                        },
                        new Label
                        {
                            Text = "Operations Console",
                            TextColor = SidebarMuted,
                            FontFamily = "PublicSans",
                            FontSize = 11
                        }
                    }
                }
            }
        });

        stack.Add(SectionLabel("Overview"));
        stack.Add(SidebarButton("Dashboard", "//AdminDashboardPage", true));

        stack.Add(SectionLabel("Operations"));
        stack.Add(SidebarButton("Emergency", nameof(AdminEmergencyRequestsPage)));
        stack.Add(SidebarButton("Users", nameof(AdminUsersPage)));
        stack.Add(SidebarButton("Shops", nameof(AdminShopsVerificationPage)));
        stack.Add(SidebarButton("Mechanics", nameof(AdminMechanicsVerificationPage)));
        stack.Add(SidebarButton("Requests", nameof(AdminRequestsPage)));
        stack.Add(SidebarButton("Payments", nameof(AdminPaymentsPage)));

        stack.Add(SectionLabel("Insights"));
        stack.Add(SidebarButton("Reports", nameof(AdminReportsPage)));
        stack.Add(SidebarButton("Revenue", nameof(AdminRevenueReportPage)));
        stack.Add(SidebarButton("Audit Logs", nameof(AdminAuditLogsPage)));

        stack.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#2D3138"), Margin = new Thickness(0, 6) });
        stack.Add(SidebarButton("Logout", null, false, true));

        return new Border
        {
            BackgroundColor = SidebarBackground,
            Stroke = Colors.Transparent,
            Content = new ScrollView { Content = stack }
        };
    }

    private View BuildCompactNav()
    {
        var row = new HorizontalStackLayout
        {
            Spacing = 8,
            Padding = new Thickness(14, 10)
        };

        row.Add(new Label
        {
            Text = "Admin",
            TextColor = Colors.White,
            FontFamily = "Inter",
            FontSize = AppTypography.BodySize,
            FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0)
        });

        row.Add(CompactNavButton("Dashboard", "//AdminDashboardPage", true));
        row.Add(CompactNavButton("Emergency", nameof(AdminEmergencyRequestsPage)));
        row.Add(CompactNavButton("Users", nameof(AdminUsersPage)));
        row.Add(CompactNavButton("Requests", nameof(AdminRequestsPage)));
        row.Add(CompactNavButton("Reports", nameof(AdminReportsPage)));
        row.Add(CompactNavButton("Audit", nameof(AdminAuditLogsPage)));
        row.Add(CompactNavButton("Logout", null, false, true));

        return new Border
        {
            BackgroundColor = SidebarBackground,
            Stroke = Colors.Transparent,
            Content = new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                Content = row
            }
        };
    }

    private View CompactNavButton(string text, string? route, bool active = false, bool danger = false)
    {
        var button = new Button
        {
            Text = text,
            FontFamily = "PublicSans",
            FontSize = 12,
            HeightRequest = 34,
            CornerRadius = 8,
            Padding = new Thickness(12, 0),
            BackgroundColor = active ? SidebarActive : Color.FromArgb("#1A1D22"),
            TextColor = danger ? Color.FromArgb("#FFB6BE") : (active ? Colors.White : SidebarMuted),
            BorderWidth = 0
        };

        if (danger)
        {
            button.Command = new Command(async () => await AppNavigation.SignOutAsync());
        }
        else if (!string.IsNullOrWhiteSpace(route))
        {
            button.Command = new Command(async () => await Shell.Current.GoToAsync(route));
        }

        return button;
    }

    private View SectionLabel(string text)
    {
        return new Label
        {
            Text = text.ToUpperInvariant(),
            TextColor = Color.FromArgb("#6B7280"),
            FontFamily = "PTSansCaptionBold",
            FontSize = AppTypography.CaptionSize
        };
    }

    private View SidebarButton(string text, string? route, bool active = false, bool danger = false)
    {
        var button = new Button
        {
            Text = text,
            FontFamily = "PublicSans",
            FontSize = 13,
            HeightRequest = 42,
            CornerRadius = 8,
            HorizontalOptions = LayoutOptions.Fill,
            Padding = new Thickness(12, 0),
            BackgroundColor = active ? SidebarActive : Colors.Transparent,
            TextColor = danger ? Color.FromArgb("#FFB6BE") : (active ? Colors.White : SidebarMuted),
            BorderWidth = 0
        };

        if (string.Equals(text, "Logout", StringComparison.OrdinalIgnoreCase))
        {
            button.Command = new Command(async () => await AppNavigation.SignOutAsync());
        }
        else if (!string.IsNullOrWhiteSpace(route))
        {
            button.Command = new Command(async () => await Shell.Current.GoToAsync(route));
        }

        return button;
    }

    private View BuildTopBar()
    {
        var user = _state.Me?.Email ?? "Admin";

        var grid = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var title = new VerticalStackLayout
        {
            Spacing = 0,
            Children =
            {
                new Label
                {
                    Text = "BIKEMATE ADMIN",
                    TextColor = Color.FromArgb("#6B7280"),
                    FontFamily = "PTSansCaptionBold",
                    FontSize = AppTypography.CaptionSize
                },
                new Label
                {
                    Text = "Operations Console",
                    TextColor = DarkText,
                    FontFamily = "Inter",
                    FontSize = AppTypography.TitleSize,
                    FontAttributes = FontAttributes.Bold
                }
            }
        };
        grid.Add(title, 0, 0);

        var actions = new HorizontalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = user,
                    TextColor = DarkText,
                    FontFamily = "PublicSans",
                    FontSize = AppTypography.BodySize,
                    VerticalTextAlignment = TextAlignment.Center
                },
                new Button
                {
                    Text = "Logout",
                    BackgroundColor = Colors.White,
                    BorderColor = Color.FromArgb("#E8B1B7"),
                    BorderWidth = 1,
                    TextColor = Red,
                    FontFamily = "Inter",
                    FontSize = AppTypography.BodySize,
                    CornerRadius = 8,
                    HeightRequest = 32,
                    Padding = new Thickness(12, 0),
                    Command = new Command(async () => await AppNavigation.SignOutAsync())
                }
            }
        };
        grid.Add(actions, 1, 0);

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = CardBorder,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(16, 12),
            Content = grid
        };
    }

    private View BuildOverviewHeader()
    {
        var grid = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Add(new Label
        {
            Text = "System Overview",
            TextColor = DarkText,
            FontFamily = "Inter",
            FontSize = AppTypography.TitleSize,
            FontAttributes = FontAttributes.Bold
        }, 0, 0);

        grid.Add(new Button
        {
            Text = _isLoading ? "Refreshing..." : "Refresh Data",
            IsEnabled = !_isLoading,
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#F2C08D"),
            BorderWidth = 1,
            TextColor = Orange,
            FontFamily = "Inter",
            FontSize = AppTypography.BodySize,
            CornerRadius = 8,
            HeightRequest = 36,
            Padding = new Thickness(12, 0),
            Command = new Command(async () => await LoadAsync())
        }, 1, 0);

        return grid;
    }

    private View BuildBanner()
    {
        var text = string.IsNullOrWhiteSpace(_banner)
            ? "All monitored services are currently in a steady state."
            : _banner;

        var grid = new Grid
        {
            ColumnSpacing = 14,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            VerticalOptions = LayoutOptions.Center
        };

        grid.Add(new Border
        {
            BackgroundColor = Color.FromArgb("#FDE8E9"),
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            WidthRequest = 38,
            HeightRequest = 38,
            Content = new Label
            {
                Text = "!",
                TextColor = Red,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        }, 0, 0);

        grid.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label
                {
                    Text = "Emergency Window",
                    TextColor = DarkText,
                    FontFamily = "Inter",
                    FontSize = AppTypography.BodySize,
                    FontAttributes = FontAttributes.Bold
                },
                new Label
                {
                    Text = text,
                    TextColor = MutedText,
                    FontFamily = "PublicSans",
                    FontSize = AppTypography.BodySize
                }
            }
        }, 1, 0);

        grid.Add(new Button
        {
            Text = _isCompact ? "Review" : "Open Emergency Window",
            BackgroundColor = Red,
            TextColor = Colors.White,
            FontFamily = "Inter",
            FontSize = AppTypography.BodySize,
            CornerRadius = 8,
            HeightRequest = 36,
            Padding = new Thickness(12, 0),
            Command = new Command(async () => await Shell.Current.GoToAsync(nameof(AdminEmergencyRequestsPage)))
        }, 2, 0);

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E6E1DF"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(14, 12),
            Content = grid
        };
    }

    private View BuildMetricsGrid()
    {
        var grid = new Grid
        {
            ColumnSpacing = 12,
            RowSpacing = 12
        };

        if (_isCompact)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.Add(BuildWeeklyGrowthCard(), 0, 0);
            grid.Add(BuildNetworkHealthCard(), 0, 1);
        }
        else
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.2, GridUnitType.Star)));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.Add(BuildWeeklyGrowthCard(), 0, 0);
            grid.Add(BuildNetworkHealthCard(), 1, 0);
        }

        return grid;
    }

    private View BuildWeeklyGrowthCard()
    {
        var chart = BuildWeeklyChart(_state.WeeklyCustomerCounts);

        var stack = new VerticalStackLayout
        {
            Spacing = 10
        };

        stack.Add(new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        });

        var header = (Grid)stack.Children[0];
        header.Add(new Label
        {
            Text = "Weekly User Growth",
            TextColor = Orange,
            FontFamily = "Inter",
            FontSize = AppTypography.BodySize,
            FontAttributes = FontAttributes.Bold
        }, 0, 0);

        header.Add(new Button
        {
            Text = "View Directory",
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#F2C08D"),
            BorderWidth = 1,
            TextColor = Orange,
            FontFamily = "Inter",
            FontSize = 11,
            CornerRadius = 8,
            HeightRequest = 32,
            Padding = new Thickness(10, 0),
            Command = new Command(async () => await Shell.Current.GoToAsync(nameof(AdminUsersPage)))
        }, 1, 0);

        stack.Add(new Label
        {
            Text = _state.Summary.TotalCustomers.ToString(CultureInfo.InvariantCulture),
            TextColor = DarkText,
            FontFamily = "Inter",
            FontSize = AppTypography.TitleSize,
            FontAttributes = FontAttributes.Bold
        });

        stack.Add(new Label
        {
            Text = "Total Customers",
            TextColor = MutedText,
            FontFamily = "PublicSans",
            FontSize = AppTypography.BodySize
        });

        stack.Add(chart);

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = CardBorder,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(14, 12),
            Content = stack
        };
    }

    private View BuildWeeklyChart(IReadOnlyList<int> weeklyCounts)
    {
        var start = DateTime.UtcNow.Date.AddDays(-6);
        var grid = new Grid
        {
            ColumnSpacing = 10,
            RowSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            RowDefinitions =
            {
                new RowDefinition(new GridLength(84)),
                new RowDefinition(GridLength.Auto)
            }
        };

        var max = Math.Max(1, weeklyCounts.DefaultIfEmpty().Max());
        for (var i = 0; i < 7; i++)
        {
            var value = weeklyCounts.ElementAtOrDefault(i);
            var ratio = Math.Max(0.08, (double)value / max);
            var dayLabel = start.AddDays(i).ToString("ddd", CultureInfo.InvariantCulture);
            var column = new VerticalStackLayout
            {
                Spacing = 6,
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.Fill,
                Children =
                {
                    new BoxView
                    {
                        HeightRequest = Math.Max(3, 62 * ratio),
                        CornerRadius = 0,
                        BackgroundColor = Orange,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.End
                    },
                    new Label
                    {
                        Text = dayLabel,
                        TextColor = MutedText,
                        FontFamily = "PublicSans",
                        FontSize = 11,
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            };

            grid.Add(column, i, 0);
        }

        return grid;
    }

    private View BuildNetworkHealthCard()
    {
        var verifiedMechanics = _state.Mechanics.Where(x => x.IsVerified).ToArray();
        var onlineMechanics = verifiedMechanics.Count(x => IsOnline(x.AvailabilityStatus));
        var verifiedShops = _state.Shops.Count(x => string.Equals(x.ShopStatus, "verified", StringComparison.OrdinalIgnoreCase));
        var totalMechanics = Math.Max(1, verifiedMechanics.Length);
        var totalShops = Math.Max(1, _state.Shops.Count);

        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Add(new Label
        {
            Text = "Network Health",
            TextColor = DarkText,
            FontFamily = "Inter",
            FontSize = AppTypography.TitleSize,
            FontAttributes = FontAttributes.Bold
        });

        stack.Add(BuildHealthRow(
            "Mechanics Online",
            $"{Math.Min(onlineMechanics, totalMechanics)} / {totalMechanics} Active",
            Green,
            totalMechanics > 0 ? (double)onlineMechanics / totalMechanics : 0,
            nameof(AdminMechanicsVerificationPage)));

        stack.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#E9E9E9"), Margin = new Thickness(0, 2) });

        stack.Add(BuildHealthRow(
            "Verified Partner Shops",
            $"{verifiedShops} / {totalShops} Total",
            Blue,
            totalShops > 0 ? (double)verifiedShops / totalShops : 0,
            nameof(AdminShopsVerificationPage)));

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = CardBorder,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(14, 12),
            Content = stack
        };
    }

    private View BuildHealthRow(string title, string value, Color accent, double progress, string route)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        });

        var rowHeader = (Grid)stack.Children[0];
        rowHeader.Add(new Label
        {
            Text = title,
            TextColor = DarkText,
            FontFamily = "Inter",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold
        }, 0, 0);
        rowHeader.Add(new Button
        {
            Text = "Manage >",
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0,
            TextColor = accent,
            FontFamily = "PublicSans",
            FontSize = AppTypography.BodySize,
            Padding = new Thickness(0),
            HeightRequest = 28,
            Command = new Command(async () => await Shell.Current.GoToAsync(route))
        }, 1, 0);

        stack.Add(new Label
        {
            Text = value,
            TextColor = DarkText,
            FontFamily = "PublicSans",
            FontSize = AppTypography.BodySize
        });

        stack.Add(new ProgressBar
        {
            Progress = Math.Clamp(progress, 0, 1),
            ProgressColor = accent,
            BackgroundColor = Color.FromArgb("#EAF0EE"),
            HeightRequest = 6
        });

        return stack;
    }

    private View BuildRequestsSection()
    {
        var activeRequests = _state.Requests
            .Where(x => IsActiveRequest(x.CurrentStatus))
            .Take(6)
            .ToArray();

        var stack = new VerticalStackLayout { Spacing = 10 };

        stack.Add(new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        });

        var header = (Grid)stack.Children[0];
        header.Add(new Label
        {
            Text = $"Live Service Requests ({activeRequests.Length} Active)",
            TextColor = DarkText,
            FontFamily = "Inter",
            FontSize = AppTypography.TitleSize,
            FontAttributes = FontAttributes.Bold
        }, 0, 0);
        header.Add(new Button
        {
            Text = "View Tracker Board",
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E1C66B"),
            BorderWidth = 1,
            TextColor = Color.FromArgb("#8A6F00"),
            FontFamily = "Inter",
            FontSize = 11,
            CornerRadius = 8,
            HeightRequest = 32,
            Padding = new Thickness(10, 0),
            Command = new Command(async () => await Shell.Current.GoToAsync(nameof(AdminRequestsPage)))
        }, 1, 0);

        if (activeRequests.Length == 0)
        {
            stack.Add(new Label
            {
                Text = "No live requests are active right now.",
                TextColor = MutedText,
                FontFamily = "PublicSans",
                FontSize = AppTypography.BodySize
            });
        }
        else
        {
            foreach (var request in activeRequests)
            {
                stack.Add(BuildRequestRow(request));
            }
        }

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = CardBorder,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(14, 12),
            Content = stack
        };
    }

    private View BuildRequestRow(ServiceRequestDto request)
    {
        var statusColor = StatusColor(request.CurrentStatus);
        var title = request.IssueDescription.Length > 42
            ? request.IssueDescription[..42] + "..."
            : request.IssueDescription;
        var subtitle = string.Join(" | ", new[]
        {
            request.CustomerName,
            request.ShopName,
            request.ServiceName
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

        var layout = new Grid
        {
            ColumnSpacing = 10,
            Padding = new Thickness(10, 8),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var left = new VerticalStackLayout { Spacing = 2 };
        left.Add(new Label
        {
            Text = $"#{request.RequestId} {title}",
            TextColor = DarkText,
            FontFamily = "Inter",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation
        });
        left.Add(new Label
        {
            Text = subtitle,
            TextColor = MutedText,
            FontFamily = "PublicSans",
            FontSize = 11,
            LineBreakMode = LineBreakMode.TailTruncation
        });
        left.Add(new Label
        {
            Text = request.ServiceLocationAddress ?? "Location unavailable",
            TextColor = Color.FromArgb("#94A3B8"),
            FontFamily = "PublicSans",
            FontSize = 11,
            LineBreakMode = LineBreakMode.TailTruncation
        });
        layout.Add(left, 0, 0);

        var right = new VerticalStackLayout
        {
            Spacing = 6,
            HorizontalOptions = LayoutOptions.End,
            Children =
            {
                StatusPill(request.CurrentStatus, statusColor),
                new Label
                {
                    Text = $"PHP {request.FinalTotal:0.00}",
                    TextColor = DarkText,
                    FontFamily = "Inter",
                    FontSize = AppTypography.BodySize,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.End
                },
                new Label
                {
                    Text = request.CreatedAt.ToLocalTime().ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture),
                    TextColor = MutedText,
                    FontFamily = "PublicSans",
                    FontSize = AppTypography.CaptionSize,
                    HorizontalTextAlignment = TextAlignment.End
                }
            }
        };
        layout.Add(right, 1, 0);

        return new Border
        {
            BackgroundColor = Color.FromArgb("#FBFBFC"),
            Stroke = Color.FromArgb("#ECECEC"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = 0,
            Content = layout
        };
    }

    private View StatusPill(string text, Color background)
    {
        return new Border
        {
            BackgroundColor = background,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(8, 3),
            HorizontalOptions = LayoutOptions.End,
            Content = new Label
            {
                Text = text,
                TextColor = Colors.White,
                FontFamily = "PTSansCaptionBold",
                FontSize = AppTypography.CaptionSize,
                HorizontalTextAlignment = TextAlignment.Center
            }
        };
    }

    private static Color StatusColor(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "completed" => Green,
            "in_progress" => Blue,
            "accepted" => Color.FromArgb("#E09A1F"),
            "arrived" => Color.FromArgb("#8E63D9"),
            "cancelled" => Red,
            "pending" => Orange,
            _ => Color.FromArgb("#64748B")
        };
    }

    private static bool IsOnline(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized is "available" or "active" or "online" or "working";
    }

    private static bool IsActiveRequest(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized is not ("completed" or "cancelled" or "rejected" or "refunded");
    }

    private static int[] BuildWeeklyCustomerCounts(IReadOnlyList<CustomerRow> customers)
    {
        var counts = new int[7];
        var today = DateTime.UtcNow.Date;
        var start = today.AddDays(-6);

        foreach (var customer in customers)
        {
            var date = customer.CreatedAt.ToUniversalTime().Date;
            if (date < start || date > today)
            {
                continue;
            }

            var index = (date - start).Days;
            if (index >= 0 && index < counts.Length)
            {
                counts[index]++;
            }
        }

        return counts;
    }

    private sealed record DashboardState(
        AdminDashboardDto Summary,
        UserProfileDto? Me,
        IReadOnlyList<CustomerRow> Customers,
        IReadOnlyList<MechanicRow> Mechanics,
        IReadOnlyList<ShopRow> Shops,
        IReadOnlyList<ServiceRequestDto> Requests,
        IReadOnlyList<ServiceRequestDto> EmergencyRequests,
        IReadOnlyList<int> WeeklyCustomerCounts)
    {
        public static DashboardState Empty { get; } = new(
            new AdminDashboardDto(0, 0, 0, 0, 0, 0, 0m, 0),
            null,
            [],
            [],
            [],
            [],
            [],
            [0, 0, 0, 0, 0, 0, 0]);
    }

    private sealed record CustomerRow(
        int CustomerId,
        int UserId,
        string FullName,
        string Email,
        string? PhoneNumber,
        string AccountStatus,
        DateTime CreatedAt);

    private sealed record MechanicRow(
        int MechanicId,
        int UserId,
        string FullName,
        bool IsVerified,
        string AvailabilityStatus,
        decimal AverageRating,
        int TotalCompletedJobs);

    private sealed record ShopRow(
        int ShopId,
        int OwnerUserId,
        string ShopName,
        string? City,
        string? Province,
        string ShopStatus);
}

public sealed class AdminUsersPage() : ConnectedModulePage(
    "Users",
    "Review accounts, roles, account status, and verification state.",
    "admin/users",
    "No user accounts were returned yet.",
    new ModuleAction("Mechanics", nameof(AdminMechanicsVerificationPage)),
    new ModuleAction("Shops", nameof(AdminShopsVerificationPage)),
    new ModuleAction("Payments", nameof(AdminPaymentsPage)));

public sealed class AdminEmergencyRequestsPage() : ConnectedModulePage(
    "Emergency Monitoring",
    "Monitor urgent service requests across the platform.",
    "admin/emergency-requests",
    "No emergency requests are active right now.",
    new ModuleAction("All Requests", nameof(AdminRequestsPage)),
    new ModuleAction("Audit Logs", nameof(AdminAuditLogsPage)));

public sealed class AdminMechanicsVerificationPage() : ConnectedModulePage(
    "Mechanic Verification",
    "Review mechanic applications that are awaiting admin approval.",
    "admin/mechanics/pending",
    "No mechanic verification requests are pending.",
    new ModuleAction("Users", nameof(AdminUsersPage)),
    new ModuleAction("Shops", nameof(AdminShopsVerificationPage)));

public sealed class AdminShopsVerificationPage() : ConnectedModulePage(
    "Shop Verification",
    "Review partner shop applications that are awaiting admin approval.",
    "admin/shops/pending",
    "No shop verification requests are pending.",
    new ModuleAction("Users", nameof(AdminUsersPage)),
    new ModuleAction("Mechanics", nameof(AdminMechanicsVerificationPage)));

public sealed class AdminRequestsPage() : ConnectedModulePage(
    "Service Requests",
    "Track customer requests, live status, assignments, and totals.",
    "admin/service-requests",
    "No service requests were returned.",
    new ModuleAction("Emergency", nameof(AdminEmergencyRequestsPage)),
    new ModuleAction("Payments", nameof(AdminPaymentsPage)),
    new ModuleAction("Audit Logs", nameof(AdminAuditLogsPage)));

public sealed class AdminPaymentsPage() : ConnectedModulePage(
    "Payments",
    "Review payment records, provider status, amounts, and references.",
    "admin/payments",
    "No payments were returned.",
    new ModuleAction("Requests", nameof(AdminRequestsPage)),
    new ModuleAction("Revenue", nameof(AdminRevenueReportPage)));

public sealed class AdminReportsPage() : ConnectedModulePage(
    "Reports",
    "Review top services and platform reporting from backend queries.",
    "admin/reports/top-services",
    "No report rows were returned.",
    new ModuleAction("Revenue", nameof(AdminRevenueReportPage)),
    new ModuleAction("Audit Logs", nameof(AdminAuditLogsPage)));

public sealed class AdminRevenueReportPage() : ConnectedModulePage(
    "Revenue Report",
    "Review revenue and paid payment count for the configured report range.",
    "admin/reports/revenue",
    "No revenue report data was returned.",
    new ModuleAction("Payments", nameof(AdminPaymentsPage)),
    new ModuleAction("Reports", nameof(AdminReportsPage)));

public sealed class AdminAuditLogsPage() : ConnectedModulePage(
    "Audit Logs",
    "Review recent system actions and traceable admin changes.",
    "admin/audit-logs",
    "No audit logs were returned.",
    new ModuleAction("Users", nameof(AdminUsersPage)),
    new ModuleAction("Reports", nameof(AdminReportsPage)));

}
