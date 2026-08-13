using BorealBoost.Core.Scanner;
using BorealBoost.System.Wmi;

namespace BorealBoost.System.Scanner;

public sealed class GraphicsScanProvider : ISystemScanProvider
{
    private readonly WmiQueryService _wmi;

    public GraphicsScanProvider(WmiQueryService wmi)
    {
        _wmi = wmi;
    }

    public string Name => "Graphics";

    public int Weight => 10;

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        return ScanProviderSupport.RunAsync(Name, DataSourceKind.Wmi, CollectCoreAsync, cancellationToken);
    }

    private async Task<SystemSnapshotPatch> CollectCoreAsync(CancellationToken cancellationToken)
    {
        var rows = await _wmi.QueryAsync(
            "SELECT Name,AdapterCompatibility,PNPDeviceID,DeviceID,DriverVersion,DriverDate,AdapterRAM,Status FROM Win32_VideoController",
            Timeout,
            cancellationToken).ConfigureAwait(false);

        var graphics = rows.Select(row =>
        {
            var name = row.String("Name");
            var vendor = HardwareVendorClassifier.ClassifyCpuOrDeviceVendor(row.String("AdapterCompatibility") ?? name);
            var pnpDeviceId = row.String("PNPDeviceID");
            var formFactor = GpuFormFactorClassifier.Classify(name, vendor);
            var vram = GpuMemoryReliabilityClassifier.FromWmiAdapterRam(row.UInt64("AdapterRAM"), formFactor);

            return new GpuSnapshot(
                name,
                vendor,
                ExtractDeviceId(pnpDeviceId) ?? row.String("DeviceID"),
                pnpDeviceId,
                row.String("DriverVersion"),
                row.CimDateTime("DriverDate"),
                vram.Bytes,
                vram.Status,
                row.String("Status"),
                formFactor,
                DataSourceKind.Wmi);
        }).ToArray();

        return new SystemSnapshotPatch(Graphics: graphics);
    }

    private static string? ExtractDeviceId(string? pnpDeviceId)
    {
        if (string.IsNullOrWhiteSpace(pnpDeviceId))
        {
            return null;
        }

        var marker = "DEV_";
        var index = pnpDeviceId.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0 || pnpDeviceId.Length < index + marker.Length + 4)
        {
            return null;
        }

        return pnpDeviceId.Substring(index + marker.Length, 4);
    }
}
