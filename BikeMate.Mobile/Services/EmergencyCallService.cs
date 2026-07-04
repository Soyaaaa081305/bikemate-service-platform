using System.Diagnostics;
using System.Net;
using System.Text.Json;
using BikeMate.Controls;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using Microsoft.Maui.ApplicationModel;
#if ANDROID
using Android.Content;
using Android.Media;
#endif

namespace BikeMate.Services;

internal interface IEmergencyCallService
{
    View? CallView { get; }
    int? ActiveRequestId { get; }
    bool IsCallActive { get; }
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
    private AgoraCallWebView? _callView;

    public View? CallView => _callView;
    public int? ActiveRequestId => _activeSession?.RequestId;
    public bool IsCallActive => _activeSession is not null && _callView is not null;

    public async Task StartCallAsync(EmergencyCallSessionDto session, CancellationToken cancellationToken = default)
    {
        if (IsCallActive && _activeSession?.RequestId == session.RequestId)
        {
            await ConfigureAudioRouteAsync(_isSpeakerEnabled);
            await EvaluateCallScriptAsync("window.bikeMateNudgeAudio && window.bikeMateNudgeAudio();");
            return;
        }

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
        _isMuted = false;
        _isCameraEnabled = true;
        _isSpeakerEnabled = true;
        await ConfigureAudioRouteAsync(true);
        _callView = new AgoraCallWebView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Source = new HtmlWebViewSource
            {
                Html = BuildCallHtml(session),
                BaseUrl = GetSecureBaseUrl()
            }
        };

        Debug.WriteLine($"Agora emergency session ready. Request={session.RequestId}, Channel={session.ChannelName}, Uid={session.Uid}");
    }

    public async Task EndCallAsync(int requestId, CancellationToken cancellationToken = default)
    {
        if (_callView is not null)
        {
            try
            {
                await _callView.EvaluateJavaScriptAsync("window.bikeMateLeave && window.bikeMateLeave();");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Agora leave script failed: {ex}");
            }
        }

        await ConfigureAudioRouteAsync(false, resetMode: true);
        _activeSession = null;
        _callView = null;
    }

    public async Task<bool> ToggleMuteAsync(CancellationToken cancellationToken = default)
    {
        _isMuted = !_isMuted;
        await EvaluateCallScriptAsync($"window.bikeMateSetMuted && window.bikeMateSetMuted({JsonSerializer.Serialize(_isMuted)});");
        return _isMuted;
    }

    public async Task<bool> ToggleCameraAsync(CancellationToken cancellationToken = default)
    {
        _isCameraEnabled = !_isCameraEnabled;
        await EvaluateCallScriptAsync($"window.bikeMateSetCamera && window.bikeMateSetCamera({JsonSerializer.Serialize(_isCameraEnabled)});");
        return _isCameraEnabled;
    }

    public async Task<bool> ToggleSpeakerAsync(CancellationToken cancellationToken = default)
    {
        _isSpeakerEnabled = !_isSpeakerEnabled;
        await ConfigureAudioRouteAsync(_isSpeakerEnabled);
        await EvaluateCallScriptAsync($"window.bikeMateSetSpeaker && window.bikeMateSetSpeaker({JsonSerializer.Serialize(_isSpeakerEnabled)});");
        await EvaluateCallScriptAsync("window.bikeMateNudgeAudio && window.bikeMateNudgeAudio();");
        return _isSpeakerEnabled;
    }

    private static async Task EnsurePermissionsAsync()
    {
        var microphone = await Permissions.RequestAsync<Permissions.Microphone>();
        if (microphone != PermissionStatus.Granted)
        {
            throw new InvalidOperationException("Microphone permission is required before joining an emergency call.");
        }

        var camera = await Permissions.RequestAsync<Permissions.Camera>();
        if (camera != PermissionStatus.Granted)
        {
            throw new InvalidOperationException("Camera permission is required before joining an emergency video call.");
        }
    }

    private async Task EvaluateCallScriptAsync(string script)
    {
        if (_callView is null)
        {
            return;
        }

        try
        {
            await _callView.EvaluateJavaScriptAsync(script);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Agora call script failed: {ex}");
        }
    }

    private static Task ConfigureAudioRouteAsync(bool speakerEnabled, bool resetMode = false)
    {
#if ANDROID
        try
        {
            var audioManager = Android.App.Application.Context.GetSystemService(Context.AudioService) as AudioManager;
            if (audioManager is null)
            {
                return Task.CompletedTask;
            }

            audioManager.Mode = resetMode ? Mode.Normal : Mode.InCommunication;
#pragma warning disable CA1422
            audioManager.SpeakerphoneOn = !resetMode && speakerEnabled;
#pragma warning restore CA1422
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Emergency call audio route failed: {ex}");
        }
#endif
        return Task.CompletedTask;
    }

    private static string GetSecureBaseUrl()
    {
        var apiBase = new Uri(ApiConfig.BaseUrl);
        return apiBase.GetLeftPart(UriPartial.Authority) + "/";
    }

    private static string BuildCallHtml(EmergencyCallSessionDto session)
    {
        var payload = JsonSerializer.Serialize(new
        {
            appId = session.AppId,
            channelName = session.ChannelName,
            token = session.Token,
            uid = session.Uid
        }).Replace("</", "<\\/", StringComparison.Ordinal);

        var encodedChannel = WebUtility.HtmlEncode(session.ChannelName ?? "Emergency call");

        return $$"""
<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
  <style>
    * { box-sizing: border-box; }
    html, body { margin: 0; width: 100%; height: 100%; overflow: hidden; background: #151515; color: #fff; font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; }
    .stage { position: relative; width: 100vw; height: 100vh; background: #101010; }
    #remote-player { position: absolute; inset: 0; display: grid; place-items: center; background: #161616; }
    #local-player { position: absolute; right: 12px; top: 12px; width: 34vw; max-width: 150px; aspect-ratio: 9 / 16; overflow: hidden; border-radius: 18px; border: 2px solid rgba(255,255,255,.65); background: #2a2a2a; box-shadow: 0 12px 30px rgba(0,0,0,.35); }
    #local-player:empty::after { content: "Camera"; display: grid; place-items: center; height: 100%; color: rgba(255,255,255,.72); font-weight: 700; }
    .status { position: absolute; left: 16px; right: 16px; bottom: 16px; padding: 12px 14px; border-radius: 16px; background: rgba(0,0,0,.68); line-height: 1.35; }
    .status strong { display: block; font-size: 13px; margin-bottom: 3px; }
    .status span { display: block; color: rgba(255,255,255,.78); font-size: 11px; }
    .placeholder { text-align: center; color: rgba(255,255,255,.74); padding: 24px; }
    .placeholder b { display: block; font-size: 18px; margin-bottom: 8px; color: #fff; }
    video { object-fit: cover !important; }
  </style>
</head>
<body>
  <main class="stage">
    <div id="remote-player"><div class="placeholder"><b>BikeMate Support</b><span id="remote-status">Waiting for remote video...</span></div></div>
    <div id="local-player"></div>
    <div class="status"><strong id="call-status">Joining {{encodedChannel}}...</strong><span id="audio-status">Starting camera and microphone.</span></div>
  </main>
  <script>window.__bikeMateSession = {{payload}};</script>
  <script src="https://download.agora.io/sdk/release/AgoraRTC_N.js"></script>
  <script>
    (function () {
      let client;
      let localAudioTrack;
      let localVideoTrack;
      let remoteAudioTrack;

      function text(id, value) {
        const el = document.getElementById(id);
        if (el) el.textContent = value;
      }

      function showError(error) {
        console.error(error);
        text("call-status", "Agora join failed");
        text("audio-status", error && error.message ? error.message : String(error));
      }

      async function cleanup() {
        if (localAudioTrack) { localAudioTrack.close(); localAudioTrack = null; }
        if (localVideoTrack) { localVideoTrack.close(); localVideoTrack = null; }
        if (client) {
          try { await client.leave(); } catch (_) { }
          client = null;
        }
      }

      async function join() {
        const session = window.__bikeMateSession;
        if (!window.AgoraRTC) throw new Error("Agora Web SDK did not load.");
        if (!session || !session.appId || !session.channelName || !session.token || session.uid == null) {
          throw new Error("Agora session is missing app id, channel, token, or uid.");
        }

        await cleanup();
        client = AgoraRTC.createClient({ mode: "rtc", codec: "vp8" });
        client.on("user-published", async function (user, mediaType) {
          await client.subscribe(user, mediaType);
          if (mediaType === "video" && user.videoTrack) {
            const remote = document.getElementById("remote-player");
            remote.innerHTML = "";
            user.videoTrack.play(remote);
            text("remote-status", "Remote video connected");
          }
          if (mediaType === "audio" && user.audioTrack) {
            remoteAudioTrack = user.audioTrack;
            remoteAudioTrack.play();
            text("audio-status", "Remote audio connected");
          }
        });
        client.on("user-unpublished", function (_, mediaType) {
          if (mediaType === "video") {
            document.getElementById("remote-player").innerHTML = '<div class="placeholder"><b>BikeMate Support</b><span>Remote camera is off</span></div>';
          }
        });
        client.on("user-left", function () {
          document.getElementById("remote-player").innerHTML = '<div class="placeholder"><b>BikeMate Support</b><span>Remote participant left</span></div>';
          text("audio-status", "Remote audio disconnected");
        });

        text("call-status", "Requesting camera and microphone...");
        await client.join(session.appId, session.channelName, session.token, Number(session.uid));
        [localAudioTrack, localVideoTrack] = await AgoraRTC.createMicrophoneAndCameraTracks();
        localVideoTrack.play("local-player");
        await client.publish([localAudioTrack, localVideoTrack]);
        text("call-status", "Connected to BikeMate emergency call");
        text("audio-status", "Microphone and camera are live.");
      }

      window.bikeMateSetMuted = async function (muted) {
        if (localAudioTrack) await localAudioTrack.setEnabled(!muted);
        text("audio-status", muted ? "Microphone muted." : "Microphone is live.");
      };

      window.bikeMateSetCamera = async function (enabled) {
        if (localVideoTrack) await localVideoTrack.setEnabled(enabled);
        const local = document.getElementById("local-player");
        local.style.opacity = enabled ? "1" : ".35";
      };

      window.bikeMateSetSpeaker = async function (enabled) {
        if (remoteAudioTrack) remoteAudioTrack.play();
        text("audio-status", enabled ? "Speaker output enabled." : "Phone audio route enabled.");
      };

      window.bikeMateNudgeAudio = async function () {
        if (remoteAudioTrack) remoteAudioTrack.play();
      };

      document.addEventListener("touchend", function () {
        if (remoteAudioTrack) remoteAudioTrack.play();
      }, { passive: true });

      window.bikeMateLeave = cleanup;
      window.addEventListener("beforeunload", cleanup);
      join().catch(showError);
    })();
  </script>
</body>
</html>
""";
    }
}
