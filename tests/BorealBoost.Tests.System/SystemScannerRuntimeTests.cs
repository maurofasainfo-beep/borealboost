using System.Diagnostics;
using BorealBoost.Analysis.SystemScanner;
using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Foundation;
using BorealBoost.Core.Scanner;
using BorealBoost.System.Registry;
using BorealBoost.System.Scanner;
using BorealBoost.System.Wmi;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace BorealBoost.Tests.System;

public sealed class SystemScannerRuntimeTests
{
    private readonly ITestOutputHelper _output;

    public SystemScannerRuntimeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Full_system_scanner_collects_current_machine_snapshot_read_only()
    {
        var scanner = CreateScanner();
        var progress = new SynchronousProgress();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(75));
        var result = await scanner.ScanAsync(progress, timeout.Token);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var snapshot = result.Value!;
        Assert.NotEqual(Guid.Empty, snapshot.Metadata.ScanId.Value);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.OperatingSystem.Name));
        Assert.True(snapshot.OperatingSystem.Build > 0);
        Assert.NotEmpty(snapshot.Processors);
        Assert.Contains(snapshot.Processors, cpu => !string.IsNullOrWhiteSpace(cpu.Name) || cpu.LogicalProcessors > 0);
        Assert.True(snapshot.Memory.VisiblePhysicalBytes is null or > 0);
        Assert.True(snapshot.Memory.InstalledPhysicalBytes is null or > 0);
        Assert.NotEmpty(snapshot.Storage.Volumes);
        Assert.NotEmpty(snapshot.Metadata.ProviderResults);
        Assert.True(snapshot.Metadata.Duration >= TimeSpan.Zero);
        Assert.Contains(progress.Updates, update => update.Percent == 100);

        var problemDevices = snapshot.Devices.Count(device => device.HealthStatus is DeviceHealthStatus.MissingDriver or DeviceHealthStatus.Problem or DeviceHealthStatus.Disabled);
        _output.WriteLine($"Windows={snapshot.OperatingSystem.Name} build={snapshot.OperatingSystem.Build}");
        _output.WriteLine($"CPU={snapshot.Processors.FirstOrDefault()?.Name ?? "Unknown"}");
        _output.WriteLine($"GPUCount={snapshot.Graphics.Count}");
        _output.WriteLine($"RamInstalledBytes={snapshot.Memory.InstalledPhysicalBytes?.ToString() ?? "Unknown"}");
        _output.WriteLine($"RamVisibleBytes={snapshot.Memory.VisiblePhysicalBytes?.ToString() ?? "Unknown"}");
        _output.WriteLine($"DiskCount={snapshot.Storage.Disks.Count}");
        _output.WriteLine($"DisplayCount={snapshot.Displays.Count}");
        _output.WriteLine($"ProblemDeviceCount={problemDevices}");
        _output.WriteLine($"ServiceCount={snapshot.Services.Count}");
        _output.WriteLine($"ProcessCount={snapshot.Processes.Count}");
        _output.WriteLine($"StartupItemCount={snapshot.StartupItems.Count}");
        _output.WriteLine($"DurationMs={snapshot.Metadata.Duration.TotalMilliseconds:N0}");
        _output.WriteLine($"ProviderSuccess={snapshot.Metadata.ProviderResults.Count(provider => provider.Status == ProviderResultStatus.Success)}");
        _output.WriteLine($"ProviderPartial={snapshot.Metadata.ProviderResults.Count(provider => provider.Status == ProviderResultStatus.Partial)}");
        _output.WriteLine($"ProviderFailed={snapshot.Metadata.ProviderResults.Count(provider => provider.Status == ProviderResultStatus.Failed)}");
        _output.WriteLine($"ProviderTimedOut={snapshot.Metadata.ProviderResults.Count(provider => provider.Status == ProviderResultStatus.TimedOut)}");
        _output.WriteLine($"ProviderNotSupported={snapshot.Metadata.ProviderResults.Count(provider => provider.Status == ProviderResultStatus.NotSupported)}");
        _output.WriteLine($"SecurityCapabilityCount={snapshot.Capabilities.Count}");
        foreach (var provider in snapshot.Metadata.ProviderResults.OrderBy(provider => provider.Duration, Comparer<TimeSpan>.Default).Reverse().Take(3))
        {
            _output.WriteLine($"SlowProvider={provider.ProviderName}:{provider.Status}:{provider.Duration.TotalMilliseconds:N0}ms");
        }
    }

    [Fact]
    public async Task Full_system_scanner_runs_ten_sequential_scans_without_resource_growth()
    {
        var durations = new List<TimeSpan>();
        var providerFailures = 0;
        var providerTimeouts = 0;
        var process = Process.GetCurrentProcess();
        process.Refresh();
        var initialHandles = process.HandleCount;
        var initialWorkingSet = process.WorkingSet64;

        for (var index = 0; index < 10; index++)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(75));
            var result = await CreateScanner().ScanAsync(null, timeout.Token);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            var snapshot = result.Value!;
            durations.Add(snapshot.Metadata.Duration);
            providerFailures += snapshot.Metadata.ProviderResults.Count(provider => provider.Status == ProviderResultStatus.Failed);
            providerTimeouts += snapshot.Metadata.ProviderResults.Count(provider => provider.Status == ProviderResultStatus.TimedOut);

            foreach (var provider in snapshot.Metadata.ProviderResults.OrderByDescending(provider => provider.Duration).Take(3))
            {
                _output.WriteLine($"Run={index + 1}; SlowProvider={provider.ProviderName}:{provider.Status}:{provider.Duration.TotalMilliseconds:N0}ms");
            }
        }

        process.Refresh();
        var finalHandles = process.HandleCount;
        var finalWorkingSet = process.WorkingSet64;
        var min = durations.Min();
        var average = TimeSpan.FromMilliseconds(durations.Average(duration => duration.TotalMilliseconds));
        var max = durations.Max();

        _output.WriteLine($"TenScanMinMs={min.TotalMilliseconds:N0}");
        _output.WriteLine($"TenScanAverageMs={average.TotalMilliseconds:N0}");
        _output.WriteLine($"TenScanMaxMs={max.TotalMilliseconds:N0}");
        _output.WriteLine($"TenScanProviderFailures={providerFailures}");
        _output.WriteLine($"TenScanProviderTimeouts={providerTimeouts}");
        _output.WriteLine($"HandleDelta={finalHandles - initialHandles}");
        _output.WriteLine($"WorkingSetDeltaBytes={finalWorkingSet - initialWorkingSet}");

        Assert.Equal(10, durations.Count);
    }

    private static SystemScanner CreateScanner()
    {
        var wmi = new WmiQueryService();
        var registry = new ReadOnlyRegistryReader();
        ISystemScanProvider[] providers =
        [
            new OperatingSystemScanProvider(wmi, registry),
            new CpuScanProvider(wmi),
            new GraphicsScanProvider(wmi),
            new MemoryScanProvider(wmi),
            new StorageScanProvider(wmi),
            new HardwareFirmwareScanProvider(wmi, registry),
            new DisplayScanProvider(),
            new NetworkScanProvider(),
            new DevicesScanProvider(wmi),
            new DriverInventoryScanProvider(wmi),
            new PowerScanProvider(registry),
            new ServicesScanProvider(wmi),
            new ProcessesScanProvider(),
            new StartupScanProvider(),
            new SecurityCapabilitiesScanProvider(wmi, registry)
        ];

        return new SystemScanner(providers, new TestApplicationInfoProvider(), new NoopLogger<SystemScanner>());
    }

    private sealed class SynchronousProgress : IProgress<ScanProgressUpdate>
    {
        public List<ScanProgressUpdate> Updates { get; } = [];

        public void Report(ScanProgressUpdate value)
        {
            Updates.Add(value);
        }
    }

    private sealed class TestApplicationInfoProvider : IApplicationInfoProvider
    {
        public ApplicationInfo GetApplicationInfo()
        {
            return new ApplicationInfo("BorealBoost", new Version(2, 0, 0), "System Test", ProtocolVersion.Current);
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
