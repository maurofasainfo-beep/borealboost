using BorealBoost.Core.Scanner;

namespace BorealBoost.Tests.Unit;

public sealed class SystemSnapshotPrivacyPolicyTests
{
    [Fact]
    public void Privacy_policy_classifies_driver_matching_ids_as_internal_technical()
    {
        Assert.Contains(SystemSnapshotPrivacyPolicy.Rules, rule =>
            rule.FieldPath == "Devices.HardwareIds" &&
            rule.Classification == SnapshotFieldClassification.InternalTechnical);
        Assert.Contains(SystemSnapshotPrivacyPolicy.Rules, rule =>
            rule.FieldPath == "MachineName" &&
            rule.Classification == SnapshotFieldClassification.DoNotReport);
    }

    [Fact]
    public void Report_safe_snapshot_redacts_sensitive_technical_fields()
    {
        var snapshot = CreateSnapshot();

        var sanitized = SystemSnapshotPrivacyPolicy.CreateReportSafeSnapshot(snapshot);

        Assert.Null(sanitized.Devices.Single().DeviceInstanceId);
        Assert.Empty(sanitized.Devices.Single().HardwareIds);
        Assert.Empty(sanitized.Devices.Single().CompatibleIds);
        Assert.Null(sanitized.Drivers.Single().DeviceInstanceId);
        Assert.Null(sanitized.Drivers.Single().InfName);
        Assert.Empty(sanitized.Processes);
        Assert.Empty(sanitized.Services);
        Assert.Empty(sanitized.StartupItems);
        Assert.Equal(NetworkAdapterKind.Ethernet.ToString(), sanitized.Network.Single().Name);
        Assert.Null(sanitized.Network.Single().Description);
    }

    private static SystemSnapshot CreateSnapshot()
    {
        return new SystemSnapshot(
            new ScanMetadata(
                ScanId.New(),
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds(1),
                "2.0.0",
                "2.0.0",
                "X64",
                [],
                false,
                [],
                []),
            new OperatingSystemSnapshot("Windows", null, null, 26200, null, null, "x64", WindowsCompatibilityStatus.Supported, null, DataSourceKind.Unknown),
            new HardwareSnapshot(null, null, MachineFormFactor.Unknown, false, null, DataSourceKind.Unknown),
            [],
            [],
            new MemorySnapshot(17_179_869_184, 17_099_431_936, 2, [], DataSourceKind.Wmi),
            new StorageSnapshot([], [], DataSourceKind.Unknown),
            new MotherboardSnapshot(null, null, null, DataSourceKind.Unknown),
            new FirmwareSnapshot(null, null, null, null, null, DataSourceKind.Unknown),
            [new DeviceSnapshot("Device", "PCI\\VEN_1234&DEV_ABCD", ["PCI\\VEN_1234"], ["PCI\\VEN_1234&CC_0300"], "Vendor", "Display", DeviceHealthStatus.Ok, 0, "OK", DataSourceKind.Wmi)],
            [new DriverSnapshot("Device", "PCI\\VEN_1234&DEV_ABCD", "Display", "Vendor", "Provider", "1.0", null, "driver.inf", "Signer", true, DeviceHealthStatus.Ok, DataSourceKind.Wmi)],
            [new NetworkAdapterSnapshot("Ethernet 1", "Vendor Adapter", NetworkAdapterKind.Ethernet, "Up", 1_000_000_000, false, DataSourceKind.NetworkInterface)],
            [],
            new PowerSnapshot(null, null, null, PowerSourceKind.Unknown, null, DataSourceKind.Unknown),
            [new ServiceSnapshot("VendorService", "Vendor Service", "Running", "Auto", "Own Process", true, DataSourceKind.Wmi)],
            [new ProcessSnapshot(1234, "customer-app", 4096, DataSourceKind.DotNetRuntime)],
            [new StartupItemSnapshot("Startup", "HKCU64\\Run", DataSourceKind.RegistryReadOnly)],
            []);
    }
}
