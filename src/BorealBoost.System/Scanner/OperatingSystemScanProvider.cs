using System.Runtime.InteropServices;
using BorealBoost.Core.Scanner;
using BorealBoost.System.Registry;
using BorealBoost.System.Wmi;

namespace BorealBoost.System.Scanner;

public sealed class OperatingSystemScanProvider : ISystemScanProvider
{
    private const string CurrentVersionKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

    private readonly WmiQueryService _wmi;
    private readonly ReadOnlyRegistryReader _registry;

    public OperatingSystemScanProvider(WmiQueryService wmi, ReadOnlyRegistryReader registry)
    {
        _wmi = wmi;
        _registry = registry;
    }

    public string Name => "OperatingSystem";

    public int Weight => 12;

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        return ScanProviderSupport.RunAsync(Name, DataSourceKind.Composite, CollectCoreAsync, cancellationToken);
    }

    private async Task<SystemSnapshotPatch> CollectCoreAsync(CancellationToken cancellationToken)
    {
        var rows = await _wmi.QueryAsync(
            "SELECT Caption,Version,BuildNumber,OSArchitecture FROM Win32_OperatingSystem",
            Timeout,
            cancellationToken).ConfigureAwait(false);

        var os = rows.FirstOrDefault();
        var name = os?.String("Caption") ?? RuntimeInformation.OSDescription.Trim();
        var version = os?.String("Version") ?? Environment.OSVersion.Version.ToString();
        var architecture = os?.String("OSArchitecture") ?? RuntimeInformation.OSArchitecture.ToString();
        var build = ParseInt(os?.String("BuildNumber")) ?? Environment.OSVersion.Version.Build;
        var revision = _registry.ReadLocalMachineInt32(CurrentVersionKey, "UBR");
        var displayVersion = _registry.ReadLocalMachineString(CurrentVersionKey, "DisplayVersion") ??
                             _registry.ReadLocalMachineString(CurrentVersionKey, "ReleaseId");
        var edition = _registry.ReadLocalMachineString(CurrentVersionKey, "EditionID");
        var productName = _registry.ReadLocalMachineString(CurrentVersionKey, "ProductName");
        var displayName = PreferWindows11Name(name, productName, build);
        var compatibility = WindowsCompatibilityClassifier.Classify(displayName, build, architecture);

        return new SystemSnapshotPatch(
            OperatingSystem: new OperatingSystemSnapshot(
                displayName,
                edition,
                version,
                build,
                revision,
                displayVersion,
                architecture,
                compatibility.Status,
                compatibility.Reason,
                DataSourceKind.Composite));
    }

    private static int? ParseInt(string? value)
    {
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string? PreferWindows11Name(string? wmiCaption, string? registryProductName, int? build)
    {
        if (!string.IsNullOrWhiteSpace(wmiCaption))
        {
            return wmiCaption;
        }

        if (build >= 22000 && registryProductName?.Contains("Windows 10", StringComparison.OrdinalIgnoreCase) == true)
        {
            return registryProductName.Replace("Windows 10", "Windows 11", StringComparison.OrdinalIgnoreCase);
        }

        return registryProductName;
    }
}
