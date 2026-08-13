using BorealBoost.Core.Scanner;
using BorealBoost.System.Wmi;

namespace BorealBoost.System.Scanner;

public sealed class DriverInventoryScanProvider : ISystemScanProvider
{
    private readonly WmiQueryService _wmi;

    public DriverInventoryScanProvider(WmiQueryService wmi)
    {
        _wmi = wmi;
    }

    public string Name => "Drivers";

    public int Weight => 10;

    public TimeSpan Timeout => TimeSpan.FromSeconds(12);

    public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        return ScanProviderSupport.RunAsync(Name, DataSourceKind.Wmi, CollectCoreAsync, cancellationToken);
    }

    private async Task<SystemSnapshotPatch> CollectCoreAsync(CancellationToken cancellationToken)
    {
        var rows = await _wmi.QueryAsync(
            "SELECT DeviceName,DeviceID,DeviceClass,Manufacturer,DriverProviderName,DriverVersion,DriverDate,InfName,Signer,IsSigned FROM Win32_PnPSignedDriver",
            Timeout,
            cancellationToken).ConfigureAwait(false);

        var drivers = rows.Select(row => new DriverSnapshot(
            row.String("DeviceName"),
            row.String("DeviceID"),
            row.String("DeviceClass"),
            row.String("Manufacturer"),
            row.String("DriverProviderName"),
            row.String("DriverVersion"),
            row.CimDateTime("DriverDate"),
            row.String("InfName"),
            row.String("Signer"),
            row.Bool("IsSigned"),
            DeviceHealthStatus.Unknown,
            DataSourceKind.Wmi)).ToArray();

        return new SystemSnapshotPatch(Drivers: drivers);
    }
}
