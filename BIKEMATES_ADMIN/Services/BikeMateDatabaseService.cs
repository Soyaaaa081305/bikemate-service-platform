using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
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

public sealed record ShopApplicationResult(
    int ShopId,
    string ShopName,
    string ShopStatus,
    string Message);

public sealed record ShopApplicationStatus(
    int ShopId,
    string ShopName,
    string ShopStatus,
    string AccountStatus,
    bool EmailVerified,
    DateTime UpdatedAt);

public sealed record UploadedFileResult(
    string Url,
    string FileName,
    string ContentType,
    long SizeBytes);

public sealed record PhilippineRegion(string Code, string Name);

public sealed record PhilippineLocality(string Code, string Name, string Type, string RegionCode, string? Province);

public sealed record PhilippineBarangay(string Code, string Name, string LocalityCode);

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
    decimal? Longitude,
    string? ShopImageUrl = null,
    string? ShopLogoUrl = null,
    bool AllowsReservations = true,
    bool AllowsPickup = true,
    bool AllowsOnsiteRepair = true);

public sealed record ShopSetupStatus(
    AdminShopProfile Profile,
    int ProductCount,
    int ServiceCount)
{
    public bool HasCoverPhoto => !string.IsNullOrWhiteSpace(Profile.ShopImageUrl);
    public bool HasProfilePicture => !string.IsNullOrWhiteSpace(Profile.ShopLogoUrl);
    public bool HasDescription => !string.IsNullOrWhiteSpace(Profile.ShopDescription);
    public bool HasProducts => ProductCount > 0;
    public bool HasServices => ServiceCount > 0;
    public bool IsComplete => HasCoverPhoto && HasProfilePicture && HasDescription && HasProducts && HasServices;
}

public sealed record AdminShopApplicationDetails(
    int ShopId,
    string ShopName,
    string ShopStatus,
    string? ShopDescription,
    string? ShopAddressLine,
    string? ShopBarangay,
    string? ShopCity,
    string? ShopProvince,
    string? ShopZipCode,
    string? ContactNumber,
    string? BusinessPermitUrl,
    string? ShopImageUrl,
    string? OwnerValidIdUrl,
    string? DtiRegistrationNumber,
    string? OwnerFirstName,
    string? OwnerMiddleName,
    string? OwnerLastName,
    string? OwnerEmail,
    string? OwnerPhoneNumber,
    string? OwnerSex,
    DateTime? OwnerBirthdate,
    string? OwnerAddressLine,
    string? OwnerBarangay,
    string? OwnerCity,
    string? OwnerProvince,
    string? OwnerZipCode,
    bool OwnerEmailVerified,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

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
    bool IsActive,
    string? ProductImageUrl);

public sealed record UpsertAdminProduct(
    string ProductName,
    string? ProductDescription,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    string? ProductImageUrl);

public sealed record AdminServiceCategory(
    int CategoryId,
    string CategoryName,
    string? Description);

public sealed record UpsertAdminServiceCategory(
    string CategoryName,
    string? Description);

public sealed record AdminShopService(
    int ShopServiceId,
    int ShopId,
    int CategoryId,
    string CategoryName,
    string ServiceName,
    string? ServiceDescription,
    decimal BasePrice,
    int EstimatedMinutes,
    bool IsActive);

public sealed record UpsertAdminShopService(
    int CategoryId,
    string ServiceName,
    string? ServiceDescription,
    decimal BasePrice,
    int EstimatedMinutes,
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

public sealed record AdminMechanicApplication(
    int MechanicId,
    int UserId,
    string FirstName,
    string? MiddleName,
    string LastName,
    string FullName,
    string Email,
    string? PhoneNumber,
    string AccountStatus,
    bool EmailVerified,
    bool IsVerified,
    string AvailabilityStatus,
    string? Sex,
    DateTime? Birthdate,
    string? AddressLine,
    string? Barangay,
    string? City,
    string? Province,
    string? ZipCode,
    string? ProfileImageUrl,
    string? ValidIdImageUrl,
    string? CertificationImageUrl,
    string? Bio,
    int? YearsExperience,
    int? ShopId,
    string? ShopName,
    bool IsAssignedToShop,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateAdminMechanicApplication(
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Sex,
    string? Birthdate,
    string Email,
    string PhoneNumber,
    string Password,
    string? AddressLine,
    string? Barangay,
    string? City,
    string? Province,
    string? ZipCode,
    string ValidIdImageUrl,
    string CertificationImageUrl,
    string? ProfileImageUrl,
    string? Bio,
    int? YearsExperience);

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
    public bool BirthdateSelected { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public string LocalityCode { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Barangay { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string ValidIdPath { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string ShopRegionCode { get; set; } = string.Empty;
    public string ShopLocalityCode { get; set; } = string.Empty;
    public string ShopProvince { get; set; } = string.Empty;
    public string ShopCity { get; set; } = string.Empty;
    public string ShopBarangay { get; set; } = string.Empty;
    public string ShopAddress { get; set; } = string.Empty;
    public string ShopZipCode { get; set; } = string.Empty;
    public string ShopDescription { get; set; } = string.Empty;
    public string BusinessPermitPath { get; set; } = string.Empty;
    public string ShopImagePath { get; set; } = string.Empty;
    public string DtiRegistrationNumber { get; set; } = string.Empty;
    public bool ShopTermsAccepted { get; set; }
    public string ApplicationStatus { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string SubmittedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
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
    private const string SubmittedShopApplicationKey = "shop_application:last";
    private const string ApiBaseUrlPreferenceKey = "bikemates_admin_api_base_url";
    private const string ApiBaseUrlEnvironmentVariable = "BIKEMATE_API_BASE_URL";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string CurrentApiBaseUrl => LoadApiBaseUrl();

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
            throw new InvalidOperationException("Use a ShopAdmin or SystemAdmin account for BikeMate Shop. Customer and mechanic accounts cannot open shop inventory/profile pages.");
        }

        var shop = await TryLoadOwnedShopAsync(http, auth.AccessToken);
        if (auth.User.AccountStatus == "pending")
        {
            throw new InvalidOperationException("Your shop application is pending BikeMate admin approval.");
        }

        if (shop is null)
        {
            throw new InvalidOperationException("No shop was returned by the API for this account. Register and approve a bike shop first, then sign in with that ShopAdmin account.");
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

    public static async Task<ShopApplicationResult> SubmitShopOwnerApplicationAsync(AccountCreationDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.Password) || draft.Password.Length <= 8)
        {
            throw new InvalidOperationException("Password must be more than 8 characters.");
        }

        using var http = CreateHttpClient();
        using var response = await http.PostAsJsonAsync(
            "shop-onboarding/apply",
            new ShopOwnerApplicationRequest(
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
                Require(draft.ValidIdPath, "Valid ID"),
                Require(draft.ShopName, "Shop name"),
                draft.ShopDescription,
                draft.ShopProvince,
                draft.ShopCity,
                draft.ShopBarangay,
                draft.ShopAddress,
                draft.ShopZipCode,
                Require(draft.BusinessPermitPath, "Business permit"),
                Require(draft.ShopImagePath, "Shop image"),
                Require(draft.DtiRegistrationNumber, "DTI registration number")),
            JsonOptions);

        return await ReadApiAsync<ShopApplicationResult>(response);
    }

    public static void SaveSubmittedShopApplication(AccountCreationDraft draft)
    {
        var snapshot = CreateSubmittedDraftSnapshot(draft);
        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        Preferences.Set(SubmittedShopApplicationKey, json);

        var email = NormalizeEmail(snapshot.Email);
        if (!string.IsNullOrWhiteSpace(email))
        {
            Preferences.Set(SubmittedShopApplicationKey + ":" + email, json);
        }
    }

    public static void ClearSubmittedShopApplication(string? email = null)
    {
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? string.Empty : NormalizeEmail(email);
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            Preferences.Remove(SubmittedShopApplicationKey + ":" + normalizedEmail);
        }

        var saved = TryGetSubmittedShopApplication();
        if (saved is null ||
            string.IsNullOrWhiteSpace(normalizedEmail) ||
            string.Equals(NormalizeEmail(saved.Email), normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            Preferences.Remove(SubmittedShopApplicationKey);
        }
    }

    public static AccountCreationDraft? TryGetSubmittedShopApplication(string? email = null)
    {
        try
        {
            var normalizedEmail = string.IsNullOrWhiteSpace(email) ? string.Empty : NormalizeEmail(email);
            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                var emailJson = Preferences.Get(SubmittedShopApplicationKey + ":" + normalizedEmail, null);
                if (!string.IsNullOrWhiteSpace(emailJson))
                {
                    return JsonSerializer.Deserialize<AccountCreationDraft>(emailJson, JsonOptions);
                }

                var sharedJson = Preferences.Get(SubmittedShopApplicationKey, null);
                var sharedDraft = string.IsNullOrWhiteSpace(sharedJson)
                    ? null
                    : JsonSerializer.Deserialize<AccountCreationDraft>(sharedJson, JsonOptions);
                return sharedDraft is not null &&
                    string.Equals(NormalizeEmail(sharedDraft.Email), normalizedEmail, StringComparison.OrdinalIgnoreCase)
                        ? sharedDraft
                        : null;
            }

            var json = Preferences.Get(SubmittedShopApplicationKey, null);
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<AccountCreationDraft>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<AccountCreationDraft?> RefreshSubmittedShopApplicationAsync(string? email = null)
    {
        var draft = TryGetSubmittedShopApplication(email);
        var normalizedEmail = NormalizeEmail(email ?? draft?.Email ?? string.Empty);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return draft;
        }

        try
        {
            using var http = CreateHttpClient();
            using var response = await http.GetAsync($"shop-onboarding/application-status?email={Uri.EscapeDataString(normalizedEmail)}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                ClearSubmittedShopApplication(normalizedEmail);
                return null;
            }

            var status = await ReadApiAsync<ShopApplicationStatus>(response);
            if (IsApprovedShopApplication(status))
            {
                ClearSubmittedShopApplication(normalizedEmail);
                return null;
            }

            draft ??= new AccountCreationDraft { Email = normalizedEmail };
            draft.Email = normalizedEmail;
            draft.ShopName = status.ShopName;
            draft.ApplicationStatus = status.ShopStatus;
            draft.EmailVerified = status.EmailVerified;
            draft.UpdatedAt = status.UpdatedAt.ToString("O");
            SaveSubmittedShopApplication(draft);
            return draft;
        }
        catch
        {
            return draft;
        }
    }

    public static async Task<UploadedFileResult> UploadOnboardingFileAsync(FileResult file, string folder)
    {
        using var http = CreateHttpClient();
        await using var stream = await file.OpenReadAsync();
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeFor(file));
        content.Add(streamContent, "file", file.FileName);
        content.Add(new StringContent(folder), "folder");

        using var response = await http.PostAsync("files/onboarding-upload", content);
        return await ReadApiAsync<UploadedFileResult>(response);
    }

    public static async Task<UploadedFileResult> UploadShopFileAsync(FileResult file, string folder)
    {
        using var http = CreateAuthorizedHttpClient();
        await using var stream = await file.OpenReadAsync();
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeFor(file));
        content.Add(streamContent, "file", file.FileName);
        content.Add(new StringContent(folder), "folder");

        using var response = await http.PostAsync("files/upload", content);
        return await ReadApiAsync<UploadedFileResult>(response);
    }

    public static async Task ForgotPasswordAsync(string email)
    {
        using var http = CreateHttpClient();
        using var response = await http.PostAsJsonAsync(
            "auth/forgot-password",
            new ForgotPasswordRequest(NormalizeEmail(Require(email, "Email"))),
            JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public static async Task ResendPasswordResetOtpAsync(string email)
    {
        using var http = CreateHttpClient();
        using var response = await http.PostAsJsonAsync(
            "auth/resend-password-reset-otp",
            new ResendPasswordResetOtpRequest(NormalizeEmail(Require(email, "Email"))),
            JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public static async Task VerifyPasswordResetOtpAsync(string email, string otpCode)
    {
        using var http = CreateHttpClient();
        using var response = await http.PostAsJsonAsync(
            "auth/verify-password-reset-otp",
            new VerifyPasswordResetOtpRequest(
                NormalizeEmail(Require(email, "Email")),
                Require(otpCode, "Reset code")),
            JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public static async Task VerifyEmailOtpAsync(string email, string otpCode)
    {
        using var http = CreateHttpClient();
        using var response = await http.PostAsJsonAsync(
            "auth/verify-otp",
            new VerifyOtpRequest(NormalizeEmail(Require(email, "Email")), Require(otpCode, "Verification code"), "email_verification"),
            JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public static async Task ResendEmailOtpAsync(string email)
    {
        using var http = CreateHttpClient();
        using var response = await http.PostAsJsonAsync(
            "auth/resend-otp",
            new ResendOtpRequest(NormalizeEmail(Require(email, "Email")), "email_verification"),
            JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public static async Task ResetPasswordAsync(string email, string token, string newPassword, string confirmPassword)
    {
        using var http = CreateHttpClient();
        using var response = await http.PostAsJsonAsync(
            "auth/reset-password",
            new ResetPasswordRequest(
                NormalizeEmail(Require(email, "Email")),
                Require(token, "Reset code"),
                Require(newPassword, "New password"),
                Require(confirmPassword, "Confirm password")),
            JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public static async Task<IReadOnlyList<PhilippineRegion>> GetPhilippineRegionsAsync()
    {
        using var http = CreateHttpClient();
        using var response = await http.GetAsync("geography/regions");
        return await ReadApiAsync<PhilippineRegion[]>(response);
    }

    public static async Task<IReadOnlyList<PhilippineLocality>> GetPhilippineLocalitiesAsync(string regionCode)
    {
        using var http = CreateHttpClient();
        using var response = await http.GetAsync($"geography/regions/{Uri.EscapeDataString(regionCode)}/localities");
        return await ReadApiAsync<PhilippineLocality[]>(response);
    }

    public static async Task<IReadOnlyList<PhilippineBarangay>> GetPhilippineBarangaysAsync(string localityCode)
    {
        using var http = CreateHttpClient();
        using var response = await http.GetAsync($"geography/localities/{Uri.EscapeDataString(localityCode)}/barangays");
        return await ReadApiAsync<PhilippineBarangay[]>(response);
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

    public static async Task<ShopSetupStatus> GetShopSetupStatusAsync()
    {
        var profile = await GetShopProfileAsync();
        var products = await GetProductsAsync();
        var services = await GetShopServicesAsync();
        return new ShopSetupStatus(
            profile,
            products.Count(product => product.IsActive),
            services.Count(service => service.IsActive));
    }

    public static async Task<AccountCreationDraft> GetSubmittedShopApplicationFromApiAsync()
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.GetAsync("shop/application");
        var details = await ReadApiAsync<AdminShopApplicationDetails>(response);
        var draft = CreateSubmittedDraftSnapshot(details);
        SaveSubmittedShopApplication(draft);
        return draft;
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
                profile.ContactNumber,
                profile.AllowsReservations,
                profile.AllowsPickup,
                profile.AllowsOnsiteRepair
            },
            JsonOptions);
        return await ReadApiAsync<AdminShopProfile>(response);
    }

    public static async Task UpdateShopCoverImageAsync(string mediaUrl)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.PostAsJsonAsync(
            "shop/profile/image",
            new UploadMediaRequest(Require(mediaUrl, "Cover photo"), "image", null),
            JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public static async Task UpdateShopLogoAsync(string mediaUrl)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.PostAsJsonAsync(
            "shop/profile/logo",
            new UploadMediaRequest(Require(mediaUrl, "Profile picture"), "image", null),
            JsonOptions);
        await EnsureSuccessAsync(response);
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

    public static async Task<IReadOnlyCollection<AdminServiceCategory>> GetServiceCategoriesAsync()
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.GetAsync("services/categories");
        return await ReadApiAsync<AdminServiceCategory[]>(response);
    }

    public static async Task<AdminServiceCategory> AddServiceCategoryAsync(UpsertAdminServiceCategory category)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.PostAsJsonAsync("services/categories", category, JsonOptions);
        return await ReadApiAsync<AdminServiceCategory>(response);
    }

    public static async Task DeleteServiceCategoryAsync(int categoryId)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.DeleteAsync($"services/categories/{categoryId}");
        await EnsureSuccessAsync(response);
    }

    public static async Task<IReadOnlyCollection<AdminShopService>> GetShopServicesAsync()
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.GetAsync("shop/services");
        return await ReadApiAsync<AdminShopService[]>(response);
    }

    public static async Task<AdminShopService> AddShopServiceAsync(UpsertAdminShopService service)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.PostAsJsonAsync("shop/services", service, JsonOptions);
        return await ReadApiAsync<AdminShopService>(response);
    }

    public static async Task<AdminShopService> UpdateShopServiceAsync(int serviceId, UpsertAdminShopService service)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.PutAsJsonAsync($"shop/services/{serviceId}", service, JsonOptions);
        return await ReadApiAsync<AdminShopService>(response);
    }

    public static async Task DeleteShopServiceAsync(int serviceId)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.DeleteAsync($"shop/services/{serviceId}");
        await EnsureSuccessAsync(response);
    }

    public static async Task<IReadOnlyCollection<AdminMechanic>> GetMechanicsAsync()
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.GetAsync("shop/mechanics");
        return await ReadApiAsync<AdminMechanic[]>(response);
    }

    public static async Task<IReadOnlyCollection<AdminMechanicApplication>> GetMechanicApplicationsAsync()
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.GetAsync("shop/mechanic-applications");
        return await ReadApiAsync<AdminMechanicApplication[]>(response);
    }

    public static async Task<AdminMechanicApplication> CreateMechanicApplicationAsync(CreateAdminMechanicApplication application)
    {
        using var http = CreateAuthorizedHttpClient();
        using var response = await http.PostAsJsonAsync("shop/mechanic-applications", application, JsonOptions);
        return await ReadApiAsync<AdminMechanicApplication>(response);
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

    private static bool IsApprovedShopApplication(ShopApplicationStatus status)
    {
        return string.Equals(status.ShopStatus, "verified", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(status.AccountStatus, "active", StringComparison.OrdinalIgnoreCase);
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler();
        var baseUrl = CurrentApiBaseUrl;
        if (baseUrl.Contains("10.0.2.2", StringComparison.OrdinalIgnoreCase) ||
            baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
            baseUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        if (baseUrl.Contains("ngrok", StringComparison.OrdinalIgnoreCase))
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
            return "This account is not allowed to access this shop data. Use the ShopAdmin account that owns the registered bike shop.";
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
        Preferences.Remove(ApiBaseUrlPreferenceKey);

        var environmentValue = Environment.GetEnvironmentVariable(ApiBaseUrlEnvironmentVariable);
        return EnsureApiPath(string.IsNullOrWhiteSpace(environmentValue)
            ? LoadPackagedOrDefaultApiBaseUrl()
            : environmentValue);
    }

    private static string LoadPackagedOrDefaultApiBaseUrl()
    {
        var buildConfiguredApiBaseUrl = ReadBuildConfiguredApiBaseUrl();
        if (!string.IsNullOrWhiteSpace(buildConfiguredApiBaseUrl))
        {
            return buildConfiguredApiBaseUrl;
        }

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
                return apiBaseUrl;
            }
        }

        var packagedApiBaseUrl = ReadPackagedApiBaseUrl();
        return string.IsNullOrWhiteSpace(packagedApiBaseUrl)
            ? DefaultApiBaseUrl()
            : packagedApiBaseUrl;
    }

    private static string? ReadBuildConfiguredApiBaseUrl()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, "BikeMateApiBaseUrl", StringComparison.OrdinalIgnoreCase))
            ?.Value;
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
        return "https://api-production-02d4.up.railway.app/api/";
    }

    private static string EnsureApiPath(string value)
    {
        var trimmed = value.Trim();
        if (!trimmed.EndsWith("/", StringComparison.Ordinal))
        {
            trimmed += "/";
        }

        return trimmed.EndsWith("/api/", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{trimmed.TrimEnd('/')}/api/";
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

    private static string ContentTypeFor(FileResult file)
    {
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            return file.ContentType;
        }

        return Path.GetExtension(file.FileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            ".3gp" => "video/3gpp",
            _ => "application/octet-stream"
        };
    }

    private static AccountCreationDraft CreateSubmittedDraftSnapshot(AccountCreationDraft draft)
    {
        return new AccountCreationDraft
        {
            FullName = draft.FullName,
            FirstName = draft.FirstName,
            MiddleName = draft.MiddleName,
            LastName = draft.LastName,
            Sex = draft.Sex,
            Birthdate = draft.Birthdate,
            BirthdateSelected = draft.BirthdateSelected,
            Email = NormalizeEmail(draft.Email),
            PhoneNumber = draft.PhoneNumber,
            Password = string.Empty,
            RegionCode = draft.RegionCode,
            LocalityCode = draft.LocalityCode,
            Province = draft.Province,
            City = draft.City,
            Barangay = draft.Barangay,
            Address = draft.Address,
            ZipCode = draft.ZipCode,
            ValidIdPath = draft.ValidIdPath,
            ShopName = draft.ShopName,
            ShopRegionCode = draft.ShopRegionCode,
            ShopLocalityCode = draft.ShopLocalityCode,
            ShopProvince = draft.ShopProvince,
            ShopCity = draft.ShopCity,
            ShopBarangay = draft.ShopBarangay,
            ShopAddress = draft.ShopAddress,
            ShopZipCode = draft.ShopZipCode,
            ShopDescription = draft.ShopDescription,
            BusinessPermitPath = draft.BusinessPermitPath,
            ShopImagePath = draft.ShopImagePath,
            DtiRegistrationNumber = draft.DtiRegistrationNumber,
            ShopTermsAccepted = draft.ShopTermsAccepted,
            ApplicationStatus = draft.ApplicationStatus,
            EmailVerified = draft.EmailVerified,
            SubmittedAt = draft.SubmittedAt,
            UpdatedAt = draft.UpdatedAt
        };
    }

    private static AccountCreationDraft CreateSubmittedDraftSnapshot(AdminShopApplicationDetails details)
    {
        var draft = new AccountCreationDraft
        {
            FirstName = details.OwnerFirstName ?? string.Empty,
            MiddleName = details.OwnerMiddleName ?? string.Empty,
            LastName = details.OwnerLastName ?? string.Empty,
            Sex = details.OwnerSex ?? string.Empty,
            Birthdate = details.OwnerBirthdate?.ToString("yyyy-MM-dd") ?? string.Empty,
            BirthdateSelected = details.OwnerBirthdate.HasValue,
            Email = NormalizeEmail(details.OwnerEmail ?? string.Empty),
            PhoneNumber = details.OwnerPhoneNumber ?? string.Empty,
            Province = details.OwnerProvince ?? string.Empty,
            City = details.OwnerCity ?? string.Empty,
            Barangay = details.OwnerBarangay ?? string.Empty,
            Address = details.OwnerAddressLine ?? string.Empty,
            ZipCode = details.OwnerZipCode ?? string.Empty,
            ValidIdPath = details.OwnerValidIdUrl ?? string.Empty,
            ShopName = details.ShopName,
            ShopDescription = details.ShopDescription ?? string.Empty,
            ShopProvince = details.ShopProvince ?? string.Empty,
            ShopCity = details.ShopCity ?? string.Empty,
            ShopBarangay = details.ShopBarangay ?? string.Empty,
            ShopAddress = details.ShopAddressLine ?? string.Empty,
            ShopZipCode = details.ShopZipCode ?? string.Empty,
            BusinessPermitPath = details.BusinessPermitUrl ?? string.Empty,
            ShopImagePath = details.ShopImageUrl ?? string.Empty,
            DtiRegistrationNumber = details.DtiRegistrationNumber ?? string.Empty,
            ShopTermsAccepted = true,
            ApplicationStatus = details.ShopStatus,
            EmailVerified = details.OwnerEmailVerified,
            SubmittedAt = details.CreatedAt.ToString("O"),
            UpdatedAt = details.UpdatedAt?.ToString("O") ?? string.Empty
        };

        draft.FullName = string.Join(" ", new[] { draft.FirstName, draft.MiddleName, draft.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
        return draft;
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

    private sealed record ShopExistsRequest(
        string ShopName,
        string? ShopProvince,
        string? ShopCity,
        string? ShopBarangay,
        string? ShopAddress,
        string? ShopZipCode);

    private sealed record ShopExistsResponse(bool Exists);

    private sealed record ForgotPasswordRequest(string Email);

    private sealed record VerifyOtpRequest(string Email, string OtpCode, string Purpose);

    private sealed record ResendOtpRequest(string Email, string Purpose);

    private sealed record VerifyPasswordResetOtpRequest(string Email, string OtpCode);

    private sealed record ResendPasswordResetOtpRequest(string Email);

    private sealed record ResetPasswordRequest(string Email, string Token, string NewPassword, string ConfirmPassword);

    private sealed record ShopOwnerApplicationRequest(
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
        string ValidIdPath,
        string ShopName,
        string? ShopDescription,
        string? ShopProvince,
        string? ShopCity,
        string? ShopBarangay,
        string? ShopAddress,
        string? ShopZipCode,
        string BusinessPermitPath,
        string ShopImagePath,
        string DtiRegistrationNumber);

    private sealed record UploadMediaRequest(string MediaUrl, string MediaType, string? Caption);
}




