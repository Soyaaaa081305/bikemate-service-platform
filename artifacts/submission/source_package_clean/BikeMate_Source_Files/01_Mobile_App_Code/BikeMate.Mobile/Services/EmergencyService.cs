using System.Net.Http.Json;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;

namespace BikeMate.Services;

internal static class EmergencyService
{
    public static async Task<EmergencyRequestStatusDto> CreateRequestAsync(CreateEmergencyRequestDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync("emergency/request", dto, cancellationToken);
        return await ReadAsync<EmergencyRequestStatusDto>(response, cancellationToken);
    }

    public static async Task<EmergencyRequestStatusDto> GetStatusAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.GetAsync($"emergency/request/{requestId}", cancellationToken);
        return await ReadAsync<EmergencyRequestStatusDto>(response, cancellationToken);
    }

    public static async Task<ConversationDto> GetConversationAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.GetAsync($"emergency/request/{requestId}/conversation", cancellationToken);
        return await ReadAsync<ConversationDto>(response, cancellationToken);
    }

    public static async Task<EmergencyRequestStatusDto> CancelAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsync($"emergency/request/{requestId}/cancel", null, cancellationToken);
        return await ReadAsync<EmergencyRequestStatusDto>(response, cancellationToken);
    }

    public static async Task<EmergencyCallSessionDto> StartCallAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsync($"emergency/request/{requestId}/call/start", null, cancellationToken);
        return await ReadAsync<EmergencyCallSessionDto>(response, cancellationToken);
    }

    public static async Task<EmergencyCallSessionDto> EndCallAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsync($"emergency/request/{requestId}/call/end", null, cancellationToken);
        return await ReadAsync<EmergencyCallSessionDto>(response, cancellationToken);
    }

    public static async Task<IReadOnlyList<NearbyResponderDto>> GetNearbyRespondersAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.GetAsync($"emergency/responders/nearby?latitude={latitude}&longitude={longitude}", cancellationToken);
        return await ReadAsync<IReadOnlyList<NearbyResponderDto>>(response, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await ApiConfig.ThrowIfAuthenticationFailedAsync(response);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                ? $"Emergency API request failed with {(int)response.StatusCode}."
                : error);
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new InvalidOperationException("The emergency API returned an empty response.");
    }
}
