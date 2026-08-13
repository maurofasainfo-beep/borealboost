namespace BorealBoost.Core.Scanner;

public enum SnapshotFieldClassification
{
    PublicTechnical,
    InternalTechnical,
    Sensitive,
    DoNotPersist,
    DoNotReport
}

public sealed record SnapshotPrivacyRule(
    string FieldPath,
    SnapshotFieldClassification Classification,
    string Reason);

public static class SystemSnapshotPrivacyPolicy
{
    public static IReadOnlyList<SnapshotPrivacyRule> Rules { get; } =
    [
        new("OperatingSystem.Name", SnapshotFieldClassification.PublicTechnical, "Needed for compatibility and customer-facing diagnostics."),
        new("Processors.Name", SnapshotFieldClassification.PublicTechnical, "Hardware summary fact."),
        new("Graphics.Name", SnapshotFieldClassification.PublicTechnical, "Hardware summary fact."),
        new("Memory.InstalledPhysicalBytes", SnapshotFieldClassification.PublicTechnical, "Hardware summary fact."),
        new("Memory.VisiblePhysicalBytes", SnapshotFieldClassification.PublicTechnical, "OS-visible hardware fact."),
        new("Devices.DeviceInstanceId", SnapshotFieldClassification.InternalTechnical, "Needed for future Driver Engine matching; not suitable for default reports."),
        new("Devices.HardwareIds", SnapshotFieldClassification.InternalTechnical, "Needed for future Driver Engine matching; fingerprinting risk."),
        new("Devices.CompatibleIds", SnapshotFieldClassification.InternalTechnical, "Needed for future Driver Engine matching; fingerprinting risk."),
        new("Drivers.DeviceInstanceId", SnapshotFieldClassification.InternalTechnical, "Needed for future Driver Engine correlation."),
        new("Drivers.InfName", SnapshotFieldClassification.InternalTechnical, "Driver diagnostic detail."),
        new("Network.Name", SnapshotFieldClassification.InternalTechnical, "May reveal VPN or enterprise tooling."),
        new("Network.Description", SnapshotFieldClassification.InternalTechnical, "May reveal VPN or enterprise tooling."),
        new("Processes.ProcessId", SnapshotFieldClassification.DoNotReport, "Ephemeral process identity."),
        new("Processes.ProcessName", SnapshotFieldClassification.DoNotReport, "May reveal customer activity."),
        new("Processes.WorkingSetBytes", SnapshotFieldClassification.InternalTechnical, "Performance fact, but not default report content."),
        new("Services.Name", SnapshotFieldClassification.InternalTechnical, "May reveal installed software and enterprise tooling."),
        new("Services.DisplayName", SnapshotFieldClassification.InternalTechnical, "May reveal installed software and enterprise tooling."),
        new("StartupItems.Name", SnapshotFieldClassification.InternalTechnical, "May reveal installed software."),
        new("StartupItems.SourceLocation", SnapshotFieldClassification.InternalTechnical, "Registry location fact, not default report content."),
        new("MachineName", SnapshotFieldClassification.DoNotReport, "Identifies customer environment and is not part of SystemSnapshot.")
    ];

    public static SystemSnapshot CreateReportSafeSnapshot(SystemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return snapshot with
        {
            Devices = snapshot.Devices.Select(device => device with
            {
                DeviceInstanceId = null,
                HardwareIds = [],
                CompatibleIds = []
            }).ToArray(),
            Drivers = snapshot.Drivers.Select(driver => driver with
            {
                DeviceInstanceId = null,
                InfName = null
            }).ToArray(),
            Network = snapshot.Network.Select(adapter => adapter with
            {
                Name = adapter.Kind.ToString(),
                Description = null
            }).ToArray(),
            Processes = [],
            Services = [],
            StartupItems = []
        };
    }
}
