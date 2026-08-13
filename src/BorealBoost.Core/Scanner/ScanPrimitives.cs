using BorealBoost.Core.Common;
using BorealBoost.Core.Identity;

namespace BorealBoost.Core.Scanner;

public readonly record struct ScanId(Guid Value)
{
    public static ScanId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}

public enum DetectionStatus
{
    Known,
    Unknown,
    Unavailable,
    NotSupported,
    Deferred
}

public enum ProviderResultStatus
{
    Success,
    Partial,
    Failed,
    NotSupported,
    TimedOut,
    Canceled
}

public enum DataSourceKind
{
    DotNetRuntime,
    Environment,
    Wmi,
    RegistryReadOnly,
    FileSystemReadOnly,
    WindowsApi,
    NetworkInterface,
    DriveInfo,
    Composite,
    Unknown
}

public enum WindowsCompatibilityStatus
{
    Supported,
    LegacySupported,
    Unsupported,
    Unknown
}

public enum MachineFormFactor
{
    Desktop,
    Laptop,
    Convertible,
    Tablet,
    VirtualMachine,
    Unknown
}

public enum HardwareVendor
{
    Intel,
    Amd,
    Nvidia,
    Microsoft,
    HyperV,
    Vmware,
    VirtualBox,
    Other,
    Unknown
}

public enum GpuFormFactor
{
    Integrated,
    Dedicated,
    Virtual,
    Unknown
}

public enum StorageMediaKind
{
    Hdd,
    Ssd,
    Nvme,
    Unknown
}

public enum NetworkAdapterKind
{
    Ethernet,
    WiFi,
    Cellular,
    Virtual,
    Loopback,
    Tunnel,
    Other,
    Unknown
}

public enum DeviceHealthStatus
{
    Ok,
    MissingDriver,
    Problem,
    Disabled,
    Unknown
}

public enum PowerSourceKind
{
    AC,
    Battery,
    Unknown
}

public enum VramDetectionStatus
{
    Known,
    Estimated,
    Unknown
}

public enum ScanSessionState
{
    Idle,
    Running,
    Cancelling,
    Completed,
    Failed,
    Cancelled
}

public sealed record ScanIssue(string Code, string Message, string ProviderName);

public sealed record ProviderResult(
    string ProviderName,
    ProviderResultStatus Status,
    DataSourceKind Source,
    TimeSpan Duration,
    IReadOnlyList<ScanIssue> Warnings,
    IReadOnlyList<ScanIssue> Errors)
{
    public static ProviderResult Succeeded(string providerName, DataSourceKind source, TimeSpan duration, IReadOnlyList<ScanIssue>? warnings = null)
    {
        return new ProviderResult(providerName, ProviderResultStatus.Success, source, duration, warnings ?? [], []);
    }

    public static ProviderResult Partial(string providerName, DataSourceKind source, TimeSpan duration, IReadOnlyList<ScanIssue> warnings)
    {
        return new ProviderResult(providerName, ProviderResultStatus.Partial, source, duration, warnings, []);
    }

    public static ProviderResult Failed(string providerName, DataSourceKind source, TimeSpan duration, string code, string message)
    {
        return new ProviderResult(providerName, ProviderResultStatus.Failed, source, duration, [], [new ScanIssue(code, message, providerName)]);
    }

    public static ProviderResult NotSupported(string providerName, DataSourceKind source, TimeSpan duration, string code, string message)
    {
        return new ProviderResult(providerName, ProviderResultStatus.NotSupported, source, duration, [new ScanIssue(code, message, providerName)], []);
    }

    public static ProviderResult TimedOut(string providerName, TimeSpan duration)
    {
        return new ProviderResult(providerName, ProviderResultStatus.TimedOut, DataSourceKind.Unknown, duration, [], [new ScanIssue("scanner.provider.timeout", "Provider timed out.", providerName)]);
    }

    public static ProviderResult Canceled(string providerName, TimeSpan duration)
    {
        return new ProviderResult(providerName, ProviderResultStatus.Canceled, DataSourceKind.Unknown, duration, [], [new ScanIssue("scanner.provider.canceled", "Provider was canceled.", providerName)]);
    }
}

public sealed record ScanProgressUpdate(
    ScanId ScanId,
    int Percent,
    string Stage,
    string ProviderName,
    int CompletedWeight,
    int TotalWeight);

public interface ISystemScanner
{
    Task<Result<SystemSnapshot>> ScanAsync(IProgress<ScanProgressUpdate>? progress, CancellationToken cancellationToken);
}

public interface ISystemSnapshotStore
{
    SystemSnapshot? Current { get; }

    void Set(SystemSnapshot snapshot);

    void Clear();
}

public interface ISystemScanSessionService
{
    ScanSessionState State { get; }

    Task<Result<SystemSnapshot>> StartAsync(IProgress<ScanProgressUpdate>? progress, CancellationToken cancellationToken);

    void Cancel();
}

public interface ISystemScanProvider
{
    string Name { get; }

    int Weight { get; }

    TimeSpan Timeout { get; }

    Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken);
}

public sealed record ProviderScanResult(ProviderResult Result, SystemSnapshotPatch Patch)
{
    public static ProviderScanResult Empty(ProviderResult result)
    {
        return new ProviderScanResult(result, SystemSnapshotPatch.Empty);
    }
}

public sealed record SystemSnapshotPatch(
    OperatingSystemSnapshot? OperatingSystem = null,
    HardwareSnapshot? Hardware = null,
    IReadOnlyList<CpuSnapshot>? Processors = null,
    IReadOnlyList<GpuSnapshot>? Graphics = null,
    MemorySnapshot? Memory = null,
    StorageSnapshot? Storage = null,
    MotherboardSnapshot? Motherboard = null,
    FirmwareSnapshot? Firmware = null,
    IReadOnlyList<DeviceSnapshot>? Devices = null,
    IReadOnlyList<DriverSnapshot>? Drivers = null,
    IReadOnlyList<NetworkAdapterSnapshot>? Network = null,
    IReadOnlyList<DisplaySnapshot>? Displays = null,
    PowerSnapshot? Power = null,
    IReadOnlyList<ServiceSnapshot>? Services = null,
    IReadOnlyList<ProcessSnapshot>? Processes = null,
    IReadOnlyList<StartupItemSnapshot>? StartupItems = null,
    IReadOnlyList<SystemCapabilitySnapshot>? Capabilities = null)
{
    public static SystemSnapshotPatch Empty { get; } = new();
}
