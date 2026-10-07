using System.Net.Sockets;

namespace Stars.Client.Features.Transport;

/// <summary>TCP transport used for Wi-Fi and USB (adb reverse) connections.</summary>
public sealed class TcpAudioTransport : StreamAudioTransport
{
    private TcpClient? _client;

    public override string Name => "TCP";

    protected override async Task<Stream> OpenStreamAsync(TransportEndpoint endpoint, CancellationToken cancellationToken)
    {
        var client = new TcpClient { NoDelay = true };

        // Fail fast instead of hanging for the OS default (~20s) when the address/port is wrong.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await client.ConnectAsync(endpoint.Host, endpoint.Port, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            client.Dispose();
            throw new TimeoutException($"La conexión a {endpoint.Host}:{endpoint.Port} agotó el tiempo de espera.");
        }
        catch
        {
            client.Dispose();
            throw;
        }

        _client = client;
        return client.GetStream();
    }

    protected override void CloseTransportSpecific()
    {
        _client?.Dispose();
        _client = null;
    }
}
