using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace BIKEMATES_ADMIN.Services;

public sealed record AuthenticatedUser(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    int? ShopId,
    string? ShopName,
    string? ShopStatus,
    bool IsOwner = true);

public sealed record ShopRegistrationResult(
    int ShopId,
    string ShopName,
    string AccessCode,
    string AdminEmail);

public sealed record AdminShopProfile(
    int ShopId,
    string ShopName,
    string? ShopDescription,
    string? AddressLine,
    string? City,
    string? Province,
    string? ContactNumber,
    string ShopStatus,
    decimal? Latitude,
    decimal? Longitude);

public sealed record AdminDashboard(
    AdminShopProfile Profile,
    int ActiveBookings,
    int TodaysBookings,
    decimal MonthlyRevenue,
    int Services,
    int InventoryAlerts,
    int Mechanics,
    decimal AverageRating);

public sealed record AdminProduct(
    int ProductId,
    int ShopId,
    string ProductName,
    string? ProductDescription,
    decimal Price,
    int StockQuantity,
    bool IsActive);

public sealed record UpsertAdminProduct(
    string ProductName,
    string? ProductDescription,
    decimal Price,
    int StockQuantity,
    bool IsActive);

public sealed record AdminMechanic(
    int MechanicId,
    string FullName,
    string? Bio,
    int? YearsExperience,
    bool IsVerified,
    string AvailabilityStatus,
    decimal AverageRating,
    int TotalCompletedJobs);

public sealed record AdminServiceRequest(
    int RequestId,
    string CurrentStatus,
    string CustomerName,
    string? MechanicName,
    string? ShopName,
    string? ServiceName,
    string IssueDescription,
    string? ServiceLocationAddress,
    DateTime? ScheduledAt,
    decimal EstimatedTotal,
    decimal FinalTotal,
    DateTime CreatedAt,
    decimal? ServiceLatitude,
    decimal? ServiceLongitude,
    decimal? DistanceKm);

public sealed record AdminConversation(
    int ConversationId,
    int? RequestId,
    string ConversationType,
    DateTime? LastMessageAt,
    string Title,
    string? Subtitle,
    int? OtherUserId,
    string? OtherProfileImageUrl,
    string? LastMessageText,
    int UnreadCount,
    string? BookingStatus,
    DateTime? ScheduledAt);

public sealed record AdminMessage(
    int MessageId,
    int ConversationId,
    int SenderUserId,
    string MessageText,
    string? AttachmentUrl,
    DateTime CreatedAt,
    DateTime? ReadAt);

public sealed record AdminNotification(
    int NotificationId,
    int UserId,
    string NotificationType,
    string Title,
    string Message,
    string? DataJson,
    bool IsRead,
    DateTime CreatedAt);

public sealed class AccountCreationDraft
{
    public string FullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Sex { get; set; } = string.Empty;
    public string Birthdate { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Barangay { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string ValidIdPath { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string ShopProvince { get; set; } = string.Empty;
    public string ShopCity { get; set; } = string.Empty;
    public string ShopBarangay { get; set; } = string.Empty;
    public string ShopAddress { get; set; } = string.Empty;
    public string ShopZipCode { get; set; } = string.Empty;
    public string AccessCode { get; set; } = string.Empty;
}

public sealed class ShopRegistrationDraft
{
    public string ShopName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string ShopDescription { get; set; } = string.Empty;
    public string ShopAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string BusinessPermitPath { get; set; } = string.Empty;
    public string ShopImagePath { get; set; } = string.Empty;
    public string DtiRegistrationNumber { get; set; } = string.Empty;
}

public static class AppSession
{
    public static AuthenticatedUser? CurrentUser { get; set; }
    public static string? AccessToken { get; set; }
}

public static class BikeMateDatabaseService
{
    private const string ShopAdminRole = "ShopAdmin";
    private const string SystemAdminRole = "SystemAdmin";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Lazy<string> ApiBaseUrl = new(LoadApiBaseUrl);

    public static async Task<AuthenticatedUser> LoginAsync(string email, string password)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Enter your email and password.");
        }

        using var http = CreateHttpClient();
        using var response = await http.PostAsJsonAsync(
            "auth/login",
            new LoginRequest(normalizedEmail, password),
            JsonOptions);
        var auth = await ReadApiAsync<AuthResponse>(response);

        var isShopAdmin = auth.User.Roles.Any(role => string.Equals(role, ShopAdminRole, StringComparison.OrdinalIgnoreCase));
        var isSystemAdmin = auth.User.Roles.Any(role => string.Equals(role, SystemAdminRole, StringComparison.OrdinalIgnoreCase));
        if (!isShopAdmin && !isSystemAdmin)
        {
            throw new InvalidOperationException("Use a ShopAdmin or SystemAdmin account for BIKEMATES_ADMIN. Customer and mechanic accounts cannot open shop inventory/profile pages.");
        }

        var shop = await TryLoadOwnedShopAsync(http, auth.AccessToken);
        if (shop is null)
        {
            throw new InvalidOperationException("No shop was returned by the API for this account. Register a bike shop first, or use admin@bikemate.test with the seeded BikeMate database.");
        }

        var user = new AuthenticatedUser(
            auth.User.UserId,
            auth.User.FirstName,
            auth.User.LastName,
            auth.User.Email,
            shop.ShopId,
            shop.ShopName,
            shop.ShopStatus,
            true);

        AppSession.CurrentUser = user;
        AppSession.AccessToken = auth.AccessToken;
        return user;
    }

    public static async Task<AuthenticatedUser> CreateShopAdminAccountAsync(AccountCreationDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.Password) || draft.Password.Length <= 8)
        {
            throw new InvalidOperationException("Password must be more than 8 characters.");
        }

        using var http = CreateHttpClient();
        using var response = await http.PostAsJsonAsync(
            "shop-onboarding/create-account",
            new ShopAdminAccountRequest(
                Require(draft.FirstName, "First name"),
                draft.MiddleName,
                Require(draft.LastName, "Last name"),
                draft.Sex,
                draft.Birthdate,
                NormalizeEmail(Require(draft.Email, "Email")),
                Require(draft.PhoneNumber, "Phone number"),
                draft.Password,
                draft.Province,
                draft.City,
                draft.Barangay,
                draft.Address,
                draft.ZipCode,
                draft.ValidIdPath,
                Require(draft.ShopName, "Shop name"),
                draft.ShopProvince,
                draft.ShopCity,
                draft.ShopBarangay,
                draft.ShopAddress,
                draft.ShopZipCode,
                Require(draft.AccessCode, "Access code")),
            JsonOptions);

        var created = await ReadApiAsync<ShopAdminAccountResponse>(response);
        return new AuthenticatedUser(
            created.UserId,
            created.FirstName,
            created.LastName,
            created.Email,
            created.ShopId,
            created.ShopName,
            created.ShopStatus);
    }

    public static async Task<ShopRegistrationResult> RegisterShopAsync(ShopRegistrationDraft draft)
    {
        using var http = CreateHttpClient();
        using var response = await http.PostAsJsonAsync(
            "shop-onboarding/register-shop",
            new ShopRegistrationRequest(
                Require(draft.ShopName, "Shop name"),
                Require(draft.OwnerName, "Shop owner"),
                draft.ShopDescription,
                Require(draft.ShopAddress, "Shop address"),
                Require(draft.City, "City"),
                Require(draft.Province, "Province"),
                draft.BusinessPermitPath,
                draft.ShopImagePath,
                draft.DtiRegistrationNumber),
            JsonOptions);

        return await ReadApiAsync<ShopRegistrationResult>(response);
    }

    public static async Task<bool> ShopExistsForAccountCreationAsync(AccountCreationDraft draft)
    {
        using var http = CreateHttpClient();
        using var response = await http.PostAsJsonAsync(
            "shop-onboarding/shop-exists",
            new ShopExistsRequest(
                Require(draft.ShopName, "Shop name"),
                draft.ShopProvince,
                draft.ShopCity,
                draft.ShopBarangay,
                draft.ShopAddress,
                draft.ShopZipCode),
            JsonOptions);

        var result = await ReadApiAsync<ShopExistsResponse>(response);
        return result.Exists;
    }

    public static async Task<AdminDashboard> GetAdminDashboardAsync()
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.GetAsync("shop/dashboard");
        return await ReadApiAsync<AdminDashboard>(response);
    }

    public static async Task<AdminShopProfile> GetShopProfileAsync()
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.GetAsync("shop/profile");
        return await ReadApiAsync<AdminShopProfile>(response);
    }

    public static async Task<AdminShopProfile> UpdateShopProfileAsync(AdminShopProfile profile)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.PutAsJsonAsync(
            "shop/profile",
            new
            {
                profile.ShopName,
                profile.ShopDescription,
                profile.AddressLine,
                profile.City,
                profile.Province,
                profile.Latitude,
                profile.Longitude,
                profile.ContactNumber
            },
            JsonOptions);
        return await ReadApiAsync<AdminShopProfile>(response);
    }

    public static async Task<IReadOnlyCollection<AdminProduct>> GetProductsAsync()
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.GetAsync("shop/inventory");
        return await ReadApiAsync<AdminProduct[]>(response);
    }

    public static async Task<AdminProduct> AddProductAsync(UpsertAdminProduct product)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.PostAsJsonAsync("shop/inventory", product, JsonOptions);
        return await ReadApiAsync<AdminProduct>(response);
    }

    public static async Task<AdminProduct> UpdateProductAsync(int productId, UpsertAdminProduct product)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.PutAsJsonAsync($"shop/inventory/{productId}", product, JsonOptions);
        return await ReadApiAsync<AdminProduct>(response);
    }

    public static async Task DeleteProductAsync(int productId)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.DeleteAsync($"shop/inventory/{productId}");
        await EnsureSuccessAsync(response);
    }

    public static async Task<IReadOnlyCollection<AdminMechanic>> GetMechanicsAsync()
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.GetAsync("shop/mechanics");
        return await ReadApiAsync<AdminMechanic[]>(response);
    }

    public static async Task<IReadOnlyCollection<AdminServiceRequest>> GetBookingsAsync()
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.GetAsync("shop/bookings");
        return await ReadApiAsync<AdminServiceRequest[]>(response);
    }

    public static async Task AssignMechanicAsync(int requestId, int mechanicId)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.PutAsJsonAsync(
            $"shop/bookings/{requestId}/assign-mechanic",
            new { MechanicId = mechanicId },
            JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public static async Task<AdminServiceRequest> UpdateRequestStatusAsync(int requestId, string status, string? notes)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.PutAsJsonAsync(
            $"service-requests/{requestId}/status",
            new { Status = status, Notes = notes },
            JsonOptions);
        return await ReadApiAsync<AdminServiceRequest>(response);
    }

    public static async Task<IReadOnlyCollection<AdminConversation>> GetConversationsAsync()
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.GetAsync("conversations");
        return await ReadApiAsync<AdminConversation[]>(response);
    }

    public static async Task<IReadOnlyCollection<AdminMessage>> GetMessagesAsync(int conversationId)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.GetAsync($"conversations/{conversationId}/messages");
        return await ReadApiAsync<AdminMessage[]>(response);
    }

    public static async Task<AdminMessage> SendMessageAsync(int conversationId, string messageText, string? attachmentUrl = null)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.PostAsJsonAsync(
            $"conversations/{conversationId}/messages",
            new { MessageText = messageText, AttachmentUrl = attachmentUrl },
            JsonOptions);
        return await ReadApiAsync<AdminMessage>(response);
    }

    public static async Task<IReadOnlyCollection<AdminNotification>> GetNotificationsAsync()
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.GetAsync("notifications");
        return await ReadApiAsync<AdminNotification[]>(response);
    }

    public static async Task MarkAllNotificationsReadAsync()
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.PutAsync("notifications/read-all", null);
        await EnsureSuccessAsync(response);
    }

    private static async Task<ShopDetails?> TryLoadOwnedShopAsync(HttpClient http, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "shops/my");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await http.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var shops = await ReadApiAsync<ShopDetails[]>(response);
        return shops.FirstOrDefault();
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler();
        var baseUrl = ApiBaseUrl.Value;
        if (baseUrl.Contains("10.0.2.2", StringComparison.OrdinalIgnoreCase) ||
            baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(EnsureTrailingSlash(baseUrl)),
            Timeout = TimeSpan.FromSeconds(30)
        };

        if (baseUrl.Contains("ngrok-free.dev", StringComparison.OrdinalIgnoreCase))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
        }

        return client;
    }

    private static HttpClient CreateAuthorizedHttpClient()
    {
        if (string.IsNullOrWhiteSpace(AppSession.AccessToken))
        {
            throw new InvalidOperationException("Please log in before loading shop admin data.");
        }

        var client = CreateHttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppSession.AccessToken);
        return client;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var payload = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(ExtractErrorMessage(payload, response));
    }

    private static async Task<T> ReadApiAsync<T>(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(ExtractErrorMessage(payload, response));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new InvalidOperationException("The BikeMate API returned an empty response.");
        }

        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
            ?? throw new InvalidOperationException("The BikeMate API returned an unreadable response.");
    }

    private static string ExtractErrorMessage(string payload, HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return "This account is not allowed to access this shop data. Use a ShopAdmin account, or use admin@bikemate.test after restarting the updated BikeMate API.";
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            AppSession.CurrentUser = null;
            AppSession.AccessToken = null;
            return "Your login session expired. Please sign in again.";
        }
if (!string.IsNullOrWhiteSpace(payload))
        {
            try
            {
                using var json = JsonDocument.Parse(payload);
                if (json.RootElement.TryGetProperty("error", out var error) &&
                    !string.IsNullOrWhiteSpace(error.GetString()))
                {
                    return error.GetString()!;
                }

                if (json.RootElement.TryGetProperty("message", out var message) &&
                    !string.IsNullOrWhiteSpace(message.GetString()))
                {
                    return message.GetString()!;
                }

                if (json.RootElement.TryGetProperty("title", out var title) &&
                    !string.IsNullOrWhiteSpace(title.GetString()))
                {
                    return title.GetString()!;
                }
            }
            catch (JsonException)
            {
                return payload;
            }
        }

        return $"BikeMate API request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
    }

    private static string LoadApiBaseUrl()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            using var stream = File.OpenRead(path);
            var apiBaseUrl = ReadApiBaseUrl(stream);
            if (!string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                return EnsureTrailingSlash(apiBaseUrl);
            }
        }

        var packagedApiBaseUrl = ReadPackagedApiBaseUrl();
        return EnsureTrailingSlash(string.IsNullOrWhiteSpace(packagedApiBaseUrl)
            ? DefaultApiBaseUrl()
            : packagedApiBaseUrl);
    }

    private static string? ReadPackagedApiBaseUrl()
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
            return ReadApiBaseUrl(stream);
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadApiBaseUrl(Stream stream)
    {
        using var json = JsonDocument.Parse(stream);
        if (!json.RootElement.TryGetProperty("Api", out var api))
        {
            return null;
        }

#if ANDROID
        if (api.TryGetProperty("AndroidBaseUrl", out var androidBaseUrl) &&
            !string.IsNullOrWhiteSpace(androidBaseUrl.GetString()))
        {
            return androidBaseUrl.GetString();
        }
#endif

        return api.TryGetProperty("BaseUrl", out var baseUrl) &&
            !string.IsNullOrWhiteSpace(baseUrl.GetString())
            ? baseUrl.GetString()
            : null;
    }

    private static string DefaultApiBaseUrl()
    {
#if ANDROID
        return "http://10.0.2.2:5000/api/";
#else
        return "https://localhost:5001/api/";
#endif
    }

    private static string EnsureTrailingSlash(string value)
    {
        return value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";
    }
private static string NormalizeEmail(string email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string Require(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{label} is required.");
        }

        return value.Trim();
    }

    private sealed record LoginRequest(string Email, string Password);

    private sealed record AuthResponse(
        string AccessToken,
        DateTimeOffset ExpiresAt,
        UserProfile User);

    private sealed record UserProfile(
        int UserId,
        string FirstName,
        string LastName,
        string Email,
        bool EmailVerified,
        string AccountStatus,
        string[] Roles);

    private sealed record ShopDetails(
        int ShopId,
        string ShopName,
        string? ShopDescription,
        string? AddressLine,
        string? City,
        string? Province,
        string? ContactNumber,
        string ShopStatus,
        decimal? Latitude,
        decimal? Longitude);

    private sealed record ShopRegistrationRequest(
        string ShopName,
        string OwnerName,
        string? ShopDescription,
        string ShopAddress,
        string City,
        string Province,
        string? BusinessPermitPath,
        string? ShopImagePath,
        string? DtiRegistrationNumber);

    private sealed record ShopExistsRequest(
        string ShopName,
        string? ShopProvince,
        string? ShopCity,
        string? ShopBarangay,
        string? ShopAddress,
        string? ShopZipCode);

    private sealed record ShopExistsResponse(bool Exists);

    private sealed record ShopAdminAccountRequest(
        string FirstName,
        string? MiddleName,
        string LastName,
        string? Sex,
        string? Birthdate,
        string Email,
        string PhoneNumber,
        string Password,
        string? Province,
        string? City,
        string? Barangay,
        string? Address,
        string? ZipCode,
        string? ValidIdPath,
        string ShopName,
        string? ShopProvince,
        string? ShopCity,
        string? ShopBarangay,
        string? ShopAddress,
        string? ShopZipCode,
        string AccessCode);

    private sealed record ShopAdminAccountResponse(
        int UserId,
        string FirstName,
        string LastName,
        string Email,
        int ShopId,
        string ShopName,
        string ShopStatus);
}




