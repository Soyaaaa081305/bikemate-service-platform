using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;
using BikeMate.Core.DTOs;
using BikeMate.Services;
using BikeMate.ViewModels.Customer.Emergency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices.Sensors;

namespace BikeMate.Views.Customer.Emergency;

internal static class EmergencyFlowState
{
    public static EmergencyLocationSnapshot? Location { get; set; }
    public static int RequestId { get; set; }
    public static EmergencyRequestStatusDto? Status { get; set; }
}

public sealed class EmergencySosPage : CustomerPageBase
{
    private readonly EmergencySosViewModel _viewModel = new();
    private Border? _outerRing;
    private bool _isAnimating;

    public EmergencySosPage()
    {
        Title = "Emergency SOS";
        BindingContext = _viewModel;
        Render();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (await EnsureCustomerApprovedForBookingAsync(BookingDraft.Customer) is null)
        {
            await Shell.Current.GoToAsync("//CustomerHomePage");
            return;
        }

        await LoadLocationAsync();
        StartPulse();
    }

    protected override void OnDisappearing()
    {
        _isAnimating = false;
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//CustomerHomePage");
        return true;
    }

    private async Task LoadLocationAsync()
    {
        if (EmergencyFlowState.Location is not null)
        {
            _viewModel.Location = EmergencyFlowState.Location;
            Render();
            return;
        }

        _viewModel.StatusMessage = "Getting your current location...";
        Render();
        _viewModel.Location = await LocationService.GetCurrentLocationAsync(this);
        EmergencyFlowState.Location = _viewModel.Location?.Latitude == 0m ? null : _viewModel.Location;
        _viewModel.StatusMessage = _viewModel.Location?.ErrorMessage ?? "";
        Render();
    }

    private void Render()
    {
        var root = new Grid
        {
            BackgroundColor = Colors.White,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };

        root.Add(TopBar("Back", new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))), 0, 0);

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(24, 12, 24, 20),
            Spacing = 14,
            HorizontalOptions = LayoutOptions.Fill
        };

        body.Add(new Label
        {
            Text = "Are you in an emergency?",
            TextColor = CustomerUi.Dark,
            FontAttributes = FontAttributes.Bold,
            FontSize = 18,
            HorizontalTextAlignment = TextAlignment.Center
        });
        body.Add(new Label
        {
            Text = "Press the button below and BikeMate will connect you with nearby help.",
            TextColor = CustomerUi.Muted,
            FontSize = 13,
            HorizontalTextAlignment = TextAlignment.Center
        });
        body.Add(new Label
        {
            Text = "No upfront payment is required for BikeMate emergency coordination and responder tracking.",
            TextColor = Color.FromArgb("#167A3A"),
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        });
        body.Add(BuildSosButton());

        var notes = new Editor
        {
            Placeholder = "What happened? Example: flat tire, engine stopped, accident",
            Text = _viewModel.Notes,
            AutoSize = EditorAutoSizeOption.TextChanges,
            MinimumHeightRequest = 76,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            FontSize = 13,
            BackgroundColor = Colors.Transparent
        };
        notes.TextChanged += (_, e) => _viewModel.Notes = e.NewTextValue ?? "";
        body.Add(Card(notes, Colors.White, 12, new Thickness(12)));

        if (!string.IsNullOrWhiteSpace(_viewModel.StatusMessage))
        {
            body.Add(Label(_viewModel.StatusMessage, 12, Color.FromArgb("#B3261E")));
        }

        root.Add(body, 0, 1);

        var bottom = new VerticalStackLayout { Padding = new Thickness(22, 8, 22, 24), Spacing = 12 };
        bottom.Add(LocationCard());
        bottom.Add(new Button
        {
            Text = "Change Location",
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Orange,
            FontAttributes = FontAttributes.Bold,
            Command = new Command(async () => await Shell.Current.GoToAsync(nameof(EmergencyLocationPickerPage)))
        });
        root.Add(bottom, 0, 2);

        SetScaffold(root, "Home", false);
    }

    private View BuildSosButton()
    {
        var grid = new Grid
        {
            HeightRequest = 260,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        _outerRing = Circle(230, Color.FromArgb("#FFB0B0"), 0.42);
        grid.Add(_outerRing);
        grid.Add(Circle(176, Color.FromArgb("#FF7070"), 0.72));
        var inner = Circle(126, Color.FromArgb("#FF1F1F"), 1);
        inner.Content = new Label
        {
            Text = "SOS",
            TextColor = Colors.White,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        inner.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await RequestHelpAsync()) });
        grid.Add(inner);
        return grid;
    }

    private static Border Circle(double size, Color color, double opacity)
    {
        return new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            Opacity = opacity,
            BackgroundColor = color,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = size / 2 },
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
    }

    private View LocationCard()
    {
        var location = _viewModel.Location;
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
            WidthRequest = 38,
            HeightRequest = 38,
            StrokeShape = new RoundRectangle { CornerRadius = 19 },
            BackgroundColor = CustomerUi.LightOrange,
            Stroke = Colors.Transparent,
            Content = new Label
            {
                Text = "!",
                TextColor = CustomerUi.Orange,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        }, 0, 0);

        var copy = new VerticalStackLayout { Spacing = 2 };
        copy.Add(Label("Your current location", 12, CustomerUi.Dark, FontAttributes.Bold));
        copy.Add(Label(location?.Address ?? "Choose a location before requesting emergency help.", 11, CustomerUi.Muted));
        grid.Add(copy, 1, 0);
        return Card(grid, Colors.White, 14, new Thickness(14));
    }

    private async Task RequestHelpAsync()
    {
        if (await EnsureCustomerApprovedForBookingAsync(BookingDraft.Customer) is null)
        {
            await Shell.Current.GoToAsync("//CustomerHomePage");
            return;
        }

        var location = _viewModel.Location;
        if (location is null || location.Latitude == 0m || location.Longitude == 0m)
        {
            await DisplayAlertAsync("Location required", "Please use current location or enter your location manually.", "OK");
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "Confirm Emergency Request?",
            "BikeMate will use your current location and notify nearby available responders.",
            "Request Help",
            "Cancel");
        if (!confirmed)
        {
            return;
        }

        try
        {
            _viewModel.IsBusy = true;
            var status = await EmergencyService.CreateRequestAsync(new CreateEmergencyRequestDto(
                location.Latitude,
                location.Longitude,
                location.Address,
                _viewModel.Notes,
                "Roadside"));
            EmergencyFlowState.RequestId = status.RequestId;
            EmergencyFlowState.Status = status;
            await Shell.Current.GoToAsync($"{nameof(CallingEmergencyPage)}?requestId={status.RequestId}");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Emergency request failed", ex.Message, "OK");
        }
        finally
        {
            _viewModel.IsBusy = false;
        }
    }

    private async void StartPulse()
    {
        if (_isAnimating || _outerRing is null)
        {
            return;
        }

        _isAnimating = true;
        while (_isAnimating && _outerRing is not null)
        {
            await Task.WhenAll(_outerRing.ScaleToAsync(1.08, 850, Easing.CubicInOut), _outerRing.FadeToAsync(0.18, 850));
            await Task.WhenAll(_outerRing.ScaleToAsync(1.0, 850, Easing.CubicInOut), _outerRing.FadeToAsync(0.42, 850));
        }
    }

    private static View TopBar(string text, ICommand command)
    {
        return new Grid
        {
            Padding = new Thickness(16, 16, 16, 6),
            Children =
            {
                new Button
                {
                    Text = text,
                    Command = command,
                    BackgroundColor = Colors.Transparent,
                    TextColor = CustomerUi.Dark,
                    HorizontalOptions = LayoutOptions.Start,
                    WidthRequest = 70,
                    HeightRequest = 40
                }
            }
        };
    }
}

public sealed class CallingEmergencyPage : CustomerPageBase, IQueryAttributable
{
    private readonly CallingEmergencyViewModel _viewModel = new();
    private IDispatcherTimer? _timer;
    private int _requestId;

    public CallingEmergencyPage()
    {
        Title = "Calling emergency";
        BindingContext = _viewModel;
        Render();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("requestId", out var value) &&
            int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var requestId))
        {
            _requestId = requestId;
            EmergencyFlowState.RequestId = requestId;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartTimer();
    }

    protected override void OnDisappearing()
    {
        _timer?.Stop();
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = ConfirmCancelAsync();
        return true;
    }

    private void StartTimer()
    {
        if (_timer is not null)
        {
            _timer.Start();
            return;
        }

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(3);
        _timer.Tick += async (_, _) =>
        {
            _viewModel.ElapsedSeconds += 3;
            await PollAsync();
        };
        _timer.Start();
        _ = PollAsync();
    }

    private async Task PollAsync()
    {
        if (_requestId <= 0 || _viewModel.IsBusy)
        {
            return;
        }

        try
        {
            _viewModel.IsBusy = true;
            _viewModel.Status = await EmergencyService.GetStatusAsync(_requestId);
            EmergencyFlowState.Status = _viewModel.Status;
            Render();
        }
        catch (Exception ex)
        {
            if (_viewModel.Status is not null)
            {
                _viewModel.Status = _viewModel.Status with { Message = $"Connection lost. Retrying... {ex.Message}" };
            }
            Render();
        }
        finally
        {
            _viewModel.IsBusy = false;
        }
    }

    private void Render()
    {
        var root = new VerticalStackLayout
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(24, 18, 24, 24),
            Spacing = 18
        };

        root.Add(new Label
        {
            Text = "Calling emergency...",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = CustomerUi.Dark,
            HorizontalTextAlignment = TextAlignment.Center
        });
        root.Add(new Label
        {
            Text = "Please stand by while BikeMate connects you with nearby available technicians and support.",
            FontSize = 13,
            TextColor = CustomerUi.Muted,
            HorizontalTextAlignment = TextAlignment.Center
        });
        root.Add(BuildRadar());
        root.Add(Label(_viewModel.Status?.Message ?? "Creating emergency request...", 12, CustomerUi.Muted));

        var buttons = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        var isClosed = CustomerRequestRules.IsClosedStatus(_viewModel.Status?.Status);
        buttons.Add(GhostButton(isClosed ? "Return Home" : "Cancel Request", new Command(async () =>
        {
            if (isClosed)
            {
                await Shell.Current.GoToAsync("//CustomerHomePage");
                return;
            }

            await ConfirmCancelAsync();
        })), 0, 0);
        buttons.Add(OrangeButton(isClosed
                ? "Emergency Completed"
                : _viewModel.Status?.AssignedMechanicId is null ? "Open Support Call" : "Track Responder",
            new Command(async () =>
            {
                if (CustomerRequestRules.IsClosedStatus(_viewModel.Status?.Status))
                {
                    await DisplayAlertAsync("Emergency completed", "Responder tracking is no longer available after emergency roadside help is completed.", "OK");
                    await Shell.Current.GoToAsync("//CustomerHomePage");
                    return;
                }

                var route = _viewModel.Status?.AssignedMechanicId is null
                    ? nameof(EmergencyLiveCallPage)
                    : nameof(ActiveEmergencyTrackingPage);
                await Shell.Current.GoToAsync($"{route}?requestId={_requestId}");
            })), 1, 0);
        root.Add(buttons);

        SetScaffold(new ScrollView { Content = root }, "Home", false);
    }

    private View BuildRadar()
    {
        var grid = new Grid { HeightRequest = 330 };
        grid.Add(Circle(290, Color.FromArgb("#FFE3D6"), 0.55));
        grid.Add(Circle(220, Color.FromArgb("#FFB59A"), 0.65));
        grid.Add(Circle(145, CustomerUi.Orange, 0.95));
        grid.Add(new Label
        {
            Text = Math.Max(1, _viewModel.Status?.NearbyResponders.Count ?? 1).ToString("00", CultureInfo.InvariantCulture),
            TextColor = Colors.White,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        });

        var responders = (_viewModel.Status?.NearbyResponders ?? []).Take(5).ToArray();
        var positions = new[]
        {
            new Thickness(30, 18, 0, 0),
            new Thickness(212, 46, 0, 0),
            new Thickness(34, 238, 0, 0),
            new Thickness(220, 232, 0, 0),
            new Thickness(132, 0, 0, 0)
        };

        for (var i = 0; i < responders.Length; i++)
        {
            grid.Add(ResponderBadge(responders[i], positions[i]));
        }

        return grid;
    }

    private static View ResponderBadge(NearbyResponderDto responder, Thickness margin)
    {
        var stack = new VerticalStackLayout { Spacing = 3, Margin = margin, WidthRequest = 78 };
        stack.Add(Avatar(EmergencyUi.InitialsFromName(responder.FullName), 46, Colors.White));
        stack.Add(new Label
        {
            Text = responder.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Responder",
            FontSize = 11,
            TextColor = CustomerUi.Dark,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        });
        return stack;
    }

    private static Border Circle(double size, Color color, double opacity)
    {
        return new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            Opacity = opacity,
            BackgroundColor = color,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = size / 2 },
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
    }

    private async Task ConfirmCancelAsync()
    {
        var cancel = await DisplayAlertAsync("Cancel emergency request?", "Only cancel if you no longer need help.", "Cancel Request", "Keep Waiting");
        if (!cancel)
        {
            return;
        }

        try
        {
            if (_requestId > 0)
            {
                await EmergencyService.CancelAsync(_requestId);
            }

            await Shell.Current.GoToAsync("//CustomerHomePage");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Cancel failed", ex.Message, "OK");
        }
    }
}

public sealed class EmergencyLiveCallPage : CustomerPageBase, IQueryAttributable
{
    private readonly EmergencyLiveCallViewModel _viewModel = new();
    private readonly IEmergencyCallService _callService =
        Application.Current?.Handler?.MauiContext?.Services.GetService<IEmergencyCallService>() ?? new EmergencyCallService();
    private int _requestId;
    private bool _started;
    private Button? _cameraButton;
    private Button? _muteButton;
    private Button? _speakerButton;

    public EmergencyLiveCallPage()
    {
        Title = "Emergency call";
        BindingContext = _viewModel;
        Render();
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
        if (_callService.IsCallActive && _callService.ActiveRequestId == _requestId)
        {
            _started = true;
            _viewModel.IsBusy = false;
            _viewModel.StatusMessage = "Emergency call is active. Keep this screen open or use Track to check responder location.";
            Render();
            return;
        }

        if (_started || _requestId <= 0)
        {
            return;
        }

        _started = true;
        _viewModel.IsBusy = true;
        _viewModel.StatusMessage = "Preparing secure Agora emergency session...";
        Render();
        try
        {
            var session = await EmergencyService.StartCallAsync(_requestId);
            _viewModel.StatusMessage = session.Message;
            await _callService.StartCallAsync(session);
            _viewModel.StatusMessage = "Connected to BikeMate Support. Keep this screen open while help is being coordinated.";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Emergency call join failed: {ex}");
            _viewModel.StatusMessage = ex.Message;
        }
        finally
        {
            _viewModel.IsBusy = false;
        }

        Render();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = ShowCallExitOptionsAsync();
        return true;
    }

    private void Render()
    {
        var root = new Grid
        {
            BackgroundColor = Color.FromArgb("#202020"),
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };

        var header = new Grid
        {
            Padding = new Thickness(16, 20, 16, 10),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        header.Add(new Button
        {
            Text = "Track",
            Command = new Command(async () => await MinimizeToTrackingAsync()),
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.White,
            WidthRequest = 64
        }, 0, 0);
        header.Add(new Label
        {
            Text = "Emergency call",
            TextColor = Colors.White,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        }, 1, 0);
        header.Add(new Border
        {
            BackgroundColor = Color.FromArgb("#FF3B30"),
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(12, 5),
            Content = new Label { Text = "Live", TextColor = Colors.White, FontSize = 11, FontAttributes = FontAttributes.Bold }
        }, 2, 0);
        root.Add(header, 0, 0);

        var center = new VerticalStackLayout
        {
            Spacing = 16,
            Padding = new Thickness(28),
            VerticalOptions = LayoutOptions.Center
        };
        center.Add(new Border
        {
            HeightRequest = 330,
            BackgroundColor = Color.FromArgb("#333333"),
            Stroke = Color.FromArgb("#555555"),
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            Content = _callService.CallView ?? new VerticalStackLayout
            {
                Spacing = 12,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    Avatar("BM", 92, CustomerUi.Orange),
                    new Label
                    {
                        Text = "BikeMate Support",
                        TextColor = Colors.White,
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                     new Label
                     {
                         Text = _viewModel.StatusMessage,
                         TextColor = Color.FromArgb("#DDDDDD"),
                        FontSize = 13,
                        HorizontalTextAlignment = TextAlignment.Center,
                         Margin = new Thickness(22, 0)
                     },
                     new ActivityIndicator
                     {
                         IsVisible = _viewModel.IsBusy,
                         IsRunning = _viewModel.IsBusy,
                         Color = Colors.White,
                         HorizontalOptions = LayoutOptions.Center
                     }
                 }
             }
         });
        root.Add(center, 0, 1);

        var controls = new HorizontalStackLayout
        {
            Padding = new Thickness(10, 0, 10, 26),
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center
        };
        _cameraButton = (Button)CallButton(_viewModel.IsCameraEnabled ? "Cam" : "Cam Off", Colors.White, CustomerUi.Dark, async () =>
        {
            _viewModel.IsCameraEnabled = await _callService.ToggleCameraAsync();
            UpdateCallButtons();
        }, !_viewModel.IsBusy);
        controls.Add(_cameraButton);

        _muteButton = (Button)CallButton(_viewModel.IsMuted ? "Muted" : "Mic", Colors.White, CustomerUi.Dark, async () =>
        {
            _viewModel.IsMuted = await _callService.ToggleMuteAsync();
            UpdateCallButtons();
        }, !_viewModel.IsBusy);
        controls.Add(_muteButton);

        _speakerButton = (Button)CallButton(_viewModel.IsSpeakerEnabled ? "Speaker" : "Phone", Colors.White, CustomerUi.Dark, async () =>
        {
            _viewModel.IsSpeakerEnabled = await _callService.ToggleSpeakerAsync();
            UpdateCallButtons();
        }, !_viewModel.IsBusy);
        controls.Add(_speakerButton);
        controls.Add(CallButton("Track", Colors.White, CustomerUi.Dark, MinimizeToTrackingAsync));
        controls.Add(CallButton("End", Color.FromArgb("#FF3B30"), Colors.White, ConfirmLeaveAsync));
        root.Add(controls, 0, 2);

        SetScaffold(root, "Home", false);
    }

    private void UpdateCallButtons()
    {
        if (_cameraButton is not null)
        {
            _cameraButton.Text = _viewModel.IsCameraEnabled ? "Cam" : "Cam Off";
        }

        if (_muteButton is not null)
        {
            _muteButton.Text = _viewModel.IsMuted ? "Muted" : "Mic";
        }

        if (_speakerButton is not null)
        {
            _speakerButton.Text = _viewModel.IsSpeakerEnabled ? "Speaker" : "Phone";
        }
    }

    private static View CallButton(string text, Color background, Color textColor, Func<Task> action, bool isEnabled = true)
    {
        return new Button
        {
            Text = text,
            BackgroundColor = background,
            TextColor = textColor,
            WidthRequest = 58,
            HeightRequest = 54,
            CornerRadius = 27,
            Padding = new Thickness(8, 0),
            FontSize = CustomerUi.CaptionSize,
            FontAttributes = FontAttributes.Bold,
            FontFamily = CustomerUi.FontDisplay,
            IsEnabled = isEnabled,
            Command = new Command(async () => await action())
        };
    }

    private async Task ConfirmLeaveAsync()
    {
        var end = await DisplayAlertAsync("End emergency call?", "The emergency request will stay active after the call ends.", "End Call", "Stay");
        if (!end)
        {
            return;
        }

        try
        {
            await _callService.EndCallAsync(_requestId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Emergency call local end failed: {ex}");
        }

        try
        {
            await EmergencyService.EndCallAsync(_requestId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Emergency API end-call failed for request {_requestId}: {ex}");
        }

        var status = await EmergencyService.GetStatusAsync(_requestId);
        EmergencyFlowState.Status = status;
        if (CustomerRequestRules.IsClosedStatus(status.Status))
        {
            await DisplayAlertAsync("Emergency completed", "Responder tracking is no longer available after emergency roadside help is completed.", "OK");
            await Shell.Current.GoToAsync("//CustomerHomePage");
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(ActiveEmergencyTrackingPage)}?requestId={_requestId}");
    }

    private async Task ShowCallExitOptionsAsync()
    {
        var action = await DisplayActionSheetAsync(
            "Emergency call",
            "Stay",
            null,
            "Keep call active and view tracking",
            "End call");

        if (action == "Keep call active and view tracking")
        {
            await MinimizeToTrackingAsync();
            return;
        }

        if (action == "End call")
        {
            await ConfirmLeaveAsync();
        }
    }

    private async Task MinimizeToTrackingAsync()
    {
        if (_requestId <= 0)
        {
            await Shell.Current.GoToAsync("//CustomerHomePage");
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(ActiveEmergencyTrackingPage)}?requestId={_requestId}");
    }
}

public sealed class EmergencyLocationPickerPage : CustomerPageBase
{
    private readonly EmergencyLocationPickerViewModel _viewModel = new();

    public EmergencyLocationPickerPage()
    {
        Title = "Emergency Location";
        BindingContext = _viewModel;
        var existing = EmergencyFlowState.Location;
        if (existing is not null)
        {
            _viewModel.Address = existing.Address;
            _viewModel.Latitude = existing.Latitude;
            _viewModel.Longitude = existing.Longitude;
        }

        Render();
    }

    private void Render()
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(18, 12, 18, 24),
            Spacing = 14,
            BackgroundColor = Colors.White
        };
        body.Add(Header("Emergency Location"));
        body.Add(Label("Search or use GPS so BikeMate can send help to the right place.", 13, CustomerUi.Muted));

        var search = new Entry
        {
            Placeholder = "Search address, landmark, or nearby place",
            Text = _viewModel.Address,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            BackgroundColor = Colors.Transparent
        };
        search.TextChanged += (_, e) => _viewModel.Address = e.NewTextValue ?? "";
        body.Add(Card(search, Colors.White, 10, new Thickness(12, 2)));
        body.Add(BuildMapPreview());

        var actions = new Grid { ColumnSpacing = 10 };
        actions.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actions.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actions.Add(GhostButton("Use GPS", new Command(async () => await UseCurrentLocationAsync())), 0, 0);
        actions.Add(GhostButton("Search Map", new Command(async () => await SearchAddressAsync())), 1, 0);
        body.Add(actions);
        body.Add(OrangeButton("Confirm Location", new Command(async () => await SaveAsync())));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private View BuildMapPreview()
    {
        var latitude = _viewModel.Latitude;
        var longitude = _viewModel.Longitude;
        var grid = new Grid
        {
            HeightRequest = 250,
            BackgroundColor = Color.FromArgb("#ECEFF3")
        };

        if (latitude != 0m && longitude != 0m)
        {
            grid.Add(new WebView
            {
                Source = BookingVisuals.GoogleMapSource(latitude, longitude),
                HeightRequest = 250
            });
        }
        else
        {
            grid.Add(new Label
            {
                Text = "Use GPS or search an address to preview the emergency location.",
                TextColor = CustomerUi.Muted,
                FontSize = 13,
                FontFamily = CustomerUi.FontBody,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(18)
            });
        }

        grid.Add(new Label
        {
            Text = string.IsNullOrWhiteSpace(_viewModel.Address) ? "No location selected" : _viewModel.Address,
            TextColor = CustomerUi.Dark,
            BackgroundColor = Color.FromRgba(255, 255, 255, 0.9),
            FontSize = 11,
            FontFamily = CustomerUi.FontCaptionBold,
            Padding = new Thickness(10, 6),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(12)
        });

        return Card(grid, Colors.White, 10, new Thickness(0));
    }

    private async Task UseCurrentLocationAsync()
    {
        var location = await LocationService.GetCurrentLocationAsync(this);
        if (location is null || location.Latitude == 0m)
        {
            await DisplayAlertAsync("Location unavailable", location?.ErrorMessage ?? "No GPS location was returned.", "OK");
            return;
        }

        _viewModel.Address = location.Address;
        _viewModel.Latitude = location.Latitude;
        _viewModel.Longitude = location.Longitude;
        Render();
    }

    private async Task SearchAddressAsync()
    {
        if (string.IsNullOrWhiteSpace(_viewModel.Address))
        {
            await DisplayAlertAsync("Search needed", "Enter an address, landmark, or nearby place first.", "OK");
            return;
        }

        try
        {
            var matches = await Geocoding.Default.GetLocationsAsync(_viewModel.Address.Trim());
            var location = matches?.FirstOrDefault();
            if (location is null)
            {
                await DisplayAlertAsync("Place not found", "BikeMate could not find that place. Try a more specific address or use GPS.", "OK");
                return;
            }

            _viewModel.Latitude = (decimal)location.Latitude;
            _viewModel.Longitude = (decimal)location.Longitude;
            var reverse = await LocationService.ReverseGeocodeAsync(location);
            if (!string.IsNullOrWhiteSpace(reverse))
            {
                _viewModel.Address = reverse;
            }

            Render();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Search unavailable", $"Maps search is unavailable right now. {ex.Message}", "OK");
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_viewModel.Address) || _viewModel.Latitude == 0m || _viewModel.Longitude == 0m)
        {
            await DisplayAlertAsync("Location required", "Use GPS or search the map before confirming this emergency location.", "OK");
            return;
        }

        EmergencyFlowState.Location = new EmergencyLocationSnapshot(
            _viewModel.Latitude,
            _viewModel.Longitude,
            _viewModel.Address.Trim(),
            false,
            null);
        await Shell.Current.GoToAsync("..");
    }
}

public sealed class ActiveEmergencyTrackingPage : CustomerPageBase, IQueryAttributable
{
    private readonly ActiveEmergencyTrackingViewModel _viewModel = new();
    private readonly IEmergencyCallService _callService =
        Application.Current?.Handler?.MauiContext?.Services.GetService<IEmergencyCallService>() ?? new EmergencyCallService();
    private IDispatcherTimer? _timer;
    private int _requestId;
    private bool _timerAttached;

    public ActiveEmergencyTrackingPage()
    {
        Title = "Emergency Tracking";
        BindingContext = _viewModel;
        Render();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("requestId", out var value) &&
            int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var requestId))
        {
            _requestId = requestId;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartPolling();
    }

    protected override void OnDisappearing()
    {
        _timer?.Stop();
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//CustomerHomePage");
        return true;
    }

    private void StartPolling()
    {
        _timer ??= Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(5);
        if (!_timerAttached)
        {
            _timer.Tick += async (_, _) => await PollAsync();
            _timerAttached = true;
        }

        if (!_timer.IsRunning)
        {
            _timer.Start();
        }
        _ = PollAsync();
    }

    private async Task PollAsync()
    {
        if (_requestId <= 0)
        {
            return;
        }

        try
        {
            _viewModel.Status = await EmergencyService.GetStatusAsync(_requestId);
            EmergencyFlowState.Status = _viewModel.Status;
            _viewModel.Banner = "";
            if (CustomerRequestRules.IsClosedStatus(_viewModel.Status.Status))
            {
                _timer?.Stop();
            }
        }
        catch (Exception ex)
        {
            _viewModel.Banner = $"Connection lost. Retrying... {ex.Message}";
        }

        Render();
    }

    private void Render()
    {
        var status = _viewModel.Status ?? EmergencyFlowState.Status;
        var isClosed = CustomerRequestRules.IsClosedStatus(status?.Status);
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(18, 12, 18, 24),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(Header(isClosed ? "Emergency Complete" : "Emergency Tracking", true));
        if (!string.IsNullOrWhiteSpace(_viewModel.Banner))
        {
            body.Add(Card(Label(_viewModel.Banner, 11, CustomerUi.Muted), Colors.White, 10));
        }

        body.Add(isClosed ? ClosedStatusCard(status) : BuildMap(status));
        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Label(status?.Status ?? "Loading", 20, CustomerUi.Dark, FontAttributes.Bold),
                Label(status?.Message ?? "Loading emergency request status...", 12, CustomerUi.Muted),
                Separator(),
                Label($"Responder: {status?.AssignedMechanicName ?? "Searching"}", 13, CustomerUi.Dark),
                Label($"Phone: {status?.AssignedMechanicPhone ?? "Not assigned"}", 12, CustomerUi.Muted),
                Label($"Location: {status?.ServiceLocation ?? "Current location"}", 12, CustomerUi.Muted),
                Label("Payment: No upfront payment required", 11, Color.FromArgb("#167A3A"), FontAttributes.Bold)
            }
        }, Colors.White, 12));

        var actions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        actions.Add(GhostButton("BikeMate Emergency chat", new Command(async () => await OpenEmergencyChatAsync())), 0, 0);
        actions.Add(OrangeButton(isClosed ? "Return Home" : "Open Maps", new Command(async () =>
        {
            if (isClosed)
            {
                await Shell.Current.GoToAsync("//CustomerHomePage");
                return;
            }

            if (status is not null)
            {
                if (status.MechanicLatitude is not null && status.MechanicLongitude is not null)
                {
                    await BookingVisuals.OpenGoogleDirectionsAsync(
                        status.MechanicLatitude.Value,
                        status.MechanicLongitude.Value,
                        status.CustomerLatitude,
                        status.CustomerLongitude);
                }
                else
                {
                    await BookingVisuals.OpenGoogleMapsAsync($"{status.CustomerLatitude.ToString(CultureInfo.InvariantCulture)},{status.CustomerLongitude.ToString(CultureInfo.InvariantCulture)}");
                }
            }
        })), 1, 0);
        body.Add(actions);

        if (!isClosed && status?.Status is "EmergencyPending" or "SearchingResponder" or "CallConnecting" or "CallConnected")
        {
            if (_callService.IsCallActive && _callService.ActiveRequestId == _requestId)
            {
                body.Add(OrangeButton("Return to emergency call", new Command(async () => await ReturnToCallAsync())));
            }

            body.Add(new Button
            {
                Text = "Cancel Emergency Request",
                BackgroundColor = Color.FromArgb("#FF3B30"),
                TextColor = Colors.White,
                CornerRadius = 12,
                HeightRequest = 48,
                Command = new Command(async () => await CancelAsync())
            });
        }

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private async Task ReturnToCallAsync()
    {
        var stack = Shell.Current.Navigation.NavigationStack;
        var callPage = stack.LastOrDefault(page => page is EmergencyLiveCallPage);
        if (callPage is not null)
        {
            while (Shell.Current.Navigation.NavigationStack.LastOrDefault() != callPage)
            {
                await Shell.Current.Navigation.PopAsync(false);
            }

            return;
        }

        await Shell.Current.GoToAsync($"{nameof(EmergencyLiveCallPage)}?requestId={_requestId}");
    }

    private static View ClosedStatusCard(EmergencyRequestStatusDto? status)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(Label(
            status?.Status == "Cancelled" ? "Emergency request cancelled" : "Emergency roadside help completed",
            15,
            CustomerUi.Dark,
            FontAttributes.Bold));
        stack.Add(Label(
            status?.Status == "Cancelled"
                ? "This emergency request is no longer active, so responder tracking is unavailable."
                : "This emergency request is already complete. Live responder tracking is no longer available.",
            12,
            CustomerUi.Muted));
        if (status is not null)
        {
            stack.Add(Separator());
            stack.Add(Label($"Location: {status.ServiceLocation}", 12, CustomerUi.Muted));
            stack.Add(Label($"Responder: {status.AssignedMechanicName ?? "Not assigned"}", 12, CustomerUi.Muted));
        }

        return Card(stack, Colors.White, 12);
    }

    private async Task OpenEmergencyChatAsync()
    {
        if (_requestId <= 0)
        {
            await DisplayAlertAsync("Emergency chat unavailable", "The emergency request is still being prepared.", "OK");
            return;
        }

        try
        {
            var conversation = await EmergencyService.GetConversationAsync(_requestId);
            await Shell.Current.Navigation.PushAsync(new CustomerChatPage(conversation.ConversationId));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Emergency chat unavailable", ex.Message, "OK");
        }
    }

    private static View BuildMap(EmergencyRequestStatusDto? status)
    {
        var grid = new Grid
        {
            HeightRequest = 260,
            BackgroundColor = Color.FromArgb("#ECEFF3")
        };

        if (status is null)
        {
            grid.Add(new Label
            {
                Text = "Loading emergency map...",
                TextColor = CustomerUi.Dark,
                FontSize = 13,
                FontFamily = CustomerUi.FontBody,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            });
            return Card(grid, Colors.White, 12, new Thickness(0));
        }

        var hasResponderLocation = status.MechanicLatitude is not null && status.MechanicLongitude is not null;
        grid.Add(new WebView
        {
            Source = hasResponderLocation
                ? BookingVisuals.GoogleDirectionsSource(
                    status.MechanicLatitude!.Value,
                    status.MechanicLongitude!.Value,
                    status.CustomerLatitude,
                    status.CustomerLongitude)
                : BookingVisuals.GoogleMapSource(status.CustomerLatitude, status.CustomerLongitude),
            HeightRequest = 260
        });
        grid.Add(new Label
        {
            Text = hasResponderLocation ? "Responder route to you" : "Waiting for responder location",
            TextColor = hasResponderLocation ? Color.FromArgb("#167A3A") : Color.FromArgb("#503CFF"),
            BackgroundColor = Color.FromRgba(255, 255, 255, 0.88),
            FontSize = 13,
            FontFamily = CustomerUi.FontCaptionBold,
            Padding = new Thickness(10, 6),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(10)
        });
        return Card(grid, Colors.White, 12, new Thickness(0));
    }

    private async Task CancelAsync()
    {
        var cancel = await DisplayAlertAsync("Cancel emergency request?", "Only cancel if you no longer need help.", "Cancel Request", "Keep Active");
        if (!cancel)
        {
            return;
        }

        try
        {
            await EmergencyService.CancelAsync(_requestId);
            await Shell.Current.GoToAsync("//CustomerHomePage");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Cancel failed", ex.Message, "OK");
        }
    }
}

internal static class EmergencyUi
{
    public static string InitialsFromName(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "BM";
        }

        return string.Concat(parts.Take(2).Select(x => x[0])).ToUpperInvariant();
    }
}
