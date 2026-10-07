using Stars.Client.Shared;
using Stars.Shared;

#if ANDROID
using Android.Bluetooth;
using Android.Content;

namespace Stars.Client.Features.Transport;

/// <summary>
/// Bluetooth Classic (RFCOMM/SPP) transport. Connects to the server's published SDP service
/// so audio can flow over Bluetooth without any Wi-Fi network.
/// </summary>
public sealed class BluetoothAudioTransport : StreamAudioTransport
{
    private BluetoothSocket? _socket;

    public override string Name => "Bluetooth";

    protected override async Task<Stream> OpenStreamAsync(TransportEndpoint endpoint, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(endpoint.BluetoothAddress))
            throw new InvalidOperationException("Falta la dirección del dispositivo Bluetooth.");

        var manager = (BluetoothManager?)Android.App.Application.Context.GetSystemService(Context.BluetoothService);
        var adapter = manager?.Adapter
            ?? throw new InvalidOperationException("Este dispositivo no tiene Bluetooth.");

        if (!adapter.IsEnabled)
            throw new InvalidOperationException("Activa el Bluetooth para continuar.");

        var device = adapter.GetRemoteDevice(endpoint.BluetoothAddress)
            ?? throw new InvalidOperationException("No se encontró el dispositivo Bluetooth.");

        var uuid = Java.Util.UUID.FromString(StreamProtocol.BluetoothServiceUuid)!;

        var socket = device.CreateRfcommSocketToServiceRecord(uuid)
            ?? throw new InvalidOperationException("No se pudo crear el socket Bluetooth.");

        try
        {
            await Task.Run(socket.Connect, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            socket.Close();
            socket.Dispose();
            throw;
        }

        _socket = socket;

        // Xamarin/Android exposes the socket ends directly as System.IO.Stream.
        return new DuplexStream(socket.InputStream!, socket.OutputStream!);
    }

    protected override void CloseTransportSpecific()
    {
        try
        {
            _socket?.Close();
        }
        catch
        {
            // ignore
        }

        _socket?.Dispose();
        _socket = null;
    }
}
#else
namespace Stars.Client.Features.Transport;

/// <summary>Bluetooth transport placeholder for platforms without an implementation yet.</summary>
public sealed class BluetoothAudioTransport : StreamAudioTransport
{
    public override string Name => "Bluetooth";

    protected override Task<Stream> OpenStreamAsync(TransportEndpoint endpoint, CancellationToken cancellationToken)
        => throw new PlatformNotSupportedException("La reproducción por Bluetooth solo está implementada en Android.");
}
#endif
