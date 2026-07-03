using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BikeMate.Core.DTOs;
using BikeMate.Core.Helpers;
using BikeMate.Helpers;
using Microsoft.Maui.Storage;

namespace BikeMate.Services;

internal static class CustomerApiClient
{
    public static async Task<CustomerMeDto> GetCustomerAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<CustomerMeDto>(http, "customers/me", cancellationToken);
    }

    public static async Task UpdateCustomerAsync(UpsertCustomerProfileDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsJsonAsync("customers/me", dto, cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task UpdateCustomerProfileImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsJsonAsync(
            "customers/me/profile-image",
            new UploadMediaDto(imageUrl, "profile_photo", "Customer profile photo"),
            cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task UpdateCustomerValidIdAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsJsonAsync(
            "customers/me/valid-id",
            new UploadMediaDto(imageUrl, "valid_id", "Customer valid ID"),
            cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task ResendEmailVerificationOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        using var response = await http.PostAsJsonAsync("auth/resend-otp", new ResendOtpRequestDto(email, "email_verification"), cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task DeleteCustomerAccountAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.DeleteAsync("customers/me", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await ReadAsync<object>(response, cancellationToken);
        }
    }

    public static async Task<CustomerAddressDto> UpsertAddressAsync(CustomerAddressDto? existing, UpsertCustomerAddressDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = existing is null
            ? await http.PostAsJsonAsync("customers/address", dto, cancellationToken)
            : await http.PutAsJsonAsync($"customers/address/{existing.AddressId}", dto, cancellationToken);
        return await ReadAsync<CustomerAddressDto>(response, cancellationToken);
    }

    public static async Task<MotorcycleDto> UpsertMotorcycleAsync(MotorcycleDto? existing, UpsertMotorcycleDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = existing is null
            ? await http.PostAsJsonAsync("customers/motorcycles", dto, cancellationToken)
            : await http.PutAsJsonAsync($"customers/motorcycles/{existing.MotorcycleId}", dto, cancellationToken);
        return await ReadAsync<MotorcycleDto>(response, cancellationToken);
    }

    public static async Task<IReadOnlyList<ServiceRequestDto>> GetMyRequestsAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<ServiceRequestDto>>(http, "service-requests/my", cancellationToken);
    }

    public static async Task<ServiceRequestDto> GetRequestAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<ServiceRequestDto>(http, $"service-requests/{requestId}", cancellationToken);
    }

    public static async Task<IReadOnlyList<PaymentDto>> GetPaymentHistoryAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<PaymentDto>>(http, "payments/history", cancellationToken);
    }

    public static async Task<PaymentDto?> GetLatestPaymentForRequestAsync(int requestId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
            return await GetAsync<PaymentDto>(http, $"payments/request/{requestId}/latest", cancellationToken);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to fetch latest payment for request {requestId}: {ex}");
            return null;
        }
    }

    public static async Task<PaymentDto> RefreshPaymentAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsync($"payments/{paymentId}/refresh", null, cancellationToken);
        return await ReadAsync<PaymentDto>(response, cancellationToken);
    }

    public static async Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<ConversationSummaryDto>>(http, "conversations", cancellationToken);
    }

    public static async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<NotificationDto>>(http, "notifications", cancellationToken);
    }

    public static async Task MarkNotificationReadAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsync($"notifications/{notificationId}/read", null, cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<MessageDto>>(http, $"conversations/{conversationId}/messages", cancellationToken);
    }

    public static async Task MarkConversationReadAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsync($"conversations/{conversationId}/read-all", null, cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task<UploadedFileDto> UploadFileAsync(FileResult file, string folder = "chat", CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        await using var stream = await file.OpenReadAsync();
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeFor(file));
        content.Add(streamContent, "file", file.FileName);
        content.Add(new StringContent(folder), "folder");

        using var response = await http.PostAsync("files/upload", content, cancellationToken);
        return await ReadAsync<UploadedFileDto>(response, cancellationToken);
    }

    public static async Task<MessageDto> SendMessageAsync(int conversationId, string messageText, string? attachmentUrl = null, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync(
            $"conversations/{conversationId}/messages",
            new SendMessageDto(messageText, attachmentUrl),
            cancellationToken);
        return await ReadAsync<MessageDto>(response, cancellationToken);
    }

    public static async Task<IReadOnlyList<ShopSummaryDto>> GetShopsAsync(
        decimal? latitude = null,
        decimal? longitude = null,
        string? concern = null,
        CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        var encodedConcern = string.IsNullOrWhiteSpace(concern)
            ? ""
            : Uri.EscapeDataString(concern);
        var endpoint = latitude is not null && longitude is not null
            ? $"shops/nearby?latitude={latitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={longitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}&radiusKm=50{(encodedConcern.Length == 0 ? "" : $"&concern={encodedConcern}")}"
            : $"services/shops{(encodedConcern.Length == 0 ? "" : $"?concern={encodedConcern}")}";
        var shops = await GetAsync<IReadOnlyList<ShopSummaryDto>>(http, endpoint, cancellationToken);
        if (shops.Count > 0 || latitude is null || longitude is null)
        {
            return shops;
        }

        var fallbackEndpoint = $"services/shops{(encodedConcern.Length == 0 ? "" : $"?concern={encodedConcern}")}";
        return await GetAsync<IReadOnlyList<ShopSummaryDto>>(http, fallbackEndpoint, cancellationToken);
    }

    public static async Task<ShopDetailsDto> GetShopDetailsAsync(int shopId, CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<ShopDetailsDto>(http, $"shops/{shopId}", cancellationToken);
    }

    public static async Task<ShopReputationDto> GetShopReputationAsync(
        int shopId,
        CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<ShopReputationDto>(http, $"shops/{shopId}/reputation", cancellationToken);
    }

    public static async Task<IReadOnlyList<ShopServiceDto>> GetShopServicesAsync(int shopId, CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<IReadOnlyList<ShopServiceDto>>(http, $"services/shops/{shopId}/services", cancellationToken);
    }

    public static async Task<IReadOnlyList<ProductDto>> GetShopProductsAsync(int shopId, CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<IReadOnlyList<ProductDto>>(http, $"products/shop/{shopId}", cancellationToken);
    }

    public static async Task<IReadOnlyList<ShopServiceDto>> SearchServicesAsync(CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<IReadOnlyList<ShopServiceDto>>(http, "services/search", cancellationToken);
    }

    public static async Task<ServiceRequestDto> CreateRequestAsync(CreateServiceRequestDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync("service-requests", dto, cancellationToken);
        return await ReadAsync<ServiceRequestDto>(response, cancellationToken);
    }

    public static async Task<ServiceRequestDto> SelectShopAsync(int requestId, SelectShopDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsJsonAsync($"service-requests/{requestId}/select-shop", dto, cancellationToken);
        return await ReadAsync<ServiceRequestDto>(response, cancellationToken);
    }

    public static async Task AttachRequestMediaAsync(int requestId, UploadMediaDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync($"service-requests/{requestId}/media", dto, cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task<LiveLocationDto?> GetLatestRequestLocationAsync(int requestId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
            return await GetAsync<LiveLocationDto>(http, $"location/request/{requestId}/latest", cancellationToken);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to fetch latest location for request {requestId}: {ex}");
            return null;
        }
    }

    public static async Task<MechanicProfileDto> GetMechanicProfileAsync(int mechanicId, CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<MechanicProfileDto>(http, $"mechanics/{mechanicId}", cancellationToken);
    }

    public static async Task<PaymentDto> CreateCheckoutAsync(CreateCheckoutSessionDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync("payments/create-checkout-session", dto, cancellationToken);
        return await ReadAsync<PaymentDto>(response, cancellationToken);
    }

    public static async Task<ReviewDto> SubmitReviewAsync(CreateReviewDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync("reviews", dto, cancellationToken);
        return await ReadAsync<ReviewDto>(response, cancellationToken);
    }

    public static async Task<IReadOnlyList<PhilippineRegionDto>> GetPhilippineRegionsAsync(CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<IReadOnlyList<PhilippineRegionDto>>(http, "geography/regions", cancellationToken);
    }

    public static async Task<IReadOnlyList<PhilippineLocalityDto>> GetPhilippineLocalitiesAsync(
        string regionCode,
        CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<IReadOnlyList<PhilippineLocalityDto>>(
            http,
            $"geography/regions/{Uri.EscapeDataString(regionCode)}/localities",
            cancellationToken);
    }

    public static async Task<IReadOnlyList<PhilippineBarangayDto>> GetPhilippineBarangaysAsync(
        string localityCode,
        CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<IReadOnlyList<PhilippineBarangayDto>>(
            http,
            $"geography/localities/{Uri.EscapeDataString(localityCode)}/barangays",
            cancellationToken);
    }

    public static async Task<PhilippineLocationMatchDto?> ResolvePhilippineLocationAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        using var response = await http.GetAsync(
            $"geography/resolve?query={Uri.EscapeDataString(query)}",
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadAsync<PhilippineLocationMatchDto>(response, cancellationToken);
    }

    public static async Task<MapPointDto> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<MapPointDto>(
            http,
            $"maps/geocode?address={Uri.EscapeDataString(address)}",
            cancellationToken);
    }

    private static Task<T> GetAsync<T>(HttpClient http, string endpoint, CancellationToken cancellationToken)
    {
        return ApiClientHelper.GetAsync<T>(http, endpoint, cancellationToken);
    }

    private static Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        return ApiClientHelper.ReadAsync<T>(response, cancellationToken);
    }

    private static string ContentTypeFor(FileResult file)
    {
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            return file.ContentType;
        }

        return ContentTypeHelper.GuessFromExtension(Path.GetExtension(file.FileName));
    }
}
