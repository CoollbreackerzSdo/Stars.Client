using Stars.Shared;

namespace Stars.Client.Features.Transport;

/// <summary>Everything needed to reach a server over a specific transport.</summary>
public sealed record TransportEndpoint(
    TransportKind Kind,
    string Host,
    int Port,
    string? BluetoothAddress = null)
{
    public static TransportEndpoint Wifi(string host, int port) => new(TransportKind.Wifi, host, port);

    public static TransportEndpoint Usb(int port) => new(TransportKind.Usb, StreamProtocol.UsbLoopbackAddress, port);

    public static TransportEndpoint Bluetooth(string address) => new(TransportKind.Bluetooth, string.Empty, 0, address);
}

/// <summary>Abstraction over the way audio frames travel from the server to this client.</summary>
public interface IAudioTransport : IDisposable
{
    /// <summary>Friendly name of the transport, shown in the UI.</summary>
    string Name { get; }

    bool IsConnected { get; }

    int SampleRate { get; }

    int Channels { get; }

    int FrameSamplesPerChannel { get; }

    event Action<byte[], int, int>? FrameReceived;

    event Action<string>? Log;

    event Action? Disconnected;

    Task ConnectAsync(TransportEndpoint endpoint, CancellationToken cancellationToken = default);

    /// <summary>Sends a control message to the server (fire and forget).</summary>
    void Send(ReadOnlySpan<byte> data);

    void Disconnect();
}
