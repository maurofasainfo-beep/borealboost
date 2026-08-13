using BorealBoost.Core.Scanner;
using BorealBoost.System.Wmi;

namespace BorealBoost.System.Scanner;

public sealed class ServicesScanProvider : ISystemScanProvider
{
    private readonly WmiQueryService _wmi;

    public ServicesScanProvider(WmiQueryService wmi)
    {
        _wmi = wmi;
    }

    public string Name => "Services";

    public int Weight => 6;

    public TimeSpan Timeout => TimeSpan.FromSeconds(10);

    public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        return ScanProviderSupport.RunAsync(Name, DataSourceKind.Wmi, CollectCoreAsync, cancellationToken);
    }

    private async Task<SystemSnapshotPatch> CollectCoreAsync(CancellationToken cancellationToken)
    {
        var rows = await _wmi.QueryAsync(
            "SELECT Name,DisplayName,State,StartMode,ServiceType,Started FROM Win32_Service",
            Timeout,
            cancellationToken).ConfigureAwait(false);

        var services = rows.Select(row => new ServiceSnapshot(
            row.String("Name") ?? "Unknown",
            row.String("DisplayName"),
            row.String("State"),
            row.String("StartMode"),
            row.String("ServiceType"),
            row.Bool("Started"),
            DataSourceKind.Wmi)).ToArray();

        return new SystemSnapshotPatch(Services: services);
    }
}
