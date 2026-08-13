using System.Diagnostics;
using System.Runtime.InteropServices;
using BorealBoost.Core.Common;
using BorealBoost.Core.Foundation;
using BorealBoost.Core.Scanner;
using Microsoft.Extensions.Logging;

namespace BorealBoost.Analysis.SystemScanner;

public sealed class SystemScanner : ISystemScanner
{
    public const string SnapshotSchemaVersion = "2.0.0";

    private readonly IReadOnlyList<ISystemScanProvider> _providers;
    private readonly IApplicationInfoProvider _applicationInfoProvider;
    private readonly ILogger<SystemScanner> _logger;

    public SystemScanner(
        IEnumerable<ISystemScanProvider> providers,
        IApplicationInfoProvider applicationInfoProvider,
        ILogger<SystemScanner> logger)
    {
        _providers = providers.ToArray();
        _applicationInfoProvider = applicationInfoProvider;
        _logger = logger;
    }

    public async Task<Result<SystemSnapshot>> ScanAsync(IProgress<ScanProgressUpdate>? progress, CancellationToken cancellationToken)
    {
        var scanId = ScanId.New();
        var startedAtUtc = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var totalWeight = Math.Max(1, _providers.Sum(provider => Math.Max(1, provider.Weight)));
        var completedWeight = 0;
        var providerResults = new List<ProviderResult>();
        var patches = new List<SystemSnapshotPatch>();

        _logger.LogInformation("System scan started. ScanId={ScanId}; ProviderCount={ProviderCount}", scanId, _providers.Count);
        progress?.Report(new ScanProgressUpdate(scanId, 0, "Preparando analise", "Scanner", 0, totalWeight));

        foreach (var provider in _providers)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("System scan canceled. ScanId={ScanId}; Provider={Provider}", scanId, provider.Name);
                return Result<SystemSnapshot>.Failure("scanner.canceled", "System scan was canceled.");
            }

            progress?.Report(new ScanProgressUpdate(scanId, ToPercent(completedWeight, totalWeight), ToFriendlyStage(provider.Name), provider.Name, completedWeight, totalWeight));

            var providerStopwatch = Stopwatch.StartNew();
            _logger.LogInformation("System scan provider started. ScanId={ScanId}; Provider={Provider}", scanId, provider.Name);

            ProviderScanResult providerResult;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(provider.Timeout);

            try
            {
                providerResult = await provider.CollectAsync(timeout.Token).ConfigureAwait(false);
                if (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
                {
                    providerResult = ProviderScanResult.Empty(ProviderResult.TimedOut(provider.Name, providerStopwatch.Elapsed));
                }
            }
            catch (TimeoutException)
            {
                providerResult = ProviderScanResult.Empty(ProviderResult.TimedOut(provider.Name, providerStopwatch.Elapsed));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                providerResult = ProviderScanResult.Empty(ProviderResult.TimedOut(provider.Name, providerStopwatch.Elapsed));
            }
            catch (OperationCanceledException)
            {
                providerResult = ProviderScanResult.Empty(ProviderResult.Canceled(provider.Name, providerStopwatch.Elapsed));
                providerResults.Add(providerResult.Result);
                _logger.LogWarning("System scan canceled. ScanId={ScanId}; Provider={Provider}", scanId, provider.Name);
                return Result<SystemSnapshot>.Failure("scanner.canceled", "System scan was canceled.");
            }
            catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException or COMException)
            {
                providerResult = ProviderScanResult.Empty(ProviderResult.Failed(provider.Name, DataSourceKind.Unknown, providerStopwatch.Elapsed, "scanner.provider.failed", exception.Message));
            }

            providerStopwatch.Stop();
            providerResults.Add(providerResult.Result);
            if (providerResult.Result.Status is ProviderResultStatus.Success or ProviderResultStatus.Partial)
            {
                patches.Add(providerResult.Patch);
            }

            completedWeight += Math.Max(1, provider.Weight);
            progress?.Report(new ScanProgressUpdate(scanId, ToPercent(completedWeight, totalWeight), "Finalizando etapa", provider.Name, completedWeight, totalWeight));

            _logger.LogInformation(
                "System scan provider finished. ScanId={ScanId}; Provider={Provider}; Status={Status}; DurationMs={DurationMs}",
                scanId,
                provider.Name,
                providerResult.Result.Status,
                providerResult.Result.Duration.TotalMilliseconds);
        }

        stopwatch.Stop();

        var completedAtUtc = DateTimeOffset.UtcNow;
        var partialScan = providerResults.Any(result => result.Status is ProviderResultStatus.Partial or ProviderResultStatus.Failed or ProviderResultStatus.NotSupported or ProviderResultStatus.TimedOut or ProviderResultStatus.Canceled);
        var warnings = providerResults.SelectMany(result => result.Warnings).ToArray();
        var errors = providerResults.SelectMany(result => result.Errors).ToArray();

        var snapshot = BuildSnapshot(
            scanId,
            startedAtUtc,
            completedAtUtc,
            stopwatch.Elapsed,
            providerResults,
            partialScan,
            warnings,
            errors,
            patches);

        progress?.Report(new ScanProgressUpdate(scanId, 100, "Analise concluida", "Scanner", totalWeight, totalWeight));
        _logger.LogInformation(
            "System scan completed. ScanId={ScanId}; DurationMs={DurationMs}; PartialScan={PartialScan}; WarningCount={WarningCount}; ErrorCount={ErrorCount}",
            scanId,
            stopwatch.Elapsed.TotalMilliseconds,
            partialScan,
            warnings.Length,
            errors.Length);

        return Result<SystemSnapshot>.Success(snapshot);
    }

    private SystemSnapshot BuildSnapshot(
        ScanId scanId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc,
        TimeSpan duration,
        IReadOnlyList<ProviderResult> providerResults,
        bool partialScan,
        IReadOnlyList<ScanIssue> warnings,
        IReadOnlyList<ScanIssue> errors,
        IReadOnlyList<SystemSnapshotPatch> patches)
    {
        var appInfo = _applicationInfoProvider.GetApplicationInfo();
        var operatingSystem = patches.Select(patch => patch.OperatingSystem).OfType<OperatingSystemSnapshot>().FirstOrDefault() ?? UnknownOperatingSystem();
        var hardware = patches.Select(patch => patch.Hardware).OfType<HardwareSnapshot>().FirstOrDefault() ?? UnknownHardware();
        var processors = patches.Select(patch => patch.Processors).OfType<IReadOnlyList<CpuSnapshot>>().SelectMany(item => item).ToArray();
        var graphics = patches.Select(patch => patch.Graphics).OfType<IReadOnlyList<GpuSnapshot>>().SelectMany(item => item).ToArray();
        var memory = patches.Select(patch => patch.Memory).OfType<MemorySnapshot>().FirstOrDefault() ?? UnknownMemory();
        var storage = patches.Select(patch => patch.Storage).OfType<StorageSnapshot>().FirstOrDefault() ?? UnknownStorage();
        var motherboard = patches.Select(patch => patch.Motherboard).OfType<MotherboardSnapshot>().FirstOrDefault() ?? UnknownMotherboard();
        var firmware = patches.Select(patch => patch.Firmware).OfType<FirmwareSnapshot>().FirstOrDefault() ?? UnknownFirmware();
        var devices = patches.Select(patch => patch.Devices).OfType<IReadOnlyList<DeviceSnapshot>>().SelectMany(item => item).ToArray();
        var drivers = patches.Select(patch => patch.Drivers).OfType<IReadOnlyList<DriverSnapshot>>().SelectMany(item => item).ToArray();
        var network = patches.Select(patch => patch.Network).OfType<IReadOnlyList<NetworkAdapterSnapshot>>().SelectMany(item => item).ToArray();
        var displays = patches.Select(patch => patch.Displays).OfType<IReadOnlyList<DisplaySnapshot>>().SelectMany(item => item).ToArray();
        var power = patches.Select(patch => patch.Power).OfType<PowerSnapshot>().FirstOrDefault() ?? UnknownPower();
        var services = patches.Select(patch => patch.Services).OfType<IReadOnlyList<ServiceSnapshot>>().SelectMany(item => item).ToArray();
        var processes = patches.Select(patch => patch.Processes).OfType<IReadOnlyList<ProcessSnapshot>>().SelectMany(item => item).ToArray();
        var startupItems = patches.Select(patch => patch.StartupItems).OfType<IReadOnlyList<StartupItemSnapshot>>().SelectMany(item => item).ToArray();
        var detectedCapabilities = patches.Select(patch => patch.Capabilities).OfType<IReadOnlyList<SystemCapabilitySnapshot>>().SelectMany(item => item).ToArray();
        drivers = MergeDriverHealth(drivers, devices);

        var metadata = new ScanMetadata(
            scanId,
            startedAtUtc,
            completedAtUtc,
            duration,
            appInfo.Version.ToString(),
            SnapshotSchemaVersion,
            RuntimeInformation.OSArchitecture.ToString(),
            providerResults,
            partialScan,
            warnings,
            errors);

        return new SystemSnapshot(
            metadata,
            operatingSystem,
            hardware,
            processors,
            graphics,
            memory,
            storage,
            motherboard,
            firmware,
            devices,
            drivers,
            network,
            displays,
            power,
            services,
            processes,
            startupItems,
            BuildCapabilities(processors, graphics, displays, power, firmware, hardware, detectedCapabilities));
    }

    private static IReadOnlyList<SystemCapabilitySnapshot> BuildCapabilities(
        IReadOnlyList<CpuSnapshot> processors,
        IReadOnlyList<GpuSnapshot> graphics,
        IReadOnlyList<DisplaySnapshot> displays,
        PowerSnapshot power,
        FirmwareSnapshot firmware,
        HardwareSnapshot hardware,
        IReadOnlyList<SystemCapabilitySnapshot> detectedCapabilities)
    {
        var capabilities = new List<SystemCapabilitySnapshot>
        {
            Capability("SecureBootAvailable", SecureBootAvailabilityStatus(firmware), SecureBootAvailable(firmware), firmware.FirmwareType, firmware.Source),
            Capability("SecureBootEnabled", BoolStatus(firmware.SecureBootEnabled), firmware.SecureBootEnabled, firmware.SecureBootEnabled?.ToString(), firmware.Source),
            Capability("BatteryPresent", BoolStatus(power.BatteryPresent), power.BatteryPresent, power.BatteryPresent?.ToString(), power.Source),
            Capability("MultipleGpus", DetectionStatus.Known, graphics.Count > 1, graphics.Count.ToString(), DataSourceKind.Composite),
            Capability("MultipleDisplays", DetectionStatus.Known, displays.Count > 1, displays.Count.ToString(), DataSourceKind.Composite),
            Capability("VirtualizationAvailable", VirtualizationStatus(processors), VirtualizationAvailable(processors), VirtualizationAvailable(processors)?.ToString(), DataSourceKind.Composite),
            Capability("VirtualMachine", DetectionStatus.Known, hardware.IsVirtualMachine, hardware.VirtualizationPlatform ?? hardware.IsVirtualMachine.ToString(), hardware.Source)
        };

        capabilities.AddRange(detectedCapabilities);
        return capabilities
            .GroupBy(capability => capability.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private static DriverSnapshot[] MergeDriverHealth(IReadOnlyList<DriverSnapshot> drivers, IReadOnlyList<DeviceSnapshot> devices)
    {
        var healthByDeviceId = devices
            .Where(device => !string.IsNullOrWhiteSpace(device.DeviceInstanceId))
            .GroupBy(device => device.DeviceInstanceId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().HealthStatus, StringComparer.OrdinalIgnoreCase);

        return drivers
            .Select(driver => driver.DeviceInstanceId is not null && healthByDeviceId.TryGetValue(driver.DeviceInstanceId, out var health)
                ? driver with { DeviceHealthStatus = health }
                : driver)
            .ToArray();
    }

    private static int ToPercent(int completedWeight, int totalWeight)
    {
        return Math.Clamp((int)Math.Round(completedWeight * 100d / totalWeight), 0, 100);
    }

    private static string ToFriendlyStage(string providerName)
    {
        return providerName switch
        {
            "OperatingSystem" => "Analisando Windows",
            "Cpu" => "Analisando processador",
            "Graphics" => "Analisando placa de video",
            "Memory" => "Verificando memoria",
            "Storage" => "Analisando armazenamento",
            "Hardware" => "Verificando hardware",
            "Displays" => "Analisando monitores",
            "Network" => "Analisando rede",
            "Devices" => "Verificando dispositivos",
            "Drivers" => "Inventariando drivers",
            "Power" => "Verificando energia",
            "Services" => "Inventariando servicos",
            "Processes" => "Inventariando processos",
            "Startup" => "Inventariando inicializacao",
            "SecurityCapabilities" => "Verificando seguranca",
            _ => "Analisando computador"
        };
    }

    private static OperatingSystemSnapshot UnknownOperatingSystem()
    {
        return new OperatingSystemSnapshot(null, null, null, null, null, null, RuntimeInformation.OSArchitecture.ToString(), WindowsCompatibilityStatus.Unknown, "Operating system provider did not return data.", DataSourceKind.Unknown);
    }

    private static HardwareSnapshot UnknownHardware()
    {
        return new HardwareSnapshot(null, null, MachineFormFactor.Unknown, false, null, DataSourceKind.Unknown);
    }

    private static MemorySnapshot UnknownMemory()
    {
        return new MemorySnapshot(null, null, 0, [], DataSourceKind.Unknown);
    }

    private static StorageSnapshot UnknownStorage()
    {
        return new StorageSnapshot([], [], DataSourceKind.Unknown);
    }

    private static MotherboardSnapshot UnknownMotherboard()
    {
        return new MotherboardSnapshot(null, null, null, DataSourceKind.Unknown);
    }

    private static FirmwareSnapshot UnknownFirmware()
    {
        return new FirmwareSnapshot(null, null, null, null, null, DataSourceKind.Unknown);
    }

    private static PowerSnapshot UnknownPower()
    {
        return new PowerSnapshot(null, null, null, PowerSourceKind.Unknown, null, DataSourceKind.Unknown);
    }

    private static SystemCapabilitySnapshot Capability(string key, DetectionStatus status, bool? isPresent, string? value, DataSourceKind source)
    {
        return new SystemCapabilitySnapshot(key, status, isPresent, value, source);
    }

    private static DetectionStatus BoolStatus(bool? value)
    {
        return value.HasValue ? DetectionStatus.Known : DetectionStatus.Unknown;
    }

    private static bool? SecureBootAvailable(FirmwareSnapshot firmware)
    {
        if (firmware.SecureBootEnabled.HasValue)
        {
            return true;
        }

        return firmware.FirmwareType switch
        {
            "UEFI" => true,
            "Legacy" => false,
            _ => null
        };
    }

    private static DetectionStatus SecureBootAvailabilityStatus(FirmwareSnapshot firmware)
    {
        if (firmware.SecureBootEnabled.HasValue || string.Equals(firmware.FirmwareType, "UEFI", StringComparison.OrdinalIgnoreCase))
        {
            return DetectionStatus.Known;
        }

        if (string.Equals(firmware.FirmwareType, "Legacy", StringComparison.OrdinalIgnoreCase))
        {
            return DetectionStatus.NotSupported;
        }

        return DetectionStatus.Unknown;
    }

    private static bool? VirtualizationAvailable(IReadOnlyList<CpuSnapshot> processors)
    {
        if (processors.Count == 0)
        {
            return null;
        }

        if (processors.Any(cpu => cpu.VirtualizationCapable == true))
        {
            return true;
        }

        return processors.All(cpu => cpu.VirtualizationCapable == false) ? false : null;
    }

    private static DetectionStatus VirtualizationStatus(IReadOnlyList<CpuSnapshot> processors)
    {
        return VirtualizationAvailable(processors).HasValue ? DetectionStatus.Known : DetectionStatus.Unknown;
    }
}
