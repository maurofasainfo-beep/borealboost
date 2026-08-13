using System.Diagnostics;
using System.Management;
using BorealBoost.Core.Scanner;
using BorealBoost.System.Registry;
using BorealBoost.System.Wmi;

namespace BorealBoost.System.Scanner;

public sealed class SecurityCapabilitiesScanProvider : ISystemScanProvider
{
    private const string DeviceGuardKey = @"SYSTEM\CurrentControlSet\Control\DeviceGuard";
    private const string MemoryIntegrityKey = @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity";

    private readonly WmiQueryService _wmi;
    private readonly ReadOnlyRegistryReader _registry;

    public SecurityCapabilitiesScanProvider(WmiQueryService wmi, ReadOnlyRegistryReader registry)
    {
        _wmi = wmi;
        _registry = registry;
    }

    public string Name => "SecurityCapabilities";

    public int Weight => 8;

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var capabilities = new List<SystemCapabilitySnapshot>();
        var warnings = new List<ScanIssue>();

        await CollectTpmAsync(capabilities, warnings, cancellationToken).ConfigureAwait(false);
        await CollectDeviceGuardAsync(capabilities, warnings, cancellationToken).ConfigureAwait(false);
        CollectRegistrySecurityFacts(capabilities, cancellationToken);
        CollectDeferredFacts(capabilities);

        stopwatch.Stop();
        var patch = new SystemSnapshotPatch(Capabilities: capabilities);
        return warnings.Count == 0
            ? new ProviderScanResult(ProviderResult.Succeeded(Name, DataSourceKind.Composite, stopwatch.Elapsed), patch)
            : new ProviderScanResult(ProviderResult.Partial(Name, DataSourceKind.Composite, stopwatch.Elapsed, warnings), patch);
    }

    private async Task CollectTpmAsync(
        ICollection<SystemCapabilitySnapshot> capabilities,
        ICollection<ScanIssue> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _wmi.QueryAsync(
                @"root\cimv2\Security\MicrosoftTpm",
                "SELECT IsEnabled_InitialValue,IsActivated_InitialValue,SpecVersion FROM Win32_Tpm",
                Timeout,
                cancellationToken).ConfigureAwait(false);

            var row = rows.FirstOrDefault();
            if (row is null)
            {
                capabilities.Add(Capability("TpmPresent", DetectionStatus.Known, false, "No TPM instance returned.", DataSourceKind.Wmi));
                return;
            }

            capabilities.Add(Capability("TpmPresent", DetectionStatus.Known, true, row.String("SpecVersion"), DataSourceKind.Wmi));
            capabilities.Add(Capability("TpmEnabled", BoolStatus(row.Bool("IsEnabled_InitialValue")), row.Bool("IsEnabled_InitialValue"), row.Bool("IsEnabled_InitialValue")?.ToString(), DataSourceKind.Wmi));
            capabilities.Add(Capability("TpmActivated", BoolStatus(row.Bool("IsActivated_InitialValue")), row.Bool("IsActivated_InitialValue"), row.Bool("IsActivated_InitialValue")?.ToString(), DataSourceKind.Wmi));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ManagementException or UnauthorizedAccessException or InvalidOperationException)
        {
            capabilities.Add(Capability("TpmPresent", DetectionStatus.NotSupported, null, exception.GetType().Name, DataSourceKind.Wmi));
            warnings.Add(new ScanIssue("scanner.security.tpm_unavailable", $"TPM capability could not be read: {exception.GetType().Name}", Name));
        }
    }

    private async Task CollectDeviceGuardAsync(
        ICollection<SystemCapabilitySnapshot> capabilities,
        ICollection<ScanIssue> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _wmi.QueryAsync(
                @"root\Microsoft\Windows\DeviceGuard",
                "SELECT VirtualizationBasedSecurityStatus,SecurityServicesRunning,SecurityServicesConfigured FROM Win32_DeviceGuard",
                Timeout,
                cancellationToken).ConfigureAwait(false);

            var row = rows.FirstOrDefault();
            if (row is null)
            {
                capabilities.Add(Capability("VbsStatus", DetectionStatus.Unknown, null, null, DataSourceKind.Wmi));
                capabilities.Add(Capability("MemoryIntegrityRunning", DetectionStatus.Unknown, null, null, DataSourceKind.Wmi));
                return;
            }

            var vbsStatus = row.UInt32("VirtualizationBasedSecurityStatus");
            capabilities.Add(Capability("VbsStatus", vbsStatus.HasValue ? DetectionStatus.Known : DetectionStatus.Unknown, vbsStatus.HasValue ? vbsStatus.Value > 0 : null, FormatVbsStatus(vbsStatus), DataSourceKind.Wmi));

            var runningServices = row.StringArray("SecurityServicesRunning");
            var configuredServices = row.StringArray("SecurityServicesConfigured");
            capabilities.Add(Capability("MemoryIntegrityRunning", runningServices.Length > 0 ? DetectionStatus.Known : DetectionStatus.Unknown, ContainsSecurityService(runningServices, 2), string.Join(",", runningServices), DataSourceKind.Wmi));
            capabilities.Add(Capability("MemoryIntegrityDeviceGuardConfigured", configuredServices.Length > 0 ? DetectionStatus.Known : DetectionStatus.Unknown, ContainsSecurityService(configuredServices, 2), string.Join(",", configuredServices), DataSourceKind.Wmi));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ManagementException or UnauthorizedAccessException or InvalidOperationException)
        {
            capabilities.Add(Capability("VbsStatus", DetectionStatus.Unknown, null, exception.GetType().Name, DataSourceKind.Wmi));
            capabilities.Add(Capability("MemoryIntegrityRunning", DetectionStatus.Unknown, null, exception.GetType().Name, DataSourceKind.Wmi));
            warnings.Add(new ScanIssue("scanner.security.deviceguard_unavailable", $"Device Guard capability could not be read: {exception.GetType().Name}", Name));
        }
    }

    private void CollectRegistrySecurityFacts(ICollection<SystemCapabilitySnapshot> capabilities, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var vbsConfigured = _registry.ReadLocalMachineInt32(DeviceGuardKey, "EnableVirtualizationBasedSecurity");
        capabilities.Add(Capability("VbsConfigured", RegistryBoolStatus(vbsConfigured), RegistryBool(vbsConfigured), vbsConfigured?.ToString(global::System.Globalization.CultureInfo.InvariantCulture), DataSourceKind.RegistryReadOnly));

        var memoryIntegrityConfigured = _registry.ReadLocalMachineInt32(MemoryIntegrityKey, "Enabled");
        capabilities.Add(Capability("MemoryIntegrityConfigured", RegistryBoolStatus(memoryIntegrityConfigured), RegistryBool(memoryIntegrityConfigured), memoryIntegrityConfigured?.ToString(global::System.Globalization.CultureInfo.InvariantCulture), DataSourceKind.RegistryReadOnly));
    }

    private static void CollectDeferredFacts(ICollection<SystemCapabilitySnapshot> capabilities)
    {
        capabilities.Add(Capability("DefenderOperationalStatus", DetectionStatus.Deferred, null, "DeferredToFutureSecurityProvider", DataSourceKind.Unknown));
        capabilities.Add(Capability("FirewallProfileStatus", DetectionStatus.Deferred, null, "DeferredToFutureSecurityProvider", DataSourceKind.Unknown));
        capabilities.Add(Capability("BitLockerProtectionStatus", DetectionStatus.Deferred, null, "DeferredToFutureSecurityProvider", DataSourceKind.Unknown));
    }

    private static SystemCapabilitySnapshot Capability(string key, DetectionStatus status, bool? isPresent, string? value, DataSourceKind source)
    {
        return new SystemCapabilitySnapshot(key, status, isPresent, value, source);
    }

    private static DetectionStatus BoolStatus(bool? value)
    {
        return value.HasValue ? DetectionStatus.Known : DetectionStatus.Unknown;
    }

    private static DetectionStatus RegistryBoolStatus(int? value)
    {
        return value is 0 or 1 ? DetectionStatus.Known : DetectionStatus.Unknown;
    }

    private static bool? RegistryBool(int? value)
    {
        return value switch
        {
            0 => false,
            1 => true,
            _ => null
        };
    }

    private static bool? ContainsSecurityService(IReadOnlyList<string> values, uint serviceId)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var expected = serviceId.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        return values.Any(value => string.Equals(value, expected, StringComparison.OrdinalIgnoreCase));
    }

    private static string? FormatVbsStatus(uint? status)
    {
        return status switch
        {
            0 => "Disabled",
            1 => "EnabledNotRunning",
            2 => "Running",
            _ => status?.ToString(global::System.Globalization.CultureInfo.InvariantCulture)
        };
    }
}
