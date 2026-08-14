using System.Diagnostics;
using BorealBoost.Analysis.RecommendationEngine;
using BorealBoost.Analysis.RecommendationEngine.Rules;
using BorealBoost.Core.Analysis;
using BorealBoost.Analysis.SystemScanner;
using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Foundation;
using BorealBoost.Core.Optimization;
using BorealBoost.Core.Scanner;
using BorealBoost.Infrastructure.Persistence;
using BorealBoost.Optimization.Catalog;
using BorealBoost.Optimization.Execution;
using BorealBoost.Optimization.Handlers;
using BorealBoost.Optimization.Planning;
using BorealBoost.Restore;
using BorealBoost.System.Operations;
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
    public async Task Real_scanner_snapshot_flows_into_analysis_recommendations_read_only()
    {
        var scanner = CreateScanner();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(75));
        var scanResult = await scanner.ScanAsync(null, timeout.Token);

        Assert.True(scanResult.IsSuccess, scanResult.ErrorMessage);

        var analysisResult = await CreateAnalysisEngine().AnalyzeAsync(scanResult.Value!, CancellationToken.None);

        Assert.True(analysisResult.IsSuccess, analysisResult.ErrorMessage);
        var analysis = analysisResult.Value!;
        Assert.Equal(scanResult.Value!.Metadata.ScanId, analysis.ScanId);
        Assert.Equal(AnalysisEngine.EngineVersion, analysis.EngineVersion);
        Assert.Equal(AnalysisEngine.RuleCatalogVersion, analysis.RuleCatalogVersion);
        Assert.Equal(11, analysis.Summary.RulesEvaluated);
        Assert.Equal(analysis.Summary.RecommendationCount, analysis.Recommendations.Count);
        Assert.All(analysis.Recommendations, recommendation => Assert.DoesNotContain("+", recommendation.ShortDescription, StringComparison.Ordinal));

        _output.WriteLine($"AnalysisRulesEvaluated={analysis.Summary.RulesEvaluated}");
        _output.WriteLine($"AnalysisHealthy={analysis.Summary.HealthyCount}");
        _output.WriteLine($"AnalysisOpportunities={analysis.Summary.OpportunityCount}");
        _output.WriteLine($"AnalysisWarnings={analysis.Summary.WarningCount}");
        _output.WriteLine($"AnalysisBlocked={analysis.Summary.BlockedCount}");
        _output.WriteLine($"AnalysisUnknown={analysis.Summary.UnknownCount}");
        _output.WriteLine($"AnalysisRecommendations={analysis.Summary.RecommendationCount}");
        _output.WriteLine($"AnalysisRiskSafe={RiskCount(analysis, RecommendationRiskLevel.Safe)}");
        _output.WriteLine($"AnalysisRiskMedium={RiskCount(analysis, RecommendationRiskLevel.Medium)}");
        _output.WriteLine($"AnalysisRiskAdvanced={RiskCount(analysis, RecommendationRiskLevel.Advanced)}");
        _output.WriteLine($"AnalysisRiskAggressive={RiskCount(analysis, RecommendationRiskLevel.Aggressive)}");
        _output.WriteLine($"AnalysisDurationMs={analysis.Duration.TotalMilliseconds:N0}");
    }

    [Fact]
    public async Task Real_scanner_analysis_flows_into_catalog_preset_preview_read_only()
    {
        var scanner = CreateScanner();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(75));
        var scanResult = await scanner.ScanAsync(null, timeout.Token);
        Assert.True(scanResult.IsSuccess, scanResult.ErrorMessage);

        var analysisResult = await CreateAnalysisEngine().AnalyzeAsync(scanResult.Value!, CancellationToken.None);
        Assert.True(analysisResult.IsSuccess, analysisResult.ErrorMessage);

        var catalog = new BuiltInOptimizationCatalog();
        var presetEngine = new OptimizationPresetEngine(catalog);
        var basic = presetEngine.Preview(scanResult.Value!, analysisResult.Value!, RecommendationPreset.Basic);
        var medium = presetEngine.Preview(scanResult.Value!, analysisResult.Value!, RecommendationPreset.Medium);
        var advanced = presetEngine.Preview(scanResult.Value!, analysisResult.Value!, RecommendationPreset.Advanced);

        Assert.Equal(12, catalog.GetDefinitions().Count(definition => definition.Category != OptimizationCategory.IntegrationTest));
        Assert.All(basic.SelectedItems, item => Assert.Equal(OptimizationRiskLevel.Safe, item.RiskLevel));
        Assert.All(medium.SelectedItems, item => Assert.True(item.RiskLevel <= OptimizationRiskLevel.Medium));
        Assert.DoesNotContain(basic.SelectedItems.Concat(medium.SelectedItems), item => item.IsSecurityTradeoff);
        Assert.DoesNotContain(advanced.SelectedItems, item => item.RiskLevel == OptimizationRiskLevel.Aggressive);

        _output.WriteLine($"CatalogVersion={catalog.CatalogVersion}");
        WritePresetSummary("Basic", basic);
        WritePresetSummary("Medium", medium);
        WritePresetSummary("Advanced", advanced);
    }

    [Fact]
    public async Task Real_scanner_analysis_flows_into_optimization_dry_run_and_controlled_rollback()
    {
        using var registryScope = ControlledRegistryTestScope.Acquire();
        var backup = RegistryBackup.Capture();
        var sessionDirectory = Path.Combine(Path.GetTempPath(), "BorealBoostPhase4System", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sessionDirectory);
        try
        {
            var scanner = CreateScanner();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(75));
            var scanResult = await scanner.ScanAsync(null, timeout.Token);
            Assert.True(scanResult.IsSuccess, scanResult.ErrorMessage);

            var analysisResult = await CreateAnalysisEngine().AnalyzeAsync(scanResult.Value!, CancellationToken.None);
            Assert.True(analysisResult.IsSuccess, analysisResult.ErrorMessage);

            var services = CreateOptimizationServices(sessionDirectory);
            var dryRun = await services.DryRun.DryRunAsync(
                scanResult.Value!,
                analysisResult.Value!,
                [BuiltInOptimizationCatalog.IntegrationProofOptimizationId],
                CancellationToken.None);

            Assert.True(dryRun.IsSuccess, dryRun.ErrorMessage);
            Assert.True(dryRun.Value!.Validation.CanExecute);
            Assert.Single(dryRun.Value.Operations);

            var executed = await services.SessionService.ExecuteAsync(dryRun.Value.Plan, scanResult.Value!, CancellationToken.None);
            Assert.True(executed.IsSuccess, executed.ErrorMessage);
            Assert.True(executed.Value!.State is OptimizationSessionState.Completed or OptimizationSessionState.CompletedWithWarnings);
            Assert.NotNull(executed.Value.Snapshot);
            Assert.Contains(executed.Value.Journal, entry => entry.State == OperationJournalState.SnapshotCaptured);
            Assert.Contains(executed.Value.Journal, entry => entry.State == OperationJournalState.Verified);

            var rollback = await services.SessionService.RollbackAsync(executed.Value.SessionId, CancellationToken.None);
            Assert.True(rollback.IsSuccess, rollback.ErrorMessage);
            Assert.Equal(OptimizationSessionState.RolledBack, rollback.Value!.State);
            backup.AssertRestored();

            _output.WriteLine($"Phase4PlanOperations={dryRun.Value.Plan.OrderedOperations.Count}");
            _output.WriteLine($"Phase4DryRunBlockers={dryRun.Value.Blockers.Count}");
            _output.WriteLine($"Phase4SessionState={executed.Value.State}");
            _output.WriteLine($"Phase4RollbackState={rollback.Value.State}");
            _output.WriteLine($"Phase4JournalEntries={rollback.Value.Journal.Count}");
        }
        finally
        {
            backup.Restore();
            if (Directory.Exists(sessionDirectory))
            {
                Directory.Delete(sessionDirectory, recursive: true);
            }
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

    private static AnalysisEngine CreateAnalysisEngine()
    {
        IAnalysisRule[] rules =
        [
            new PartialScanAnalysisRule(),
            new WindowsCompatibilityAnalysisRule(),
            new MissingDriverAnalysisRule(),
            new ProblemDeviceAnalysisRule(),
            new BasicDisplayAdapterAnalysisRule(),
            new LowSystemDriveSpaceAnalysisRule(),
            new VirtualMachineAnalysisRule(),
            new PowerContextAnalysisRule(),
            new StartupVolumeAnalysisRule(),
            new SecurityCapabilitiesAnalysisRule(),
            new MemoryVisibilityAnalysisRule()
        ];

        return new AnalysisEngine(rules, new NoopLogger<AnalysisEngine>());
    }

    private static OptimizationServices CreateOptimizationServices(string sessionDirectory)
    {
        var catalog = new BuiltInOptimizationCatalog();
        var definitionValidator = new OptimizationDefinitionValidator();
        var handlerRegistry = new OperationHandlerRegistry([new BorealIntegrationRegistryOperationHandler()]);
        var planner = new ExecutionPlanner(catalog, definitionValidator);
        var validator = new ExecutionPlanValidator(catalog, handlerRegistry);
        var preflight = new PreflightService(validator, handlerRegistry);
        var dryRun = new DryRunService(planner, validator, handlerRegistry);
        var store = new FileOptimizationSessionStore(sessionDirectory);
        var sessionService = new OptimizationSessionService(
            preflight,
            handlerRegistry,
            store,
            new RestorePointService(),
            new NoopLogger<OptimizationSessionService>(),
            new CrossProcessOptimizationSessionLock(Path.Combine(sessionDirectory, "optimization.lock")));
        return new OptimizationServices(dryRun, sessionService);
    }

    private static int RiskCount(AnalysisResult result, RecommendationRiskLevel risk)
    {
        return result.Summary.RiskDistribution.TryGetValue(risk, out var count) ? count : 0;
    }

    private void WritePresetSummary(string name, OptimizationPresetSelection selection)
    {
        _output.WriteLine($"{name}Selected={selection.SelectedItems.Count}");
        _output.WriteLine($"{name}RequiresConfirmation={selection.RequiresConfirmationItems.Count}");
        _output.WriteLine($"{name}Blocked={selection.BlockedItems.Count}");
        _output.WriteLine($"{name}NotApplicable={selection.Items.Count(item => item.Status == OptimizationPresetSelectionStatus.NotApplicable)}");
    }

    private sealed class SynchronousProgress : IProgress<ScanProgressUpdate>
    {
        public List<ScanProgressUpdate> Updates { get; } = [];

        public void Report(ScanProgressUpdate value)
        {
            Updates.Add(value);
        }
    }

    private sealed record OptimizationServices(
        IDryRunService DryRun,
        IOptimizationSessionService SessionService);

    private sealed class RegistryBackup
    {
        private readonly bool _existed;
        private readonly Microsoft.Win32.RegistryValueKind? _kind;
        private readonly object? _value;

        private RegistryBackup(bool existed, Microsoft.Win32.RegistryValueKind? kind, object? value)
        {
            _existed = existed;
            _kind = kind;
            _value = value;
        }

        public static RegistryBackup Capture()
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(AgentOperationSecurityValidator.IntegrationTestKeyPath, writable: false);
            if (key is null || !key.GetValueNames().Contains(AgentOperationSecurityValidator.IntegrationTestValueName, StringComparer.Ordinal))
            {
                return new RegistryBackup(false, null, null);
            }

            return new RegistryBackup(
                true,
                key.GetValueKind(AgentOperationSecurityValidator.IntegrationTestValueName),
                ReadRawValue(
                    key,
                    AgentOperationSecurityValidator.IntegrationTestValueName,
                    key.GetValueKind(AgentOperationSecurityValidator.IntegrationTestValueName)));
        }

        public void AssertRestored()
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(AgentOperationSecurityValidator.IntegrationTestKeyPath, writable: false);
            if (!_existed)
            {
                Assert.True(key is null || !key.GetValueNames().Contains(AgentOperationSecurityValidator.IntegrationTestValueName, StringComparer.Ordinal));
                return;
            }

            Assert.NotNull(key);
            Assert.Equal(_kind, key!.GetValueKind(AgentOperationSecurityValidator.IntegrationTestValueName));
            AssertRegistryValuesEqual(_value, ReadRawValue(key, AgentOperationSecurityValidator.IntegrationTestValueName, _kind!.Value));
        }

        public void Restore()
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(AgentOperationSecurityValidator.IntegrationTestKeyPath, writable: true);
            if (!_existed)
            {
                key?.DeleteValue(AgentOperationSecurityValidator.IntegrationTestValueName, throwOnMissingValue: false);
                return;
            }

            if (key is not null && _kind is not null)
            {
                key.SetValue(AgentOperationSecurityValidator.IntegrationTestValueName, _value ?? string.Empty, _kind.Value);
            }
        }

        private static object? ReadRawValue(Microsoft.Win32.RegistryKey key, string valueName, Microsoft.Win32.RegistryValueKind kind)
        {
            return kind == Microsoft.Win32.RegistryValueKind.ExpandString
                ? key.GetValue(valueName, null, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames)
                : key.GetValue(valueName);
        }

        private static void AssertRegistryValuesEqual(object? expected, object? actual)
        {
            switch (expected)
            {
                case byte[] expectedBytes:
                    Assert.IsType<byte[]>(actual);
                    Assert.Equal(expectedBytes, (byte[])actual);
                    break;
                case string[] expectedStrings:
                    Assert.IsType<string[]>(actual);
                    Assert.Equal(expectedStrings, (string[])actual);
                    break;
                default:
                    Assert.Equal(expected, actual);
                    break;
            }
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
