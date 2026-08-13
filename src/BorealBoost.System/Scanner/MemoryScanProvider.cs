using BorealBoost.Core.Scanner;
using BorealBoost.System.Wmi;

namespace BorealBoost.System.Scanner;

public sealed class MemoryScanProvider : ISystemScanProvider
{
    private readonly WmiQueryService _wmi;

    public MemoryScanProvider(WmiQueryService wmi)
    {
        _wmi = wmi;
    }

    public string Name => "Memory";

    public int Weight => 10;

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        return ScanProviderSupport.RunAsync(Name, DataSourceKind.Wmi, CollectCoreAsync, cancellationToken);
    }

    private async Task<SystemSnapshotPatch> CollectCoreAsync(CancellationToken cancellationToken)
    {
        var computerRows = await _wmi.QueryAsync(
            "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem",
            Timeout,
            cancellationToken).ConfigureAwait(false);
        var moduleRows = await _wmi.QueryAsync(
            "SELECT Capacity,Manufacturer,PartNumber,ConfiguredClockSpeed,Speed FROM Win32_PhysicalMemory",
            Timeout,
            cancellationToken).ConfigureAwait(false);

        var modules = moduleRows.Select(row => new MemoryModuleSnapshot(
            row.UInt64("Capacity"),
            row.String("Manufacturer"),
            row.String("PartNumber"),
            row.Int32("ConfiguredClockSpeed"),
            row.Int32("Speed"),
            DataSourceKind.Wmi)).ToArray();

        var visiblePhysicalBytes = computerRows.FirstOrDefault()?.UInt64("TotalPhysicalMemory");
        var installedPhysicalBytes = modules.Length > 0 && modules.All(module => module.CapacityBytes.HasValue)
            ? (ulong?)modules.Aggregate(0UL, (totalBytes, module) => totalBytes + module.CapacityBytes!.Value)
            : null;

        return new SystemSnapshotPatch(Memory: new MemorySnapshot(installedPhysicalBytes, visiblePhysicalBytes, modules.Length, modules, DataSourceKind.Wmi));
    }
}
