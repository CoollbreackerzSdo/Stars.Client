using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Stars.Client.Features.Discovery;
using Stars.Client.Features.Platforms;
using Stars.Client.Features.Transport;
using Stars.Shared;

namespace Stars.Client;

/// <summary>View model for the client slice: discover/connect the server and render the stream.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly AppServices _services = new();
    private CancellationTokenSource? _tickCts;

    public MainViewModel()
    {
        _services.Log += message => MainThread.BeginInvokeOnMainThread(() => AppendLog(message));
        _services.PlaybackStopped += () => MainThread.BeginInvokeOnMainThread(HandleStopped);
        _services.ReconnectingChanged += value => MainThread.BeginInvokeOnMainThread(() => OnReconnectingChanged(value));

        _services.Volume = Volume;
        BluetoothSupported = BluetoothDiscovery.IsSupported;
        IsDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        _selectedBitrate = BitrateOptions[2];

        LoadOutputs();

        AppendLog("Elige un transporte, busca servidores o escribe una dirección y pulsa Conectar.");
    }

    public ObservableCollection<string> LogLines { get; } = new();

    public ObservableCollection<DiscoveredServer> Servers { get; } = new();

    /// <summary>Audio outputs available on this device.</summary>
    public ObservableCollection<AudioOutputDevice> Outputs { get; } = new();

    [ObservableProperty]
    public partial AudioOutputDevice SelectedOutput { get; set; } = AudioOutputDevice.Default;

    partial void OnSelectedOutputChanged(AudioOutputDevice value)
    {
        if (value is null)
            return;

        _services.Output = value;
    }

    private void LoadOutputs()
    {
        var currentId = SelectedOutput?.Id ?? AudioOutputDevice.DefaultId;
        Outputs.Clear();
        foreach (var device in _services.GetOutputs())
        {
            Outputs.Add(device);
            if (device.Id == currentId)
                SelectedOutput = device;
        }

        if (Outputs.Count > 0 && SelectedOutput is null)
            SelectedOutput = Outputs[0];
    }

    // ---- Appearance --------------------------------------------------------
    [ObservableProperty]
    public partial bool IsDark { get; set; }

    partial void OnIsDarkChanged(bool value)
    {
        var theme = value ? AppTheme.Dark : AppTheme.Light;
        if (Application.Current is not null && Application.Current.UserAppTheme != theme)
            Application.Current.UserAppTheme = theme;

        Preferences.Default.Set("stars-theme-dark", value);
    }

    // ---- Audio quality -----------------------------------------------------
    public sealed record BitrateOption(int Value, string Label);

    public ObservableCollection<BitrateOption> BitrateOptions { get; } = new()
    {
        new(64_000, "64 kbps"),
        new(96_000, "96 kbps"),
        new(128_000, "128 kbps"),
        new(160_000, "160 kbps"),
        new(192_000, "192 kbps"),
        new(256_000, "256 kbps"),
    };

    private BitrateOption _selectedBitrate;

    public BitrateOption SelectedBitrate
    {
        get => _selectedBitrate;
        set
        {
            if (value is null || !SetProperty(ref _selectedBitrate, value))
                return;

            _services.TargetBitrate = value.Value;
            _services.ApplyAudioSettings();
        }
    }

    private bool _useFec = true;
    public bool UseFec
    {
        get => _useFec;
        set
        {
            if (!SetProperty(ref _useFec, value))
                return;

            _services.UseFec = value;
            _services.ApplyAudioSettings();
        }
    }

    private bool _adaptiveBitrate;
    public bool AdaptiveBitrate
    {
        get => _adaptiveBitrate;
        set
        {
            if (!SetProperty(ref _adaptiveBitrate, value))
                return;

            _services.AdaptiveBitrate = value;
            OnPropertyChanged(nameof(IsManualBitrate));
        }
    }

    public bool IsManualBitrate => !AdaptiveBitrate;

    [ObservableProperty]
    public partial string QualityLabel { get; private set; } = "128 kbps · FEC";

    [ObservableProperty]
    public partial TransportKind SelectedKind { get; set; } = TransportKind.Wifi;

    [ObservableProperty]
    public partial bool BluetoothSupported { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWifiMode))]
    [NotifyPropertyChangedFor(nameof(IsBluetoothMode))]
    public partial string Host { get; set; } = "192.168.1.10";

    public bool IsWifiMode => SelectedKind != TransportKind.Bluetooth;

    public bool IsBluetoothMode => SelectedKind == TransportKind.Bluetooth;

    [ObservableProperty]
    public partial string Port { get; set; } = StreamProtocol.DefaultTcpPort.ToString();

    [ObservableProperty]
    public partial string BluetoothAddress { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DiscoverCommand))]
    public partial bool IsConnecting { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    public partial bool IsPlaying { get; private set; }

    public bool IsIdle => !IsPlaying;

    [ObservableProperty]
    public partial bool IsReconnecting { get; private set; }

    [ObservableProperty]
    public partial string StatusText { get; private set; } = "Desconectado";

    [ObservableProperty]
    public partial Color StatusColor { get; private set; } = Color.FromArgb("#6E668F");

    /// <summary>Playback volume between 0 and 1.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VolumeLabel))]
    public partial double Volume { get; set; } = 0.8;

    public string VolumeLabel => $"{Volume * 100:0}%";

    partial void OnVolumeChanged(double value)
    {
        _services.Volume = value;
        if (value <= 0.001)
            _ = Toast.Make("Silencio").Show();
    }

    partial void OnSelectedKindChanged(TransportKind value)
    {
        OnPropertyChanged(nameof(IsWifiMode));
        OnPropertyChanged(nameof(IsBluetoothMode));
    }

    /// <summary>Pseudo level for the fluffy equalizer animation (0..1).</summary>
    [ObservableProperty]
    public partial double Level { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PacketsLabel))]
    public partial long Packets { get; private set; }

    public string PacketsLabel => $"{Packets} paquetes recibidos";

    /// <summary>Human-readable description of the adaptive jitter buffer state.</summary>
    [ObservableProperty]
    public partial string BufferLabel { get; private set; } = "Buffer —";

    private bool CanConnect() => !IsPlaying && !IsConnecting;

    private bool CanDisconnect() => IsPlaying;

    private bool CanDiscover() => !IsConnecting;

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        TransportEndpoint endpoint;
        try
        {
            endpoint = BuildEndpoint();
        }
        catch (Exception ex)
        {
            await Toast.Make(ex.Message).Show();
            return;
        }

        IsConnecting = true;
        StatusText = "Conectando…";
        StatusColor = Color.FromArgb("#FF9E6D");

        try
        {
            _services.AutoReconnectEnabled = true;
            await _services.StartAsync(endpoint);
            IsPlaying = true;
            StatusText = "En vivo";
            StatusColor = Color.FromArgb("#4ED8B4");
            StartTicker();
        }
        catch (Exception ex)
        {
            IsPlaying = false;
            StatusText = "Error de conexión";
            StatusColor = Color.FromArgb("#FF6B6B");
            AppendLog($"No se pudo conectar: {ex.Message}");
            await Toast.Make($"No se pudo conectar: {ex.Message}").Show();
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private TransportEndpoint BuildEndpoint()
    {
        switch (SelectedKind)
        {
            case TransportKind.Usb:
                if (!int.TryParse(Port, out var usbPort))
                    throw new InvalidOperationException("El puerto debe ser un número.");
                return TransportEndpoint.Usb(usbPort);

            case TransportKind.Bluetooth:
                if (string.IsNullOrWhiteSpace(BluetoothAddress))
                    throw new InvalidOperationException("Selecciona un dispositivo Bluetooth emparejado.");
                return TransportEndpoint.Bluetooth(BluetoothAddress);

            default:
                if (!int.TryParse(Port, out var port))
                    throw new InvalidOperationException("El puerto debe ser un número.");
                return TransportEndpoint.Wifi(Host, port);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        _tickCts?.Cancel();
        await Task.Run(_services.Stop);
        HandleStopped();
    }

    [RelayCommand(CanExecute = nameof(CanDiscover))]
    private async Task DiscoverAsync()
    {
        IsConnecting = true;
        StatusText = "Buscando…";
        StatusColor = Color.FromArgb("#4EA8FF");

        try
        {
            Servers.Clear();

            if (SelectedKind == TransportKind.Bluetooth)
            {
                foreach (var device in _services.Bluetooth.GetBondedDevices())
                    Servers.Add(device);

                if (Servers.Count > 0)
                {
                    BluetoothAddress = Servers[0].Address;
                    AppendLog($"Encontrados {Servers.Count} dispositivo(s) emparejado(s). Pulsa Conectar.");
                }
                else
                {
                    AppendLog("No hay dispositivos Bluetooth emparejados. Empareja el PC primero.");
                }
            }
            else
            {
                var found = await _services.Discovery.DiscoverAsync(TimeSpan.FromSeconds(2));
                foreach (var server in found)
                    Servers.Add(server);

                if (found.Count > 0)
                {
                    UseServer(found[0]);
                    AppendLog($"Encontrados {found.Count} servidor(es). Pulsa Conectar.");
                }
                else
                {
                    AppendLog("No se encontraron servidores. Usa el botón 'Copiar dirección' del servidor y escribe la IP y el puerto.");
                }
            }

            StatusText = "Desconectado";
            StatusColor = Color.FromArgb("#6E668F");
        }
        catch (Exception ex)
        {
            AppendLog($"Error al buscar: {ex.Message}");
        }
        finally
        {
            IsConnecting = false;
        }
    }

    public void UseServer(DiscoveredServer server)
    {
        SelectedKind = server.Kind;
        if (server.Kind == TransportKind.Bluetooth)
        {
            BluetoothAddress = server.Address;
        }
        else
        {
            Host = server.Address;
            Port = server.Port.ToString();
        }
    }

    private void OnReconnectingChanged(bool reconnecting)
    {
        IsReconnecting = reconnecting;
        if (reconnecting)
        {
            StatusText = "Reconectando…";
            StatusColor = Color.FromArgb("#FF9E6D");
        }
        else if (_services.IsConnected)
        {
            StatusText = "En vivo";
            StatusColor = Color.FromArgb("#4ED8B4");
        }
    }

    private void HandleStopped()
    {
        _tickCts?.Cancel();
        IsPlaying = false;
        IsReconnecting = false;
        Level = 0;
        StatusText = "Desconectado";
        StatusColor = Color.FromArgb("#6E668F");
    }

    private void StartTicker()
    {
        _tickCts?.Cancel();
        _tickCts = new CancellationTokenSource();
        var token = _tickCts.Token;
        var activity = 0.0;
        long packets = 0;

        _ = Task.Run(async () =>
        {
            var random = new Random();
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(450));
            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    packets += random.Next(15, 24);
                    activity = Math.Clamp(activity + random.NextDouble() * 0.5 - 0.15, 0.15, 1.0);
                    var level = _services.IsPlaying ? activity : 0;
                    var snapshot = packets;
                    var buffer = _services.IsPlaying
                        ? $"Buffer {_services.BufferMilliseconds} ms · jitter {_services.JitterMilliseconds:0.0} ms · {_services.TargetBitrate / 1000} kbps"
                        : "Buffer —";
                    var quality = (_services.AdaptiveBitrate ? "Adaptativo" : "Manual") +
                                  $" · {_services.TargetBitrate / 1000} kbps · FEC {(_services.UseFec ? "on" : "off")}";

                    // Adjust the encoder bitrate to the measured link quality.
                    _services.EvaluateAdaptiveBitrate();

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Level = level;
                        Packets = snapshot;
                        BufferLabel = buffer;
                        QualityLabel = quality;
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }, token);
    }

    private void AppendLog(string message)
    {
        LogLines.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        while (LogLines.Count > 40)
            LogLines.RemoveAt(LogLines.Count - 1);
    }
}
