using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Stars.Shared;

namespace Stars.Client.Features.Discovery;

/// <summary>
/// Broadcasts discovery probes on the LAN and collects answers for a short window.
/// </summary>
public sealed class UdpDiscoveryClient
{
    public async Task<IReadOnlyList<DiscoveredServer>> DiscoverAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var found = new Dictionary<string, DiscoveredServer>(StringComparer.OrdinalIgnoreCase);

        using var udp = new UdpClient();
        udp.EnableBroadcast = true;
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

        var probe = StreamProtocol.DiscoveryRequest.ToArray();

        // Broadcast reaches the whole subnet on most networks; the extra subnet-directed
        // probes cover routers that block 255.255.255.255.
        var endpoints = BuildProbeEndpoints();

        foreach (var endpoint in endpoints)
        {
            try
            {
                await udp.SendAsync(probe, endpoint, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Some adapters reject directed broadcast; ignore and keep probing.
            }
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(timeout);

        var prefix = StreamProtocol.DiscoveryResponsePrefix.ToArray();

        while (!linked.IsCancellationRequested)
        {
            try
            {
                var result = await udp.ReceiveAsync(linked.Token).ConfigureAwait(false);
                var buffer = result.Buffer;
                if (!buffer.AsSpan().StartsWith(prefix))
                    continue;

                if (!StreamProtocol.TryParseDiscoveryResponse(Encoding.UTF8.GetString(buffer), out var name, out var address, out var port))
                    continue;

                var server = new DiscoveredServer(name, address, port);
                found[server.Display] = server;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // ignore malformed packets
            }
        }

        return found.Values.ToList();
    }

    /// <summary>Builds broadcast, subnet-directed and loopback probe targets for the current adapters.</summary>
    private static List<IPEndPoint> BuildProbeEndpoints()
    {
        var endpoints = new List<IPEndPoint>();
        var seen = new HashSet<string>();

        void Add(IPAddress address)
        {
            if (seen.Add(address.ToString()))
                endpoints.Add(new IPEndPoint(address, StreamProtocol.DiscoveryUdpPort));
        }

        Add(IPAddress.Broadcast);
        Add(IPAddress.Loopback);

        try
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up ||
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                    if (unicast.IPv4Mask is null)
                        continue;

                    var directed = GetDirectedBroadcast(unicast.Address, unicast.IPv4Mask);
                    if (directed is not null)
                        Add(directed);
                }
            }
        }
        catch
        {
            // keep the default endpoints
        }

        return endpoints;
    }

    private static IPAddress? GetDirectedBroadcast(IPAddress address, IPAddress mask)
    {
        var ip = address.GetAddressBytes();
        var m = mask.GetAddressBytes();
        if (ip.Length != 4 || m.Length != 4)
            return null;

        // 255.255.255.255 masks have no subnet to target.
        if (m[0] == 255 && m[1] == 255 && m[2] == 255 && m[3] == 255)
            return null;

        var broadcast = new byte[4];
        for (var i = 0; i < 4; i++)
            broadcast[i] = (byte)(ip[i] | (byte)~m[i]);

        return new IPAddress(broadcast);
    }
}
