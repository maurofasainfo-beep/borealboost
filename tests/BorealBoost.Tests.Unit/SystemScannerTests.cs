using BorealBoost.Analysis.SystemScanner;
using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Common;
using BorealBoost.Core.Foundation;
using BorealBoost.Core.Scanner;
using Microsoft.Extensions.Logging;

namespace BorealBoost.Tests.Unit;

public sealed class SystemScannerTests
{
    [Fact]
    public async Task Scanner_returns_partial_snapshot_when_provider_fails()
    {
        var scanner = CreateScanner(
            new SuccessfulProvider("Cpu", 10, new SystemSnapshotPatch(Processors:
            [
                new CpuSnapshot(null, "Test CPU", HardwareVendor.Unknown, "X64", 8, 4, 1, null, null, null, null, null, DataSourceKind.Unknown)
            ])),
            new ThrowingProvider("Graphics", 10));

        var result = await scanner.ScanAsync(null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Metadata.PartialScan);
        Assert.Contains(result.Value.Metadata.ProviderResults, provider => provider.ProviderName == "Graphics" && provider.Status == ProviderResultStatus.Failed);
        Assert.Equal("Test CPU", result.Value.Processors[0].Name);
    }

    [Fact]
    public async Task Scanner_marks_provider_timeout_without_completing_as_success()
    {
        var provider = new SlowProvider("Storage", TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(50));
        var scanner = CreateScanner(provider);

        var result = await scanner.ScanAsync(null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(provider.Completed);
        Assert.True(result.Value!.Metadata.PartialScan);
        Assert.Contains(result.Value.Metadata.ProviderResults, provider => provider.Status == ProviderResultStatus.TimedOut);
    }

    [Fact]
    public async Task Scanner_cancellation_returns_failure_without_snapshot()
    {
        var scanner = CreateScanner(new CooperativeCancelProvider("Memory"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await scanner.ScanAsync(null, cancellation.Token);

        Assert.True(result.IsFailure);
        Assert.Equal("scanner.canceled", result.ErrorCode);
    }

    [Fact]
    public async Task Scanner_cancellation_during_provider_returns_failure_without_snapshot()
    {
        var scanner = CreateScanner(new DelayWithCancellationProvider("Devices", TimeSpan.FromSeconds(5)));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(25));

        var result = await scanner.ScanAsync(null, cancellation.Token);

        Assert.True(result.IsFailure);
        Assert.Equal("scanner.canceled", result.ErrorCode);
    }

    [Fact]
    public async Task Scanner_marks_not_supported_provider_as_partial()
    {
        var scanner = CreateScanner(new NotSupportedProvider("SecurityCapabilities"));

        var result = await scanner.ScanAsync(null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Metadata.PartialScan);
        Assert.Contains(result.Value.Metadata.ProviderResults, provider => provider.ProviderName == "SecurityCapabilities" && provider.Status == ProviderResultStatus.NotSupported);
    }

    [Fact]
    public async Task Scanner_reports_weighted_progress_to_completion()
    {
        var scanner = CreateScanner(
            new SuccessfulProvider("Cpu", 10, SystemSnapshotPatch.Empty),
            new SuccessfulProvider("Memory", 30, SystemSnapshotPatch.Empty));
        var progress = new SynchronousProgress();

        var result = await scanner.ScanAsync(progress, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(progress.Updates, update => update.Percent == 0);
        Assert.Contains(progress.Updates, update => update.Percent == 100);
        Assert.All(progress.Updates, update => Assert.InRange(update.Percent, 0, 100));
    }

    [Fact]
    public async Task Scanner_preserves_detected_security_capabilities()
    {
        var scanner = CreateScanner(new SuccessfulProvider("SecurityCapabilities", 5, new SystemSnapshotPatch(Capabilities:
        [
            new SystemCapabilitySnapshot("TpmPresent", DetectionStatus.Known, true, "2.0", DataSourceKind.Wmi)
        ])));

        var result = await scanner.ScanAsync(null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Capabilities, capability => capability.Key == "TpmPresent" && capability.Status == DetectionStatus.Known && capability.IsPresent == true);
    }

    [Fact]
    public async Task Scanner_keeps_installed_and_visible_memory_separate()
    {
        var scanner = CreateScanner(new SuccessfulProvider("Memory", 5, new SystemSnapshotPatch(Memory: new MemorySnapshot(
            InstalledPhysicalBytes: 17_179_869_184,
            VisiblePhysicalBytes: 17_099_431_936,
            ModuleCount: 2,
            Modules: [],
            Source: DataSourceKind.Wmi))));

        var result = await scanner.ScanAsync(null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((ulong)17_179_869_184, result.Value!.Memory.InstalledPhysicalBytes);
        Assert.Equal((ulong)17_099_431_936, result.Value.Memory.VisiblePhysicalBytes);
    }

    [Fact]
    public async Task Scanner_merges_driver_inventory_with_device_problem_facts()
    {
        var deviceId = @"PCI\VEN_1234&DEV_ABCD";
        var scanner = CreateScanner(
            new SuccessfulProvider("Devices", 10, new SystemSnapshotPatch(Devices:
            [
                new DeviceSnapshot("Device", deviceId, [], [], "Vendor", "Display", DeviceHealthStatus.MissingDriver, 28, "Error", DataSourceKind.Wmi)
            ])),
            new SuccessfulProvider("Drivers", 10, new SystemSnapshotPatch(Drivers:
            [
                new DriverSnapshot("Device", deviceId, "Display", "Vendor", "Provider", "1.0", null, "test.inf", null, true, DeviceHealthStatus.Unknown, DataSourceKind.Wmi)
            ])));

        var result = await scanner.ScanAsync(null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DeviceHealthStatus.MissingDriver, result.Value!.Drivers.Single().DeviceHealthStatus);
    }

    private static SystemScanner CreateScanner(params ISystemScanProvider[] providers)
    {
        return new SystemScanner(providers, new TestApplicationInfoProvider(), new NoopLogger<SystemScanner>());
    }

    private sealed class SuccessfulProvider : ISystemScanProvider
    {
        private readonly SystemSnapshotPatch _patch;

        public SuccessfulProvider(string name, int weight, SystemSnapshotPatch patch)
        {
            Name = name;
            Weight = weight;
            _patch = patch;
        }

        public string Name { get; }

        public int Weight { get; }

        public TimeSpan Timeout => TimeSpan.FromSeconds(1);

        public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new ProviderScanResult(ProviderResult.Succeeded(Name, DataSourceKind.Unknown, TimeSpan.FromMilliseconds(1)), _patch));
        }
    }

    private sealed class ThrowingProvider : ISystemScanProvider
    {
        public ThrowingProvider(string name, int weight)
        {
            Name = name;
            Weight = weight;
        }

        public string Name { get; }

        public int Weight { get; }

        public TimeSpan Timeout => TimeSpan.FromSeconds(1);

        public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Provider failed.");
        }
    }

    private sealed class SlowProvider : ISystemScanProvider
    {
        private readonly TimeSpan _delay;

        public SlowProvider(string name, TimeSpan timeout, TimeSpan delay)
        {
            Name = name;
            Timeout = timeout;
            _delay = delay;
        }

        public string Name { get; }

        public int Weight => 1;

        public TimeSpan Timeout { get; }

        public bool Completed { get; private set; }

        public async Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(_delay, CancellationToken.None);
            Completed = true;
            return ProviderScanResult.Empty(ProviderResult.Succeeded(Name, DataSourceKind.Unknown, TimeSpan.Zero));
        }
    }

    private sealed class DelayWithCancellationProvider : ISystemScanProvider
    {
        private readonly TimeSpan _delay;

        public DelayWithCancellationProvider(string name, TimeSpan delay)
        {
            Name = name;
            _delay = delay;
        }

        public string Name { get; }

        public int Weight => 1;

        public TimeSpan Timeout => TimeSpan.FromSeconds(10);

        public async Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(_delay, cancellationToken);
            return ProviderScanResult.Empty(ProviderResult.Succeeded(Name, DataSourceKind.Unknown, TimeSpan.Zero));
        }
    }

    private sealed class NotSupportedProvider : ISystemScanProvider
    {
        public NotSupportedProvider(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public int Weight => 1;

        public TimeSpan Timeout => TimeSpan.FromSeconds(1);

        public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ProviderScanResult.Empty(ProviderResult.NotSupported(Name, DataSourceKind.Unknown, TimeSpan.FromMilliseconds(1), "scanner.provider.not_supported", "Provider not supported.")));
        }
    }

    private sealed class CooperativeCancelProvider : ISystemScanProvider
    {
        public CooperativeCancelProvider(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public int Weight => 1;

        public TimeSpan Timeout => TimeSpan.FromSeconds(1);

        public Task<ProviderScanResult> CollectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProviderScanResult.Empty(ProviderResult.Succeeded(Name, DataSourceKind.Unknown, TimeSpan.Zero)));
        }
    }

    private sealed class TestApplicationInfoProvider : IApplicationInfoProvider
    {
        public ApplicationInfo GetApplicationInfo()
        {
            return new ApplicationInfo("BorealBoost", new Version(2, 0, 0), "Test", ProtocolVersion.Current);
        }
    }

    private sealed class SynchronousProgress : IProgress<ScanProgressUpdate>
    {
        public List<ScanProgressUpdate> Updates { get; } = [];

        public void Report(ScanProgressUpdate value)
        {
            Updates.Add(value);
        }
    }

    private sealed class NoopLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }
}
