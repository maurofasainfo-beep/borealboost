using System.Runtime.InteropServices;
using BorealBoost.Core.Scanner;
using BorealBoost.System.Registry;

namespace BorealBoost.System.Scanner;

public sealed class PowerScanProvider : ISystemScanProvider
{
    private const string PowerSchemesKey = @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes";

    private readonly ReadOnlyRegistryReader _registry;

    public PowerScanProvider(ReadOnlyRegistryReader registry)
    {
        _registry = registry;
    }

    public string Name => "Power";

    public int Weight => 6;

    public TimeSpan Timeout => TimeSpan.FromSeconds(5);

    public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        return ScanProviderSupport.RunAsync(Name, DataSourceKind.Composite, CollectCoreAsync, cancellationToken);
    }

    private Task<SystemSnapshotPatch> CollectCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var hasPowerStatus = GetSystemPowerStatus(out var status);
        bool? batteryPresent = hasPowerStatus ? (status.BatteryFlag & 128) == 0 && status.BatteryFlag != 255 : null;
        bool? acConnected = status.ACLineStatus switch
        {
            0 => false,
            1 => true,
            _ => null
        };
        int? batteryPercent = status.BatteryLifePercent is <= 100 ? status.BatteryLifePercent : null;
        var activeScheme = _registry.ReadLocalMachineString(PowerSchemesKey, "ActivePowerScheme");
        var source = acConnected switch
        {
            true => PowerSourceKind.AC,
            false when batteryPresent == true => PowerSourceKind.Battery,
            _ => PowerSourceKind.Unknown
        };

        var snapshot = new PowerSnapshot(
            batteryPresent,
            acConnected,
            batteryPercent,
            source,
            activeScheme,
            DataSourceKind.Composite);

        return Task.FromResult(new SystemSnapshotPatch(Power: snapshot));
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus lpSystemPowerStatus);

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }
}
