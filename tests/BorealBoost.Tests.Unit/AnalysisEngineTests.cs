using BorealBoost.Analysis.RecommendationEngine;
using BorealBoost.Analysis.RecommendationEngine.Rules;
using BorealBoost.Analysis.SystemScanner;
using BorealBoost.Core.Analysis;
using BorealBoost.Core.Common;
using BorealBoost.Core.Scanner;
using Microsoft.Extensions.Logging;

namespace BorealBoost.Tests.Unit;

public sealed class AnalysisEngineTests
{
    [Fact]
    public async Task Engine_returns_versioned_deterministic_result()
    {
        var snapshots = new[]
        {
            new SnapshotBuilder().Build(),
            new SnapshotBuilder { FormFactor = MachineFormFactor.Laptop, Power = new PowerSnapshot(true, false, 80, PowerSourceKind.Battery, null, DataSourceKind.WindowsApi) }.Build(),
            new SnapshotBuilder
            {
                FormFactor = MachineFormFactor.VirtualMachine,
                IsVirtualMachine = true,
                VirtualizationPlatform = "Hyper-V",
                Graphics = [new GpuSnapshot("Hyper-V Video", HardwareVendor.HyperV, null, null, null, null, null, VramDetectionStatus.Unknown, "OK", GpuFormFactor.Virtual, DataSourceKind.Wmi)]
            }.Build(),
            new SnapshotBuilder
            {
                OsName = "Microsoft Windows 10 Pro",
                OsBuild = 19045,
                WindowsCompatibility = WindowsCompatibilityStatus.LegacySupported
            }.Build(),
            new SnapshotBuilder
            {
                OsName = "Microsoft Windows 11 Pro",
                OsBuild = 26200,
                WindowsCompatibility = WindowsCompatibilityStatus.Supported
            }.Build(),
            new SnapshotBuilder().WithDevice(DeviceHealthStatus.MissingDriver, 28).Build(),
            new SnapshotBuilder { PartialScan = true }.WithProvider("Drivers", ProviderResultStatus.TimedOut).Build(),
            new SnapshotBuilder { Graphics = [] }.Build(),
            new SnapshotBuilder().WithSystemDrive(totalBytes: 100L * 1024 * 1024 * 1024, freeBytes: 4L * 1024 * 1024 * 1024).Build(),
            new SnapshotBuilder().WithCapability("SecureBootEnabled", DetectionStatus.Unknown, null).Build()
        };
        var engine = CreateEngine();

        foreach (var snapshot in snapshots)
        {
            var first = await engine.AnalyzeAsync(snapshot, CancellationToken.None);
            var second = await engine.AnalyzeAsync(snapshot, CancellationToken.None);

            Assert.True(first.IsSuccess, first.ErrorMessage);
            Assert.True(second.IsSuccess, second.ErrorMessage);
            Assert.Equal(AnalysisEngine.EngineVersion, first.Value!.EngineVersion);
            Assert.Equal(AnalysisEngine.RuleCatalogVersion, first.Value.RuleCatalogVersion);
            Assert.Equal(snapshot.Metadata.ScanId, first.Value.ScanId);
            AssertDecisionSignatureEqual(first.Value, second.Value!);
        }
    }

    [Fact]
    public async Task Partial_scan_creates_warning_recommendation()
    {
        var snapshot = new SnapshotBuilder { PartialScan = true }.WithProvider("Drivers", ProviderResultStatus.TimedOut).Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.SYSTEM.001" && rule.Status == AnalysisRuleStatus.Warning);
        Assert.Contains(result.Value.Recommendations, recommendation => recommendation.RecommendationId == "BB.REC.SYSTEM.RESCAN.PARTIAL");
    }

    [Fact]
    public async Task Complete_scan_is_healthy_for_partial_scan_rule()
    {
        var result = await CreateEngine().AnalyzeAsync(new SnapshotBuilder().Build(), CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.SYSTEM.001" && rule.Status == AnalysisRuleStatus.Healthy);
    }

    [Fact]
    public async Task Windows_10_legacy_is_warning_not_blocker()
    {
        var snapshot = new SnapshotBuilder
        {
            OsName = "Microsoft Windows 10 Pro",
            OsBuild = 19045,
            WindowsCompatibility = WindowsCompatibilityStatus.LegacySupported
        }.Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.WINDOWS.001" && rule.Status == AnalysisRuleStatus.Warning);
        Assert.Contains(result.Value.Recommendations, recommendation => recommendation.RecommendationId == "BB.REC.WINDOWS.LEGACY_TARGET");
    }

    [Fact]
    public async Task Unsupported_windows_blocks_automatic_planning()
    {
        var snapshot = new SnapshotBuilder
        {
            OsName = "Microsoft Windows 8.1",
            OsBuild = 9600,
            WindowsCompatibility = WindowsCompatibilityStatus.Unsupported
        }.Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);
        var recommendation = result.Value!.Recommendations.Single(item => item.RecommendationId == "BB.REC.WINDOWS.UNSUPPORTED_BLOCK");

        Assert.Contains(result.Value.RuleResults, rule => rule.Rule.RuleId == "BB.WINDOWS.001" && rule.Status == AnalysisRuleStatus.Blocked);
        Assert.Equal(RecommendationCompatibilityStatus.Incompatible, recommendation.Compatibility.Status);
        Assert.Equal(RecommendationPresetEligibility.None, recommendation.PresetEligibility);
    }

    [Fact]
    public async Task Unknown_windows_does_not_create_opportunity()
    {
        var snapshot = new SnapshotBuilder
        {
            OsName = null,
            OsBuild = null,
            WindowsCompatibility = WindowsCompatibilityStatus.Unknown
        }.Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.WINDOWS.001" && rule.Status == AnalysisRuleStatus.Unknown);
        Assert.DoesNotContain(result.Value.Recommendations, recommendation => recommendation.RuleId == "BB.WINDOWS.001");
    }

    [Fact]
    public async Task Missing_driver_creates_medium_recommendation_without_outdated_claim()
    {
        var snapshot = new SnapshotBuilder().WithDevice(DeviceHealthStatus.MissingDriver, 28).Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);
        var recommendation = result.Value!.Recommendations.Single(item => item.RecommendationId == "BB.REC.DRIVER.MISSING_INVESTIGATE");

        Assert.Equal(RecommendationRiskLevel.Medium, recommendation.RiskLevel);
        Assert.DoesNotContain("desatualizado", recommendation.TechnicalReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Missing_driver_rule_is_healthy_without_missing_devices()
    {
        var result = await CreateEngine().AnalyzeAsync(new SnapshotBuilder().Build(), CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.DRIVER.001" && rule.Status == AnalysisRuleStatus.Healthy);
    }

    [Fact]
    public async Task Problem_device_creates_objective_recommendation()
    {
        var snapshot = new SnapshotBuilder().WithDevice(DeviceHealthStatus.Disabled, 22).Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);

        Assert.Contains(result.Value!.Recommendations, recommendation => recommendation.RecommendationId == "BB.REC.DRIVER.PROBLEM_DEVICE_REVIEW");
        Assert.Contains(result.Value.RuleResults, rule => rule.Rule.RuleId == "BB.DRIVER.002" && rule.Status == AnalysisRuleStatus.Opportunity);
    }

    [Fact]
    public async Task Problem_device_rule_is_healthy_without_problem_devices()
    {
        var result = await CreateEngine().AnalyzeAsync(new SnapshotBuilder().Build(), CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.DRIVER.002" && rule.Status == AnalysisRuleStatus.Healthy);
    }

    [Fact]
    public async Task Basic_display_adapter_creates_graphics_recommendation()
    {
        var snapshot = new SnapshotBuilder
        {
            Graphics =
            [
                new GpuSnapshot("Microsoft Basic Display Adapter", HardwareVendor.Microsoft, null, null, null, null, null, VramDetectionStatus.Unknown, "OK", GpuFormFactor.Virtual, DataSourceKind.Wmi)
            ]
        }.Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);

        Assert.Contains(result.Value!.Recommendations, recommendation => recommendation.RecommendationId == "BB.REC.GRAPHICS.BASIC_DISPLAY_REVIEW");
    }

    [Fact]
    public async Task Virtual_machine_virtual_gpu_is_not_graphics_driver_opportunity()
    {
        var snapshot = new SnapshotBuilder
        {
            FormFactor = MachineFormFactor.VirtualMachine,
            IsVirtualMachine = true,
            VirtualizationPlatform = "Hyper-V",
            Graphics =
            [
                new GpuSnapshot("Hyper-V Video", HardwareVendor.HyperV, null, null, null, null, null, VramDetectionStatus.Unknown, "OK", GpuFormFactor.Virtual, DataSourceKind.Wmi)
            ]
        }.Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.GRAPHICS.001" && rule.Status == AnalysisRuleStatus.NotApplicable);
        Assert.DoesNotContain(result.Value.Recommendations, recommendation => recommendation.RecommendationId == "BB.REC.GRAPHICS.BASIC_DISPLAY_REVIEW");
        Assert.Contains(result.Value.Recommendations, recommendation => recommendation.RecommendationId == "BB.REC.SYSTEM.VM_CONSERVATIVE_MODE");
    }

    [Fact]
    public async Task Virtual_machine_unknown_gpu_remains_unknown_without_graphics_recommendation()
    {
        var snapshot = new SnapshotBuilder
        {
            FormFactor = MachineFormFactor.VirtualMachine,
            IsVirtualMachine = true,
            VirtualizationPlatform = "Hyper-V",
            Graphics =
            [
                new GpuSnapshot(null, HardwareVendor.Unknown, null, null, null, null, null, VramDetectionStatus.Unknown, null, GpuFormFactor.Unknown, DataSourceKind.Wmi)
            ]
        }.Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.GRAPHICS.001" && rule.Status == AnalysisRuleStatus.Unknown);
        Assert.DoesNotContain(result.Value.Recommendations, recommendation => recommendation.RuleId == "BB.GRAPHICS.001");
    }

    [Fact]
    public async Task Physical_machine_unknown_gpu_remains_unknown_without_graphics_recommendation()
    {
        var snapshot = new SnapshotBuilder
        {
            Graphics =
            [
                new GpuSnapshot(null, HardwareVendor.Unknown, null, null, null, null, null, VramDetectionStatus.Unknown, null, GpuFormFactor.Unknown, DataSourceKind.Wmi)
            ]
        }.Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.GRAPHICS.001" && rule.Status == AnalysisRuleStatus.Unknown);
        Assert.DoesNotContain(result.Value.Recommendations, recommendation => recommendation.RuleId == "BB.GRAPHICS.001");
    }

    [Fact]
    public async Task Unknown_gpu_does_not_create_graphics_opportunity()
    {
        var snapshot = new SnapshotBuilder { Graphics = [] }.Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.GRAPHICS.001" && rule.Status == AnalysisRuleStatus.Unknown);
        Assert.DoesNotContain(result.Value.Recommendations, recommendation => recommendation.RuleId == "BB.GRAPHICS.001");
    }

    [Fact]
    public async Task Low_system_drive_space_creates_safe_recommendation()
    {
        var snapshot = new SnapshotBuilder().WithSystemDrive(totalBytes: 100L * 1024 * 1024 * 1024, freeBytes: 4L * 1024 * 1024 * 1024).Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);
        var recommendation = result.Value!.Recommendations.Single(item => item.RecommendationId == "BB.REC.STORAGE.SYSTEM_DRIVE_SPACE");

        Assert.Equal(RecommendationRiskLevel.Safe, recommendation.RiskLevel);
        Assert.Equal(ExpectedImpactLevel.WorkloadDependent, recommendation.ExpectedImpact);
    }

    [Fact]
    public async Task Adequate_system_drive_space_is_healthy()
    {
        var snapshot = new SnapshotBuilder().WithSystemDrive(totalBytes: 100L * 1024 * 1024 * 1024, freeBytes: 50L * 1024 * 1024 * 1024).Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.STORAGE.001" && rule.Status == AnalysisRuleStatus.Healthy);
    }

    [Fact]
    public async Task Virtual_machine_blocks_physical_hardware_recommendations()
    {
        var snapshot = new SnapshotBuilder
        {
            FormFactor = MachineFormFactor.VirtualMachine,
            IsVirtualMachine = true,
            VirtualizationPlatform = "Hyper-V"
        }.Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.SYSTEM.002" && rule.Status == AnalysisRuleStatus.Blocked);
        Assert.Contains(result.Value.Recommendations, recommendation => recommendation.RecommendationId == "BB.REC.SYSTEM.VM_CONSERVATIVE_MODE");
    }

    [Fact]
    public async Task Physical_desktop_is_healthy_for_vm_rule()
    {
        var result = await CreateEngine().AnalyzeAsync(new SnapshotBuilder().Build(), CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.SYSTEM.002" && rule.Status == AnalysisRuleStatus.Healthy);
    }

    [Fact]
    public async Task Laptop_power_context_requires_advanced_confirmation()
    {
        var snapshot = new SnapshotBuilder
        {
            FormFactor = MachineFormFactor.Laptop,
            Power = new PowerSnapshot(true, false, 80, PowerSourceKind.Battery, null, DataSourceKind.WindowsApi)
        }.Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);
        var recommendation = result.Value!.Recommendations.Single(item => item.RecommendationId == "BB.REC.POWER.PORTABLE_GUARD");

        Assert.Equal(RecommendationRiskLevel.Advanced, recommendation.RiskLevel);
        Assert.True(recommendation.UserConfirmationRequired);
        Assert.True(recommendation.PresetEligibility.HasFlag(RecommendationPresetEligibility.Advanced));
        Assert.False(recommendation.PresetEligibility.HasFlag(RecommendationPresetEligibility.Basic));
    }

    [Fact]
    public async Task Desktop_power_context_is_healthy()
    {
        var result = await CreateEngine().AnalyzeAsync(new SnapshotBuilder().Build(), CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.POWER.001" && rule.Status == AnalysisRuleStatus.Healthy);
    }

    [Fact]
    public async Task Excessive_startup_volume_creates_review_recommendation()
    {
        var snapshot = new SnapshotBuilder().WithStartupItems(StartupVolumeAnalysisRule.ExcessiveStartupItemThreshold).Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);
        var recommendation = result.Value!.Recommendations.Single(recommendation => recommendation.RecommendationId == "BB.REC.STARTUP.VOLUME_REVIEW");

        Assert.Contains(result.Value.RuleResults, rule => rule.Rule.RuleId == "BB.STARTUP.001" && rule.Status == AnalysisRuleStatus.Warning);
        Assert.Equal(RecommendationRiskLevel.Safe, recommendation.RiskLevel);
        Assert.Equal(RecommendationEvidenceLevel.Experimental, recommendation.EvidenceLevel);
        Assert.Equal(ExpectedImpactLevel.Unknown, recommendation.ExpectedImpact);
        Assert.False(recommendation.PresetEligibility.HasFlag(RecommendationPresetEligibility.Basic));
        Assert.False(recommendation.PresetEligibility.HasFlag(RecommendationPresetEligibility.Medium));
        Assert.True(recommendation.PresetEligibility.HasFlag(RecommendationPresetEligibility.Advanced));
        Assert.Contains("nao prova degradacao", recommendation.ShortDescription, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Low_startup_volume_is_healthy()
    {
        var snapshot = new SnapshotBuilder().WithStartupItems(5).Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.STARTUP.001" && rule.Status == AnalysisRuleStatus.Healthy);
    }

    [Fact]
    public async Task Secure_boot_disabled_is_advanced_security_warning()
    {
        var snapshot = new SnapshotBuilder().WithCapability("SecureBootEnabled", DetectionStatus.Known, false).Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);
        var recommendation = result.Value!.Recommendations.Single(item => item.RecommendationId == "BB.REC.SECURITY.SECURE_BOOT_REVIEW");

        Assert.Equal(RecommendationRiskLevel.Advanced, recommendation.RiskLevel);
        Assert.Equal(ExpectedImpactLevel.Unknown, recommendation.ExpectedImpact);
    }

    [Fact]
    public async Task Secure_boot_enabled_is_healthy()
    {
        var result = await CreateEngine().AnalyzeAsync(new SnapshotBuilder().Build(), CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.SECURITY.001" && rule.Status == AnalysisRuleStatus.Healthy);
    }

    [Fact]
    public async Task Memory_visible_gap_is_warning_but_not_upgrade_claim()
    {
        var snapshot = new SnapshotBuilder
        {
            Memory = new MemorySnapshot(16UL * 1024 * 1024 * 1024, 14UL * 1024 * 1024 * 1024, 2, [], DataSourceKind.Wmi)
        }.Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);
        var recommendation = result.Value!.Recommendations.Single(item => item.RecommendationId == "BB.REC.MEMORY.VISIBLE_GAP_REVIEW");

        Assert.Contains(result.Value.RuleResults, rule => rule.Rule.RuleId == "BB.MEMORY.001" && rule.Status == AnalysisRuleStatus.Warning);
        Assert.DoesNotContain("upgrade", recommendation.TechnicalReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Memory_without_gap_is_healthy()
    {
        var snapshot = new SnapshotBuilder
        {
            Memory = new MemorySnapshot(16UL * 1024 * 1024 * 1024, 16UL * 1024 * 1024 * 1024, 2, [], DataSourceKind.Wmi)
        }.Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);

        Assert.Contains(result.Value!.RuleResults, rule => rule.Rule.RuleId == "BB.MEMORY.001" && rule.Status == AnalysisRuleStatus.Healthy);
    }

    [Fact]
    public async Task Rule_exception_is_isolated_without_false_recommendation()
    {
        var engine = new AnalysisEngine([new ThrowingRule()], new NoopLogger<AnalysisEngine>());

        var result = await engine.AnalyzeAsync(new SnapshotBuilder().Build(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Warnings);
        Assert.Empty(result.Value.Recommendations);
        Assert.Equal(AnalysisRuleStatus.Unknown, result.Value.RuleResults.Single().Status);
    }

    [Fact]
    public async Task Preset_previews_group_recommendations_by_eligibility()
    {
        var snapshot = new SnapshotBuilder()
            .WithSystemDrive(totalBytes: 100L * 1024 * 1024 * 1024, freeBytes: 4L * 1024 * 1024 * 1024)
            .WithStartupItems(StartupVolumeAnalysisRule.ExcessiveStartupItemThreshold)
            .Build();

        var result = await CreateEngine().AnalyzeAsync(snapshot, CancellationToken.None);

        var basic = result.Value!.RecommendationPlan.Presets.Single(preset => preset.Preset == RecommendationPreset.Basic);
        var medium = result.Value.RecommendationPlan.Presets.Single(preset => preset.Preset == RecommendationPreset.Medium);

        Assert.True(basic.EligibleRecommendationCount > 0);
        Assert.True(medium.EligibleRecommendationCount >= basic.EligibleRecommendationCount);
    }

    [Fact]
    public async Task Duplicate_recommendation_id_fails_validation_instead_of_silent_deduplication()
    {
        var engine = new AnalysisEngine(
            [
                new StaticRecommendationRule("BB.TEST.001", ValidRecommendation("BB.REC.TEST.DUPLICATE", "BB.TEST.001")),
                new StaticRecommendationRule("BB.TEST.002", ValidRecommendation("BB.REC.TEST.DUPLICATE", "BB.TEST.002"))
            ],
            new NoopLogger<AnalysisEngine>());

        var result = await engine.AnalyzeAsync(new SnapshotBuilder().Build(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("analysis.validation.failed", result.ErrorCode);
    }

    [Fact]
    public async Task Empty_recommendation_id_fails_invariant_validation()
    {
        var engine = new AnalysisEngine(
            [new StaticRecommendationRule("BB.TEST.001", ValidRecommendation(string.Empty, "BB.TEST.001"))],
            new NoopLogger<AnalysisEngine>());

        var result = await engine.AnalyzeAsync(new SnapshotBuilder().Build(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("analysis.validation.failed", result.ErrorCode);
    }

    [Fact]
    public async Task Advanced_recommendation_without_confirmation_fails_invariant_validation()
    {
        var invalid = ValidRecommendation("BB.REC.TEST.ADVANCED", "BB.TEST.001") with
        {
            RiskLevel = RecommendationRiskLevel.Advanced,
            PresetEligibility = RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
            UserConfirmationRequired = false
        };
        var engine = new AnalysisEngine([new StaticRecommendationRule("BB.TEST.001", invalid)], new NoopLogger<AnalysisEngine>());

        var result = await engine.AnalyzeAsync(new SnapshotBuilder().Build(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("analysis.validation.failed", result.ErrorCode);
    }

    [Fact]
    public async Task Experimental_recommendation_cannot_enter_basic_or_medium_presets()
    {
        var invalid = ValidRecommendation("BB.REC.TEST.EXPERIMENTAL", "BB.TEST.001") with
        {
            EvidenceLevel = RecommendationEvidenceLevel.Experimental,
            PresetEligibility = RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom
        };
        var engine = new AnalysisEngine([new StaticRecommendationRule("BB.TEST.001", invalid)], new NoopLogger<AnalysisEngine>());

        var result = await engine.AnalyzeAsync(new SnapshotBuilder().Build(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("analysis.validation.failed", result.ErrorCode);
    }

    [Fact]
    public async Task Recommendation_self_conflict_fails_invariant_validation()
    {
        var invalid = ValidRecommendation("BB.REC.TEST.SELF", "BB.TEST.001") with
        {
            ConflictsWith = ["BB.REC.TEST.SELF"]
        };
        var engine = new AnalysisEngine([new StaticRecommendationRule("BB.TEST.001", invalid)], new NoopLogger<AnalysisEngine>());

        var result = await engine.AnalyzeAsync(new SnapshotBuilder().Build(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("analysis.validation.failed", result.ErrorCode);
    }

    [Fact]
    public async Task Recommendation_unknown_requires_fails_invariant_validation()
    {
        var invalid = ValidRecommendation("BB.REC.TEST.REQUIRES", "BB.TEST.001") with
        {
            Requires = ["BB.REC.TEST.MISSING"]
        };
        var engine = new AnalysisEngine([new StaticRecommendationRule("BB.TEST.001", invalid)], new NoopLogger<AnalysisEngine>());

        var result = await engine.AnalyzeAsync(new SnapshotBuilder().Build(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("analysis.validation.failed", result.ErrorCode);
    }

    [Fact]
    public async Task Analysis_session_rejects_two_concurrent_starts_across_clients()
    {
        var snapshotStore = new InMemorySystemSnapshotStore();
        snapshotStore.Set(new SnapshotBuilder().Build());
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = CreateSessionService(
            snapshotStore,
            new DelegatingAnalysisEngine(async (snapshot, cancellationToken) =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(cancellationToken);
                return await CreateEngine().AnalyzeAsync(snapshot, cancellationToken);
            }));

        var first = service.AnalyzeCurrentSnapshotAsync(CancellationToken.None);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = await service.AnalyzeCurrentSnapshotAsync(CancellationToken.None);
        release.SetResult();
        var firstResult = await first;

        Assert.True(firstResult.IsSuccess, firstResult.ErrorMessage);
        Assert.True(second.IsFailure);
        Assert.Equal("analysis.already_running", second.ErrorCode);
        Assert.Equal(AnalysisSessionState.Completed, service.State);
    }

    [Fact]
    public async Task Analysis_session_allows_new_analysis_after_completion()
    {
        var snapshotStore = new InMemorySystemSnapshotStore();
        snapshotStore.Set(new SnapshotBuilder().Build());
        var service = CreateSessionService(snapshotStore, CreateEngine());

        var first = await service.AnalyzeCurrentSnapshotAsync(CancellationToken.None);
        var second = await service.AnalyzeCurrentSnapshotAsync(CancellationToken.None);

        Assert.True(first.IsSuccess, first.ErrorMessage);
        Assert.True(second.IsSuccess, second.ErrorMessage);
        Assert.Equal(AnalysisSessionState.Completed, service.State);
    }

    [Fact]
    public async Task Analysis_session_cancels_active_run_and_allows_new_run()
    {
        var snapshotStore = new InMemorySystemSnapshotStore();
        snapshotStore.Set(new SnapshotBuilder().Build());
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var invocation = 0;
        var service = CreateSessionService(
            snapshotStore,
            new DelegatingAnalysisEngine(async (snapshot, cancellationToken) =>
            {
                if (Interlocked.Increment(ref invocation) == 1)
                {
                    started.TrySetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }

                return await CreateEngine().AnalyzeAsync(snapshot, cancellationToken);
            }));

        var first = service.AnalyzeCurrentSnapshotAsync(CancellationToken.None);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        service.Cancel();
        var canceled = await first;
        var second = await service.AnalyzeCurrentSnapshotAsync(CancellationToken.None);

        Assert.True(canceled.IsFailure);
        Assert.Equal("analysis.canceled", canceled.ErrorCode);
        Assert.True(second.IsSuccess, second.ErrorMessage);
        Assert.Equal(AnalysisSessionState.Completed, service.State);
    }

    [Fact]
    public async Task Analysis_session_discards_result_when_snapshot_changes_during_analysis()
    {
        var snapshotStore = new InMemorySystemSnapshotStore();
        snapshotStore.Set(new SnapshotBuilder().Build());
        var resultStore = new InMemoryAnalysisResultStore();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = CreateSessionService(
            snapshotStore,
            new DelegatingAnalysisEngine(async (snapshot, cancellationToken) =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(cancellationToken);
                return await CreateEngine().AnalyzeAsync(snapshot, cancellationToken);
            }),
            resultStore);

        var first = service.AnalyzeCurrentSnapshotAsync(CancellationToken.None);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        snapshotStore.Set(new SnapshotBuilder().WithDevice(DeviceHealthStatus.MissingDriver, 28).Build());
        release.SetResult();
        var result = await first;

        Assert.True(result.IsFailure);
        Assert.Equal("analysis.snapshot_changed", result.ErrorCode);
        Assert.Null(resultStore.Current);
        Assert.Equal(AnalysisSessionState.Failed, service.State);
    }

    private static AnalysisEngine CreateEngine()
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

    private static AnalysisSessionService CreateSessionService(
        InMemorySystemSnapshotStore snapshotStore,
        IAnalysisEngine engine,
        InMemoryAnalysisResultStore? resultStore = null)
    {
        return new AnalysisSessionService(
            engine,
            snapshotStore,
            resultStore ?? new InMemoryAnalysisResultStore(),
            new NoopLogger<AnalysisSessionService>());
    }

    private static Recommendation ValidRecommendation(string recommendationId, string ruleId)
    {
        return new Recommendation(
            recommendationId,
            ruleId,
            "Recommendation title",
            "Recommendation description",
            "Technical reason based on snapshot facts.",
            AnalysisCategory.System,
            RecommendationRiskLevel.Safe,
            RecommendationEvidenceLevel.Strong,
            new RecommendationCompatibility(RecommendationCompatibilityStatus.Compatible, ["Read-only recommendation."]),
            "Detected state",
            "Desired future state",
            ExpectedImpactLevel.Unknown,
            ["Compatibility"],
            [],
            false,
            RecommendationReversibility.Full,
            RecommendationPresetEligibility.Basic | RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
            false,
            null,
            [new AnalysisEvidence("SystemSnapshot", "Metadata.ScanId", "test", RecommendationEvidenceLevel.Strong)],
            [],
            []);
    }

    private static void AssertDecisionSignatureEqual(AnalysisResult expected, AnalysisResult actual)
    {
        Assert.Equal(
            expected.RuleResults
                .Select(rule => $"{rule.Rule.RuleId}:{rule.Status}:{string.Join(",", rule.Issues.Select(issue => issue.Code).Order(StringComparer.Ordinal))}"),
            actual.RuleResults
                .Select(rule => $"{rule.Rule.RuleId}:{rule.Status}:{string.Join(",", rule.Issues.Select(issue => issue.Code).Order(StringComparer.Ordinal))}"));

        Assert.Equal(
            expected.Findings.Select(finding => $"{finding.RuleId}:{finding.Status}:{finding.EvidenceLevel}:{string.Join(",", finding.RelatedRecommendationIds.Order(StringComparer.Ordinal))}"),
            actual.Findings.Select(finding => $"{finding.RuleId}:{finding.Status}:{finding.EvidenceLevel}:{string.Join(",", finding.RelatedRecommendationIds.Order(StringComparer.Ordinal))}"));

        Assert.Equal(
            expected.Recommendations.Select(RecommendationSignature),
            actual.Recommendations.Select(RecommendationSignature));

        Assert.Equal(
            expected.RecommendationPlan.Presets.Select(PresetSignature),
            actual.RecommendationPlan.Presets.Select(PresetSignature));
    }

    private static string RecommendationSignature(Recommendation recommendation)
    {
        return string.Join(
            ":",
            recommendation.RecommendationId,
            recommendation.RuleId,
            recommendation.RiskLevel,
            recommendation.EvidenceLevel,
            recommendation.Compatibility.Status,
            recommendation.PresetEligibility,
            string.Join(",", recommendation.ConflictsWith.Order(StringComparer.Ordinal)),
            string.Join(",", recommendation.Requires.Order(StringComparer.Ordinal)));
    }

    private static string PresetSignature(PresetPreview preset)
    {
        var distribution = string.Join(
            ",",
            preset.RiskDistribution
                .OrderBy(pair => pair.Key)
                .Select(pair => $"{pair.Key}={pair.Value}"));
        return $"{preset.Preset}:{preset.EligibleRecommendationCount}:{string.Join(",", preset.RecommendationIds)}:{distribution}";
    }

    private sealed class SnapshotBuilder
    {
        private readonly List<ProviderResult> _providerResults =
        [
            ProviderResult.Succeeded("OperatingSystem", DataSourceKind.Composite, TimeSpan.FromMilliseconds(1)),
            ProviderResult.Succeeded("Graphics", DataSourceKind.Wmi, TimeSpan.FromMilliseconds(1)),
            ProviderResult.Succeeded("Memory", DataSourceKind.Wmi, TimeSpan.FromMilliseconds(1)),
            ProviderResult.Succeeded("Storage", DataSourceKind.Composite, TimeSpan.FromMilliseconds(1)),
            ProviderResult.Succeeded("Devices", DataSourceKind.Wmi, TimeSpan.FromMilliseconds(1)),
            ProviderResult.Succeeded("Startup", DataSourceKind.RegistryReadOnly, TimeSpan.FromMilliseconds(1)),
            ProviderResult.Succeeded("SecurityCapabilities", DataSourceKind.Composite, TimeSpan.FromMilliseconds(1))
        ];

        public bool PartialScan { get; init; }

        public string? OsName { get; init; } = "Microsoft Windows 11 Pro";

        public int? OsBuild { get; init; } = 26200;

        public WindowsCompatibilityStatus WindowsCompatibility { get; init; } = WindowsCompatibilityStatus.Supported;

        public MachineFormFactor FormFactor { get; init; } = MachineFormFactor.Desktop;

        public bool IsVirtualMachine { get; init; }

        public string? VirtualizationPlatform { get; init; }

        public List<GpuSnapshot> Graphics { get; init; } =
        [
            new GpuSnapshot("NVIDIA GeForce RTX Test", HardwareVendor.Nvidia, "1234", null, "1.0", null, null, VramDetectionStatus.Unknown, "OK", GpuFormFactor.Unknown, DataSourceKind.Wmi)
        ];

        public MemorySnapshot Memory { get; init; } = new(16UL * 1024 * 1024 * 1024, 16UL * 1024 * 1024 * 1024, 2, [], DataSourceKind.Wmi);

        public PowerSnapshot Power { get; init; } = new(false, true, null, PowerSourceKind.AC, "Balanced", DataSourceKind.Composite);

        public List<DeviceSnapshot> Devices { get; } = [];

        public List<StartupItemSnapshot> StartupItems { get; } = [];

        public List<SystemCapabilitySnapshot> Capabilities { get; } =
        [
            new("SecureBootAvailable", DetectionStatus.Known, true, "UEFI", DataSourceKind.Composite),
            new("SecureBootEnabled", DetectionStatus.Known, true, "True", DataSourceKind.Composite)
        ];

        public StorageSnapshot Storage { get; private set; } = new(
            [new StorageDiskSnapshot("Disk", "Vendor", 256UL * 1024 * 1024 * 1024, StorageMediaKind.Ssd, "SATA", "Healthy", DataSourceKind.Wmi)],
            [new StorageVolumeSnapshot("C:\\", "System", "Fixed", 256L * 1024 * 1024 * 1024, 128L * 1024 * 1024 * 1024, true, DataSourceKind.DriveInfo)],
            DataSourceKind.Composite);

        public SnapshotBuilder WithDevice(DeviceHealthStatus health, uint? problemCode)
        {
            Devices.Add(new DeviceSnapshot("Device", null, [], [], "Vendor", "System", health, problemCode, health == DeviceHealthStatus.Ok ? "OK" : "Error", DataSourceKind.Wmi));
            return this;
        }

        public SnapshotBuilder WithSystemDrive(long totalBytes, long freeBytes)
        {
            Storage = Storage with
            {
                Volumes =
                [
                    new StorageVolumeSnapshot("C:\\", "System", "Fixed", totalBytes, freeBytes, true, DataSourceKind.DriveInfo)
                ]
            };
            return this;
        }

        public SnapshotBuilder WithStartupItems(int count)
        {
            StartupItems.Clear();
            for (var index = 0; index < count; index++)
            {
                StartupItems.Add(new StartupItemSnapshot($"StartupItem{index}", "Test", DataSourceKind.RegistryReadOnly));
            }

            return this;
        }

        public SnapshotBuilder WithCapability(string key, DetectionStatus status, bool? isPresent)
        {
            Capabilities.RemoveAll(capability => capability.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            Capabilities.Add(new SystemCapabilitySnapshot(key, status, isPresent, isPresent?.ToString(), DataSourceKind.Composite));
            return this;
        }

        public SnapshotBuilder WithProvider(string providerName, ProviderResultStatus status)
        {
            _providerResults.RemoveAll(provider => provider.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
            var result = status switch
            {
                ProviderResultStatus.Success => ProviderResult.Succeeded(providerName, DataSourceKind.Unknown, TimeSpan.FromMilliseconds(1)),
                ProviderResultStatus.Partial => ProviderResult.Partial(providerName, DataSourceKind.Unknown, TimeSpan.FromMilliseconds(1), [new ScanIssue("partial", "partial", providerName)]),
                ProviderResultStatus.NotSupported => ProviderResult.NotSupported(providerName, DataSourceKind.Unknown, TimeSpan.FromMilliseconds(1), "not_supported", "not supported"),
                ProviderResultStatus.TimedOut => ProviderResult.TimedOut(providerName, TimeSpan.FromMilliseconds(1)),
                ProviderResultStatus.Canceled => ProviderResult.Canceled(providerName, TimeSpan.FromMilliseconds(1)),
                _ => ProviderResult.Failed(providerName, DataSourceKind.Unknown, TimeSpan.FromMilliseconds(1), "failed", "failed")
            };
            _providerResults.Add(result);
            return this;
        }

        public SystemSnapshot Build()
        {
            var started = DateTimeOffset.UtcNow.AddMilliseconds(-5);
            var completed = DateTimeOffset.UtcNow;
            var providerResults = _providerResults
                .OrderBy(provider => provider.ProviderName, StringComparer.Ordinal)
                .ToArray();
            var partial = PartialScan || providerResults.Any(provider => provider.Status is not ProviderResultStatus.Success);

            return new SystemSnapshot(
                new ScanMetadata(ScanId.New(), started, completed, completed - started, "test", "2.0.0", "X64", providerResults, partial, [], []),
                new OperatingSystemSnapshot(OsName, "Pro", "10.0", OsBuild, 0, "25H2", "X64", WindowsCompatibility, "test", DataSourceKind.Composite),
                new HardwareSnapshot("Vendor", "Model", FormFactor, IsVirtualMachine, VirtualizationPlatform, DataSourceKind.Wmi),
                [new CpuSnapshot("AMD", "AMD Ryzen Test", HardwareVendor.Amd, "X64", 12, 6, 1, null, null, null, null, true, DataSourceKind.Wmi)],
                Graphics,
                Memory,
                Storage,
                new MotherboardSnapshot("Vendor", "Board", null, DataSourceKind.Wmi),
                new FirmwareSnapshot("Vendor", "1.0", null, "UEFI", true, DataSourceKind.Composite),
                Devices,
                [],
                [],
                [new DisplaySnapshot("\\\\.\\DISPLAY1", "Display", 1920, 1080, 144, 96, true, DataSourceKind.WindowsApi)],
                Power,
                [],
                [],
                StartupItems,
                Capabilities);
        }
    }

    private sealed class ThrowingRule : IAnalysisRule
    {
        public AnalysisRuleMetadata Metadata { get; } = new("BB.TEST.THROW", AnalysisCategory.System, "Throw", "Throw", "1.0.0");

        public AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot)
        {
            throw new InvalidOperationException("Rule failure.");
        }
    }

    private sealed class StaticRecommendationRule : IAnalysisRule
    {
        private readonly IReadOnlyList<Recommendation> _recommendations;

        public StaticRecommendationRule(string ruleId, params Recommendation[] recommendations)
        {
            Metadata = new AnalysisRuleMetadata(ruleId, AnalysisCategory.System, "Static", "Static test rule", "1.0.0");
            _recommendations = recommendations;
        }

        public AnalysisRuleMetadata Metadata { get; }

        public AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot)
        {
            return new AnalysisRuleEvaluation(
                Metadata,
                AnalysisRuleStatus.Opportunity,
                new AnalysisFinding(
                    $"{Metadata.RuleId}.finding",
                    Metadata.RuleId,
                    Metadata.Category,
                    AnalysisRuleStatus.Opportunity,
                    "Static finding",
                    "Static summary",
                    "Static details",
                    RecommendationEvidenceLevel.Strong,
                    [new AnalysisEvidence("SystemSnapshot", "Metadata.ScanId", snapshot.Metadata.ScanId.ToString(), RecommendationEvidenceLevel.Strong)],
                    _recommendations.Select(recommendation => recommendation.RecommendationId).ToArray()),
                _recommendations,
                []);
        }
    }

    private sealed class DelegatingAnalysisEngine : IAnalysisEngine
    {
        private readonly Func<SystemSnapshot, CancellationToken, Task<Result<AnalysisResult>>> _analyze;

        public DelegatingAnalysisEngine(Func<SystemSnapshot, CancellationToken, Task<Result<AnalysisResult>>> analyze)
        {
            _analyze = analyze;
        }

        public Task<Result<AnalysisResult>> AnalyzeAsync(SystemSnapshot snapshot, CancellationToken cancellationToken)
        {
            return _analyze(snapshot, cancellationToken);
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
