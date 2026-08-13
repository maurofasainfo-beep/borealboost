namespace BorealBoost.Core.Scanner;

public sealed record SystemSnapshot(
    ScanMetadata Metadata,
    OperatingSystemSnapshot OperatingSystem,
    HardwareSnapshot Hardware,
    IReadOnlyList<CpuSnapshot> Processors,
    IReadOnlyList<GpuSnapshot> Graphics,
    MemorySnapshot Memory,
    StorageSnapshot Storage,
    MotherboardSnapshot Motherboard,
    FirmwareSnapshot Firmware,
    IReadOnlyList<DeviceSnapshot> Devices,
    IReadOnlyList<DriverSnapshot> Drivers,
    IReadOnlyList<NetworkAdapterSnapshot> Network,
    IReadOnlyList<DisplaySnapshot> Displays,
    PowerSnapshot Power,
    IReadOnlyList<ServiceSnapshot> Services,
    IReadOnlyList<ProcessSnapshot> Processes,
    IReadOnlyList<StartupItemSnapshot> StartupItems,
    IReadOnlyList<SystemCapabilitySnapshot> Capabilities);

public sealed record ScanMetadata(
    ScanId ScanId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    TimeSpan Duration,
    string AppVersion,
    string SchemaVersion,
    string MachineArchitecture,
    IReadOnlyList<ProviderResult> ProviderResults,
    bool PartialScan,
    IReadOnlyList<ScanIssue> Warnings,
    IReadOnlyList<ScanIssue> Errors);

public sealed record OperatingSystemSnapshot(
    string? Name,
    string? Edition,
    string? Version,
    int? Build,
    int? Revision,
    string? DisplayVersion,
    string Architecture,
    WindowsCompatibilityStatus BorealBoostCompatibility,
    string? CompatibilityReason,
    DataSourceKind Source);

public sealed record HardwareSnapshot(
    string? Manufacturer,
    string? Model,
    MachineFormFactor FormFactor,
    bool IsVirtualMachine,
    string? VirtualizationPlatform,
    DataSourceKind Source);

public sealed record CpuSnapshot(
    string? Manufacturer,
    string? Name,
    HardwareVendor Vendor,
    string Architecture,
    int LogicalProcessors,
    int? PhysicalCores,
    int? Sockets,
    int? MaxClockMHz,
    int? CurrentClockMHz,
    string? ProcessorIdentifier,
    ushort? Family,
    bool? VirtualizationCapable,
    DataSourceKind Source);

public sealed record GpuSnapshot(
    string? Name,
    HardwareVendor Vendor,
    string? DeviceId,
    string? PnpDeviceId,
    string? DriverVersion,
    DateTimeOffset? DriverDate,
    ulong? AdapterRamBytes,
    VramDetectionStatus AdapterRamStatus,
    string? Status,
    GpuFormFactor FormFactor,
    DataSourceKind Source);

public sealed record MemorySnapshot(
    ulong? InstalledPhysicalBytes,
    ulong? VisiblePhysicalBytes,
    int ModuleCount,
    IReadOnlyList<MemoryModuleSnapshot> Modules,
    DataSourceKind Source);

public sealed record MemoryModuleSnapshot(
    ulong? CapacityBytes,
    string? Manufacturer,
    string? PartNumber,
    int? ConfiguredClockMHz,
    int? NominalSpeedMHz,
    DataSourceKind Source);

public sealed record StorageSnapshot(
    IReadOnlyList<StorageDiskSnapshot> Disks,
    IReadOnlyList<StorageVolumeSnapshot> Volumes,
    DataSourceKind Source);

public sealed record StorageDiskSnapshot(
    string? Model,
    string? Manufacturer,
    ulong? CapacityBytes,
    StorageMediaKind MediaKind,
    string? BusType,
    string? Status,
    DataSourceKind Source);

public sealed record StorageVolumeSnapshot(
    string Name,
    string? VolumeLabel,
    string DriveType,
    long? TotalBytes,
    long? FreeBytes,
    bool IsSystemDrive,
    DataSourceKind Source);

public sealed record MotherboardSnapshot(
    string? Manufacturer,
    string? Product,
    string? Version,
    DataSourceKind Source);

public sealed record FirmwareSnapshot(
    string? Manufacturer,
    string? Version,
    DateTimeOffset? ReleaseDate,
    string? FirmwareType,
    bool? SecureBootEnabled,
    DataSourceKind Source);

public sealed record DeviceSnapshot(
    string? Name,
    string? DeviceInstanceId,
    IReadOnlyList<string> HardwareIds,
    IReadOnlyList<string> CompatibleIds,
    string? Manufacturer,
    string? Class,
    DeviceHealthStatus HealthStatus,
    uint? ProblemCode,
    string? Status,
    DataSourceKind Source);

public sealed record DriverSnapshot(
    string? DeviceName,
    string? DeviceInstanceId,
    string? DeviceClass,
    string? Manufacturer,
    string? Provider,
    string? Version,
    DateTimeOffset? Date,
    string? InfName,
    string? Signer,
    bool? IsSigned,
    DeviceHealthStatus DeviceHealthStatus,
    DataSourceKind Source);

public sealed record NetworkAdapterSnapshot(
    string Name,
    string? Description,
    NetworkAdapterKind Kind,
    string Status,
    long? LinkSpeedBitsPerSecond,
    bool? IsVirtual,
    DataSourceKind Source);

public sealed record DisplaySnapshot(
    string? DeviceName,
    string? FriendlyName,
    int? Width,
    int? Height,
    int? RefreshRateHz,
    int? Dpi,
    bool IsPrimary,
    DataSourceKind Source);

public sealed record PowerSnapshot(
    bool? BatteryPresent,
    bool? AcConnected,
    int? BatteryPercentage,
    PowerSourceKind PowerSource,
    string? ActivePowerScheme,
    DataSourceKind Source);

public sealed record SystemCapabilitySnapshot(
    string Key,
    DetectionStatus Status,
    bool? IsPresent,
    string? Value,
    DataSourceKind Source);

public sealed record ServiceSnapshot(
    string Name,
    string? DisplayName,
    string? State,
    string? StartMode,
    string? ServiceType,
    bool? Started,
    DataSourceKind Source);

public sealed record ProcessSnapshot(
    int ProcessId,
    string ProcessName,
    long? WorkingSetBytes,
    DataSourceKind Source);

public sealed record StartupItemSnapshot(
    string Name,
    string SourceLocation,
    DataSourceKind Source);
