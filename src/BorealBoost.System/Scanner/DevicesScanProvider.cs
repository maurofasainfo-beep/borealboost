using BorealBoost.Core.Scanner;
using BorealBoost.System.Wmi;

namespace BorealBoost.System.Scanner;

public sealed class DevicesScanProvider : ISystemScanProvider
{
    private readonly WmiQueryService _wmi;

    public DevicesScanProvider(WmiQueryService wmi)
    {
        _wmi = wmi;
    }

    public string Name => "Devices";

    public int Weight => 10;

    public TimeSpan Timeout => TimeSpan.FromSeconds(12);

    public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        return ScanProviderSupport.RunAsync(Name, DataSourceKind.Wmi, CollectCoreAsync, cancellationToken);
    }

    private async Task<SystemSnapshotPatch> CollectCoreAsync(CancellationToken cancellationToken)
    {
        var rows = await _wmi.QueryAsync(
            "SELECT Name,DeviceID,HardwareID,CompatibleID,Manufacturer,PNPClass,Status,ConfigManagerErrorCode FROM Win32_PnPEntity",
            Timeout,
            cancellationToken).ConfigureAwait(false);

        var devices = rows.Select(row =>
        {
            var problemCode = row.UInt32("ConfigManagerErrorCode");
            var status = row.String("Status");
            return new DeviceSnapshot(
                row.String("Name"),
                row.String("DeviceID"),
                row.StringArray("HardwareID"),
                row.StringArray("CompatibleID"),
                row.String("Manufacturer"),
                row.String("PNPClass"),
                DeviceHealthClassifier.Classify(problemCode, status),
                problemCode,
                status,
                DataSourceKind.Wmi);
        }).ToArray();

        return new SystemSnapshotPatch(Devices: devices);
    }
}
