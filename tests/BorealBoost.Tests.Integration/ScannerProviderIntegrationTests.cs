using BorealBoost.Core.Scanner;
using BorealBoost.System.Registry;
using BorealBoost.System.Scanner;
using BorealBoost.System.Wmi;

namespace BorealBoost.Tests.Integration;

public sealed class ScannerProviderIntegrationTests
{
    private readonly WmiQueryService _wmi = new();
    private readonly ReadOnlyRegistryReader _registry = new();

    [Fact]
    public async Task Operating_system_provider_detects_current_windows_read_only()
    {
        var provider = new OperatingSystemScanProvider(_wmi, _registry);

        var result = await provider.CollectAsync(CancellationToken.None);

        Assert.Equal(ProviderResultStatus.Success, result.Result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.Patch.OperatingSystem?.Name));
        Assert.True(result.Patch.OperatingSystem?.Build > 0);
        Assert.True(result.Patch.OperatingSystem?.BorealBoostCompatibility is WindowsCompatibilityStatus.Supported or WindowsCompatibilityStatus.LegacySupported or WindowsCompatibilityStatus.Unsupported or WindowsCompatibilityStatus.Unknown);
    }

    [Fact]
    public async Task Cpu_provider_detects_at_least_one_processor_when_wmi_is_available()
    {
        var provider = new CpuScanProvider(_wmi);

        var result = await provider.CollectAsync(CancellationToken.None);

        Assert.Equal(ProviderResultStatus.Success, result.Result.Status);
        Assert.NotEmpty(result.Patch.Processors!);
        Assert.All(result.Patch.Processors!, cpu => Assert.True(cpu.LogicalProcessors > 0));
    }

    [Fact]
    public async Task Memory_provider_detects_total_ram_or_reports_unknown_without_serials()
    {
        var provider = new MemoryScanProvider(_wmi);

        var result = await provider.CollectAsync(CancellationToken.None);

        Assert.Equal(ProviderResultStatus.Success, result.Result.Status);
        Assert.True(result.Patch.Memory?.VisiblePhysicalBytes is null or > 0);
        Assert.True(result.Patch.Memory?.InstalledPhysicalBytes is null or > 0);
        Assert.DoesNotContain(result.Patch.Memory!.Modules, module => module.PartNumber?.Contains("Serial", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task Storage_provider_collects_ready_volumes_without_benchmarking()
    {
        var provider = new StorageScanProvider(_wmi);

        var result = await provider.CollectAsync(CancellationToken.None);

        Assert.Equal(ProviderResultStatus.Success, result.Result.Status);
        Assert.NotNull(result.Patch.Storage);
        Assert.NotEmpty(result.Patch.Storage!.Volumes);
        Assert.All(result.Patch.Storage.Volumes, volume => Assert.True(volume.TotalBytes is null or > 0));
    }

    [Fact]
    public async Task Display_provider_runs_without_requiring_a_specific_monitor()
    {
        var provider = new DisplayScanProvider();

        var result = await provider.CollectAsync(CancellationToken.None);

        Assert.Equal(ProviderResultStatus.Success, result.Result.Status);
        Assert.NotNull(result.Patch.Displays);
    }

    [Fact]
    public async Task Device_provider_collects_pnp_inventory_read_only()
    {
        var provider = new DevicesScanProvider(_wmi);

        var result = await provider.CollectAsync(CancellationToken.None);

        Assert.Equal(ProviderResultStatus.Success, result.Result.Status);
        Assert.NotNull(result.Patch.Devices);
        Assert.NotEmpty(result.Patch.Devices!);
    }

    [Fact]
    public async Task Services_provider_collects_service_inventory_read_only()
    {
        var provider = new ServicesScanProvider(_wmi);

        var result = await provider.CollectAsync(CancellationToken.None);

        Assert.Equal(ProviderResultStatus.Success, result.Result.Status);
        Assert.NotNull(result.Patch.Services);
        Assert.NotEmpty(result.Patch.Services!);
        Assert.All(result.Patch.Services!, service => Assert.False(string.IsNullOrWhiteSpace(service.Name)));
    }

    [Fact]
    public async Task Processes_provider_collects_process_inventory_read_only()
    {
        var provider = new ProcessesScanProvider();

        var result = await provider.CollectAsync(CancellationToken.None);

        Assert.Equal(ProviderResultStatus.Success, result.Result.Status);
        Assert.NotNull(result.Patch.Processes);
        Assert.NotEmpty(result.Patch.Processes!);
    }

    [Fact]
    public async Task Startup_provider_collects_registry_run_entries_read_only()
    {
        var provider = new StartupScanProvider();

        var result = await provider.CollectAsync(CancellationToken.None);

        Assert.Equal(ProviderResultStatus.Success, result.Result.Status);
        Assert.NotNull(result.Patch.StartupItems);
    }

    [Fact]
    public async Task Security_capabilities_provider_returns_read_only_facts_or_explicit_unknowns()
    {
        var provider = new SecurityCapabilitiesScanProvider(_wmi, _registry);

        var result = await provider.CollectAsync(CancellationToken.None);

        Assert.True(result.Result.Status is ProviderResultStatus.Success or ProviderResultStatus.Partial);
        Assert.NotNull(result.Patch.Capabilities);
        Assert.Contains(result.Patch.Capabilities!, capability => capability.Key == "TpmPresent");
        Assert.Contains(result.Patch.Capabilities!, capability => capability.Key == "MemoryIntegrityConfigured");
        Assert.All(result.Patch.Capabilities!, capability => Assert.True(capability.Status is DetectionStatus.Known or DetectionStatus.Unknown or DetectionStatus.NotSupported or DetectionStatus.Deferred));
    }
}
