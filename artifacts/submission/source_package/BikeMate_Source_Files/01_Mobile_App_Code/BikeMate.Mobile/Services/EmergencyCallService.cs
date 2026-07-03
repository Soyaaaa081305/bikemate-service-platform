using BikeMate.Core.DTOs;
using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;

namespace BikeMate.Services;

internal interface IEmergencyCallService
{
    Task StartCallAsync(EmergencyCallSessionDto session, CancellationToken cancellationToken = default);
    Task EndCallAsync(int requestId, CancellationToken cancellationToken = default);
    Task<bool> ToggleMuteAsync(CancellationToken cancellationToken = default);
    Task<bool> ToggleCameraAsync(CancellationToken cancellationToken = default);
    Task<bool> ToggleSpeakerAsync(CancellationToken cancellationToken = default);
}

internal sealed class EmergencyCallService : IEmergencyCallService
{
    private bool _isMuted;
    private bool _isCameraEnabled = true;
    private bool _isSpeakerEnabled = true;
    private EmergencyCallSessionDto? _activeSession;

    public async Task StartCallAsync(EmergencyCallSessionDto session, CancellationToken cancellationToken = default)
    {
        await EnsurePermissionsAsync();
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(session.AppId) ||
            string.IsNullOrWhiteSpace(session.ChannelName) ||
            session.Uid is null ||
            string.IsNullOrWhiteSpace(session.Token))
        {
            throw new InvalidOperationException(session.Message);
        }

        _activeSession = session;
        Debug.WriteLine($"Agora emergency session ready. Request={session.RequestId}, Channel={session.ChannelName}, Uid={session.Uid}");
    }

    public Task EndCallAsync(int requestId, CancellationToken cancellationToken = default)
    {
        _activeSession = null;
        return Task.CompletedTask;
    }

    public Task<bool> ToggleMuteAsync(CancellationToken cancellationToken = default)
    {
        _isMuted = !_isMuted;
        return Task.FromResult(_isMuted);
    }

    public Task<bool> ToggleCameraAsync(CancellationToken cancellationToken = default)
    {
        _isCameraEnabled = !_isCameraEnabled;
        return Task.FromResult(_isCameraEnabled);
    }

    public Task<bool> ToggleSpeakerAsync(CancellationToken cancellationToken = default)
    {
        _isSpeakerEnabled = !_isSpeakerEnabled;
        return Task.FromResult(_isSpeakerEnabled);
    }

    private static async Task EnsurePermissionsAsync()
    {
        var microphone = await Permissions.RequestAsync<Permissions.Microphone>();
        if (microphone != PermissionStatus.Granted)
        {
            throw new InvalidOperationException("Microphone permission is required before joining an emergency call.");
        }

        await Permissions.RequestAsync<Permissions.Camera>();
    }
}
