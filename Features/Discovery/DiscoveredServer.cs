using Stars.Shared;

namespace Stars.Client.Features.Discovery;

/// <summary>One server found on the local network (Wi-Fi) or among bonded Bluetooth devices.</summary>
public sealed record DiscoveredServer(string Name, string Address, int Port, TransportKind Kind = TransportKind.Wifi)
{
    public string Display => Kind switch
    {
        TransportKind.Bluetooth => $"{Name} · Bluetooth",
        _ => $"{Name} · {Address}:{Port}"
    };

    public override string ToString() => Display;
}
