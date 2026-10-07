using Stars.Shared;

#if ANDROID
using Android.Bluetooth;
using Android.Content;

namespace Stars.Client.Features.Discovery;

/// <summary>Lists bonded (previously paired) Bluetooth Classic devices on Android.</summary>
public sealed class BluetoothDiscovery
{
    public static bool IsSupported
    {
        get
        {
            try
            {
                var manager = (BluetoothManager?)Android.App.Application.Context.GetSystemService(Context.BluetoothService);
                return manager?.Adapter is not null;
            }
            catch
            {
                return false;
            }
        }
    }

    public IReadOnlyList<DiscoveredServer> GetBondedDevices()
    {
        var result = new List<DiscoveredServer>();

        try
        {
            var manager = (BluetoothManager?)Android.App.Application.Context.GetSystemService(Context.BluetoothService);
            var adapter = manager?.Adapter;
            if (adapter is null)
                return result;

            foreach (var device in adapter.BondedDevices ?? [])
            {
                var name = device.Name ?? "Dispositivo Bluetooth";
                var address = device.Address ?? string.Empty;
                if (!string.IsNullOrEmpty(address))
                    result.Add(new DiscoveredServer(name, address, 0, TransportKind.Bluetooth));
            }
        }
        catch
        {
            // ignore and return whatever was found
        }

        return result;
    }
}
#else
namespace Stars.Client.Features.Discovery;

/// <summary>Bluetooth discovery placeholder for platforms without an implementation yet.</summary>
public sealed class BluetoothDiscovery
{
    public static bool IsSupported => false;

    public IReadOnlyList<DiscoveredServer> GetBondedDevices() => [];
}
#endif
