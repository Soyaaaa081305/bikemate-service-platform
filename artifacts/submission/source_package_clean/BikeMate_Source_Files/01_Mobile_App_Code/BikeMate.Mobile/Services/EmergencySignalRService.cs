using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using Microsoft.AspNetCore.SignalR.Client;

namespace BikeMate.Services;

internal sealed class EmergencySignalRService : IAsyncDisposable
{
    private HubConnection? _connection;

    public event Action<EmergencyRequestStatusDto>? EmergencyStatusChanged;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            return;
        }

        var hubUrl = ApiConfig.BaseUrl.Replace("/api/", "/hubs/emergency", StringComparison.OrdinalIgnoreCase);
        var token = await SecureStorage.Default.GetAsync("access_token");
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<EmergencyRequestStatusDto>("EmergencyStatusChanged", status => EmergencyStatusChanged?.Invoke(status));
        _connection.On<EmergencyRequestStatusDto>("EmergencyResponderAssigned", status => EmergencyStatusChanged?.Invoke(status));
        _connection.On<EmergencyRequestStatusDto>("EmergencyRequestCancelled", status => EmergencyStatusChanged?.Invoke(status));
        await _connection.StartAsync(cancellationToken);
    }

    public async Task JoinEmergencyRequestAsync(int requestId, CancellationToken cancellationToken = default)
    {
        if (_connection?.State != HubConnectionState.Connected)
        {
            await ConnectAsync(cancellationToken);
        }

        if (_connection is not null)
        {
            await _connection.InvokeAsync("JoinEmergencyRequestGroup", requestId, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
