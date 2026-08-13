using System.Net.NetworkInformation;
using BorealBoost.Core.Scanner;

namespace BorealBoost.System.Scanner;

public sealed class NetworkScanProvider : ISystemScanProvider
{
    public string Name => "Network";

    public int Weight => 8;

    public TimeSpan Timeout => TimeSpan.FromSeconds(5);

    public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        return ScanProviderSupport.RunAsync(Name, DataSourceKind.NetworkInterface, CollectCoreAsync, cancellationToken);
    }

    private static Task<SystemSnapshotPatch> CollectCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var adapters = NetworkInterface.GetAllNetworkInterfaces()
            .Select(adapter => new NetworkAdapterSnapshot(
                adapter.Name,
                adapter.Description,
                Classify(adapter),
                adapter.OperationalStatus.ToString(),
                adapter.Speed > 0 ? adapter.Speed : null,
                IsKnownVirtual(adapter),
                DataSourceKind.NetworkInterface))
            .ToArray();

        return Task.FromResult(new SystemSnapshotPatch(Network: adapters));
    }

    private static NetworkAdapterKind Classify(NetworkInterface adapter)
    {
        if (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
        {
            return NetworkAdapterKind.WiFi;
        }

        if (adapter.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet or NetworkInterfaceType.FastEthernetFx or NetworkInterfaceType.FastEthernetT)
        {
            return IsKnownVirtual(adapter) == true ? NetworkAdapterKind.Virtual : NetworkAdapterKind.Ethernet;
        }

        if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
        {
            return NetworkAdapterKind.Loopback;
        }

        if (adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
        {
            return NetworkAdapterKind.Tunnel;
        }

        if (adapter.NetworkInterfaceType is NetworkInterfaceType.Wwanpp or NetworkInterfaceType.Wwanpp2)
        {
            return NetworkAdapterKind.Cellular;
        }

        return NetworkAdapterKind.Other;
    }

    private static bool? IsKnownVirtual(NetworkInterface adapter)
    {
        var text = $"{adapter.Name} {adapter.Description}";
        if (text.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("VMware", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Loopback", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("TAP", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (adapter.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211)
        {
            return false;
        }

        return null;
    }
}
