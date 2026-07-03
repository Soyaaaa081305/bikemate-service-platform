using System.Net.Http.Json;
using System.Net;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using Microsoft.Maui.Storage;

namespace BikeMate.Services;

internal static class RiderApiClient
{
    public static async Task<RiderDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<RiderDashboardDto>(http, "rider/dashboard", cancellationToken);
    }

    public static async Task<UserProfileDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<UserProfileDto>(http, "auth/me", cancellationToken);
    }

    public static async Task<IReadOnlyList<ServiceRequestDto>> GetIncomingAsync(decimal radiusKm = 8m, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<ServiceRequestDto>>(http, $"rider/requests/incoming?radiusKm={radiusKm}", cancellationToken);
    }

    public static async Task<IReadOnlyList<ServiceRequestDto>> GetEmergencyAsync(decimal radiusKm = 12m, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<ServiceRequestDto>>(http, $"rider/requests/emergency?radiusKm={radiusKm}", cancellationToken);
    }

    public static async Task<ServiceRequestDto?> GetCurrentJobAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.GetAsync("rider/jobs/current", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                ? $"API request failed with {(int)response.StatusCode}."
                : error);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return System.Text.Json.JsonSerializer.Deserialize<ServiceRequestDto>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public static async Task<ServiceRequestDto> GetJobAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<ServiceRequestDto>(http, $"rider/jobs/{requestId}", cancellationToken);
    }

    public static async Task<IReadOnlyList<ServiceRequestDto>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<ServiceRequestDto>>(http, "rider/jobs/history", cancellationToken);
    }

    public static async Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<ConversationSummaryDto>>(http, "conversations", cancellationToken);
    }

    public static async Task<ConversationSummaryDto> GetConversationAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<ConversationSummaryDto>(http, $"conversations/{conversationId}", cancellationToken);
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

    public static async Task<MessageDto> SendMessageAsync(int conversationId, string messageText, string? attachmentUrl = null, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync($"conversations/{conversationId}/messages", new SendMessageDto(messageText, attachmentUrl), cancellationToken);
        return await ReadAsync<MessageDto>(response, cancellationToken);
    }

    public static async Task<MechanicProfileDto> GoOnlineAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsync("rider/status/online", null, cancellationToken);
        return await ReadAsync<MechanicProfileDto>(response, cancellationToken);
    }

    public static async Task<MechanicProfileDto> GoOfflineAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsync("rider/status/offline", null, cancellationToken);
        return await ReadAsync<MechanicProfileDto>(response, cancellationToken);
    }

    public static async Task<MechanicProfileDto> UpdateProfileAsync(UpdateMechanicProfileDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsJsonAsync("mechanics/me", dto, cancellationToken);
        return await ReadAsync<MechanicProfileDto>(response, cancellationToken);
    }

    public static async Task<ServiceRequestDto> AcceptAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsync($"rider/jobs/{requestId}/accept", null, cancellationToken);
        return await ReadAsync<ServiceRequestDto>(response, cancellationToken);
    }

    public static async Task<ServiceRequestDto> RejectAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsync($"rider/jobs/{requestId}/reject", null, cancellationToken);
        return await ReadAsync<ServiceRequestDto>(response, cancellationToken);
    }

    public static async Task<ServiceRequestDto> UpdateStatusAsync(int requestId, string status, string? notes = null, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsJsonAsync($"rider/jobs/{requestId}/status", new UpdateRequestStatusDto(status, notes), cancellationToken);
        return await ReadAsync<ServiceRequestDto>(response, cancellationToken);
    }

    public static async Task<LiveLocationDto> UpdateLocationAsync(int? requestId, decimal latitude, decimal longitude, decimal? accuracyMeters = null, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync("rider/location", new LocationUpdateDto(requestId, null, latitude, longitude, accuracyMeters), cancellationToken);
        return await ReadAsync<LiveLocationDto>(response, cancellationToken);
    }

    public static async Task<UploadedFileDto> UploadFileAsync(FileResult file, string folder = "mechanic-jobs", CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        await using var stream = await file.OpenReadAsync();
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(ContentTypeFor(file));
        content.Add(streamContent, "file", file.FileName);
        content.Add(new StringContent(folder), "folder");

        using var response = await http.PostAsync("files/upload", content, cancellationToken);
        return await ReadAsync<UploadedFileDto>(response, cancellationToken);
    }

    public static async Task AttachBeforePhotoAsync(int requestId, UploadMediaDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync($"rider/jobs/{requestId}/before-photo", dto, cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task AttachAfterPhotoAsync(int requestId, UploadMediaDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync($"rider/jobs/{requestId}/after-photo", dto, cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task<RiderEarningsDto> GetEarningsAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<RiderEarningsDto>(http, "rider/earnings", cancellationToken);
    }

    public static async Task<IReadOnlyList<ReviewDto>> GetRatingsAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<ReviewDto>>(http, "rider/ratings", cancellationToken);
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

        return BikeMate.Core.Helpers.ContentTypeHelper.GuessFromExtension(Path.GetExtension(file.FileName));
    }
}

internal sealed record RiderDashboardDto(
    MechanicProfileDto Profile,
    string AvailabilityStatus,
    ServiceRequestDto? ActiveJob,
    int IncomingRequests,
    int EmergencyRequests,
    decimal TotalEarnings,
    decimal AverageRating,
    int TotalCompletedJobs,
    int UnreadNotifications);

internal sealed record RiderEarningsDto(decimal Total, IReadOnlyList<PaymentDto> Payments);
