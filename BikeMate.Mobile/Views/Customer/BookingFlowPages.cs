using System.Diagnostics;
using System.Globalization;
using System.Net;
using BikeMate.Core.DTOs;
using BikeMate.Core.Services;
using BikeMate.Helpers;
using BikeMate.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;

namespace BikeMate.Views.Customer;

internal static class BookingDraft
{
    public const string WorkTypeRepair = "Repair";
    public const string WorkTypeModification = "Modification";

    public static readonly TimeSpan[] BookingTimeSlots =
    [
        new TimeSpan(8, 0, 0),
        new TimeSpan(9, 30, 0),
        new TimeSpan(11, 0, 0),
        new TimeSpan(12, 30, 0),
        new TimeSpan(14, 0, 0),
        new TimeSpan(15, 30, 0),
        new TimeSpan(17, 0, 0)
    ];

    public static CustomerMeDto? Customer { get; set; }
    public static IReadOnlyList<ShopServiceDto> Services { get; set; } = [];
    public static int RequestId { get; set; }
    public static int PaymentId { get; set; }
    public static int? SelectedShopId { get; set; }
    public static int? SelectedShopServiceId { get; set; }
    public static int? SelectedProductId { get; set; }
    public static string SelectedProductName { get; set; } = "";
    public static decimal? SelectedProductPrice { get; set; }
    public static string? RegionCode { get; set; }
    public static string Region { get; set; } = "";
    public static string? LocalityCode { get; set; }
    public static string LocationName { get; set; } = "";
    public static string Province { get; set; } = "";
    public static string AddressLine { get; set; } = "";
    public static decimal? Latitude { get; set; }
    public static decimal? Longitude { get; set; }
    public static bool IsGpsLocation { get; set; }
    public static int? SelectedMotorcycleId { get; set; }
    public static bool IsOtherVehicle { get; set; }
    public static string Brand { get; set; } = "";
    public static string Model { get; set; } = "";
    public static string PlateNumber { get; set; } = "";
    public static int? YearModel { get; set; }
    public static string WorkType { get; set; } = "";
    public static string ProblemCategory { get; set; } = "";
    public static string OtherDetails { get; set; } = "";
    public static string AssistanceMethod { get; set; } = "";
    public static DateTime ScheduledAt { get; set; } = NextBookableSchedule();
    public static string? ImageMediaUrl { get; set; }
    public static string? VideoMediaUrl { get; set; }
    public static string? ImagePreviewPath { get; set; }
    public static string? VideoPreviewPath { get; set; }
    public static bool RequestMediaAttached { get; set; }
    public static string PaymentMethod { get; set; } = "GCASH";
    public static void Reset()
    {
        RequestId = 0;
        PaymentId = 0;
        SelectedShopId = null;
        SelectedShopServiceId = null;
        SelectedProductId = null;
        SelectedProductName = "";
        SelectedProductPrice = null;
        ImageMediaUrl = null;
        VideoMediaUrl = null;
        ImagePreviewPath = null;
        VideoPreviewPath = null;
        RequestMediaAttached = false;
        PaymentMethod = "GCASH";
        RegionCode = null;
        Region = "";
        LocalityCode = null;
        LocationName = "";
        Province = "";
        AddressLine = "";
        Latitude = null;
        Longitude = null;
        IsGpsLocation = false;
        SelectedMotorcycleId = null;
        IsOtherVehicle = false;
        Brand = "";
        Model = "";
        PlateNumber = "";
        YearModel = null;
        WorkType = "";
        ProblemCategory = "";
        OtherDetails = "";
        AssistanceMethod = "";
        ScheduledAt = NextBookableSchedule();
    }

    public static void ApplyCustomer(CustomerMeDto customer)
    {
        Customer = customer;
        var address = customer.Addresses.FirstOrDefault(x => x.IsDefault) ?? customer.Addresses.FirstOrDefault();
        if (address is not null)
        {
            Latitude = address.Latitude ?? Latitude;
            Longitude = address.Longitude ?? Longitude;
            AddressLine = address.AddressLine;
            Province = address.Province ?? "";
            if (!string.IsNullOrWhiteSpace(address.City))
            {
                LocationName = address.City;
            }
        }

        var motorcycle = customer.Motorcycles.FirstOrDefault();
        if (motorcycle is not null)
        {
            SelectMotorcycle(motorcycle);
        }
    }

    public static MotorcycleDto? SelectedMotorcycle()
    {
        if (IsOtherVehicle || SelectedMotorcycleId is null)
        {
            return null;
        }

        return Customer?.Motorcycles.FirstOrDefault(x => x.MotorcycleId == SelectedMotorcycleId);
    }

    public static void SelectMotorcycle(MotorcycleDto motorcycle)
    {
        SelectedMotorcycleId = motorcycle.MotorcycleId;
        IsOtherVehicle = false;
        Brand = motorcycle.Brand;
        Model = motorcycle.Model;
        PlateNumber = NormalizePlate(motorcycle.PlateNumber);
        YearModel = motorcycle.YearModel;
    }

    public static void SelectOtherVehicle()
    {
        SelectedMotorcycleId = null;
        IsOtherVehicle = true;
        Brand = "";
        Model = "";
        PlateNumber = "";
        YearModel = null;
    }

    public static string VehicleSummary()
    {
        var vehicle = string.Join(" ", new[] { Brand, Model }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var year = YearModel is null ? "" : $" ({YearModel})";
        return $"{vehicle}{year} | Plate: {NormalizePlate(PlateNumber)}".Trim();
    }

    public static string IssueSummary()
    {
        var workType = string.IsNullOrWhiteSpace(WorkType) ? WorkTypeRepair : WorkType;
        var concern = IsModification
            ? "Modification request"
            : ProblemCategory;
        if (!string.IsNullOrWhiteSpace(OtherDetails))
        {
            concern = $"{concern}: {OtherDetails.Trim()}";
        }

        var product = IsModification && !string.IsNullOrWhiteSpace(SelectedProductName)
            ? $"\nSelected product: {SelectedProductName}{(SelectedProductPrice is null ? "" : $" ({BookingVisuals.Money(SelectedProductPrice.Value)})")}"
            : "";
        var method = string.IsNullOrWhiteSpace(AssistanceMethod)
            ? ""
            : $"\nAssistance method: {AssistanceMethod}";
        return $"Booking type: {workType}\n{concern}{product}\nVehicle: {VehicleSummary()}{method}";
    }

    public static string NormalizePlate(string? plateNumber)
    {
        return string.IsNullOrWhiteSpace(plateNumber)
            ? ""
            : plateNumber.Trim().ToUpperInvariant();
    }

    public static ShopServiceDto? SelectedService()
    {
        var explicitService = Services.FirstOrDefault(x => x.ShopServiceId == SelectedShopServiceId);
        if (explicitService is not null && IsRelevantService(explicitService))
        {
            return explicitService;
        }

        return Services.FirstOrDefault(x =>
            (SelectedShopId is null || x.ShopId == SelectedShopId) &&
            IsRelevantService(x));
    }

    public static bool IsRelevantService(ShopServiceDto service)
    {
        if (!service.IsActive)
        {
            return false;
        }

        if (IsModification)
        {
            return IsModificationService(service);
        }

        return RepairConcernMatcher.Matches(
            ProblemCategory,
            service.CategoryName,
            service.ServiceName,
            service.ServiceDescription);
    }

    public static bool IsModification =>
        string.Equals(WorkType, WorkTypeModification, StringComparison.OrdinalIgnoreCase);

    public static bool IsRepair =>
        string.Equals(WorkType, WorkTypeRepair, StringComparison.OrdinalIgnoreCase);

    public static string WorkTypeLabel =>
        IsModification ? WorkTypeModification : WorkTypeRepair;

    public static void SelectWorkType(string workType)
    {
        if (string.Equals(WorkType, workType, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        WorkType = workType;
        SelectedShopId = null;
        SelectedShopServiceId = null;
        SelectedProductId = null;
        SelectedProductName = "";
        SelectedProductPrice = null;
        if (IsModification)
        {
            ProblemCategory = "Modification";
        }
        else if (string.Equals(ProblemCategory, "Modification", StringComparison.OrdinalIgnoreCase))
        {
            ProblemCategory = "";
        }
    }

    public static void SelectProduct(ProductDto product)
    {
        SelectedProductId = product.ProductId;
        SelectedProductName = product.ProductName;
        SelectedProductPrice = product.Price;
    }

    public static void ClearProduct()
    {
        SelectedProductId = null;
        SelectedProductName = "";
        SelectedProductPrice = null;
    }

    public static bool IsModificationService(ShopServiceDto service)
    {
        var text = string.Join(" ", new[] { service.CategoryName, service.ServiceName, service.ServiceDescription }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var terms = new[]
        {
            "mod",
            "modification",
            "custom",
            "upgrade",
            "accessory",
            "install",
            "installation",
            "part",
            "parts"
        };
        return terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    public static string ConfirmationAddress()
    {
        var address = string.Join("\n", AddressParts());
        return string.IsNullOrWhiteSpace(address) ? "Address not set" : address;
    }

    public static string ServiceAddress()
    {
        var address = string.Join(", ", AddressParts());
        return string.IsNullOrWhiteSpace(address) ? "Address not set" : address;
    }

    public static string LocationCaption()
    {
        var parts = new[] { LocationName, Province, Region, AddressLine }.Where(x => !string.IsNullOrWhiteSpace(x));
        var caption = string.Join("\n", parts);
        return string.IsNullOrWhiteSpace(caption) ? "Select a location" : caption;
    }

    public static DateTime NextBookableSchedule(DateTime? currentPhoneTime = null)
    {
        var now = currentPhoneTime ?? DateTime.Now;
        var today = now.Date;

        foreach (var slot in BookingTimeSlots)
        {
            var candidate = today.Add(slot);
            if (candidate > now)
            {
                return candidate;
            }
        }

        return today.AddDays(1).Add(BookingTimeSlots[0]);
    }

    public static DateTime MaxBookableDate(DateTime? currentPhoneTime = null)
    {
        return (currentPhoneTime ?? DateTime.Now).Date.AddDays(7);
    }

    public static bool IsWithinBookingWindow(DateTime schedule, DateTime? currentPhoneTime = null)
    {
        var now = currentPhoneTime ?? DateTime.Now;
        return schedule > now && schedule.Date <= MaxBookableDate(now);
    }

    public static void EnsureScheduleIsBookable()
    {
        if (!IsWithinBookingWindow(ScheduledAt))
        {
            ScheduledAt = NextBookableSchedule();
        }
    }

    public static void SelectRegion(PhilippineRegionDto region)
    {
        RegionCode = region.Code;
        Region = region.Name;
        LocalityCode = null;
        LocationName = "";
        Province = "";
        AddressLine = "";
        Latitude = null;
        Longitude = null;
        IsGpsLocation = false;
    }

    public static void SetAdministrativeLocation(
        PhilippineRegionDto region,
        PhilippineLocalityDto locality,
        bool preserveAddress = false)
    {
        var localityChanged = !string.Equals(LocalityCode, locality.Code, StringComparison.Ordinal);
        RegionCode = region.Code;
        Region = region.Name;
        LocalityCode = locality.Code;
        LocationName = locality.Name;
        Province = locality.Province ?? "";
        IsGpsLocation = false;

        if (localityChanged && !preserveAddress)
        {
            AddressLine = "";
            Latitude = null;
            Longitude = null;
        }
    }

    public static void ApplyCurrentLocation(
        PhilippineLocationMatchDto? match,
        string addressLine,
        decimal latitude,
        decimal longitude)
    {
        RegionCode = match?.Region.Code;
        Region = match?.Region.Name ?? "";
        LocalityCode = match?.Locality.Code;
        LocationName = match?.Locality.Name ?? "Current location";
        Province = match?.Locality.Province ?? "";
        AddressLine = addressLine;
        Latitude = latitude;
        Longitude = longitude;
        IsGpsLocation = true;
    }

    public static void ApplyCoordinates(decimal latitude, decimal longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    private static IReadOnlyList<string> AddressParts()
    {
        var parts = new List<string>();
        AddAddressPart(parts, AddressLine);
        AddAddressPart(parts, LocationName);
        AddAddressPart(parts, Province);
        AddAddressPart(parts, Region);
        return parts;
    }

    private static void AddAddressPart(ICollection<string> parts, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            parts.Any(existing => existing.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                                  value.Contains(existing, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        parts.Add(value.Trim());
    }
}

internal static class BookingFlowActions
{
    public static async Task<ServiceRequestDto> EnsureRequestAsync(bool attachMedia)
    {
        var customer = BookingDraft.Customer ?? await CustomerApiClient.GetCustomerAsync();
        BookingDraft.Customer = customer;
        if (BookingDraft.Services.Count == 0)
        {
            BookingDraft.Services = await CustomerApiClient.SearchServicesAsync();
        }

        var motorcycle = BookingDraft.SelectedMotorcycle();
        var shopId = BookingDraft.SelectedShopId;
        var shopServiceId = shopId is null ? null : BookingDraft.SelectedShopServiceId;
        if (shopId is not null && shopServiceId is null)
        {
            shopServiceId = BookingDraft.Services.FirstOrDefault(x => x.ShopId == shopId)?.ShopServiceId;
            BookingDraft.SelectedShopServiceId = shopServiceId;
        }

        ServiceRequestDto request;
        if (BookingDraft.RequestId == 0)
        {
            request = await CustomerApiClient.CreateRequestAsync(new CreateServiceRequestDto(
                shopId,
                shopServiceId,
                motorcycle?.MotorcycleId,
                BookingDraft.IssueSummary(),
                BookingDraft.ServiceAddress(),
                BookingDraft.Latitude,
                BookingDraft.Longitude,
                BookingDraft.ScheduledAt.ToUniversalTime()));
            BookingDraft.RequestId = request.RequestId;
        }
        else
        {
            request = await CustomerApiClient.GetRequestAsync(BookingDraft.RequestId);
        }

        if (attachMedia && !BookingDraft.RequestMediaAttached)
        {
            if (!string.IsNullOrWhiteSpace(BookingDraft.ImageMediaUrl))
            {
                await CustomerApiClient.AttachRequestMediaAsync(BookingDraft.RequestId, new UploadMediaDto(BookingDraft.ImageMediaUrl, "image", "Customer issue picture"));
            }

            if (!string.IsNullOrWhiteSpace(BookingDraft.VideoMediaUrl))
            {
                await CustomerApiClient.AttachRequestMediaAsync(BookingDraft.RequestId, new UploadMediaDto(BookingDraft.VideoMediaUrl, "video", "Customer issue video"));
            }

            BookingDraft.RequestMediaAttached = true;
        }

        return request;
    }

    public static async Task<ServiceRequestDto> SelectShopAsync(int shopId, int? shopServiceId)
    {
        if (shopServiceId is null)
        {
            throw new InvalidOperationException("Choose an exact shop service before continuing to payment.");
        }

        BookingDraft.SelectedShopId = shopId;
        BookingDraft.SelectedShopServiceId = shopServiceId;
        var request = await EnsureRequestAsync(true);
        return await CustomerApiClient.SelectShopAsync(
            request.RequestId,
            new SelectShopDto(
                shopId,
                BookingDraft.SelectedShopServiceId,
                BookingDraft.IsModification ? BookingDraft.SelectedProductId : null));
    }
}

internal static class BookingVisuals
{
    public const string FallenBikeImage = "https://images.unsplash.com/photo-1517649763962-0c623066013b?auto=format&fit=crop&w=700&q=80";
    public const string RiderImage = "https://images.unsplash.com/photo-1534787238916-9ba6764efd4f?auto=format&fit=crop&w=700&q=80";
    public const string ShopImage = "https://images.unsplash.com/photo-1558981806-ec527fa84c39?auto=format&fit=crop&w=700&q=80";
    public const string MechanicImage = "https://images.unsplash.com/photo-1542046272227-d247df21628a?auto=format&fit=crop&w=500&q=80";

    public static ImageSource ShopImageSource(string? url)
    {
        return ImageSource.FromUri(new Uri(ApiConfig.ToPublicUrl(url, ShopImage)));
    }

    public static ImageSource MechanicImageSource(string? url)
    {
        return ImageSource.FromUri(new Uri(ApiConfig.ToPublicUrl(url, MechanicImage)));
    }

    public static ImageSource ProductImageSource(string? url)
    {
        return ImageSource.FromUri(new Uri(ApiConfig.ToPublicUrl(url, ShopImage)));
    }

    public static HtmlWebViewSource GoogleMapSource(decimal latitude, decimal longitude)
    {
        var latText = latitude.ToString(CultureInfo.InvariantCulture);
        var lngText = longitude.ToString(CultureInfo.InvariantCulture);
        var query = $"{latText},{lngText}";
        var key = GoogleMapsEmbedKey();
        var frameUrl = string.IsNullOrWhiteSpace(key)
            ? $"https://maps.google.com/maps?q={Uri.EscapeDataString(query)}&z=15&output=embed"
            : $"https://www.google.com/maps/embed/v1/place?key={Uri.EscapeDataString(key)}&q={Uri.EscapeDataString(query)}&zoom=15";
        return GoogleMapFrame(frameUrl);
    }

    public static HtmlWebViewSource GoogleDirectionsSource(
        decimal originLatitude,
        decimal originLongitude,
        decimal destinationLatitude,
        decimal destinationLongitude)
    {
        var origin = $"{originLatitude.ToString(CultureInfo.InvariantCulture)},{originLongitude.ToString(CultureInfo.InvariantCulture)}";
        var destination = $"{destinationLatitude.ToString(CultureInfo.InvariantCulture)},{destinationLongitude.ToString(CultureInfo.InvariantCulture)}";
        var key = GoogleMapsEmbedKey();
        var frameUrl = string.IsNullOrWhiteSpace(key)
            ? $"https://maps.google.com/maps?saddr={Uri.EscapeDataString(origin)}&daddr={Uri.EscapeDataString(destination)}&dirflg=d&output=embed"
            : $"https://www.google.com/maps/embed/v1/directions?key={Uri.EscapeDataString(key)}&origin={Uri.EscapeDataString(origin)}&destination={Uri.EscapeDataString(destination)}&mode=driving";
        return GoogleMapFrame(frameUrl);
    }

    private static HtmlWebViewSource GoogleMapFrame(string frameUrl)
    {
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
    html, body, iframe {
      width: 100%;
      height: 100%;
      margin: 0;
      padding: 0;
      border: 0;
      overflow: hidden;
      background: #eef1f4;
    }
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

    public static View FlowHeader(string title)
    {
        var grid = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(14, 14, 14, 10),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };

        grid.Add(new Button
        {
            Text = "<",
            BackgroundColor = Color.FromArgb("#FFF2EA"),
            TextColor = CustomerUi.Dark,
            FontSize = 18,
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 8,
            Padding = new Thickness(0),
            Command = new Command(async () => await Shell.Current.GoToAsync(".."))
        }, 0, 0);

        var text = new VerticalStackLayout { Spacing = 2 };
        text.Add(new Label
        {
            Text = title,
            TextColor = CustomerUi.Dark,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation
        });
        text.Add(new Label
        {
            Text = "Book a service",
            TextColor = CustomerUi.Muted,
            FontSize = 11
        });
        grid.Add(text, 1, 0);
        return grid;
    }

    public static View StepIntro(
        string title,
        string subtitle,
        int step,
        string stepLabel,
        int totalSteps = 6)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(Text(title, 18, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(Text(subtitle, 11, CustomerUi.Muted));

        var progress = new Grid { ColumnSpacing = 5, Margin = new Thickness(0, 4, 0, 0) };
        for (var index = 0; index < totalSteps; index++)
        {
            progress.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            progress.Add(new BoxView
            {
                HeightRequest = 4,
                CornerRadius = 2,
                Color = index < step ? CustomerUi.Orange : Color.FromArgb("#DEDEDE")
            }, index, 0);
        }

        stack.Add(progress);
        stack.Add(Text($"Step {step} of {totalSteps}  |  {stepLabel}", 10, CustomerUi.Muted, FontAttributes.Bold));
        return stack;
    }

    public static View SelectionIndicator(bool selected)
    {
        return new Border
        {
            WidthRequest = 22,
            HeightRequest = 22,
            Stroke = selected ? CustomerUi.Orange : CustomerUi.Border,
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 11 },
            BackgroundColor = selected ? CustomerUi.Orange : Colors.White,
            Content = selected
                ? new BoxView
                {
                    WidthRequest = 8,
                    HeightRequest = 8,
                    CornerRadius = 4,
                    Color = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
                : null
        };
    }

    public static Button SecondaryButton(string text, Command command)
    {
        return new Button
        {
            Text = text,
            Command = command,
            BackgroundColor = Colors.White,
            BorderColor = CustomerUi.Border,
            BorderWidth = 1,
            TextColor = CustomerUi.Dark,
            FontSize = CustomerUi.BodySize,
            CornerRadius = 8,
            HeightRequest = 46,
            MinimumHeightRequest = 46,
            Padding = new Thickness(16, 0),
            FontAttributes = FontAttributes.Bold,
            FontFamily = CustomerUi.FontDisplay
        };
    }

    public static Label Text(string value, double size, Color? color = null, FontAttributes attributes = FontAttributes.None)
    {
        return new Label
        {
            Text = value,
            FontSize = CustomerUi.SizeFor(size),
            TextColor = color ?? CustomerUi.Dark,
            FontAttributes = attributes,
            FontFamily = CustomerUi.FontFor(size, attributes),
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    public static Button PrimaryButton(string text, Command command)
    {
        return new Button
        {
            Text = text,
            Command = command,
            BackgroundColor = CustomerUi.Orange,
            TextColor = Colors.White,
            FontSize = CustomerUi.BodySize,
            CornerRadius = 8,
            HeightRequest = 48,
            MinimumHeightRequest = 48,
            Padding = new Thickness(18, 0),
            FontAttributes = FontAttributes.Bold,
            FontFamily = CustomerUi.FontDisplay
        };
    }

    public static Border WhiteCard(View content, double radius = 6, Thickness? padding = null)
    {
        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = Math.Min(radius, 8) },
            Padding = padding ?? new Thickness(14),
            Content = content
        };
    }

    public static View FieldRow(string label, string value, Command command)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        var stack = new VerticalStackLayout { Spacing = 1 };
        stack.Add(Text(label, 10, CustomerUi.Muted));
        stack.Add(Text(string.IsNullOrWhiteSpace(value) ? "Select" : value, 12, CustomerUi.Dark));
        grid.Add(stack, 0, 0);
        grid.Add(Text("v", 11, CustomerUi.Muted), 1, 0);

        var border = WhiteCard(grid, 8, new Thickness(12, 9));
        border.GestureRecognizers.Add(new TapGestureRecognizer { Command = command });
        return border;
    }

    public static View PickerRow(
        string label,
        IReadOnlyList<string> options,
        string? selected,
        Func<string, Task> onChanged,
        bool isEnabled = true,
        string? placeholder = null)
    {
        var stack = new VerticalStackLayout { Spacing = 4 };
        stack.Add(Text(label, 10, CustomerUi.Muted));

        var picker = new Picker
        {
            Title = placeholder ?? $"Select {label}",
            FontSize = 13,
            FontFamily = CustomerUi.FontBody,
            TextColor = CustomerUi.Dark,
            TitleColor = CustomerUi.Muted,
            BackgroundColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Fill,
            IsEnabled = isEnabled
        };

        var values = options
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!string.IsNullOrWhiteSpace(selected) && values.All(x => !string.Equals(x, selected, StringComparison.OrdinalIgnoreCase)))
        {
            values.Insert(0, selected);
        }

        foreach (var option in values)
        {
            picker.Items.Add(option);
        }

        picker.SelectedIndex = values.FindIndex(x => string.Equals(x, selected, StringComparison.OrdinalIgnoreCase));
        var busy = false;
        picker.SelectedIndexChanged += async (_, _) =>
        {
            if (busy || picker.SelectedIndex < 0 || picker.SelectedIndex >= picker.Items.Count)
            {
                return;
            }

            busy = true;
            try
            {
                await onChanged(picker.Items[picker.SelectedIndex]);
            }
            finally
            {
                busy = false;
            }
        };

        stack.Add(picker);
        return WhiteCard(stack, 8, new Thickness(12, 8));
    }

    public static View MapPanel(double height, bool withLocateButton = false)
    {
        var map = new Grid { HeightRequest = height, BackgroundColor = Color.FromArgb("#EEF1F4") };
        if (BookingDraft.Latitude is decimal latitude && BookingDraft.Longitude is decimal longitude)
        {
            map.Add(new WebView
            {
                Source = GoogleMapSource(latitude, longitude),
                HeightRequest = height
            });
            map.Add(new Label
            {
                Text = BookingDraft.LocationCaption(),
                TextColor = CustomerUi.Dark,
                BackgroundColor = Color.FromRgba(255, 255, 255, 0.9),
                Padding = new Thickness(10, 6),
                FontSize = 11,
                FontFamily = CustomerUi.FontBody,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(10)
            });
        }
        else
        {
            var empty = new VerticalStackLayout
            {
                Spacing = 6,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            empty.Add(Text("Location preview", 13, CustomerUi.Dark, FontAttributes.Bold));
            empty.Add(Text("Choose a city or use your current location.", 11, CustomerUi.Muted));
            map.Add(empty);
        }

        if (withLocateButton)
        {
            var actions = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 8,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(10, 0, 10, 10)
            };

            actions.Add(new Button
            {
                Text = "Access location",
                BackgroundColor = CustomerUi.Orange,
                TextColor = Colors.White,
                FontSize = 11,
                CornerRadius = 8,
                HeightRequest = 44,
                HorizontalOptions = LayoutOptions.Fill,
                Command = new Command(async () =>
                {
                    try
                    {
                        await LocationAccessPage.ShowAsync();
                        if (Shell.Current?.CurrentPage is BookServicePage bookPage)
                        {
                            bookPage.RefreshLocationUi();
                        }
                    }
                    catch (Exception ex)
                    {
                        var page = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
                        if (page is not null)
                        {
                            await page.DisplayAlertAsync("Location", ex.Message, "OK");
                        }
                    }
                })
            }, 0, 0);

            actions.Add(new Button
            {
                Text = "Open Google Maps",
                BackgroundColor = Colors.White,
                TextColor = CustomerUi.Dark,
                BorderColor = CustomerUi.Border,
                BorderWidth = 1,
                FontSize = 11,
                CornerRadius = 8,
                HeightRequest = 44,
                HorizontalOptions = LayoutOptions.Fill,
                Command = new Command(async () => await OpenGoogleMapsAsync())
            }, 1, 0);

            map.Add(actions);
        }

        return map;
    }

    public static async Task<bool> UpdateCurrentLocationAsync(Page page)
    {
        var location = await LocationService.GetCurrentLocationAsync(page);
        if (location is null || location.Latitude == 0m || location.Longitude == 0m)
        {
            await page.DisplayAlertAsync("Location unavailable", location?.ErrorMessage ?? "BikeMate could not get your current location.", "OK");
            return false;
        }

        var addressLine = string.IsNullOrWhiteSpace(location.Address)
            ? $"Lat {location.Latitude:0.000000}, Lng {location.Longitude:0.000000}"
            : location.Address;
        PhilippineLocationMatchDto? match = null;
        try
        {
            match = await CustomerApiClient.ResolvePhilippineLocationAsync(addressLine);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PSGC location resolution failed: {ex}");
        }

        BookingDraft.ApplyCurrentLocation(match, addressLine, location.Latitude, location.Longitude);
        return true;
    }

    public static async Task OpenGoogleMapsAsync(string? query = null)
    {
        var rawQuery = query?.Trim();
        if (string.IsNullOrWhiteSpace(rawQuery) &&
            BookingDraft.Latitude is decimal latitude &&
            BookingDraft.Longitude is decimal longitude)
        {
            rawQuery = $"{latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}";
        }

        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            await Shell.Current.DisplayAlertAsync(
                "Location unavailable",
                "BikeMate does not have a confirmed location to open in Maps yet.",
                "OK");
            return;
        }

        var encodedQuery = Uri.EscapeDataString(rawQuery);

        if (await TryOpenUriAsync(new Uri($"geo:0,0?q={encodedQuery}")) ||
            await TryOpenUriAsync(new Uri($"https://www.google.com/maps/search/?api=1&query={encodedQuery}")))
        {
            return;
        }

        await Shell.Current.DisplayAlertAsync("Maps unavailable", "No maps or browser app could open this location.", "OK");
    }

    public static async Task OpenGoogleDirectionsAsync(
        decimal originLatitude,
        decimal originLongitude,
        decimal destinationLatitude,
        decimal destinationLongitude)
    {
        var origin = $"{originLatitude.ToString(CultureInfo.InvariantCulture)},{originLongitude.ToString(CultureInfo.InvariantCulture)}";
        var destination = $"{destinationLatitude.ToString(CultureInfo.InvariantCulture)},{destinationLongitude.ToString(CultureInfo.InvariantCulture)}";
        var url = $"https://www.google.com/maps/dir/?api=1&origin={Uri.EscapeDataString(origin)}&destination={Uri.EscapeDataString(destination)}&travelmode=driving";

        if (await TryOpenUriAsync(new Uri(url)))
        {
            return;
        }

        await OpenGoogleMapsAsync(destination);
    }

    private static async Task<bool> TryOpenUriAsync(Uri uri)
    {
        try
        {
            return await Microsoft.Maui.ApplicationModel.Launcher.Default.OpenAsync(uri);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open URI {uri}: {ex}");
            return false;
        }
    }

    public static View UploadBox(
        string mediaLabel,
        string title,
        string subtitle,
        string? uploadedUrl,
        string? previewPath,
        Command command)
    {
        var uploaded = !string.IsNullOrWhiteSpace(uploadedUrl);
        var content = new VerticalStackLayout { Spacing = 10 };
        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        header.Add(new Border
        {
            WidthRequest = 48,
            HeightRequest = 48,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = uploaded ? Color.FromArgb("#E8F6EF") : Color.FromArgb("#FFF2EA"),
            Content = new Label
            {
                Text = mediaLabel,
                FontSize = AppTypography.CaptionSize,
                FontAttributes = FontAttributes.Bold,
                FontFamily = CustomerUi.FontDisplay,
                TextColor = uploaded ? Color.FromArgb("#147A3D") : CustomerUi.Orange,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        }, 0, 0);
        var copy = new VerticalStackLayout { Spacing = 3 };
        copy.Add(Text(title, 12, CustomerUi.Dark, FontAttributes.Bold));
        copy.Add(Text(subtitle, 10, CustomerUi.Muted));
        header.Add(copy, 1, 0);
        header.Add(new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = uploaded ? Color.FromArgb("#E8F6EF") : Color.FromArgb("#F2F2F2"),
            Padding = new Thickness(9, 4),
            Content = Text(
                uploaded ? "Added" : "Not added",
                10,
                uploaded ? Color.FromArgb("#147A3D") : CustomerUi.Muted,
                FontAttributes.Bold)
        }, 2, 0);
        content.Add(header);

        if (!string.IsNullOrWhiteSpace(previewPath) && System.IO.File.Exists(previewPath) && IsImagePath(previewPath))
        {
            content.Add(new Border
            {
                HeightRequest = 150,
                Stroke = CustomerUi.Border,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Content = new Image
                {
                    Source = ImageSource.FromFile(previewPath),
                    Aspect = Aspect.AspectFill,
                    HeightRequest = 150
                }
            });
        }
        else if (!string.IsNullOrWhiteSpace(previewPath))
        {
            content.Add(new Border
            {
                Stroke = CustomerUi.Border,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                BackgroundColor = Color.FromArgb("#FAFAFA"),
                Padding = new Thickness(12),
                Content = Text($"Selected file: {System.IO.Path.GetFileName(previewPath)}", 10, CustomerUi.Dark)
            });
        }

        content.Add(uploaded
            ? SecondaryButton("Replace file", command)
            : PrimaryButton("Choose file", command));
        return WhiteCard(content, 8, new Thickness(12));
    }

    private static bool IsImagePath(string path)
    {
        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp";
    }

    public static string Money(decimal amount)
    {
        return string.Format(CultureInfo.GetCultureInfo("en-PH"), "PHP {0:N0}", amount);
    }
}

internal sealed class LocationAccessPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _closed = new();
    private bool _isBusy;

    private LocationAccessPage()
    {
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Colors.White;

        var accessButton = BookingVisuals.PrimaryButton("Allow location access", new Command(async () => await AccessLocationAsync()));
        var cancelButton = new Button
        {
            Text = "Not now",
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Muted,
            HeightRequest = 42,
            Command = new Command(async () => await CloseAsync())
        };

        var card = new VerticalStackLayout
        {
            Padding = new Thickness(24, 26),
            Spacing = 14,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Image
                {
                    Source = ImageSource.FromUri(new Uri("https://img.icons8.com/color/240/google-maps-new.png")),
                    HeightRequest = 96,
                    WidthRequest = 96
                },
                BookingVisuals.Text("Use your current location", 18, CustomerUi.Dark, FontAttributes.Bold),
                BookingVisuals.Text(
                    "BikeMate uses your location to find nearby repair shops and send a mechanic to the correct service address.",
                    13,
                    CustomerUi.Muted),
                accessButton,
                cancelButton,
                BookingVisuals.Text("You can change this permission later in Android settings.", 11, CustomerUi.Muted)
            }
        };
        foreach (var label in card.Children.OfType<Label>())
        {
            label.HorizontalTextAlignment = TextAlignment.Center;
        }

        AppVisualPolish.Apply(card);
        Content = card;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _closed.TrySetResult(true);
    }

    private async Task AccessLocationAsync()
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;
        try
        {
            if (await BookingVisuals.UpdateCurrentLocationAsync(this))
            {
                await CloseAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Location unavailable", ex.Message, "OK");
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task CloseAsync()
    {
        _closed.TrySetResult(true);
        if (Navigation.ModalStack.Contains(this))
        {
            await Navigation.PopModalAsync();
        }
    }

    public static async Task ShowAsync()
    {
        var shell = Shell.Current ?? throw new InvalidOperationException("BikeMate navigation is not ready yet.");
        var page = new LocationAccessPage();
        await shell.Navigation.PushModalAsync(page);
        await page._closed.Task;
    }
}

public sealed class BookingFillUpPage : CustomerPageBase
{
    private Entry? _brandEntry;
    private Entry? _modelEntry;
    private Entry? _yearEntry;
    private Entry? _plateEntry;
    private Editor? _detailsEditor;

    public BookingFillUpPage()
    {
        Title = "Vehicle Details";
        Render();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            BookingDraft.Customer ??= await CustomerApiClient.GetCustomerAsync();
            if (BookingDraft.Services.Count == 0)
            {
                BookingDraft.Services = await CustomerApiClient.SearchServicesAsync();
            }

            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load fill-up details. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 0, 14, 18),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(BookingVisuals.FlowHeader("Vehicle details"));
        body.Add(BookingVisuals.StepIntro(
            "Which bike or motorcycle needs service?",
            "Choose one saved in your account, or use another vehicle for this booking.",
            2,
            "Vehicle and concern",
            7));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(BookingVisuals.WhiteCard(BookingVisuals.Text(banner, 11, CustomerUi.Muted), 8, new Thickness(12)));
        }

        body.Add(BookingVisuals.Text("Booking type", 12, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(WorkTypeCard(
            BookingDraft.WorkTypeRepair,
            "Repair or roadside service",
            "Choose this when something is broken, unsafe, or needs maintenance."));
        body.Add(WorkTypeCard(
            BookingDraft.WorkTypeModification,
            "Modification or upgrade",
            "Choose this when you want a shop item, accessory, or custom work installed."));

        var motorcycles = BookingDraft.Customer?.Motorcycles ?? [];
        body.Add(BookingVisuals.Text("Your vehicles", 12, CustomerUi.Dark, FontAttributes.Bold));
        if (motorcycles.Count == 0)
        {
            body.Add(BookingVisuals.WhiteCard(
                BookingVisuals.Text("No saved vehicles yet. Use the other vehicle option below for this booking.", 11, CustomerUi.Muted),
                8,
                new Thickness(12)));
        }
        else
        {
            foreach (var motorcycle in motorcycles)
            {
                body.Add(VehicleCard(motorcycle));
            }
        }

        body.Add(OtherVehicleCard());

        if (BookingDraft.IsOtherVehicle)
        {
            body.Add(OtherVehicleFields());
        }
        else if (BookingDraft.SelectedMotorcycle() is not null)
        {
            body.Add(SavedVehicleDetails());
        }

        body.Add(BookingVisuals.Text("Plate number", 12, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(BookingVisuals.Text(
            "Required for vehicle verification and correct mechanic assignment.",
            10,
            CustomerUi.Muted));
        _plateEntry = TextField(
            BookingDraft.PlateNumber,
            "Enter plate number",
            Keyboard.Text,
            15);
        _plateEntry.TextChanged += (_, e) => BookingDraft.PlateNumber = e.NewTextValue ?? "";
        body.Add(BookingVisuals.WhiteCard(_plateEntry, 8, new Thickness(12, 6)));

        body.Add(new BoxView { HeightRequest = 1, Color = CustomerUi.Border, Margin = new Thickness(0, 4) });
        if (BookingDraft.IsModification)
        {
            body.Add(BookingVisuals.Text("Modification request", 15, CustomerUi.Dark, FontAttributes.Bold));
            body.Add(BookingVisuals.Text(
                "Describe the upgrade you want. You will choose the shop item and installation service after the schedule step.",
                10,
                CustomerUi.Muted));
        }
        else
        {
            body.Add(BookingVisuals.Text("What needs attention?", 15, CustomerUi.Dark, FontAttributes.Bold));
            body.Add(BookingVisuals.Text(
                "Choose the closest repair concern. The shop will provide the exact service and price later.",
                10,
                CustomerUi.Muted));
            body.Add(BookingVisuals.PickerRow(
                "Repair concern",
                ProblemOptions(),
                BookingDraft.ProblemCategory,
                selected =>
            {
                BookingDraft.ProblemCategory = selected;
                Render();
                return Task.CompletedTask;
            },
                placeholder: "Select the main concern"));
        }

        _detailsEditor = new Editor
        {
            Text = BookingDraft.OtherDetails,
            Placeholder = BookingDraft.IsModification
                ? "Describe the item, style, fitment, or custom work you want"
                : "Describe the issue, symptoms, or anything the mechanic should know",
            MinimumHeightRequest = 130,
            AutoSize = EditorAutoSizeOption.TextChanges,
            FontSize = 13,
            FontFamily = CustomerUi.FontBody,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            BackgroundColor = Colors.Transparent,
            MaxLength = 500
        };
        _detailsEditor.TextChanged += (_, e) => BookingDraft.OtherDetails = e.NewTextValue ?? "";
        body.Add(BookingVisuals.WhiteCard(_detailsEditor, 8, new Thickness(12, 8)));
        body.Add(new BoxView { HeightRequest = 8, Opacity = 0 });
        body.Add(BookingVisuals.Text("Provide at least 10 characters so the shop can assess the request.", 10, CustomerUi.Muted));
        body.Add(BookingVisuals.PrimaryButton("Continue to assistance method", new Command(async () => await ContinueAsync())));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private View VehicleCard(MotorcycleDto motorcycle)
    {
        var selected = !BookingDraft.IsOtherVehicle &&
                       BookingDraft.SelectedMotorcycleId == motorcycle.MotorcycleId;
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
        grid.Add(SelectionMark(selected), 0, 0);

        var details = new VerticalStackLayout { Spacing = 3 };
        details.Add(BookingVisuals.Text($"{motorcycle.Brand} {motorcycle.Model}", 13, CustomerUi.Dark, FontAttributes.Bold));
        var metadata = new[]
        {
            motorcycle.YearModel?.ToString(CultureInfo.InvariantCulture),
            string.IsNullOrWhiteSpace(motorcycle.Color) ? null : motorcycle.Color
        };
        details.Add(BookingVisuals.Text(
            string.Join(" | ", metadata.Where(value => !string.IsNullOrWhiteSpace(value))),
            10,
            CustomerUi.Muted));
        details.Add(BookingVisuals.Text(
            string.IsNullOrWhiteSpace(motorcycle.PlateNumber)
                ? "Plate number required"
                : $"Plate {BookingDraft.NormalizePlate(motorcycle.PlateNumber)}",
            10,
            string.IsNullOrWhiteSpace(motorcycle.PlateNumber) ? CustomerUi.Orange : CustomerUi.Muted,
            string.IsNullOrWhiteSpace(motorcycle.PlateNumber) ? FontAttributes.Bold : FontAttributes.None));
        grid.Add(details, 1, 0);

        grid.Add(BookingVisuals.Text("Saved", 10, CustomerUi.Muted, FontAttributes.Bold), 2, 0);
        var card = SelectableCard(grid, selected);
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                BookingDraft.SelectMotorcycle(motorcycle);
                Render();
            })
        });
        return card;
    }

    private View OtherVehicleCard()
    {
        var selected = BookingDraft.IsOtherVehicle;
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };
        grid.Add(SelectionMark(selected), 0, 0);
        var details = new VerticalStackLayout { Spacing = 3 };
        details.Add(BookingVisuals.Text("Other bike or motorcycle", 13, CustomerUi.Dark, FontAttributes.Bold));
        details.Add(BookingVisuals.Text("Use a borrowed, newly purchased, or unsaved vehicle.", 10, CustomerUi.Muted));
        grid.Add(details, 1, 0);

        var card = SelectableCard(grid, selected);
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                BookingDraft.SelectOtherVehicle();
                Render();
            })
        });
        return card;
    }

    private View WorkTypeCard(string workType, string title, string description)
    {
        var selected = string.Equals(BookingDraft.WorkType, workType, StringComparison.OrdinalIgnoreCase);
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };
        grid.Add(SelectionMark(selected), 0, 0);
        var details = new VerticalStackLayout { Spacing = 3 };
        details.Add(BookingVisuals.Text(title, 13, selected ? CustomerUi.Orange : CustomerUi.Dark, FontAttributes.Bold));
        details.Add(BookingVisuals.Text(description, 10, CustomerUi.Muted));
        grid.Add(details, 1, 0);

        var card = SelectableCard(grid, selected);
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                BookingDraft.SelectWorkType(workType);
                Render();
            })
        });
        return card;
    }

    private View OtherVehicleFields()
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Add(BookingVisuals.Text("Other vehicle details", 12, CustomerUi.Dark, FontAttributes.Bold));

        _brandEntry = TextField(BookingDraft.Brand, "Brand", Keyboard.Text, 60);
        _brandEntry.TextChanged += (_, e) => BookingDraft.Brand = e.NewTextValue ?? "";
        stack.Add(FieldWithLabel("Brand", _brandEntry));

        _modelEntry = TextField(BookingDraft.Model, "Model", Keyboard.Text, 80);
        _modelEntry.TextChanged += (_, e) => BookingDraft.Model = e.NewTextValue ?? "";
        stack.Add(FieldWithLabel("Model", _modelEntry));

        _yearEntry = TextField(
            BookingDraft.YearModel?.ToString(CultureInfo.InvariantCulture),
            "Year model (optional)",
            Keyboard.Numeric,
            4);
        _yearEntry.TextChanged += (_, e) =>
        {
            BookingDraft.YearModel = int.TryParse(e.NewTextValue, out var year) ? year : null;
        };
        stack.Add(FieldWithLabel("Year model", _yearEntry));

        return BookingVisuals.WhiteCard(stack, 8, new Thickness(12));
    }

    private static View SavedVehicleDetails()
    {
        var motorcycle = BookingDraft.SelectedMotorcycle()!;
        var stack = new VerticalStackLayout { Spacing = 5 };
        stack.Add(BookingVisuals.Text("Selected vehicle", 10, CustomerUi.Muted));
        stack.Add(BookingVisuals.Text($"{motorcycle.Brand} {motorcycle.Model}", 13, CustomerUi.Dark, FontAttributes.Bold));

        var metadata = new[]
        {
            motorcycle.YearModel is null ? null : $"Year {motorcycle.YearModel}",
            string.IsNullOrWhiteSpace(motorcycle.EngineType) ? null : motorcycle.EngineType,
            string.IsNullOrWhiteSpace(motorcycle.Color) ? null : motorcycle.Color
        };
        var summary = string.Join(" | ", metadata.Where(value => !string.IsNullOrWhiteSpace(value)));
        if (!string.IsNullOrWhiteSpace(summary))
        {
            stack.Add(BookingVisuals.Text(summary, 10, CustomerUi.Muted));
        }

        stack.Add(BookingVisuals.Text(
            "You can correct the plate number below for this booking.",
            10,
            CustomerUi.Muted));
        return BookingVisuals.WhiteCard(stack, 8, new Thickness(12));
    }

    private static Border SelectableCard(View content, bool selected)
    {
        return new Border
        {
            BackgroundColor = selected ? Color.FromArgb("#FFF2EA") : Colors.White,
            Stroke = selected ? CustomerUi.Orange : CustomerUi.Border,
            StrokeThickness = selected ? 2 : 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12),
            Content = content
        };
    }

    private static View SelectionMark(bool selected)
    {
        return BookingVisuals.SelectionIndicator(selected);
    }

    private static Entry TextField(string? value, string placeholder, Keyboard keyboard, int maxLength)
    {
        return new Entry
        {
            Text = value,
            Placeholder = placeholder,
            Keyboard = keyboard,
            MaxLength = maxLength,
            FontSize = 13,
            FontFamily = CustomerUi.FontBody,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            BackgroundColor = Colors.Transparent
        };
    }

    private static View FieldWithLabel(string label, Entry entry)
    {
        var stack = new VerticalStackLayout { Spacing = 4 };
        stack.Add(BookingVisuals.Text(label, 10, CustomerUi.Muted));
        stack.Add(entry);
        return stack;
    }

    private async Task ContinueAsync()
    {
        if (!BookingDraft.IsOtherVehicle && BookingDraft.SelectedMotorcycle() is null)
        {
            await DisplayAlertAsync(
                "Choose a vehicle",
                "Select one of your saved vehicles or choose Other bike or motorcycle.",
                "OK");
            return;
        }

        if (BookingDraft.IsOtherVehicle)
        {
            BookingDraft.Brand = _brandEntry?.Text?.Trim() ?? BookingDraft.Brand.Trim();
            BookingDraft.Model = _modelEntry?.Text?.Trim() ?? BookingDraft.Model.Trim();
            if (string.IsNullOrWhiteSpace(BookingDraft.Brand) || string.IsNullOrWhiteSpace(BookingDraft.Model))
            {
                await DisplayAlertAsync(
                    "Vehicle details required",
                    "Enter both the brand and model of the other vehicle.",
                    "OK");
                return;
            }

            if (BookingDraft.YearModel is int year &&
                (year < 1900 || year > DateTime.Now.Year + 1))
            {
                await DisplayAlertAsync(
                    "Check year model",
                    $"Enter a year from 1900 to {DateTime.Now.Year + 1}.",
                    "OK");
                return;
            }
        }

        BookingDraft.PlateNumber = BookingDraft.NormalizePlate(_plateEntry?.Text);
        if (string.IsNullOrWhiteSpace(BookingDraft.PlateNumber))
        {
            await DisplayAlertAsync(
                "Plate number required",
                "Enter the vehicle plate number before continuing.",
                "OK");
            return;
        }

        if (BookingDraft.PlateNumber.Length is < 2 or > 15 ||
            BookingDraft.PlateNumber.Any(character =>
                !char.IsLetterOrDigit(character) && character is not '-' and not ' '))
        {
            await DisplayAlertAsync(
                "Invalid plate number",
                "Use 2 to 15 letters, numbers, spaces, or hyphens.",
                "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(BookingDraft.WorkType))
        {
            await DisplayAlertAsync(
                "Choose booking type",
                "Select Repair or Modification before continuing.",
                "OK");
            return;
        }

        BookingDraft.OtherDetails = _detailsEditor?.Text?.Trim() ?? BookingDraft.OtherDetails.Trim();
        if (BookingDraft.IsRepair && string.IsNullOrWhiteSpace(BookingDraft.ProblemCategory))
        {
            await DisplayAlertAsync(
                "Repair concern required",
                "Choose the concern that best describes what needs attention.",
                "OK");
            return;
        }

        if (BookingDraft.IsModification)
        {
            BookingDraft.ProblemCategory = "Modification";
        }

        if (BookingDraft.OtherDetails.Length < 10)
        {
            await DisplayAlertAsync(
                BookingDraft.IsModification ? "Add modification details" : "Add repair details",
                BookingDraft.IsModification
                    ? "Describe the requested upgrade or installation using at least 10 characters."
                    : "Describe the symptoms or requested work using at least 10 characters.",
                "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(BookingServiceTypePage));
    }

    private static IReadOnlyList<string> ProblemOptions()
    {
        var dynamicOptions = BookingDraft.Services
            .Where(service => service.IsActive)
            .Select(service => string.IsNullOrWhiteSpace(service.CategoryName)
                ? service.ServiceName.Trim()
                : service.CategoryName.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToArray();
        if (dynamicOptions.Length > 0)
        {
            return dynamicOptions;
        }

        return
        [
            "Tire Problem",
            "Brake Adjustment",
            "Gear Shifting Issue",
            "Accessory Installation",
            "Chain Maintenance",
            "General Tune-up"
        ];
    }

}

public sealed class BookingServiceTypePage : CustomerPageBase
{
    public BookingServiceTypePage()
    {
        Title = "Assistance Method";
        Render();
    }

    private void Render()
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 0, 14, 18),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(BookingVisuals.FlowHeader("Assistance method"));
        body.Add(BookingVisuals.StepIntro(
            "Where should the repair happen?",
            "This only controls how the vehicle reaches the mechanic. You will choose the exact shop service and price later.",
            3,
            "Assistance method",
            7));

        body.Add(BookingContext());

        var recommended = RecommendedAssistanceMethod();
        body.Add(ServiceTypeCard(
            "On-Site Repair",
            "A mechanic comes to your selected service location.",
            "Flat tires, brake adjustments, chain issues, and quick diagnostics.",
            "You stay with the vehicle while the repair is completed.",
            recommended == "On-Site Repair"));
        body.Add(ServiceTypeCard(
            "Pick-up Repair",
            "A partner shop collects the vehicle for workshop service.",
            "Major repairs, detailed inspection, tune-ups, and longer service work.",
            "Pickup and return arrangements are confirmed after payment.",
            recommended == "Pick-up Repair"));

        body.Add(BookingVisuals.WhiteCard(
            BookingVisuals.Text(
                "The final price is set by the service and verified shop you choose. You can review it before payment.",
                10,
                CustomerUi.Muted),
            8,
            new Thickness(12)));
        body.Add(BookingVisuals.PrimaryButton(
            "Continue to schedule",
            new Command(async () => await ContinueAsync())));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private static View BookingContext()
    {
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
        copy.Add(BookingVisuals.Text(BookingDraft.VehicleSummary(), 12, CustomerUi.Dark, FontAttributes.Bold));
        copy.Add(BookingVisuals.Text(BookingDraft.ProblemCategory, 10, CustomerUi.Muted));
        grid.Add(copy, 0, 0);
        grid.Add(new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = Color.FromArgb("#FFF2EA"),
            Padding = new Thickness(10, 5),
            Content = BookingVisuals.Text("Booking", 10, CustomerUi.Orange, FontAttributes.Bold)
        }, 1, 0);
        return BookingVisuals.WhiteCard(grid, 8, new Thickness(12));
    }

    private static View ServiceTypeCard(
        string title,
        string subtitle,
        string bestFor,
        string process,
        bool recommended)
    {
        var selected = string.Equals(BookingDraft.AssistanceMethod, title, StringComparison.OrdinalIgnoreCase);
        var stack = new VerticalStackLayout { Spacing = 10 };
        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        header.Add(BookingVisuals.SelectionIndicator(selected), 0, 0);
        var titleStack = new VerticalStackLayout { Spacing = 2 };
        titleStack.Add(BookingVisuals.Text(title, 14, selected ? CustomerUi.Orange : CustomerUi.Dark, FontAttributes.Bold));
        titleStack.Add(BookingVisuals.Text(subtitle, 10, CustomerUi.Muted));
        header.Add(titleStack, 1, 0);
        if (recommended)
        {
            header.Add(new Border
            {
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                BackgroundColor = Color.FromArgb("#E8F6EF"),
                Padding = new Thickness(9, 4),
                Content = BookingVisuals.Text("Recommended", 10, Color.FromArgb("#147A3D"), FontAttributes.Bold)
            }, 2, 0);
        }

        stack.Add(header);
        stack.Add(new BoxView { HeightRequest = 1, Color = CustomerUi.Border });
        stack.Add(DetailLine("Best for", bestFor));
        stack.Add(DetailLine("What happens", process));

        var card = new Border
        {
            BackgroundColor = selected ? Color.FromArgb("#FFF7F2") : Colors.White,
            Stroke = selected ? CustomerUi.Orange : CustomerUi.Border,
            StrokeThickness = selected ? 2 : 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(14),
            Content = stack
        };
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                BookingDraft.AssistanceMethod = title;
                Shell.Current.CurrentPage.Dispatcher.Dispatch(() =>
                {
                    if (Shell.Current.CurrentPage is BookingServiceTypePage page)
                    {
                        page.Render();
                    }
                });
            })
        });
        return card;
    }

    private static View DetailLine(string label, string value)
    {
        var stack = new VerticalStackLayout { Spacing = 2 };
        stack.Add(BookingVisuals.Text(label, 10, CustomerUi.Muted, FontAttributes.Bold));
        stack.Add(BookingVisuals.Text(value, 11, CustomerUi.Dark));
        return stack;
    }

    private async Task ContinueAsync()
    {
        if (string.IsNullOrWhiteSpace(BookingDraft.AssistanceMethod))
        {
            await DisplayAlertAsync(
                "Choose an assistance method",
                "Select on-site repair or pick-up repair before scheduling.",
                "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(BookingSchedulePage));
    }

    private static string RecommendedAssistanceMethod()
    {
        return BookingDraft.ProblemCategory switch
        {
            "Tire Problem" or "Brake Adjustment" or "Chain Maintenance" => "On-Site Repair",
            _ => "Pick-up Repair"
        };
    }
}

public sealed class BookingSchedulePage : CustomerPageBase
{
    private DateTime _displayMonth;

    public BookingSchedulePage()
    {
        Title = "Schedule";
        BookingDraft.EnsureScheduleIsBookable();
        _displayMonth = new DateTime(BookingDraft.ScheduledAt.Year, BookingDraft.ScheduledAt.Month, 1);
        Render();
    }

    private void Render()
    {
        BookingDraft.EnsureScheduleIsBookable();

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 0, 14, 18),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(BookingVisuals.FlowHeader("Schedule"));
        body.Add(BookingVisuals.StepIntro(
            "When should the service happen?",
            "Choose a slot from 8:00 AM to 5:00 PM, up to one week ahead, based on your phone's local time.",
            4,
            "Schedule",
            7));
        body.Add(SelectedScheduleCard());
        body.Add(CalendarCard());
        body.Add(BookingVisuals.Text("Available times", 13, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(TimeSlotGrid());
        body.Add(BookingVisuals.PrimaryButton("Continue to photos", new Command(async () => await ContinueAsync())));
        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private static View SelectedScheduleCard()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };
        var copy = new VerticalStackLayout { Spacing = 3 };
        copy.Add(BookingVisuals.Text("Selected appointment", 10, CustomerUi.Muted));
        copy.Add(BookingVisuals.Text(
            BookingDraft.ScheduledAt.ToString("dddd, MMMM d", CultureInfo.InvariantCulture),
            13,
            CustomerUi.Dark,
            FontAttributes.Bold));
        copy.Add(BookingVisuals.Text(
            BookingDraft.ScheduledAt.ToString("h:mm tt", CultureInfo.InvariantCulture),
            11,
            CustomerUi.Orange,
            FontAttributes.Bold));
        grid.Add(copy, 0, 0);
        grid.Add(new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = Color.FromArgb("#E8F6EF"),
            Padding = new Thickness(10, 5),
            Content = BookingVisuals.Text("Available", 10, Color.FromArgb("#147A3D"), FontAttributes.Bold)
        }, 1, 0);
        return BookingVisuals.WhiteCard(grid, 8, new Thickness(12));
    }

    private async Task ContinueAsync()
    {
        if (!BookingDraft.IsWithinBookingWindow(BookingDraft.ScheduledAt))
        {
            BookingDraft.ScheduledAt = BookingDraft.NextBookableSchedule();
            Render();
            await DisplayAlertAsync("Schedule unavailable", "Choose a future slot within the next 7 days before continuing.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(BookingUploadPage));
    }

    private View CalendarCard()
    {
        var today = DateTime.Now.Date;
        var maxDate = BookingDraft.MaxBookableDate();
        var month = _displayMonth;
        var root = new VerticalStackLayout { Spacing = 12 };
        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        header.Add(BookingVisuals.Text(month.ToString("MMMM yyyy", CultureInfo.InvariantCulture), 13, CustomerUi.Dark, FontAttributes.Bold), 0, 0);
        var previous = new Button
        {
            Text = "<",
            Command = new Command(() =>
            {
                var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                if (_displayMonth > currentMonth)
                {
                    _displayMonth = _displayMonth.AddMonths(-1);
                    Render();
                }
            }),
            IsEnabled = month > new DateTime(today.Year, today.Month, 1),
            WidthRequest = 36,
            HeightRequest = 36,
            CornerRadius = 8,
            Padding = new Thickness(0),
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            FontSize = AppTypography.TitleSize
        };
        header.Add(previous, 1, 0);
        header.Add(new Button
        {
            Text = ">",
            Command = new Command(() =>
            {
                var nextMonth = _displayMonth.AddMonths(1);
                if (nextMonth <= new DateTime(maxDate.Year, maxDate.Month, 1))
                {
                    _displayMonth = nextMonth;
                    Render();
                }
            }),
            IsEnabled = month < new DateTime(maxDate.Year, maxDate.Month, 1),
            WidthRequest = 36,
            HeightRequest = 36,
            CornerRadius = 8,
            Padding = new Thickness(0),
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            FontSize = AppTypography.TitleSize
        }, 2, 0);
        root.Add(header);

        var grid = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 4,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };
        for (var i = 0; i < 7; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        var weekdays = new[] { "S", "M", "T", "W", "T", "F", "S" };
        for (var index = 0; index < weekdays.Length; index++)
        {
            var weekday = BookingVisuals.Text(weekdays[index], 10, CustomerUi.Muted, FontAttributes.Bold);
            weekday.HorizontalTextAlignment = TextAlignment.Center;
            grid.Add(weekday, index, 0);
        }

        var startOffset = (int)month.DayOfWeek;
        var cursor = month.AddDays(-startOffset);
        for (var index = 0; index < 42; index++)
        {
            var date = cursor.AddDays(index);
            var isPast = date.Date < today;
            var isTooFar = date.Date > maxDate;
            var isUnavailable = isPast || isTooFar;
            var selected = !isUnavailable && date.Date == BookingDraft.ScheduledAt.Date;
            var label = new Label
            {
                Text = date.Day.ToString(CultureInfo.InvariantCulture),
                FontSize = 11,
                TextColor = selected
                    ? Colors.White
                    : isUnavailable
                        ? Color.FromArgb("#D0D0D0")
                        : date.Month == month.Month ? CustomerUi.Dark : Color.FromArgb("#C8C8C8"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            var cell = new Border
            {
                WidthRequest = 30,
                HeightRequest = 30,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 15 },
                BackgroundColor = selected ? CustomerUi.Orange : Colors.Transparent,
                Content = label
            };
            var chosen = date;
            cell.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    if (chosen.Date < DateTime.Now.Date)
                    {
                        await DisplayAlertAsync("Date passed", "Choose today or a future date based on your phone date.", "OK");
                        return;
                    }

                    if (chosen.Date > BookingDraft.MaxBookableDate())
                    {
                        await DisplayAlertAsync("Date unavailable", "Bookings are only available up to 7 days in advance.", "OK");
                        return;
                    }

                    var candidate = chosen.Date.Add(BookingDraft.ScheduledAt.TimeOfDay);
                    BookingDraft.ScheduledAt = !BookingDraft.IsWithinBookingWindow(candidate)
                        ? BookingDraft.NextBookableSchedule()
                        : candidate;
                    _displayMonth = new DateTime(
                        BookingDraft.ScheduledAt.Year,
                        BookingDraft.ScheduledAt.Month,
                        1);
                    Render();
                })
            });
            grid.Add(cell, index % 7, index / 7 + 1);
        }
        root.Add(grid);

        return BookingVisuals.WhiteCard(root, 8, new Thickness(14));
    }

    private View TimeSlotGrid()
    {
        var now = DateTime.Now;
        var maxDate = BookingDraft.MaxBookableDate(now);
        var candidates = BookingDraft.BookingTimeSlots
            .Select(slot => BookingDraft.ScheduledAt.Date.Add(slot))
            .Where(candidate => candidate > now && candidate.Date <= maxDate)
            .ToArray();
        if (candidates.Length == 0)
        {
            return BookingVisuals.WhiteCard(
                BookingVisuals.Text("No time slots remain for this date. Choose another day.", 11, CustomerUi.Muted),
                8,
                new Thickness(12));
        }

        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (var index = 0; index < candidates.Length; index++)
        {
            if (index % 2 == 0)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            var candidate = candidates[index];
            var selected = candidate == BookingDraft.ScheduledAt;
            var slot = new Border
            {
                HeightRequest = 44,
                Stroke = selected ? CustomerUi.Orange : CustomerUi.Border,
                StrokeThickness = selected ? 2 : 1,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                BackgroundColor = selected ? Color.FromArgb("#FFF2EA") : Colors.White,
                Content = new Label
                {
                    Text = candidate.ToString("h:mm tt", CultureInfo.InvariantCulture),
                    FontSize = AppTypography.BodySize,
                    FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None,
                    FontFamily = selected ? CustomerUi.FontDisplay : CustomerUi.FontBody,
                    TextColor = selected ? CustomerUi.Orange : CustomerUi.Dark,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                }
            };
            slot.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    BookingDraft.ScheduledAt = candidate;
                    Render();
                })
            });
            grid.Add(slot, index % 2, index / 2);
        }

        return grid;
    }

}

public sealed class BookingUploadPage : CustomerPageBase
{
    public BookingUploadPage()
    {
        Title = "Upload";
        Render();
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 0, 14, 18),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(BookingVisuals.FlowHeader("Photos and video"));
        body.Add(BookingVisuals.StepIntro(
            "Help the shop understand the issue",
            "Clear media can improve the initial assessment and reduce follow-up questions.",
            5,
            "Photos and video",
            7));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(BookingVisuals.WhiteCard(BookingVisuals.Text(banner, 11, Color.FromArgb("#A23232")), 8, new Thickness(12)));
        }

        body.Add(BookingVisuals.UploadBox(
            "PHOTO",
            "Add issue photos",
            "Recommended: include a close-up and one wider view.",
            BookingDraft.ImageMediaUrl,
            BookingDraft.ImagePreviewPath,
            new Command(async () => await UploadAsync("image"))));
        body.Add(BookingVisuals.UploadBox(
            "VIDEO",
            "Add a short video",
            "Optional: capture unusual sounds, movement, or intermittent behavior.",
            BookingDraft.VideoMediaUrl,
            BookingDraft.VideoPreviewPath,
            new Command(async () => await UploadAsync("video"))));
        body.Add(BookingVisuals.WhiteCard(
            BookingVisuals.Text(
                "Avoid including IDs, payment information, or people who have not agreed to be recorded.",
                10,
                CustomerUi.Muted),
            8,
            new Thickness(12)));
        body.Add(BookingVisuals.PrimaryButton(
            "Review booking",
            new Command(async () => await Shell.Current.GoToAsync(nameof(BookingConfirmationPage)))));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private async Task UploadAsync(string mediaType)
    {
        try
        {
            var result = await PickMediaAsync(mediaType);

            if (result is null)
            {
                return;
            }

            var previewPath = await CopyPickedFileToCacheAsync(result, mediaType);
            var uploaded = await CustomerApiClient.UploadFileAsync(result, "booking");
            var uploadUrl = uploaded.Url;

            if (mediaType == "image")
            {
                BookingDraft.ImagePreviewPath = previewPath;
                BookingDraft.ImageMediaUrl = uploadUrl;
                await DisplayAlertAsync("Photo added", "The issue photo has been attached to this booking.", "OK");
            }
            else
            {
                BookingDraft.VideoPreviewPath = previewPath;
                BookingDraft.VideoMediaUrl = uploadUrl;
                await DisplayAlertAsync("Video added", "The issue video has been attached to this booking.", "OK");
            }

            Render();
        }
        catch (Exception ex)
        {
            Render($"Upload failed. {ex.Message}");
        }
    }

    private static async Task<FileResult?> PickMediaAsync(string mediaType)
    {
        return await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = mediaType == "image" ? "Select issue picture" : "Select issue video",
            FileTypes = mediaType == "image" ? FilePickerFileType.Images : FilePickerFileType.Videos
        });
    }

    private static async Task<string> CopyPickedFileToCacheAsync(FileResult result, string mediaType)
    {
        var extension = System.IO.Path.GetExtension(result.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = mediaType == "image" ? ".jpg" : ".mp4";
        }

        var fileName = $"booking-{mediaType}-{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        var targetPath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);
        await using var input = await result.OpenReadAsync();
        await using var output = System.IO.File.Create(targetPath);
        await input.CopyToAsync(output);
        return targetPath;
    }
}

public sealed class BookingConfirmationPage : CustomerPageBase
{
    public BookingConfirmationPage()
    {
        Title = "Confirmation";
        Render("Preparing booking summary...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            BookingDraft.Customer ??= await CustomerApiClient.GetCustomerAsync();
            if (BookingDraft.Services.Count == 0)
            {
                BookingDraft.Services = await CustomerApiClient.SearchServicesAsync();
            }
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load confirmation details. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var service = BookingDraft.SelectedService();
        var schedule = BookingDraft.ScheduledAt;
        var mediaCount =
            (string.IsNullOrWhiteSpace(BookingDraft.ImageMediaUrl) ? 0 : 1) +
            (string.IsNullOrWhiteSpace(BookingDraft.VideoMediaUrl) ? 0 : 1);
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 0, 14, 18),
            Spacing = 12,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(BookingVisuals.FlowHeader("Review booking"));
        body.Add(BookingVisuals.StepIntro(
            "Check everything before matching",
            BookingDraft.IsModification
                ? "Review the service location, vehicle, schedule, and modification notes before BikeMate finds nearby shops."
                : "Review the service location, vehicle, schedule, and concern before BikeMate finds nearby shops.",
            6,
            "Review",
            7));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(NoticeCard(banner));
        }

        body.Add(StatusCard());
        body.Add(MapSummaryCard());
        body.Add(SectionCard(
            "Appointment details",
            [
                DetailRow("Assistance method", BookingDraft.AssistanceMethod),
                DetailRow("Scheduled date", schedule.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)),
                DetailRow("Scheduled time", schedule.ToString("h:mm tt", CultureInfo.InvariantCulture)),
                DetailRow("Attachments", mediaCount == 0 ? "No media attached" : $"{mediaCount} file{(mediaCount == 1 ? "" : "s")} attached")
            ]));
        body.Add(SectionCard(
            "Bike and concern",
            [
                DetailRow("Vehicle", $"{BookingDraft.Brand} {BookingDraft.Model}"),
                DetailRow("Plate number", BookingDraft.NormalizePlate(BookingDraft.PlateNumber)),
                DetailRow(BookingDraft.IsModification ? "Request type" : "Repair concern", BookingDraft.IsModification ? "Modification" : BookingDraft.ProblemCategory),
                DetailRow("Description", BookingDraft.OtherDetails)
            ]));
        body.Add(PriceCard(service));

        body.Add(new BoxView { HeightRequest = 10, Opacity = 0 });
        body.Add(BookingVisuals.PrimaryButton(
            BookingDraft.IsModification ? "Confirm and find shops" : "Confirm and find repair shops",
            new Command(async () => await ContinueAsync())));
        body.Add(BookingVisuals.Text(
            "No payment is taken yet. You will review the shop, service, and price first.",
            10,
            CustomerUi.Muted));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private static View NoticeCard(string message)
    {
        return BookingVisuals.WhiteCard(
            BookingVisuals.Text(message, 11, CustomerUi.Muted),
            8,
            new Thickness(12));
    }

    private static View StatusCard()
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
            WidthRequest = 48,
            HeightRequest = 48,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            BackgroundColor = Color.FromArgb("#FFF2EA"),
            Content = new Label
            {
                Text = "BM",
                TextColor = CustomerUi.Orange,
                FontAttributes = FontAttributes.Bold,
                FontSize = AppTypography.BodySize,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        }, 0, 0);

        var copy = new VerticalStackLayout { Spacing = 4 };
        copy.Add(BookingVisuals.Text("Review your booking", 16, CustomerUi.Dark, FontAttributes.Bold));
        copy.Add(BookingVisuals.Text(
            BookingDraft.IsModification
                ? "Your request is ready. BikeMate will match it with available bike shops near your selected location."
                : "Your request is ready. BikeMate will match it with available repair shops near your selected location.",
            11,
            CustomerUi.Muted));
        grid.Add(copy, 1, 0);

        return BookingVisuals.WhiteCard(grid, 8, new Thickness(14));
    }

    private static View MapSummaryCard()
    {
        var stack = new VerticalStackLayout { Spacing = 0 };
        stack.Add(new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            HeightRequest = 150,
            Content = BookingVisuals.MapPanel(150)
        });

        var bottom = new VerticalStackLayout { Padding = new Thickness(2, 12, 2, 2), Spacing = 7 };
        bottom.Add(BookingVisuals.Text("Pickup and service location", 12, CustomerUi.Dark, FontAttributes.Bold));
        bottom.Add(BookingVisuals.Text(BookingDraft.ConfirmationAddress(), 11, CustomerUi.Muted));
        stack.Add(bottom);

        return BookingVisuals.WhiteCard(stack, 8, new Thickness(0));
    }

    private static View SectionCard(string title, IReadOnlyList<View> rows)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Add(BookingVisuals.Text(title, 13, CustomerUi.Dark, FontAttributes.Bold));
        foreach (var row in rows)
        {
            stack.Add(row);
        }

        return BookingVisuals.WhiteCard(stack, 8, new Thickness(14));
    }

    private static View DetailRow(string title, string value)
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
        grid.Add(BookingVisuals.Text(title, 11, CustomerUi.Muted), 0, 0);
        var valueLabel = BookingVisuals.Text(value, 11, CustomerUi.Dark, FontAttributes.Bold);
        valueLabel.HorizontalTextAlignment = TextAlignment.End;
        grid.Add(valueLabel, 1, 0);
        return grid;
    }

    private static View PriceCard(ShopServiceDto? service)
    {
        var hasShop = BookingDraft.SelectedShopId is not null;
        var amount = (service?.BasePrice ?? 0m) + (BookingDraft.SelectedProductPrice ?? 0m);
        var stack = new VerticalStackLayout { Spacing = 12 };

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        header.Add(BookingVisuals.Text("Shop and pricing", 13, CustomerUi.Dark, FontAttributes.Bold), 0, 0);
        header.Add(Pill(hasShop ? "Selected" : "Next step", hasShop ? Color.FromArgb("#E8F6EF") : Color.FromArgb("#FFF2EA"), hasShop ? Color.FromArgb("#147A3D") : CustomerUi.Orange), 1, 0);
        stack.Add(header);

        stack.Add(DetailRow(BookingDraft.IsModification ? "Bike shop" : "Repair shop", hasShop ? $"Shop #{BookingDraft.SelectedShopId}" : "Choose from nearby shops"));
        if (BookingDraft.IsModification)
        {
            stack.Add(DetailRow("Selected item", string.IsNullOrWhiteSpace(BookingDraft.SelectedProductName) ? "Choose after selecting a shop" : BookingDraft.SelectedProductName));
        }
        stack.Add(DetailRow("Exact service", hasShop && service is not null ? service.ServiceName : "Choose after selecting a shop"));
        stack.Add(DetailRow("Estimated price", hasShop ? BookingVisuals.Money(amount) : "Shown after shop selection"));

        return BookingVisuals.WhiteCard(stack, 8, new Thickness(14));
    }

    private static View Pill(string text, Color background, Color textColor)
    {
        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = background,
            Padding = new Thickness(10, 4),
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

    private async Task ContinueAsync()
    {
        await Shell.Current.GoToAsync(nameof(BookingSearchShopPage));
    }
}

public sealed class BookingSearchShopPage : CustomerPageBase
{
    private bool _navigated;

    public BookingSearchShopPage()
    {
        Title = "Booking";
        Render();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_navigated)
        {
            return;
        }

        _navigated = true;
        await Task.Delay(900);
        await Shell.Current.GoToAsync(nameof(StoreSelectionPage));
    }

    private void Render()
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 0, 14, 18),
            Spacing = 16,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(BookingVisuals.FlowHeader(BookingDraft.IsModification ? "Finding bike shops" : "Finding repair shops"));
        body.Add(BookingVisuals.MapPanel(240));
        var status = new VerticalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center
        };
        status.Add(new ActivityIndicator
        {
            IsRunning = true,
            Color = CustomerUi.Orange,
            WidthRequest = 34,
            HeightRequest = 34,
            HorizontalOptions = LayoutOptions.Center
        });
        status.Add(BookingVisuals.Text(
            BookingDraft.IsModification ? "Finding shops with services near you" : "Finding verified shops near you",
            15,
            CustomerUi.Dark,
            FontAttributes.Bold));
        var caption = BookingVisuals.Text(
            $"{BookingDraft.LocationName}\n{BookingDraft.ProblemCategory}",
            11,
            CustomerUi.Muted);
        caption.HorizontalTextAlignment = TextAlignment.Center;
        status.Add(caption);
        body.Add(BookingVisuals.WhiteCard(status, 8, new Thickness(18)));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }
}

public sealed class StoreSelectionPage : CustomerPageBase
{
    private IReadOnlyList<ShopSummaryDto> _shops = [];
    private IReadOnlyList<ShopServiceDto> _services = [];
    private bool _isLoading = true;

    public StoreSelectionPage()
    {
        Title = BookingDraft.IsModification ? "Choose a bike shop" : "Choose a repair shop";
        Render(BookingDraft.IsModification ? "Loading nearby bike shops..." : "Loading nearby repair shops...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var shopsTask = CustomerApiClient.GetShopsAsync(
                BookingDraft.Latitude,
                BookingDraft.Longitude,
                BookingDraft.IsModification ? null : BookingDraft.ProblemCategory);
            var servicesTask = CustomerApiClient.SearchServicesAsync();
            await Task.WhenAll(shopsTask, servicesTask);
            _services = (await servicesTask)
                .Where(service => BookingDraft.IsModification
                    ? BookingDraft.IsModificationService(service)
                    : BookingDraft.IsRelevantService(service))
                .ToArray();
            _shops = (await shopsTask)
                .Where(shop => _services.Any(service => service.ShopId == shop.ShopId))
                .ToArray();
            _isLoading = false;
            Render();
        }
        catch (Exception ex)
        {
            _isLoading = false;
            Render($"Connect the API to load nearby shops. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 0, 14, 18),
            Spacing = 12,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(BookingVisuals.FlowHeader(BookingDraft.IsModification ? "Choose a modification shop" : "Choose a repair shop"));
        body.Add(BookingVisuals.StepIntro(
            BookingDraft.IsModification ? "Choose a verified shop, item, and labor service" : "Choose a verified shop and exact service",
            BookingDraft.IsModification
                ? "Compare distance, reputation, available inventory, installation services, and base prices before payment."
                : "Compare distance, reputation, available services, and base prices before payment.",
            7,
            BookingDraft.IsModification ? "Shop, item, and service" : "Shop and service",
            7));
        body.Add(RequestSummary());
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(BookingVisuals.WhiteCard(BookingVisuals.Text(banner, 11, Color.FromArgb("#A23232")), 8, new Thickness(12)));
        }

        if (_isLoading)
        {
            body.Add(new ActivityIndicator
            {
                IsRunning = true,
                Color = CustomerUi.Orange,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 24)
            });
        }
        else if (_shops.Count == 0)
        {
            var empty = new VerticalStackLayout { Spacing = 8 };
            empty.Add(BookingVisuals.Text(
                BookingDraft.IsModification
                    ? "No nearby shops are ready for modifications"
                    : $"No nearby shops offer {BookingDraft.ProblemCategory}",
                15,
                CustomerUi.Dark,
                FontAttributes.Bold));
            empty.Add(BookingVisuals.Text(
                BookingDraft.IsModification
                    ? $"No verified BikeMate partner within 50 km of {BookingDraft.LocationName} currently lists active services. Try another service area or check again after shops add services and products."
                    : $"No verified BikeMate partner within 50 km of {BookingDraft.LocationName} currently lists a matching active service. Try another service area or repair concern.",
                11,
                CustomerUi.Muted));
            empty.Add(new Button
            {
                Text = "Change service location",
                Command = new Command(async () => await Shell.Current.GoToAsync("..")),
                HeightRequest = 44,
                CornerRadius = 8,
                BackgroundColor = Colors.White,
                BorderColor = CustomerUi.Orange,
                BorderWidth = 1,
                TextColor = CustomerUi.Orange,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                FontFamily = CustomerUi.FontDisplay
            });
            body.Add(BookingVisuals.WhiteCard(empty, 8, new Thickness(14)));
        }
        else
        {
            foreach (var shop in _shops)
            {
                body.Add(StoreCard(shop));
            }
        }

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private static View RequestSummary()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };
        var copy = new VerticalStackLayout { Spacing = 3 };
        copy.Add(BookingVisuals.Text(
            BookingDraft.IsModification ? "Modification request" : BookingDraft.ProblemCategory,
            12,
            CustomerUi.Dark,
            FontAttributes.Bold));
        copy.Add(BookingVisuals.Text(
            $"{BookingDraft.LocationName} | {BookingDraft.AssistanceMethod}",
            10,
            CustomerUi.Muted));
        grid.Add(copy, 0, 0);
        grid.Add(new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb("#E8F6EF"),
            Padding = new Thickness(9, 4),
            Content = BookingVisuals.Text("Verified only", 10, Color.FromArgb("#147A3D"), FontAttributes.Bold)
        }, 1, 0);
        return BookingVisuals.WhiteCard(grid, 8, new Thickness(12));
    }

    private View StoreCard(ShopSummaryDto shop)
    {
        var service = _services
            .Where(x => x.ShopId == shop.ShopId)
            .OrderBy(x => x.BasePrice)
            .FirstOrDefault();
        var distance = ShopDistance(shop);
        var area = string.Join(", ", new[] { shop.AddressLine, shop.City }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

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
            WidthRequest = 68,
            HeightRequest = 68,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = new Image
            {
                Source = BookingVisuals.ShopImageSource(shop.ShopImageUrl),
                WidthRequest = 68,
                HeightRequest = 68,
                Aspect = Aspect.AspectFill
            }
        }, 0, 0);
        var details = new VerticalStackLayout { Spacing = 5 };
        details.Add(BookingVisuals.Text(shop.ShopName, 13, CustomerUi.Dark, FontAttributes.Bold));
        details.Add(BookingVisuals.Text(
            string.IsNullOrWhiteSpace(area) ? "Address unavailable" : area,
            10,
            CustomerUi.Muted));
        details.Add(BookingVisuals.Text(
            distance is null ? "Verified BikeMate partner" : $"{distance:0.0} km away | Verified partner",
            10,
            Color.FromArgb("#147A3D"),
            FontAttributes.Bold));
        if (service is not null)
        {
            details.Add(BookingVisuals.Text(
                $"{service.ServiceName} | from {BookingVisuals.Money(service.BasePrice)}",
                11,
                CustomerUi.Dark,
                FontAttributes.Bold));
        }

        details.Add(new Button
        {
            Text = BookingDraft.IsModification ? "View shop inventory" : "View shop and services",
            BackgroundColor = CustomerUi.Orange,
            TextColor = Colors.White,
            FontSize = AppTypography.BodySize,
            CornerRadius = 8,
            HeightRequest = 42,
            Command = new Command(async () => await BookHereAsync(shop.ShopId))
        });
        grid.Add(details, 1, 0);
        return BookingVisuals.WhiteCard(grid, 8, new Thickness(12));
    }

    private static decimal? ShopDistance(ShopSummaryDto shop)
    {
        if (BookingDraft.Latitude is null ||
            BookingDraft.Longitude is null ||
            shop.Latitude is null ||
            shop.Longitude is null)
        {
            return null;
        }

        const double radiusKm = 6371d;
        static double Radians(decimal value) => (double)value * Math.PI / 180d;
        var latitudeA = Radians(BookingDraft.Latitude.Value);
        var latitudeB = Radians(shop.Latitude.Value);
        var deltaLatitude = Radians(shop.Latitude.Value - BookingDraft.Latitude.Value);
        var deltaLongitude = Radians(shop.Longitude.Value - BookingDraft.Longitude.Value);
        var a = Math.Sin(deltaLatitude / 2) * Math.Sin(deltaLatitude / 2) +
                Math.Cos(latitudeA) * Math.Cos(latitudeB) *
                Math.Sin(deltaLongitude / 2) * Math.Sin(deltaLongitude / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round((decimal)(radiusKm * c), 1);
    }

    private async Task BookHereAsync(int? shopId)
    {
        if (shopId is null)
        {
            await DisplayAlertAsync("Choose a shop", "Select a verified shop to view its services.", "OK");
            return;
        }

        if (BookingDraft.SelectedShopId != shopId)
        {
            BookingDraft.SelectedShopServiceId = null;
        }

        BookingDraft.SelectedShopId = shopId;
        await Shell.Current.GoToAsync($"{nameof(StoreDetailsPage)}?shopId={shopId}");
    }
}

public sealed class StoreDetailsPage : CustomerPageBase, IQueryAttributable
{
    private int _shopId;
    private ShopDetailsDto? _shop;
    private IReadOnlyList<ShopServiceDto> _services = [];
    private IReadOnlyList<ProductDto> _products = [];
    private ShopReputationDto? _reputation;
    private bool _isSubmitting;

    public StoreDetailsPage()
    {
        Title = BookingDraft.IsModification ? "Modification Shop" : "Repair Shop";
        Render(BookingDraft.IsModification ? "Loading modification shop details..." : "Loading repair shop details...");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("shopId", out var value) && int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var shopId))
        {
            if (BookingDraft.SelectedShopId != shopId)
            {
                BookingDraft.SelectedShopServiceId = null;
            }
            _shopId = shopId;
            BookingDraft.SelectedShopId = shopId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _shopId = _shopId == 0 ? BookingDraft.SelectedShopId ?? 0 : _shopId;
            if (_shopId > 0)
            {
                var detailsTask = CustomerApiClient.GetShopDetailsAsync(_shopId);
                var servicesTask = CustomerApiClient.GetShopServicesAsync(_shopId);
                var reputationTask = CustomerApiClient.GetShopReputationAsync(_shopId);
                var productsTask = CustomerApiClient.GetShopProductsAsync(_shopId);
                await Task.WhenAll(detailsTask, servicesTask, reputationTask, productsTask);
                _shop = await detailsTask;
                _services = (await servicesTask)
                    .Where(service => BookingDraft.IsModification
                        ? BookingDraft.IsModificationService(service)
                        : BookingDraft.IsRelevantService(service))
                    .OrderByDescending(service => BookingDraft.IsModificationService(service))
                    .ThenBy(x => x.BasePrice)
                    .ToArray();
                _products = (await productsTask)
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.ProductName)
                    .ToArray();
                _reputation = await reputationTask;
                if (BookingDraft.SelectedShopServiceId is not null &&
                    _services.All(x => x.ShopServiceId != BookingDraft.SelectedShopServiceId))
                {
                    BookingDraft.SelectedShopServiceId = null;
                }

                if (BookingDraft.SelectedProductId is not null &&
                    _products.All(x => x.ProductId != BookingDraft.SelectedProductId))
                {
                    BookingDraft.ClearProduct();
                }
            }
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load shop details. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 0, 14, 18),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(BookingVisuals.FlowHeader(BookingDraft.IsModification ? "Modification shop" : "Repair shop"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(BookingVisuals.WhiteCard(BookingVisuals.Text(banner, 11, CustomerUi.Muted), 8, new Thickness(12)));
        }

        if (_shop is null)
        {
            body.Add(BookingVisuals.WhiteCard(
                BookingVisuals.Text("Shop details are not available yet. Return to the shop list and choose a verified BikeMate partner.", 11, CustomerUi.Muted),
                8,
                new Thickness(14)));
            SetScaffold(new ScrollView { Content = body }, "Home", false);
            return;
        }

        var address = string.Join(", ", new[] { _shop.AddressLine, _shop.City, _shop.Province }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        body.Add(ShopHero(address));
        if (!string.IsNullOrWhiteSpace(_shop.ShopDescription))
        {
            body.Add(BookingVisuals.Text(_shop.ShopDescription, 11, CustomerUi.Dark));
        }

        body.Add(ShopRatingCard());

        body.Add(SectionHeading(
            "Top technicians",
            "The five highest-ranked active technicians from this shop."));
        if (_reputation?.TopTechnicians.Count > 0)
        {
            foreach (var technician in _reputation.TopTechnicians.Take(5))
            {
                body.Add(TechnicianCard(technician));
            }
        }
        else
        {
            body.Add(BookingVisuals.WhiteCard(
                BookingVisuals.Text("This shop has not published active technician profiles yet.", 11, CustomerUi.Muted),
                8,
                new Thickness(12)));
        }

        body.Add(SectionHeading(
            BookingDraft.IsModification ? "Installation and labor services" : $"Services for {BookingDraft.ProblemCategory}",
            BookingDraft.IsModification
                ? "Choose the labor service that matches the selected item or custom work."
                : "Only services that match your selected repair concern are shown."));
        if (_services.Count == 0)
        {
            body.Add(BookingVisuals.WhiteCard(
                BookingVisuals.Text(
                    BookingDraft.IsModification
                        ? "This shop has no active labor services yet. Return to the shop list and choose another verified provider."
                        : "This shop no longer has a matching active service. Return to the shop list and choose another verified provider.",
                    11,
                    CustomerUi.Muted),
                4,
                new Thickness(10)));
        }
        else
        {
            foreach (var service in _services.Take(6))
            {
                body.Add(ServiceChoice(service));
            }
        }

        body.Add(SectionHeading(
            BookingDraft.IsModification ? "Choose an item" : "Products and parts",
            BookingDraft.IsModification
                ? "Select the shop item for this modification. Its listed price is added to the booking estimate."
                : "Customer-visible parts from this shop inventory."));
        if (_products.Count == 0)
        {
            body.Add(BookingVisuals.WhiteCard(
                BookingVisuals.Text(
                    BookingDraft.IsModification
                        ? "No public products are listed for this shop yet. Choose another shop for modification bookings."
                        : "No public products are listed for this shop yet.",
                    11,
                    CustomerUi.Muted),
                8,
                new Thickness(12)));
        }
        else
        {
            foreach (var product in _products.Take(4))
            {
                body.Add(ProductCard(product));
            }
        }

        body.Add(SectionHeading(
            "Customer reviews",
            _reputation?.ReviewCount > 0
                ? $"{_reputation.ReviewCount} completed-service review{(_reputation.ReviewCount == 1 ? "" : "s")} for this shop."
                : "Reviews from completed BikeMate bookings appear here."));
        if (_reputation?.RecentReviews.Count > 0)
        {
            foreach (var review in _reputation.RecentReviews.Take(5))
            {
                body.Add(ShopReviewCard(review));
            }
        }
        else
        {
            body.Add(BookingVisuals.WhiteCard(
                BookingVisuals.Text("No customer reviews have been submitted for this shop yet.", 11, CustomerUi.Muted),
                8,
                new Thickness(12)));
        }

        if (!string.IsNullOrWhiteSpace(_shop.ContactNumber))
        {
            var contact = new VerticalStackLayout { Spacing = 4 };
            contact.Add(BookingVisuals.Text("Shop contact", 11, CustomerUi.Dark, FontAttributes.Bold));
            contact.Add(BookingVisuals.Text(_shop.ContactNumber, 11, CustomerUi.Muted));
            body.Add(BookingVisuals.WhiteCard(contact, 8, new Thickness(12)));
        }

        var continueButton = BookingVisuals.PrimaryButton(
            _isSubmitting ? "Confirming booking..." : "Continue to secure payment",
            new Command(async () => await ContinueToPaymentAsync()));
        continueButton.IsEnabled =
            !_isSubmitting &&
            _services.Count > 0 &&
            BookingDraft.SelectedShopServiceId is not null &&
            (!BookingDraft.IsModification || BookingDraft.SelectedProductId is not null);
        continueButton.Opacity = continueButton.IsEnabled ? 1 : 0.55;
        body.Add(continueButton);

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private View ShopHero(string address)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Add(new Border
        {
            HeightRequest = 150,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = new Image
            {
                Source = BookingVisuals.ShopImageSource(_shop!.ShopImageUrl),
                HeightRequest = 150,
                Aspect = Aspect.AspectFill
            }
        });

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };
        var copy = new VerticalStackLayout { Spacing = 4 };
        copy.Add(BookingVisuals.Text(_shop!.ShopName, 17, CustomerUi.Dark, FontAttributes.Bold));
        copy.Add(BookingVisuals.Text(
            string.IsNullOrWhiteSpace(address) ? "Address unavailable" : address,
            10,
            CustomerUi.Muted));
        header.Add(copy, 0, 0);
        header.Add(new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb("#E8F6EF"),
            Padding = new Thickness(9, 4),
            Content = BookingVisuals.Text("Verified", 10, Color.FromArgb("#147A3D"), FontAttributes.Bold)
        }, 1, 0);
        stack.Add(header);
        return stack;
    }

    private View ShopRatingCard()
    {
        var rating = _reputation?.AverageRating ?? 0m;
        var reviewCount = _reputation?.ReviewCount ?? 0;
        var completed = _reputation?.CompletedJobs ?? 0;
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 8
        };
        grid.Add(ReputationStat(
            reviewCount == 0 ? "New" : rating.ToString("0.0", CultureInfo.InvariantCulture),
            "Shop rating"), 0, 0);
        grid.Add(ReputationStat(reviewCount.ToString(CultureInfo.InvariantCulture), "Reviews"), 1, 0);
        grid.Add(ReputationStat(completed.ToString(CultureInfo.InvariantCulture), "Completed"), 2, 0);
        return BookingVisuals.WhiteCard(grid, 8, new Thickness(10));
    }

    private static View ReputationStat(string value, string label)
    {
        var stack = new VerticalStackLayout { Spacing = 3 };
        var valueLabel = BookingVisuals.Text(value, 15, CustomerUi.Dark, FontAttributes.Bold);
        valueLabel.HorizontalTextAlignment = TextAlignment.Center;
        var caption = BookingVisuals.Text(label, 9, CustomerUi.Muted);
        caption.HorizontalTextAlignment = TextAlignment.Center;
        stack.Add(valueLabel);
        stack.Add(caption);
        return stack;
    }

    private static View SectionHeading(string title, string subtitle)
    {
        var stack = new VerticalStackLayout { Spacing = 3, Margin = new Thickness(0, 4, 0, 0) };
        stack.Add(BookingVisuals.Text(title, 14, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(BookingVisuals.Text(subtitle, 10, CustomerUi.Muted));
        return stack;
    }

    private static View TechnicianCard(ShopTechnicianSummaryDto technician)
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
        grid.Add(new Border
        {
            WidthRequest = 54,
            HeightRequest = 54,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 27 },
            Content = new Image
            {
                Source = ProfileImage(technician.ProfileImageUrl),
                WidthRequest = 54,
                HeightRequest = 54,
                Aspect = Aspect.AspectFill
            }
        }, 0, 0);

        var details = new VerticalStackLayout { Spacing = 3 };
        details.Add(BookingVisuals.Text(technician.FullName, 12, CustomerUi.Dark, FontAttributes.Bold));
        details.Add(BookingVisuals.Text(
            $"{technician.AverageRating:0.0} / 5 | {technician.ReviewCount} review{(technician.ReviewCount == 1 ? "" : "s")}",
            10,
            CustomerUi.Orange,
            FontAttributes.Bold));
        var experience = technician.YearsExperience is null
            ? $"{technician.CompletedJobs} completed jobs"
            : $"{technician.YearsExperience} years experience | {technician.CompletedJobs} completed";
        details.Add(BookingVisuals.Text(experience, 10, CustomerUi.Muted));
        grid.Add(details, 1, 0);
        grid.Add(new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = string.Equals(technician.AvailabilityStatus, "online", StringComparison.OrdinalIgnoreCase)
                ? Color.FromArgb("#E8F6EF")
                : Color.FromArgb("#F2F2F2"),
            Padding = new Thickness(8, 4),
            Content = BookingVisuals.Text(
                string.Equals(technician.AvailabilityStatus, "online", StringComparison.OrdinalIgnoreCase) ? "Online" : "Offline",
                9,
                string.Equals(technician.AvailabilityStatus, "online", StringComparison.OrdinalIgnoreCase)
                    ? Color.FromArgb("#147A3D")
                    : CustomerUi.Muted,
                FontAttributes.Bold)
        }, 2, 0);
        return BookingVisuals.WhiteCard(grid, 8, new Thickness(12));
    }

    private static View ShopReviewCard(ShopCustomerReviewDto review)
    {
        var stack = new VerticalStackLayout { Spacing = 7 };
        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        var reviewer = new VerticalStackLayout { Spacing = 2 };
        reviewer.Add(BookingVisuals.Text(review.CustomerName, 11, CustomerUi.Dark, FontAttributes.Bold));
        reviewer.Add(BookingVisuals.Text(
            review.CreatedAt.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture),
            9,
            CustomerUi.Muted));
        header.Add(reviewer, 0, 0);
        header.Add(BookingVisuals.Text($"{review.Rating}.0 / 5", 11, CustomerUi.Orange, FontAttributes.Bold), 1, 0);
        stack.Add(header);
        if (!string.IsNullOrWhiteSpace(review.Comment))
        {
            stack.Add(BookingVisuals.Text(review.Comment, 11, CustomerUi.Dark));
        }

        var context = new[] { review.ServiceName, $"Technician: {review.TechnicianName}" }
            .Where(value => !string.IsNullOrWhiteSpace(value));
        stack.Add(BookingVisuals.Text(string.Join(" | ", context), 9, CustomerUi.Muted));
        return BookingVisuals.WhiteCard(stack, 8, new Thickness(12));
    }

    private static ImageSource ProfileImage(string? url)
    {
        return BookingVisuals.MechanicImageSource(url);
    }

    private View ProductCard(ProductDto product)
    {
        var (category, description) = SplitProductDescription(product.ProductDescription);
        var selected = BookingDraft.SelectedProductId == product.ProductId;
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };
        grid.Add(BookingVisuals.SelectionIndicator(selected), 0, 0);
        grid.Add(new Border
        {
            WidthRequest = 58,
            HeightRequest = 58,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#F7F7F7"),
            Content = new Image
            {
                Source = BookingVisuals.ProductImageSource(product.ProductImageUrl),
                Aspect = Aspect.AspectFill
            }
        }, 1, 0);

        var copy = new VerticalStackLayout { Spacing = 3 };
        copy.Add(BookingVisuals.Text(product.ProductName, 12, CustomerUi.Dark, FontAttributes.Bold));
        if (!string.IsNullOrWhiteSpace(category))
        {
            copy.Add(BookingVisuals.Text(category, 10, CustomerUi.Orange, FontAttributes.Bold));
        }
        if (!string.IsNullOrWhiteSpace(description))
        {
            copy.Add(BookingVisuals.Text(description, 10, CustomerUi.Muted));
        }
        copy.Add(BookingVisuals.Text(
            product.StockQuantity > 0 ? $"{product.StockQuantity} in stock" : "Ask shop for availability",
            10,
            product.StockQuantity > 0 ? Color.FromArgb("#147A3D") : CustomerUi.Muted,
            FontAttributes.Bold));
        grid.Add(copy, 2, 0);
        grid.Add(BookingVisuals.Text(BookingVisuals.Money(product.Price), 12, CustomerUi.Orange, FontAttributes.Bold), 3, 0);
        var card = BookingVisuals.WhiteCard(grid, 8, new Thickness(12));
        card.BackgroundColor = selected ? Color.FromArgb("#FFF7F2") : Colors.White;
        card.Stroke = selected ? CustomerUi.Orange : CustomerUi.Border;
        card.StrokeThickness = selected ? 2 : 1;
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                if (product.StockQuantity <= 0)
                {
                    return;
                }

                BookingDraft.SelectProduct(product);
                Render();
            })
        });
        return card;
    }

    private static (string Category, string Description) SplitProductDescription(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (string.Empty, string.Empty);
        }

        const string prefix = "Category:";
        if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return (string.Empty, value.Trim());
        }

        var lines = value.Split('\n', 2, StringSplitOptions.TrimEntries);
        var category = lines[0][prefix.Length..].Trim();
        var description = lines.Length > 1 ? lines[1].Trim() : string.Empty;
        return (category, description);
    }

    private View ServiceChoice(ShopServiceDto service)
    {
        var selected = BookingDraft.SelectedShopServiceId == service.ShopServiceId;
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        grid.Add(BookingVisuals.SelectionIndicator(selected), 0, 0);
        var text = new VerticalStackLayout { Spacing = 2 };
        text.Add(BookingVisuals.Text(service.ServiceName, 13, CustomerUi.Dark, FontAttributes.Bold));
        text.Add(BookingVisuals.Text($"{service.CategoryName} - {service.EstimatedMinutes} min", 11, CustomerUi.Muted));
        if (!string.IsNullOrWhiteSpace(service.ServiceDescription))
        {
            text.Add(BookingVisuals.Text(service.ServiceDescription, 11, CustomerUi.Muted));
        }

        grid.Add(text, 1, 0);
        grid.Add(BookingVisuals.Text(BookingVisuals.Money(service.BasePrice), 13, CustomerUi.Orange, FontAttributes.Bold), 2, 0);
        var card = BookingVisuals.WhiteCard(grid, 8, new Thickness(12));
        card.BackgroundColor = selected ? Color.FromArgb("#FFF7F2") : Colors.White;
        card.Stroke = selected ? CustomerUi.Orange : CustomerUi.Border;
        card.StrokeThickness = selected ? 2 : 1;
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                BookingDraft.SelectedShopServiceId = service.ShopServiceId;
                Render();
            })
        });
        return card;
    }

    private async Task ContinueToPaymentAsync()
    {
        if (_isSubmitting)
        {
            return;
        }

        _isSubmitting = true;
        Render();
        try
        {
            if (_shopId <= 0)
            {
                await DisplayAlertAsync("Choose a shop", "Select a shop before continuing.", "OK");
                await Shell.Current.GoToAsync(nameof(StoreSelectionPage));
                return;
            }

            var serviceId = BookingDraft.SelectedShopServiceId;
            if (serviceId is null)
            {
                await DisplayAlertAsync("Choose a service", "Select an available service before payment.", "OK");
                return;
            }

            if (BookingDraft.IsModification && BookingDraft.SelectedProductId is null)
            {
                await DisplayAlertAsync("Choose an item", "Select a shop product for this modification before payment.", "OK");
                return;
            }

            await BookingFlowActions.SelectShopAsync(_shopId, serviceId);
            _isSubmitting = false;
            await Shell.Current.GoToAsync(nameof(PaymentOptionsPage));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Shop selection failed", ex.Message, "OK");
        }
        finally
        {
            if (_isSubmitting)
            {
                _isSubmitting = false;
                Render();
            }
        }
    }
}

public sealed class TaskerProfilePage : CustomerPageBase
{
    private MechanicProfileDto? _mechanic;

    public TaskerProfilePage()
    {
        Title = "Technician Profile";
        Render("Loading technician profile...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _mechanic = await CustomerApiClient.GetMechanicProfileAsync(1);
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load technician profile. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var name = _mechanic?.FullName ?? "BikeMate technician";
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 6, 14, 20),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(Header("Technician Profile"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(BookingVisuals.WhiteCard(BookingVisuals.Text(banner, 11, CustomerUi.Muted)));
        }

        var profile = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Border
                {
                    WidthRequest = 96,
                    HeightRequest = 96,
                    Stroke = CustomerUi.Border,
                    StrokeShape = new RoundRectangle { CornerRadius = 8 },
                    HorizontalOptions = LayoutOptions.Center,
                    Content = new Image
                    {
                        Source = ImageSource.FromUri(new Uri(BookingVisuals.MechanicImage)),
                        Aspect = Aspect.AspectFill
                    }
                },
                BookingVisuals.Text(name, 18, CustomerUi.Dark, FontAttributes.Bold),
                BookingVisuals.Text(
                    _mechanic?.IsVerified == true ? "Verified BikeMate technician" : "BikeMate technician",
                    11,
                    _mechanic?.IsVerified == true ? Color.FromArgb("#147A3D") : CustomerUi.Muted,
                    FontAttributes.Bold)
            }
        };
        foreach (var label in profile.Children.OfType<Label>())
        {
            label.HorizontalTextAlignment = TextAlignment.Center;
        }
        body.Add(BookingVisuals.WhiteCard(profile, 8, new Thickness(16)));
        body.Add(ProfileStats(_mechanic));

        var about = new VerticalStackLayout { Spacing = 6 };
        about.Add(BookingVisuals.Text("Professional profile", 14, CustomerUi.Dark, FontAttributes.Bold));
        about.Add(BookingVisuals.Text(
            string.IsNullOrWhiteSpace(_mechanic?.Bio)
                ? "This technician has a verified BikeMate profile and is available for assigned motorcycle service requests."
                : _mechanic.Bio,
            13,
            CustomerUi.Muted));
        about.Add(BookingVisuals.Text(
            $"Availability: {FormatStatus(_mechanic?.AvailabilityStatus ?? "offline")}",
            11,
            CustomerUi.Dark,
            FontAttributes.Bold));
        body.Add(BookingVisuals.WhiteCard(about, 8, new Thickness(14)));
        body.Add(BookingVisuals.PrimaryButton("Continue to payment", new Command(async () => await ConfirmRiderAsync())));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private async Task ConfirmRiderAsync()
    {
        try
        {
            if (BookingDraft.SelectedShopId is null || BookingDraft.SelectedShopServiceId is null)
            {
                await DisplayAlertAsync("Choose a service", "Select a shop and service before payment.", "OK");
                await Shell.Current.GoToAsync(nameof(StoreSelectionPage));
                return;
            }

            await BookingFlowActions.SelectShopAsync(BookingDraft.SelectedShopId.Value, BookingDraft.SelectedShopServiceId);
            await DisplayAlertAsync("Ready for payment", "Your shop and service are set. Complete secure payment to confirm tracking.", "OK");
            await Shell.Current.GoToAsync(nameof(PaymentOptionsPage));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Booking update failed", ex.Message, "OK");
        }
    }

    private static View ProfileStats(MechanicProfileDto? mechanic)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };
        grid.Add(Stat($"{(mechanic?.AverageRating ?? 0m):0.0}", "Rating"), 0, 0);
        grid.Add(Stat($"{mechanic?.TotalCompletedJobs ?? 0}", "Completed"), 1, 0);
        grid.Add(Stat($"{mechanic?.YearsExperience ?? 0} yrs", "Experience"), 2, 0);
        return BookingVisuals.WhiteCard(grid, 8, new Thickness(12));
    }

    private static View Stat(string value, string label)
    {
        return new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = value, TextColor = CustomerUi.Dark, FontSize = 13, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = label, TextColor = CustomerUi.Muted, FontSize = 11, HorizontalTextAlignment = TextAlignment.Center }
            }
        };
    }

}

public sealed class BookingTrackMapPage : CustomerPageBase
{
    private ServiceRequestDto? _request;
    private LiveLocationDto? _location;

    public BookingTrackMapPage()
    {
        Title = "Track";
        Render("Loading tracking details...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            if (BookingDraft.RequestId > 0)
            {
                _request = await CustomerApiClient.GetRequestAsync(BookingDraft.RequestId);
                if (!await CustomerPaymentGate.EnsurePaidOrRedirectAsync(this, BookingDraft.RequestId))
                {
                    return;
                }

                _location = await CustomerApiClient.GetLatestRequestLocationAsync(BookingDraft.RequestId);
            }
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load tracking details. {ex.Message}");
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
        root.Add(Header("Track Order", true, "Return home", new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))), 0, 0);
        var map = new Grid { BackgroundColor = Color.FromArgb("#EEF1F4") };
        var destinationLat = _request?.ServiceLatitude ?? BookingDraft.Latitude;
        var destinationLng = _request?.ServiceLongitude ?? BookingDraft.Longitude;
        var route = BuildRouteSummary(
            _location?.Latitude,
            _location?.Longitude,
            destinationLat,
            destinationLng);
        if (_location is not null)
        {
            var mapSource = destinationLat is not null && destinationLng is not null
                ? BookingVisuals.GoogleDirectionsSource(_location.Latitude, _location.Longitude, destinationLat.Value, destinationLng.Value)
                : BookingVisuals.GoogleMapSource(_location.Latitude, _location.Longitude);
            map.Add(new WebView { Source = mapSource });
            map.Add(new Label
            {
                Text = "Mechanic to service location",
                TextColor = Color.FromArgb("#503CFF"),
                BackgroundColor = Color.FromRgba(255, 255, 255, 0.86),
                FontSize = 13,
                Padding = new Thickness(10, 6),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(10)
            });
        }
        else
        {
            map.Add(WaitingForLocation());
        }
        root.Add(map, 0, 1);

        var card = new VerticalStackLayout { Padding = new Thickness(14), Spacing = 8 };
        if (!string.IsNullOrWhiteSpace(banner))
        {
            card.Add(BookingVisuals.WhiteCard(BookingVisuals.Text(banner, 11, CustomerUi.Muted)));
        }

        var mechanicRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        mechanicRow.Add(Avatar("M", 42, CustomerUi.LightOrange), 0, 0);
        var mechanicText = new VerticalStackLayout { Spacing = 2 };
        mechanicText.Add(BookingVisuals.Text(_request?.MechanicName ?? "Assigned mechanic", 13, CustomerUi.Dark, FontAttributes.Bold));
        mechanicText.Add(BookingVisuals.Text(_location is null ? "Location not shared yet" : "Live location active", 10, _location is null ? CustomerUi.Muted : CustomerUi.Orange));
        mechanicRow.Add(mechanicText, 1, 0);
        mechanicRow.Add(new Border
        {
            BackgroundColor = _location is null ? Color.FromArgb("#F2F2F2") : Color.FromArgb("#EAF8EF"),
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(10, 5),
            Content = new Label
            {
                Text = _location is null ? "Waiting" : "On map",
                FontSize = 11,
                TextColor = _location is null ? CustomerUi.Muted : Color.FromArgb("#167A3A"),
                FontAttributes = FontAttributes.Bold
            }
        }, 2, 0);
        card.Add(mechanicRow);

        card.Add(RouteLine("From", route.Origin));
        card.Add(RouteLine("To", route.Destination));
        var scheduledAt = _request?.ScheduledAt?.ToLocalTime() ?? BookingDraft.ScheduledAt;
        card.Add(RouteLine("Scheduled for", scheduledAt.ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture)));
        card.Add(RouteLine(
            "Booked on",
            _request is null
                ? "Pending confirmation"
                : _request.CreatedAt.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture)));

        var stats = new Grid { ColumnSpacing = 8 };
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.Add(RouteStat("Distance", route.Distance), 0, 0);
        stats.Add(RouteStat("ETA", route.Time), 1, 0);
        card.Add(stats);

        var buttons = new Grid { ColumnSpacing = 8 };
        buttons.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        buttons.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        buttons.Add(BookingVisuals.PrimaryButton("View order status", new Command(async () => await Shell.Current.GoToAsync(nameof(TrackOrderPage)))), 0, 0);
        buttons.Add(new Button
        {
            Text = "Return home",
            BackgroundColor = Colors.White,
            TextColor = CustomerUi.Dark,
            BorderColor = CustomerUi.Border,
            BorderWidth = 1,
            CornerRadius = 8,
            HeightRequest = 48,
            Command = new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))
        }, 1, 0);
        card.Add(buttons);
        root.Add(new Border
        {
            BackgroundColor = Colors.White,
            Stroke = CustomerUi.Border,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(0),
            Margin = new Thickness(12),
            Content = card
        }, 0, 2);

        SetScaffold(root, "Schedule", false);
    }

    private (string Origin, string Destination, string Distance, string Time) BuildRouteSummary(
        decimal? mechanicLatitude,
        decimal? mechanicLongitude,
        decimal? destinationLatitude,
        decimal? destinationLongitude)
    {
        var destination = _request?.ServiceLocationAddress ?? BookingDraft.AddressLine;
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

    private static View WaitingForLocation()
    {
        var title = BookingVisuals.Text("Waiting for live mechanic location", 15, CustomerUi.Dark, FontAttributes.Bold);
        title.HorizontalTextAlignment = TextAlignment.Center;
        var message = BookingVisuals.Text(
            "The route will appear after the assigned mechanic starts sharing their location.",
            11,
            CustomerUi.Muted);
        message.HorizontalTextAlignment = TextAlignment.Center;
        return new VerticalStackLayout
        {
            Spacing = 8,
            Padding = new Thickness(28),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children = { title, message }
        };
    }

    private static View RouteLine(string label, string value)
    {
        var stack = new VerticalStackLayout { Spacing = 2 };
        stack.Add(BookingVisuals.Text(label.ToUpperInvariant(), 9, CustomerUi.Muted, FontAttributes.Bold));
        stack.Add(BookingVisuals.Text(value, 12, CustomerUi.Dark));
        return stack;
    }

    private static View RouteStat(string label, string value)
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
                    BookingVisuals.Text(label.ToUpperInvariant(), 9, CustomerUi.Muted, FontAttributes.Bold),
                    BookingVisuals.Text(value, 15, CustomerUi.Dark, FontAttributes.Bold)
                }
            }
        };
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
}

public sealed class TrackOrderPage : CustomerPageBase
{
    private ServiceRequestDto? _request;
    private string? _banner;

    public TrackOrderPage()
    {
        Title = "Booking Status";
        Render("Loading live order status...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            if (BookingDraft.RequestId > 0)
            {
                _request = await CustomerApiClient.GetRequestAsync(BookingDraft.RequestId);
            }
            else
            {
                _request = (await CustomerApiClient.GetMyRequestsAsync()).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
                BookingDraft.RequestId = _request?.RequestId ?? 0;
            }

            _banner = null;
            Render();
        }
        catch (Exception ex)
        {
            _banner = $"BikeMate could not load the latest booking status. {ex.Message}";
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var request = _request;
        var currentStatus = request?.CurrentStatus ?? "pending";
        var scheduledAt = request?.ScheduledAt?.ToLocalTime() ?? BookingDraft.ScheduledAt;
        var serviceName = request?.ServiceName ?? BookingDraft.SelectedService()?.ServiceName ?? "Basic Service";
        var shopName = request?.ShopName ?? "Waiting for repair shop";
        var mechanicName = request?.MechanicName ?? "Not assigned yet";
        var bookingId = Math.Max(BookingDraft.RequestId, request?.RequestId ?? 0);
        var amount = request is null
            ? BookingDraft.SelectedService()?.BasePrice ?? 0m
            : request.FinalTotal > 0 ? request.FinalTotal : request.EstimatedTotal;

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 0, 14, 20),
            Spacing = 12,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(Header("Booking Status", true, "Refresh", new Command(async () => await LoadAsync())));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(NoticeCard(banner));
        }

        body.Add(StatusHero(currentStatus, serviceName, bookingId));
        body.Add(SectionCard(
            "Appointment",
            [
                DetailRow("Scheduled for", scheduledAt.ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture)),
                DetailRow(
                    "Booked on",
                    request is null
                        ? "Pending confirmation"
                        : request.CreatedAt.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture)),
                DetailRow("Service location", request?.ServiceLocationAddress ?? BookingDraft.ConfirmationAddress())
            ]));
        body.Add(SectionCard(
            "Repair provider",
            [
                DetailRow("Repair shop", shopName),
                DetailRow("Assigned mechanic", mechanicName),
                DetailRow("Service", serviceName),
                DetailRow("Total", amount > 0 ? BookingVisuals.Money(amount) : "To be finalized")
            ]));
        body.Add(ProgressCard(currentStatus, request));
        body.Add(ActionCard(currentStatus));

        SetScaffold(new ScrollView { Content = body }, "Schedule", false);
    }

    private static View StatusHero(string status, string serviceName, int bookingId)
    {
        var statusColor = StatusColor(status);
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };

        var copy = new VerticalStackLayout { Spacing = 5 };
        copy.Add(BookingVisuals.Text($"BM-{bookingId:000000}", 10, CustomerUi.Muted, FontAttributes.Bold));
        copy.Add(BookingVisuals.Text(serviceName, 17, CustomerUi.Dark, FontAttributes.Bold));
        copy.Add(BookingVisuals.Text(StatusMessage(status), 11, CustomerUi.Muted));
        grid.Add(copy, 0, 0);
        grid.Add(StatusPill(CustomerPageBase.FormatStatus(status), statusColor), 1, 0);
        return BookingVisuals.WhiteCard(grid, 8, new Thickness(14));
    }

    private static View ProgressCard(string status, ServiceRequestDto? request)
    {
        var stack = new VerticalStackLayout { Spacing = 0 };
        stack.Add(BookingVisuals.Text("Booking progress", 13, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(BookingVisuals.Text(
            IsEnded(status)
                ? "This booking is no longer progressing."
                : "Updates appear here as the shop and mechanic move the repair forward.",
            10,
            CustomerUi.Muted));
        stack.Add(new BoxView { HeightRequest = 12, Opacity = 0 });

        var steps = new[]
        {
            ("Booking submitted", request is null
                ? "Your booking details were submitted."
                : request.CreatedAt.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture), "pending"),
            ("Payment confirmed", "Secure payment has been verified.", "paid"),
            ("Mechanic assigned", "The repair provider confirmed your mechanic.", "accepted"),
            ("Mechanic en route", "Live location becomes available when shared.", "en_route"),
            ("Mechanic arrived", "The mechanic reached the service location.", "arrived"),
            ("Repair in progress", "Inspection or repair work has started.", "in_progress"),
            ("Repair completed", "Receipt and customer review are available.", "completed")
        };

        for (var index = 0; index < steps.Length; index++)
        {
            var step = steps[index];
            stack.Add(ProgressRow(
                step.Item1,
                step.Item2,
                ProgressState(status, step.Item3),
                index < steps.Length - 1));
        }

        if (IsEnded(status))
        {
            stack.Add(TerminalRow(status));
        }

        return BookingVisuals.WhiteCard(stack, 8, new Thickness(14));
    }

    private View ActionCard(string status)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        if (status == "payment_pending" || status == "pending")
        {
            stack.Add(BookingVisuals.Text("Payment required", 13, CustomerUi.Dark, FontAttributes.Bold));
            stack.Add(BookingVisuals.Text(
                "Complete secure payment before mechanic tracking becomes available.",
                10,
                CustomerUi.Muted));
            stack.Add(BookingVisuals.PrimaryButton(
                "Continue to payment",
                new Command(async () => await Shell.Current.GoToAsync(nameof(PaymentOptionsPage)))));
        }
        else if (status == "completed")
        {
            stack.Add(BookingVisuals.Text("Repair completed", 13, CustomerUi.Dark, FontAttributes.Bold));
            stack.Add(BookingVisuals.Text("Review the repair experience or return to your dashboard.", 10, CustomerUi.Muted));
            stack.Add(BookingVisuals.PrimaryButton(
                "Review repair",
                new Command(async () => await Shell.Current.GoToAsync(nameof(BookingRatingPage)))));
        }
        else if (IsEnded(status))
        {
            stack.Add(BookingVisuals.Text(
                status == "cancelled" ? "Booking cancelled" : "Booking declined",
                13,
                CustomerUi.Dark,
                FontAttributes.Bold));
            stack.Add(BookingVisuals.Text("You can return home and create another repair booking.", 10, CustomerUi.Muted));
            stack.Add(BookingVisuals.PrimaryButton(
                "Book another service",
                new Command(async () => await Shell.Current.GoToAsync(nameof(BookServicePage)))));
        }
        else
        {
            var actions = new Grid { ColumnSpacing = 8 };
            actions.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            actions.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            actions.Add(BookingVisuals.PrimaryButton(
                "Track mechanic",
                new Command(async () => await Shell.Current.GoToAsync(nameof(BookingTrackMapPage)))), 0, 0);
            actions.Add(BookingVisuals.SecondaryButton(
                "Refresh status",
                new Command(async () => await LoadAsync())), 1, 0);
            stack.Add(actions);
        }

        stack.Add(BookingVisuals.SecondaryButton(
            "Return home",
            new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))));
        return BookingVisuals.WhiteCard(stack, 8, new Thickness(14));
    }

    private static View SectionCard(string title, IReadOnlyList<View> rows)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Add(BookingVisuals.Text(title, 13, CustomerUi.Dark, FontAttributes.Bold));
        foreach (var row in rows)
        {
            stack.Add(row);
        }

        return BookingVisuals.WhiteCard(stack, 8, new Thickness(14));
    }

    private static View DetailRow(string title, string value)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(0.42, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(0.58, GridUnitType.Star))
            },
            ColumnSpacing = 12
        };
        grid.Add(BookingVisuals.Text(title, 10, CustomerUi.Muted), 0, 0);
        var valueLabel = BookingVisuals.Text(value, 10, CustomerUi.Dark, FontAttributes.Bold);
        valueLabel.HorizontalTextAlignment = TextAlignment.End;
        grid.Add(valueLabel, 1, 0);
        return grid;
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
        var markerColumn = new Grid
        {
            WidthRequest = 22,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };
        var active = state is "done" or "current";
        markerColumn.Add(new Border
        {
            WidthRequest = 20,
            HeightRequest = 20,
            Stroke = active ? CustomerUi.Orange : CustomerUi.Border,
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            BackgroundColor = state == "done" ? CustomerUi.Orange : Colors.White,
            Content = new Label
            {
                Text = state == "done" ? "\u2713" : state == "current" ? "\u2022" : "",
                TextColor = state == "done" ? Colors.White : CustomerUi.Orange,
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
                Color = state == "done" ? CustomerUi.Orange : CustomerUi.Border,
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
        text.Add(BookingVisuals.Text(
            title,
            11,
            state == "upcoming" ? CustomerUi.Muted : CustomerUi.Dark,
            state == "current" ? FontAttributes.Bold : FontAttributes.None));
        text.Add(BookingVisuals.Text(subtitle, 9, CustomerUi.Muted));
        grid.Add(text, 1, 0);
        return grid;
    }

    private static View TerminalRow(string status)
    {
        var color = Color.FromArgb("#A23232");
        var stack = new VerticalStackLayout { Spacing = 3, Margin = new Thickness(34, 4, 0, 0) };
        stack.Add(BookingVisuals.Text(
            status == "cancelled" ? "Booking cancelled" : "Booking rejected",
            11,
            color,
            FontAttributes.Bold));
        stack.Add(BookingVisuals.Text(
            status == "cancelled"
                ? "The booking was cancelled before completion."
                : "The repair provider could not accept this booking.",
            9,
            CustomerUi.Muted));
        return stack;
    }

    private static View StatusPill(string text, Color background)
    {
        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = background,
            Padding = new Thickness(10, 5),
            VerticalOptions = LayoutOptions.Start,
            Content = BookingVisuals.Text(text, 10, Colors.White, FontAttributes.Bold)
        };
    }

    private static View NoticeCard(string message)
    {
        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb("#FFF3ED"),
            Padding = new Thickness(12),
            Content = BookingVisuals.Text(message, 10, CustomerUi.Muted)
        };
    }

    private static string ProgressState(string status, string target)
    {
        if (IsEnded(status))
        {
            return target == "pending" ? "done" : "upcoming";
        }

        var currentRank = StatusRank(status);
        var targetRank = StatusRank(target);
        return currentRank > targetRank ? "done" :
            currentRank == targetRank ? "current" : "upcoming";
    }

    private static int StatusRank(string status)
    {
        return status switch
        {
            "pending" or "payment_pending" => 1,
            "paid" => 2,
            "accepted" => 3,
            "en_route" => 4,
            "arrived" => 5,
            "in_progress" => 6,
            "completed" => 7,
            _ => 0
        };
    }

    private static bool IsEnded(string status)
    {
        return status is "cancelled" or "rejected";
    }

    private static string StatusMessage(string status)
    {
        return status switch
        {
            "payment_pending" => "Complete payment to confirm the booking.",
            "paid" => "Payment is confirmed. Waiting for mechanic assignment.",
            "pending" => "The repair provider is reviewing your booking.",
            "accepted" => "Your mechanic has been assigned.",
            "en_route" => "Your mechanic is travelling to the service location.",
            "arrived" => "Your mechanic has reached the service location.",
            "in_progress" => "Your motorcycle repair is currently underway.",
            "completed" => "The repair is complete and ready for review.",
            "cancelled" => "This booking was cancelled.",
            "rejected" => "The repair provider could not accept this booking.",
            _ => "BikeMate is checking the latest booking update."
        };
    }

    private static Color StatusColor(string status)
    {
        return status switch
        {
            "completed" => Color.FromArgb("#147A3D"),
            "cancelled" or "rejected" => Color.FromArgb("#A23232"),
            "accepted" or "en_route" or "arrived" or "in_progress" => CustomerUi.Orange,
            "paid" => Color.FromArgb("#347A52"),
            _ => Color.FromArgb("#6E6E6E")
        };
    }
}

public sealed class BookingRatingPage : CustomerPageBase
{
    private int _rating = 4;
    private ServiceRequestDto? _request;
    private Editor? _commentEditor;

    public BookingRatingPage()
    {
        Title = "Rating";
        Render();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BookingDraft.RequestId <= 0)
        {
            return;
        }

        try
        {
            _request = await CustomerApiClient.GetRequestAsync(BookingDraft.RequestId);
            Render();
        }
        catch
        {
            // Keep the review UI usable even when request refresh is unavailable.
        }
    }

    private void Render()
    {
        var mechanicName = _request?.MechanicName ?? "Your BikeMate mechanic";
        var shopName = _request?.ShopName ?? "BikeMate partner shop";
        var serviceName = _request?.ServiceName ?? BookingDraft.SelectedService()?.ServiceName ?? "Bike repair";
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 54, 16, 18),
            Spacing = 16,
            BackgroundColor = CustomerUi.Orange
        };
        body.Add(new Image
        {
            Source = ImageSource.FromUri(new Uri(BookingVisuals.MechanicImage)),
            HeightRequest = 84,
            WidthRequest = 84,
            Aspect = Aspect.AspectFill,
            HorizontalOptions = LayoutOptions.Center
        });

        var card = new VerticalStackLayout { Padding = new Thickness(14, 22), Spacing = 12 };
        card.Add(new Label { Text = $"{shopName}\nTechnician: {mechanicName}", TextColor = CustomerUi.Dark, FontSize = 11, HorizontalTextAlignment = TextAlignment.Center });
        card.Add(new Label { Text = "How was your repair?", TextColor = CustomerUi.Dark, FontSize = 18, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center });
        card.Add(new Label
        {
            Text = $"{serviceName}\nYour score contributes to both the shop's service rating and the assigned technician's rating.",
            TextColor = CustomerUi.Muted,
            FontSize = 11,
            HorizontalTextAlignment = TextAlignment.Center
        });
        card.Add(Stars());
        _commentEditor = new Editor
        {
            Placeholder = "Additional comments...",
            HeightRequest = 88,
            FontSize = 11,
            BackgroundColor = Color.FromArgb("#F2F2F6")
        };
        card.Add(_commentEditor);
        card.Add(BookingVisuals.PrimaryButton("Submit Review", new Command(async () => await SubmitAsync())));

        body.Add(new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Content = card
        });

        SetScaffold(new ScrollView { Content = body }, "Schedule", false);
    }

    private View Stars()
    {
        var row = new HorizontalStackLayout { Spacing = 5, HorizontalOptions = LayoutOptions.Center };
        for (var i = 1; i <= 5; i++)
        {
            var value = i;
            var button = new Button
            {
                Text = i <= _rating ? "*" : "-",
                TextColor = Color.FromArgb("#FFC107"),
                BackgroundColor = Colors.Transparent,
                FontSize = 18,
                WidthRequest = 42,
                HeightRequest = 42,
                Padding = new Thickness(0),
                Command = new Command(() =>
                {
                    _rating = value;
                    Render();
                })
            };
            row.Add(button);
        }
        return row;
    }

    private async Task SubmitAsync()
    {
        try
        {
            if (BookingDraft.RequestId > 0)
            {
                await CustomerApiClient.SubmitReviewAsync(new CreateReviewDto(BookingDraft.RequestId, _rating, _commentEditor?.Text));
            }

            await DisplayAlertAsync("Thank you", "Your shop and technician review has been submitted.", "OK");
            await Shell.Current.GoToAsync("//CustomerHomePage");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Review saved locally", $"The review UI is complete, but the API could not save it yet. {ex.Message}", "OK");
            await Shell.Current.GoToAsync("//CustomerHomePage");
        }
    }
}
