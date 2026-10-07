using System.Threading;
using Stars.Shared;

namespace Stars.Client.Features.Transport;

/// <summary>
/// Shared logic for any transport that exposes a <see cref="Stream"/>: reads the stream
/// header, then continuously raises one event per received Opus packet.
/// </summary>
public abstract class StreamAudioTransport : IAudioTransport
{
    private readonly Lock _gate = new();
    private Stream? _stream;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public abstract string Name { get; }

    public bool IsConnected { get; private set; }

    public int SampleRate { get; private set; }

    public int Channels { get; private set; }

    public int FrameSamplesPerChannel { get; private set; }

    public event Action<byte[], int, int>? FrameReceived;

    public event Action<string>? Log;

    public event Action? Disconnected;

    /// <summary>Opens the underlying stream for the given endpoint.</summary>
    protected abstract Task<Stream> OpenStreamAsync(TransportEndpoint endpoint, CancellationToken cancellationToken);

    public async Task ConnectAsync(TransportEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        Disconnect();

        var stream = await OpenStreamAsync(endpoint, cancellationToken).ConfigureAwait(false);

        var header = new byte[StreamProtocol.HeaderSize];
        try
        {
            await stream.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            stream.Dispose();
            throw;
        }

        if (!StreamProtocol.TryParseStreamHeader(header, out var sampleRate, out var channels, out var frameSamples))
        {
            stream.Dispose();
            throw new InvalidDataException("El servidor respondió con un formato desconocido.");
        }

        lock (_gate)
        {
            _stream = stream;
            SampleRate = sampleRate;
            Channels = channels;
            FrameSamplesPerChannel = frameSamples;
            IsConnected = true;
        }

        Log?.Invoke($"[{Name}] conectado · {sampleRate} Hz · {channels} ch");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loop = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        var lengthHeader = new byte[2];
        var payload = new byte[StreamProtocol.MaxPacketBytes];

        try
        {
            while (!token.IsCancellationRequested)
            {
                await _stream!.ReadExactlyAsync(lengthHeader, token).ConfigureAwait(false);
                var length = StreamProtocol.ReadFrameLength(lengthHeader);

                if (length <= 0 || length > payload.Length)
                    break;

                await _stream.ReadExactlyAsync(payload.AsMemory(0, length), token).ConfigureAwait(false);
                FrameReceived?.Invoke(payload, 0, length);
            }
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[{Name}] conexión finalizada: {ex.Message}");
        }
        finally
        {
            Disconnected?.Invoke();
        }
    }

    public void Disconnect()
    {
        CancellationTokenSource? cts;
        Task? loop;
        Stream? stream;

        lock (_gate)
        {
            cts = _cts;
            loop = _loop;
            stream = _stream;
            _cts = null;
            _loop = null;
            _stream = null;
            IsConnected = false;
        }

        cts?.Cancel();
        try
        {
            loop?.Wait(TimeSpan.FromMilliseconds(500));
        }
        catch
        {
            // ignore
        }

        stream?.Dispose();
        cts?.Dispose();
        CloseTransportSpecific();
    }

    /// <summary>Writes a control message to the server without blocking the receive loop.</summary>
    public void Send(ReadOnlySpan<byte> data)
    {
        Stream? stream;
        lock (_gate)
            stream = _stream;

        if (stream is null || !IsConnected || data.Length == 0)
            return;

        try
        {
            stream.Write(data);
            stream.Flush();
        }
        catch
        {
            // A failed control message is not fatal; the stream will report disconnection.
        }
    }

    /// <summary>Hook for transports that hold extra resources (sockets, adapters) to release.</summary>
    protected virtual void CloseTransportSpecific()
    {
    }

    public void Dispose() => Disconnect();
}
