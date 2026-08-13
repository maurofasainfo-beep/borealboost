using System.Runtime.InteropServices;
using BorealBoost.Core.Scanner;
using BorealBoost.System.Wmi;

namespace BorealBoost.System.Scanner;

public sealed class CpuScanProvider : ISystemScanProvider
{
    private readonly WmiQueryService _wmi;

    public CpuScanProvider(WmiQueryService wmi)
    {
        _wmi = wmi;
    }

    public string Name => "Cpu";

    public int Weight => 10;

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        return ScanProviderSupport.RunAsync(Name, DataSourceKind.Wmi, CollectCoreAsync, cancellationToken);
    }

    private async Task<SystemSnapshotPatch> CollectCoreAsync(CancellationToken cancellationToken)
    {
        var rows = await _wmi.QueryAsync(
            "SELECT Manufacturer,Name,NumberOfLogicalProcessors,NumberOfCores,SocketDesignation,MaxClockSpeed,CurrentClockSpeed,ProcessorId,Family,VirtualizationFirmwareEnabled FROM Win32_Processor",
            Timeout,
            cancellationToken).ConfigureAwait(false);

        var processors = rows.Select(row =>
        {
            var manufacturer = row.String("Manufacturer");
            var name = row.String("Name");
            var vendor = HardwareVendorClassifier.ClassifyCpuOrDeviceVendor(manufacturer ?? name);
            return new CpuSnapshot(
                manufacturer,
                name,
                vendor,
                RuntimeInformation.OSArchitecture.ToString(),
                Math.Max(1, row.Int32("NumberOfLogicalProcessors") ?? Environment.ProcessorCount),
                row.Int32("NumberOfCores"),
                string.IsNullOrWhiteSpace(row.String("SocketDesignation")) ? null : 1,
                row.Int32("MaxClockSpeed"),
                row.Int32("CurrentClockSpeed"),
                row.String("ProcessorId"),
                row.UInt16("Family"),
                row.Bool("VirtualizationFirmwareEnabled"),
                DataSourceKind.Wmi);
        }).ToArray();

        if (processors.Length == 0)
        {
            processors =
            [
                new CpuSnapshot(
                    null,
                    null,
                    HardwareVendor.Unknown,
                    RuntimeInformation.OSArchitecture.ToString(),
                    Environment.ProcessorCount,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    DataSourceKind.Environment)
            ];
        }

        return new SystemSnapshotPatch(Processors: processors);
    }
}
