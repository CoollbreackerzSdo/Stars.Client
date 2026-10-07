using Stars.Client.Features.Discovery;
using Stars.Client.Features.Platforms;
using Stars.Client.Features.Playback;
using Stars.Client.Features.Transport;
using Stars.Shared;

namespace Stars.Client;

/// <summary>
/// Composition root for the client features. Selects the transport (Wi-Fi / USB / Bluetooth),
/// decodes received Opus frames and plays them, and auto-reconnects when the link drops.
/// </summary>
public sealed class AppServices : IDisposable
{
    private readonly AudioPlayer _player = new();
    private readonly INotificationService _notifications = NotificationServiceFactory.Create();
    private IAudioTransport? _transport;
    private TransportEndpoint? _endpoint;
    private CancellationTokenSource? _reconnectCts;
    private int _attempt;

    public UdpDiscoveryClient Discovery { get; } = new();

    public BluetoothDiscovery Bluetooth { get; } = new();

    public bool IsPlaying => _player.IsRunning;

    public bool IsConnected => _transport?.IsConnected ?? false;

    public bool IsReconnecting { get; private set; }

    public bool AutoReconnectEnabled { get; set; } = true;

    public double Volume
    {
        get => _player.Volume;
        set => _player.Volume = value;
    }

    /// <summary>The currently selected audio output.</summary>
    public AudioOutputDevice Output
    {
        get => _player.Output;
        set => _player.SetOutput(value);
    }

    /// <summary>Lists the audio outputs available on this device.</summary>
    public IReadOnlyList<AudioOutputDevice> GetOutputs() => AudioOutputs.List();

    // ---- Audio tuning (pushed to the server over the control channel) ------
    public int TargetBitrate { get; set; } = 128_000;

    public bool UseFec { get; set; } = true;

    public bool AdaptiveBitrate { get; set; }

    /// <summary>Pushes the current audio settings to the server.</summary>
    public void ApplyAudioSettings()
    {
        // Rough loss estimate: underruns against the expected frame count.
        var expected = Math.Max(1, _player.Received);
        var loss = (int)Math.Clamp(_player.Underruns * 100 / expected, 0, 50);

        var control = StreamProtocol.CreateAudioSettingsControl(TargetBitrate, UseFec, loss);
        _transport?.Send(control);
    }

    /// <summary>Updates the selector shown to the user based on measured jitter and loss.</summary>
    public void EvaluateAdaptiveBitrate()
    {
        if (!AdaptiveBitrate || !IsPlaying)
            return;

        var expected = Math.Max(1, _player.Received);
        var loss = (int)Math.Clamp(_player.Underruns * 100 / expected, 0, 50);
        var jitter = _player.JitterMilliseconds;

        int target;
        if (loss > 2 || jitter > 40)
            target = 96_000;
        else if (loss == 0 && jitter < 12)
            target = 160_000;
        else
            target = 128_000;

        if (target != TargetBitrate)
        {
            TargetBitrate = target;
            ApplyAudioSettings();
        }
    }

    /// <summary>Current adaptive jitter buffer depth, in milliseconds.</summary>
    public int BufferMilliseconds => _player.BufferMilliseconds;

    /// <summary>Measured inter-arrival jitter, in milliseconds.</summary>
    public double JitterMilliseconds => _player.JitterMilliseconds;

    /// <summary>Accumulated underruns since playback started.</summary>
    public long Underruns => _player.Underruns;

    /// <summary>Total frames received (for loss estimation).</summary>
    public long Received => _player.Received;

    public event Action<string>? Log;

    public event Action? PlaybackStopped;

    public event Action<bool>? ReconnectingChanged;

    public async Task StartAsync(TransportEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        _endpoint = endpoint;
        _attempt = 0;
        await EnsureNotificationPermissionAsync().ConfigureAwait(false);
        await ConnectInternalAsync(endpoint, cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureNotificationPermissionAsync()
    {
#if ANDROID
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
                await Permissions.RequestAsync<Permissions.PostNotifications>();
        }
        catch
        {
            // Notifications are optional; ignore if the user declines or the API is unavailable.
        }
#else
        await Task.CompletedTask;
#endif
    }

    private async Task ConnectInternalAsync(TransportEndpoint endpoint, CancellationToken cancellationToken)
    {
        StopTransport();

        var transport = CreateTransport(endpoint.Kind);
        transport.Log += message => Log?.Invoke(message);
        transport.FrameReceived += (buffer, offset, length) => _player.Enqueue(buffer, offset, length);
        transport.Disconnected += OnTransportDisconnected;

        await transport.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
        _transport = transport;

        _player.Start(transport.SampleRate, transport.Channels, transport.FrameSamplesPerChannel);

        // Tell the server which audio settings to use.
        ApplyAudioSettings();

        _notifications.ShowPlaying($"Reproduciendo audio de {endpoint.Host}");
        Log?.Invoke("Reproduciendo salida del sistema en tiempo real.");
    }

    private static IAudioTransport CreateTransport(TransportKind kind) => kind switch
    {
        TransportKind.Bluetooth => new BluetoothAudioTransport(),
        _ => new TcpAudioTransport()
    };

    private void OnTransportDisconnected()
    {
        if (!AutoReconnectEnabled || _endpoint is null)
        {
            PlaybackStopped?.Invoke();
            return;
        }

        _ = ReconnectLoopAsync(_endpoint);
    }

    private async Task ReconnectLoopAsync(TransportEndpoint endpoint)
    {
        if (_reconnectCts is not null)
            return;

        _reconnectCts = new CancellationTokenSource();
        var token = _reconnectCts.Token;
        SetReconnecting(true);

        try
        {
            while (!token.IsCancellationRequested && _attempt < 8)
            {
                _attempt++;
                var delay = TimeSpan.FromSeconds(Math.Min(1 + _attempt, 6));
                Log?.Invoke($"Reconectando… intento {_attempt} en {delay.TotalSeconds:0} s.");

                try
                {
                    await Task.Delay(delay, token).ConfigureAwait(false);
                    await ConnectInternalAsync(endpoint, token).ConfigureAwait(false);
                    Log?.Invoke("Reconectado.");
                    SetReconnecting(false);
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"Reintento fallido: {ex.Message}");
                }
            }
        }
        finally
        {
            SetReconnecting(false);
            _reconnectCts?.Dispose();
            _reconnectCts = null;
            if (_attempt >= 8)
                PlaybackStopped?.Invoke();
        }
    }

    private void SetReconnecting(bool value)
    {
        if (IsReconnecting == value)
            return;

        IsReconnecting = value;
        ReconnectingChanged?.Invoke(value);
    }

    public void Stop()
    {
        AutoReconnectEnabled = false;
        _reconnectCts?.Cancel();
        StopTransport();
        _player.Stop();
        _notifications.Clear();
        Log?.Invoke("Reproducción detenida.");
    }

    private void StopTransport()
    {
        if (_transport is null)
            return;

        _transport.Disconnected -= OnTransportDisconnected;
        _transport.Disconnect();
        _transport.Dispose();
        _transport = null;
    }

    public void Dispose()
    {
        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        StopTransport();
        _player.Dispose();
        _notifications.Clear();
    }
}
