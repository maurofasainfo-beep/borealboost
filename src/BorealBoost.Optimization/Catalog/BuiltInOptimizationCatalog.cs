using System.Security.Cryptography;
using System.Text;
using BorealBoost.Core.Analysis;
using BorealBoost.Core.Optimization;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Optimization.Catalog;

public sealed class BuiltInOptimizationCatalog : IOptimizationCatalog
{
    public const string CurrentSchemaVersion = "5.1.0";
    public const string CurrentCatalogVersion = "5.1.0-built-in-v1";
    public static readonly OptimizationId IntegrationProofOptimizationId = new("BB.OPT.INTEGRATION.REGISTRY_PROOF");
    public static readonly OperationId IntegrationProofOperationId = new("BB.OP.INTEGRATION.REGISTRY_PROOF.SET_VALUE");
    public const string IntegrationProofValue = "BorealBoost-Controlled-Phase4";

    private static readonly SupportedWindowsRequirement Windows10And11X64 = new(
        [WindowsCompatibilityStatus.Supported, WindowsCompatibilityStatus.LegacySupported],
        MinimumBuild: 19045,
        MaximumBuild: null,
        "X64");

    private static readonly SupportedWindowsRequirement Windows11X64 = new(
        [WindowsCompatibilityStatus.Supported],
        MinimumBuild: 22000,
        MaximumBuild: null,
        "X64");

    private static readonly SupportedWindowsRequirement Windows10DesktopX64 = new(
        [WindowsCompatibilityStatus.LegacySupported],
        MinimumBuild: 19045,
        MaximumBuild: 19045,
        "X64");

    private readonly IReadOnlyList<OptimizationDefinition> _definitions;

    public BuiltInOptimizationCatalog()
    {
        _definitions =
        [
            CreateIntegrationProof(),
            RegistryDefinition(
                "BB.OPT.VISUAL.TRANSPARENCY.DISABLE",
                "Disable transparency effects",
                "Turns off Windows transparency effects. This is a low-impact responsiveness preference, not an FPS optimization.",
                OptimizationCategory.Visual,
                OptimizationTechnicalCategory.Responsiveness,
                OptimizationRiskLevel.Safe,
                OptimizationEvidenceLevel.Moderate,
                ConfigurationEvidenceKind.DocumentedSupportedMechanism,
                ExpectedImpactLevel.Low,
                OptimizationPerformanceRelevance.Low,
                AutomaticPresetSuitability.Automatic,
                OptimizationUserPreferenceImpact.Medium,
                ConfigurationMechanism.Preference,
                ActivationBoundary.ExplorerRestart,
                OptimizationVerificationLevel.RequiresActivationBoundary,
                RollbackValidationLevel.OptimizationIntegrationValidated,
                PlatformValidationLevel.UnitTested,
                PlatformValidationLevel.IntegrationValidated,
                ["VisualEffects", "Responsiveness"],
                ["Windows UI loses translucent surfaces until rollback."],
                ["https://learn.microsoft.com/windows/apps/develop/settings/settings-common#colors"],
                RecommendationPresetEligibility.Basic | RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
                Windows10And11X64),
            RegistryDefinition(
                "BB.OPT.WINDOWS.EXPLORER.SHOW_EXTENSIONS",
                "Show known file extensions",
                "Shows file extensions in File Explorer so executable and script file names are visible. This is a UX/security-visibility preference, not a performance optimization.",
                OptimizationCategory.Windows,
                OptimizationTechnicalCategory.UXPreference,
                OptimizationRiskLevel.Safe,
                OptimizationEvidenceLevel.Moderate,
                ConfigurationEvidenceKind.ObservedRegistryBehavior,
                ExpectedImpactLevel.Low,
                OptimizationPerformanceRelevance.None,
                AutomaticPresetSuitability.CustomOnly,
                OptimizationUserPreferenceImpact.Medium,
                ConfigurationMechanism.ImplementationDetail,
                ActivationBoundary.ExplorerRestart,
                OptimizationVerificationLevel.RequiresActivationBoundary,
                RollbackValidationLevel.HandlerValidated,
                PlatformValidationLevel.UnitTested,
                PlatformValidationLevel.UnitTested,
                ["Maintenance", "SecurityVisibility"],
                ["File names become longer because known extensions are visible."],
                ["https://learn.microsoft.com/windows/apps/develop/settings/settings-common#file-explorer-classic"],
                RecommendationPresetEligibility.Custom,
                Windows10And11X64),
            RegistryDefinition(
                "BB.OPT.WINDOWS.AUTOPLAY.DISABLE",
                "Disable removable media AutoPlay",
                "Disables AutoPlay prompts for removable media/devices. This affects future AutoPlay behavior and does not disable AutoRun policy.",
                OptimizationCategory.Windows,
                OptimizationTechnicalCategory.SystemBehavior,
                OptimizationRiskLevel.Safe,
                OptimizationEvidenceLevel.Moderate,
                ConfigurationEvidenceKind.DocumentedSupportedMechanism,
                ExpectedImpactLevel.Low,
                OptimizationPerformanceRelevance.Low,
                AutomaticPresetSuitability.Automatic,
                OptimizationUserPreferenceImpact.Medium,
                ConfigurationMechanism.Preference,
                ActivationBoundary.Immediate,
                OptimizationVerificationLevel.StateVerified,
                RollbackValidationLevel.HandlerValidated,
                PlatformValidationLevel.UnitTested,
                PlatformValidationLevel.UnitTested,
                ["Maintenance", "BackgroundContention"],
                ["Removable media no longer opens AutoPlay automatically."],
                ["https://learn.microsoft.com/windows/apps/develop/settings/settings-common#autoplay"],
                RecommendationPresetEligibility.Basic | RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
                Windows10And11X64),
            RegistryDefinition(
                "BB.OPT.PRIVACY.START.RECOMMENDATIONS.DISABLE",
                "Disable Start recommendations",
                "Turns off Windows 11 Start recommendations for tips, shortcuts, apps and related promotional content. This is a privacy/UX preference.",
                OptimizationCategory.Privacy,
                OptimizationTechnicalCategory.Privacy,
                OptimizationRiskLevel.Safe,
                OptimizationEvidenceLevel.Moderate,
                ConfigurationEvidenceKind.DocumentedSupportedMechanism,
                ExpectedImpactLevel.Low,
                OptimizationPerformanceRelevance.None,
                AutomaticPresetSuitability.OptIn,
                OptimizationUserPreferenceImpact.Medium,
                ConfigurationMechanism.Preference,
                ActivationBoundary.ExplorerRestart,
                OptimizationVerificationLevel.RequiresActivationBoundary,
                RollbackValidationLevel.HandlerValidated,
                PlatformValidationLevel.NotApplicable,
                PlatformValidationLevel.UnitTested,
                ["Privacy", "VisualEffects"],
                ["Start may show fewer suggested items."],
                ["https://learn.microsoft.com/windows/apps/develop/settings/settings-windows-11#personalization---start---recommendations"],
                RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
                Windows11X64),
            RegistryDefinition(
                "BB.OPT.WINDOWS.START.MORE_PINS",
                "Prefer more Start pins",
                "Uses the Windows 11 Start layout that emphasizes pinned apps over recommendation rows. This is a Start layout preference.",
                OptimizationCategory.Windows,
                OptimizationTechnicalCategory.UXPreference,
                OptimizationRiskLevel.Safe,
                OptimizationEvidenceLevel.Moderate,
                ConfigurationEvidenceKind.DocumentedSupportedMechanism,
                ExpectedImpactLevel.Low,
                OptimizationPerformanceRelevance.None,
                AutomaticPresetSuitability.CustomOnly,
                OptimizationUserPreferenceImpact.High,
                ConfigurationMechanism.Preference,
                ActivationBoundary.ExplorerRestart,
                OptimizationVerificationLevel.RequiresActivationBoundary,
                RollbackValidationLevel.HandlerValidated,
                PlatformValidationLevel.NotApplicable,
                PlatformValidationLevel.UnitTested,
                ["VisualEffects", "Maintenance"],
                ["The Start menu layout changes to show more pinned applications."],
                ["https://learn.microsoft.com/windows/apps/develop/settings/settings-windows-11#personalization---start---layout---pins-and-recommendations"],
                RecommendationPresetEligibility.Custom,
                Windows11X64),
            RegistryDefinition(
                "BB.OPT.GAMING.GAMEBAR.CONTROLLER_SHORTCUT.DISABLE",
                "Disable controller Game Bar shortcut",
                "Prevents the controller shortcut from opening Game Bar. This changes a gaming feature shortcut, not rendering or FPS behavior.",
                OptimizationCategory.Gaming,
                OptimizationTechnicalCategory.GamingFeaturePreference,
                OptimizationRiskLevel.Safe,
                OptimizationEvidenceLevel.Moderate,
                ConfigurationEvidenceKind.DocumentedSupportedMechanism,
                ExpectedImpactLevel.Low,
                OptimizationPerformanceRelevance.None,
                AutomaticPresetSuitability.OptIn,
                OptimizationUserPreferenceImpact.Medium,
                ConfigurationMechanism.Preference,
                ActivationBoundary.ApplicationRestart,
                OptimizationVerificationLevel.RequiresActivationBoundary,
                RollbackValidationLevel.HandlerValidated,
                PlatformValidationLevel.NotApplicable,
                PlatformValidationLevel.UnitTested,
                ["GamingConsistency"],
                ["Controller shortcut no longer opens Game Bar."],
                ["https://learn.microsoft.com/windows/apps/develop/settings/settings-windows-11#gaming-game-bar-game-mode-gaming-shortcuts"],
                RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
                Windows11X64),
            RegistryDefinition(
                "BB.OPT.GAMING.GAMEBAR.RECORDING_SHORTCUT.DISABLE",
                "Disable Game Bar recording shortcut",
                "Disables the Game Bar recording shortcut path so accidental recording is less likely from that shortcut.",
                OptimizationCategory.Gaming,
                OptimizationTechnicalCategory.GamingFeaturePreference,
                OptimizationRiskLevel.Medium,
                OptimizationEvidenceLevel.Moderate,
                ConfigurationEvidenceKind.DocumentedSupportedMechanism,
                ExpectedImpactLevel.Low,
                OptimizationPerformanceRelevance.None,
                AutomaticPresetSuitability.OptIn,
                OptimizationUserPreferenceImpact.Medium,
                ConfigurationMechanism.Preference,
                ActivationBoundary.ApplicationRestart,
                OptimizationVerificationLevel.RequiresActivationBoundary,
                RollbackValidationLevel.HandlerValidated,
                PlatformValidationLevel.NotApplicable,
                PlatformValidationLevel.UnitTested,
                ["GamingConsistency"],
                ["Game Bar recording shortcut is disabled until rollback; users who record clips may want to keep it enabled."],
                ["https://learn.microsoft.com/windows/apps/develop/settings/settings-windows-11#gaming-game-bar-game-mode-gaming-shortcuts"],
                RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
                Windows11X64),
            RegistryDefinition(
                "BB.OPT.GAMING.GAMEBAR.BROADCAST_SHORTCUT.DISABLE",
                "Disable Game Bar broadcast shortcut",
                "Disables the Game Bar broadcast shortcut path to avoid accidental streaming activation from that shortcut.",
                OptimizationCategory.Gaming,
                OptimizationTechnicalCategory.GamingFeaturePreference,
                OptimizationRiskLevel.Medium,
                OptimizationEvidenceLevel.Moderate,
                ConfigurationEvidenceKind.DocumentedSupportedMechanism,
                ExpectedImpactLevel.Low,
                OptimizationPerformanceRelevance.None,
                AutomaticPresetSuitability.OptIn,
                OptimizationUserPreferenceImpact.Medium,
                ConfigurationMechanism.Preference,
                ActivationBoundary.ApplicationRestart,
                OptimizationVerificationLevel.RequiresActivationBoundary,
                RollbackValidationLevel.HandlerValidated,
                PlatformValidationLevel.NotApplicable,
                PlatformValidationLevel.UnitTested,
                ["GamingConsistency"],
                ["Game Bar broadcast shortcut is disabled until rollback."],
                ["https://learn.microsoft.com/windows/apps/develop/settings/settings-windows-11#gaming-game-bar-game-mode-gaming-shortcuts"],
                RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
                Windows11X64),
            RegistryDefinition(
                "BB.OPT.PRIVACY.GAMEBAR.CAMERA_CAPTURE_SHORTCUT.DISABLE",
                "Disable Game Bar camera capture shortcut",
                "Disables the Game Bar camera capture shortcut path. This is a privacy/gaming feature preference.",
                OptimizationCategory.Privacy,
                OptimizationTechnicalCategory.Privacy,
                OptimizationRiskLevel.Medium,
                OptimizationEvidenceLevel.Moderate,
                ConfigurationEvidenceKind.DocumentedSupportedMechanism,
                ExpectedImpactLevel.Low,
                OptimizationPerformanceRelevance.None,
                AutomaticPresetSuitability.OptIn,
                OptimizationUserPreferenceImpact.Medium,
                ConfigurationMechanism.Preference,
                ActivationBoundary.ApplicationRestart,
                OptimizationVerificationLevel.RequiresActivationBoundary,
                RollbackValidationLevel.HandlerValidated,
                PlatformValidationLevel.NotApplicable,
                PlatformValidationLevel.UnitTested,
                ["Privacy", "GamingConsistency"],
                ["Game Bar camera capture shortcut is disabled until rollback."],
                ["https://learn.microsoft.com/windows/apps/develop/settings/settings-windows-11#gaming-game-bar-game-mode-gaming-shortcuts"],
                RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
                Windows11X64),
            RegistryDefinition(
                "BB.OPT.PRIVACY.GAMEBAR.MIC_CAPTURE_SHORTCUT.DISABLE",
                "Disable Game Bar microphone capture shortcut",
                "Disables the Game Bar microphone capture shortcut path. This is a privacy/gaming feature preference.",
                OptimizationCategory.Privacy,
                OptimizationTechnicalCategory.Privacy,
                OptimizationRiskLevel.Medium,
                OptimizationEvidenceLevel.Moderate,
                ConfigurationEvidenceKind.DocumentedSupportedMechanism,
                ExpectedImpactLevel.Low,
                OptimizationPerformanceRelevance.None,
                AutomaticPresetSuitability.OptIn,
                OptimizationUserPreferenceImpact.Medium,
                ConfigurationMechanism.Preference,
                ActivationBoundary.ApplicationRestart,
                OptimizationVerificationLevel.RequiresActivationBoundary,
                RollbackValidationLevel.HandlerValidated,
                PlatformValidationLevel.NotApplicable,
                PlatformValidationLevel.UnitTested,
                ["Privacy", "GamingConsistency"],
                ["Game Bar microphone capture shortcut is disabled until rollback."],
                ["https://learn.microsoft.com/windows/apps/develop/settings/settings-windows-11#gaming-game-bar-game-mode-gaming-shortcuts"],
                RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
                Windows11X64),
            RegistryDefinition(
                "BB.OPT.PRIVACY.ADVERTISING_ID.DISABLE",
                "Disable Windows advertising ID",
                "Applies Microsoft's documented advertising ID policy so apps cannot use the Windows advertising ID for cross-app personalization. This is privacy policy, not a performance optimization.",
                OptimizationCategory.Privacy,
                OptimizationTechnicalCategory.Privacy,
                OptimizationRiskLevel.Medium,
                OptimizationEvidenceLevel.Strong,
                ConfigurationEvidenceKind.DocumentedPolicy,
                ExpectedImpactLevel.Low,
                OptimizationPerformanceRelevance.None,
                AutomaticPresetSuitability.OptIn,
                OptimizationUserPreferenceImpact.Medium,
                ConfigurationMechanism.Policy,
                ActivationBoundary.PolicyRefresh,
                OptimizationVerificationLevel.RequiresActivationBoundary,
                RollbackValidationLevel.HandlerValidated,
                PlatformValidationLevel.UnvalidatedForRelease,
                PlatformValidationLevel.UnvalidatedForRelease,
                ["Privacy"],
                ["Personalized in-app advertising may become less relevant; this is privacy, not FPS optimization."],
                ["https://learn.microsoft.com/windows/client-management/mdm/policy-csp-privacy#disableadvertisingid"],
                RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
                Windows10And11X64,
                RequiresElevation: true),
            RegistryDefinition(
                "BB.OPT.GAMING.GAME_DVR_POLICY.DISABLE",
                "Disable Windows Game Recording policy",
                "Applies Microsoft's documented Windows 10 desktop policy to prevent Windows Game Recording and Broadcasting. This disables a capture feature and is workload-dependent, not a guaranteed FPS gain.",
                OptimizationCategory.Gaming,
                OptimizationTechnicalCategory.GamingPerformance,
                OptimizationRiskLevel.Advanced,
                OptimizationEvidenceLevel.Strong,
                ConfigurationEvidenceKind.DocumentedPolicy,
                ExpectedImpactLevel.WorkloadDependent,
                OptimizationPerformanceRelevance.WorkloadDependent,
                AutomaticPresetSuitability.AdvancedOnly,
                OptimizationUserPreferenceImpact.High,
                ConfigurationMechanism.Policy,
                ActivationBoundary.PolicyRefresh,
                OptimizationVerificationLevel.RequiresActivationBoundary,
                RollbackValidationLevel.HandlerValidated,
                PlatformValidationLevel.UnvalidatedForRelease,
                PlatformValidationLevel.NotApplicable,
                ["GamingConsistency", "BackgroundContention"],
                ["Windows Game Recording and Broadcasting are unavailable until rollback; recording users should not select this item."],
                ["https://learn.microsoft.com/windows/client-management/mdm/policy-csp-applicationmanagement#allowgamedvr"],
                RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
                Windows10DesktopX64,
                RequiresElevation: true,
                RequiresUserConfirmation: true,
                CompatibilityRequirements: [new CompatibilityRequirement("NotVirtualMachine", "true", Required: true)])
        ];

        Manifest = new CatalogManifestMetadata(
            CurrentSchemaVersion,
            CurrentCatalogVersion,
            "BorealBoost BuiltIn",
            "BorealBoost.Optimization.Catalog.BuiltInOptimizationCatalog",
            ComputeCatalogContentHash(_definitions),
            DateTimeOffset.Parse("2026-08-13T00:00:00Z", global::System.Globalization.CultureInfo.InvariantCulture));
    }

    public string SchemaVersion => CurrentSchemaVersion;

    public string CatalogVersion => CurrentCatalogVersion;

    public CatalogManifestMetadata Manifest { get; }

    public IReadOnlyList<OptimizationDefinition> GetDefinitions() => _definitions;

    public OptimizationDefinition? Find(OptimizationId optimizationId)
    {
        return _definitions.FirstOrDefault(definition => definition.OptimizationId == optimizationId);
    }

    private static OptimizationDefinition CreateIntegrationProof()
    {
        var snapshotRequirement = RegistrySnapshot("HKCU registry value read before controlled mutation", "InternalTechnical");

        var operation = new OperationSpec(
            IntegrationProofOperationId,
            OperationType.BorealIntegrationRegistryValue,
            new RegistryValueOperationParameters(
                new RegistryOperationTarget(
                    RegistryHiveKind.CurrentUser,
                    AgentOperationSecurityValidator.IntegrationTestKeyPath,
                    AgentOperationSecurityValidator.IntegrationTestValueName,
                    RegistryViewKind.Default),
                new RegistryValueState(
                    Exists: true,
                    RegistryValueDataKind.String,
                    IntegrationProofValue,
                    null)),
            new OperationTimeoutPolicy(TimeSpan.FromSeconds(5)),
            new OperationRetryPolicy(
                RetryAllowed: true,
                MaxAttempts: 2,
                Backoff: TimeSpan.FromMilliseconds(50),
                [OperationErrorCategory.ApplyFailed]),
            OperationIdempotency.Idempotent,
            OperationReversibility.Full,
            RebootBoundary.None,
            OperationFailurePolicy.AttemptRollback,
            new OperationVerificationStrategy(OperationVerificationKind.ExactState, "Read back the controlled registry value and compare exact state."),
            new OperationRollbackStrategy(OperationRollbackKind.SnapshotRestore, "Restore the pre-operation value or absence captured in OperationSnapshot."),
            [snapshotRequirement]);

        return new OptimizationDefinition(
            IntegrationProofOptimizationId,
            "4.0.1",
            "Controlled engine proof",
            "Validates the transaction pipeline using only BorealBoost's HKCU integration-test registry value.",
            OptimizationCategory.IntegrationTest,
            OptimizationTechnicalCategory.Maintenance,
            OptimizationRiskLevel.Safe,
            OptimizationEvidenceLevel.Strong,
            ConfigurationEvidenceKind.Experimental,
            ExpectedImpactLevel.Low,
            OptimizationPerformanceRelevance.None,
            AutomaticPresetSuitability.CustomOnly,
            OptimizationUserPreferenceImpact.None,
            ConfigurationMechanism.ImplementationDetail,
            ActivationBoundary.Immediate,
            OptimizationVerificationLevel.StateVerified,
            RollbackValidationLevel.OptimizationIntegrationValidated,
            PlatformValidationLevel.IntegrationValidated,
            PlatformValidationLevel.IntegrationValidated,
            ["Maintenance"],
            ["Internal validation item; not shown in presets."],
            ["OPTIMIZATION_ENGINE.md"],
            RecommendationPresetEligibility.None,
            IsSecurityTradeoff: false,
            RequiresUserConfirmation: false,
            Windows10And11X64,
            [],
            [],
            [],
            [],
            RequiresElevation: false,
            RequiresRestart: false,
            SupportsUndo: true,
            RestorePointRequirement.NotRequired,
            [snapshotRequirement],
            [operation],
            [new VerificationSpec(operation.OperationId, operation.VerificationStrategy)],
            [new RollbackSpec(operation.OperationId, operation.RollbackStrategy)],
            OperationFailurePolicy.AttemptRollback,
            new TimeoutPolicy(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)),
            new SourceMetadata("BuiltIn", CurrentCatalogVersion, "OPTIMIZATION_ENGINE.md", null));
    }

    private static OptimizationDefinition RegistryDefinition(
        string optimizationId,
        string title,
        string description,
        OptimizationCategory category,
        OptimizationTechnicalCategory technicalCategory,
        OptimizationRiskLevel risk,
        OptimizationEvidenceLevel evidence,
        ConfigurationEvidenceKind configurationEvidence,
        ExpectedImpactLevel impact,
        OptimizationPerformanceRelevance performanceRelevance,
        AutomaticPresetSuitability automaticPresetSuitability,
        OptimizationUserPreferenceImpact userPreferenceImpact,
        ConfigurationMechanism configurationMechanism,
        ActivationBoundary activationBoundary,
        OptimizationVerificationLevel verificationLevel,
        RollbackValidationLevel rollbackValidationLevel,
        PlatformValidationLevel windows10ValidationLevel,
        PlatformValidationLevel windows11ValidationLevel,
        IReadOnlyList<string> impactAreas,
        IReadOnlyList<string> sideEffects,
        IReadOnlyList<string> evidenceReferences,
        RecommendationPresetEligibility presetEligibility,
        SupportedWindowsRequirement supportedWindows,
        bool RequiresElevation = false,
        bool RequiresRestart = false,
        bool RequiresUserConfirmation = false,
        bool IsSecurityTradeoff = false,
        IReadOnlyList<CompatibilityRequirement>? CompatibilityRequirements = null)
    {
        var id = new OptimizationId(optimizationId);
        var trusted = TrustedRegistryOperationTargets.CatalogV1.Single(target => target.OptimizationId == id);
        var snapshotRequirement = RegistrySnapshot(
            $"Read {trusted.Target.Hive}\\{trusted.Target.KeyPath}\\{trusted.Target.ValueName} before mutation.",
            trusted.Target.Hive == RegistryHiveKind.LocalMachine ? "InternalTechnical" : "PublicTechnical");
        var operation = new OperationSpec(
            trusted.OperationId,
            trusted.OperationType,
            new RegistryValueOperationParameters(trusted.Target, trusted.DesiredState),
            new OperationTimeoutPolicy(TimeSpan.FromSeconds(5)),
            new OperationRetryPolicy(
                RetryAllowed: true,
                MaxAttempts: 2,
                Backoff: TimeSpan.FromMilliseconds(50),
                [OperationErrorCategory.ApplyFailed]),
            OperationIdempotency.Idempotent,
            OperationReversibility.Full,
            RebootBoundary.None,
            OperationFailurePolicy.AttemptRollback,
            new OperationVerificationStrategy(OperationVerificationKind.ExactState, "Read back the registry value and compare exact desired state."),
            new OperationRollbackStrategy(OperationRollbackKind.SnapshotRestore, "Restore the original registry value kind, data, view, and existence from OperationSnapshot."),
            [snapshotRequirement]);

        return new OptimizationDefinition(
            id,
            "1.0.0",
            title,
            description,
            category,
            technicalCategory,
            risk,
            evidence,
            configurationEvidence,
            impact,
            performanceRelevance,
            automaticPresetSuitability,
            userPreferenceImpact,
            configurationMechanism,
            activationBoundary,
            verificationLevel,
            rollbackValidationLevel,
            windows10ValidationLevel,
            windows11ValidationLevel,
            impactAreas,
            sideEffects,
            evidenceReferences,
            presetEligibility,
            IsSecurityTradeoff,
            RequiresUserConfirmation,
            supportedWindows,
            CompatibilityRequirements ?? [],
            [],
            [],
            [],
            RequiresElevation,
            RequiresRestart,
            SupportsUndo: true,
            RestorePointRequirement.NotRequired,
            [snapshotRequirement],
            [operation],
            [new VerificationSpec(operation.OperationId, operation.VerificationStrategy)],
            [new RollbackSpec(operation.OperationId, operation.RollbackStrategy)],
            OperationFailurePolicy.AttemptRollback,
            new TimeoutPolicy(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)),
            new SourceMetadata("BuiltIn", CurrentCatalogVersion, evidenceReferences.FirstOrDefault(), null));
    }

    private static SnapshotRequirement RegistrySnapshot(string captureMethod, string dataClassification)
    {
        return new SnapshotRequirement(
            OperationResourceType.RegistryValue,
            SnapshotRequirementKind.Required,
            captureMethod,
            BlockIfUnavailable: true,
            dataClassification);
    }

    public static string ComputeCatalogContentHash(IReadOnlyList<OptimizationDefinition> definitions)
    {
        var builder = new StringBuilder();
        foreach (var definition in definitions.OrderBy(definition => definition.OptimizationId.Value, StringComparer.Ordinal))
        {
            builder.Append(definition.OptimizationId.Value).Append('|')
                .Append(definition.Version).Append('|')
                .Append(definition.Title).Append('|')
                .Append(definition.Description).Append('|')
                .Append(definition.Category).Append('|')
                .Append(definition.TechnicalCategory).Append('|')
                .Append(definition.RiskLevel).Append('|')
                .Append(definition.EvidenceLevel).Append('|')
                .Append(definition.ConfigurationEvidence).Append('|')
                .Append(definition.ExpectedImpact).Append('|')
                .Append(definition.PerformanceRelevance).Append('|')
                .Append(definition.AutomaticPresetSuitability).Append('|')
                .Append(definition.UserPreferenceImpact).Append('|')
                .Append(definition.ConfigurationMechanism).Append('|')
                .Append(definition.ActivationBoundary).Append('|')
                .Append(definition.VerificationLevel).Append('|')
                .Append(definition.RollbackValidationLevel).Append('|')
                .Append(definition.Windows10ValidationLevel).Append('|')
                .Append(definition.Windows11ValidationLevel).Append('|')
                .Append(definition.PresetEligibility).Append('|')
                .Append(definition.IsSecurityTradeoff).Append('|')
                .Append(definition.RequiresUserConfirmation).Append('|')
                .Append(definition.RequiresElevation).Append('|')
                .Append(definition.RequiresRestart).Append('|')
                .Append(definition.SupportsUndo).Append('|')
                .Append(definition.RestorePointRequirement).Append('|')
                .Append(definition.FailurePolicy).Append('|')
                .Append(definition.TimeoutPolicy.PlanTimeout.Ticks.ToString(global::System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(definition.TimeoutPolicy.OperationTimeout.Ticks.ToString(global::System.Globalization.CultureInfo.InvariantCulture)).Append('|');

            AppendWindows(builder, definition.SupportedWindows);
            AppendValues(builder, definition.ImpactAreas);
            AppendValues(builder, definition.SideEffects);
            AppendValues(builder, definition.EvidenceReferences);
            AppendRequirements(builder, definition.CompatibilityRequirements);
            AppendValues(builder, definition.RequiredCapabilities);
            AppendValues(builder, definition.Conflicts.Select(id => id.Value));
            AppendValues(builder, definition.Dependencies.Select(id => id.Value));
            AppendSnapshotRequirements(builder, definition.SnapshotRequirements);
            AppendVerificationSpecs(builder, definition.VerificationSpecs);
            AppendRollbackSpecs(builder, definition.RollbackSpecs);

            foreach (var operation in definition.OperationSpecs.OrderBy(operation => operation.OperationId.Value, StringComparer.Ordinal))
            {
                builder.Append(operation.OperationId.Value).Append('|')
                    .Append(operation.OperationType).Append('|')
                    .Append(operation.TimeoutPolicy.Timeout.Ticks.ToString(global::System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                    .Append(operation.RetryPolicy.RetryAllowed).Append('|')
                    .Append(operation.RetryPolicy.MaxAttempts.ToString(global::System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                    .Append(operation.RetryPolicy.Backoff.Ticks.ToString(global::System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                    .Append(operation.Idempotency).Append('|')
                    .Append(operation.Reversibility).Append('|')
                    .Append(operation.RebootBoundary).Append('|')
                    .Append(operation.FailurePolicy).Append('|')
                    .Append(operation.VerificationStrategy.Kind).Append('|')
                    .Append(operation.VerificationStrategy.Description).Append('|')
                    .Append(operation.RollbackStrategy.Kind).Append('|')
                    .Append(operation.RollbackStrategy.Description).Append('|');
                AppendValues(builder, operation.RetryPolicy.RetryableFailures.Select(failure => failure.ToString()));
                AppendSnapshotRequirements(builder, operation.SnapshotRequirements);
                if (operation.RegistryValue is { } registry)
                {
                    builder.Append(registry.Target.Hive).Append('\\')
                        .Append(registry.Target.KeyPath).Append('\\')
                        .Append(registry.Target.ValueName).Append('|')
                        .Append(registry.Target.View).Append('|')
                        .Append(registry.DesiredState.Exists).Append('|')
                        .Append(registry.DesiredState.ValueKind).Append('|')
                        .Append(registry.DesiredState.DWordValue?.ToString(global::System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                        .Append(registry.DesiredState.QWordValue?.ToString(global::System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                        .Append(registry.DesiredState.StringValue).Append('|')
                        .Append(registry.DesiredState.BinaryValue is null ? string.Empty : Convert.ToHexString(registry.DesiredState.BinaryValue)).Append('|');
                    AppendValues(builder, registry.DesiredState.MultiStringValue ?? []);
                }
            }
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static void AppendWindows(StringBuilder builder, SupportedWindowsRequirement requirement)
    {
        AppendValues(builder, requirement.CompatibilityStatuses.Select(status => status.ToString()));
        builder.Append(requirement.MinimumBuild?.ToString(global::System.Globalization.CultureInfo.InvariantCulture)).Append('|')
            .Append(requirement.MaximumBuild?.ToString(global::System.Globalization.CultureInfo.InvariantCulture)).Append('|')
            .Append(requirement.Architecture).Append('|');
    }

    private static void AppendRequirements(StringBuilder builder, IReadOnlyList<CompatibilityRequirement> requirements)
    {
        foreach (var requirement in requirements
                     .OrderBy(requirement => requirement.Key, StringComparer.Ordinal)
                     .ThenBy(requirement => requirement.ExpectedValue, StringComparer.Ordinal))
        {
            builder.Append(requirement.Key).Append('|')
                .Append(requirement.ExpectedValue).Append('|')
                .Append(requirement.Required).Append('|');
        }
    }

    private static void AppendSnapshotRequirements(StringBuilder builder, IReadOnlyList<SnapshotRequirement> requirements)
    {
        foreach (var requirement in requirements
                     .OrderBy(requirement => requirement.ResourceType)
                     .ThenBy(requirement => requirement.CaptureMethod, StringComparer.Ordinal))
        {
            builder.Append(requirement.ResourceType).Append('|')
                .Append(requirement.Requirement).Append('|')
                .Append(requirement.CaptureMethod).Append('|')
                .Append(requirement.BlockIfUnavailable).Append('|')
                .Append(requirement.DataClassification).Append('|');
        }
    }

    private static void AppendVerificationSpecs(StringBuilder builder, IReadOnlyList<VerificationSpec> specs)
    {
        foreach (var spec in specs.OrderBy(spec => spec.OperationId.Value, StringComparer.Ordinal))
        {
            builder.Append(spec.OperationId.Value).Append('|')
                .Append(spec.Strategy.Kind).Append('|')
                .Append(spec.Strategy.Description).Append('|');
        }
    }

    private static void AppendRollbackSpecs(StringBuilder builder, IReadOnlyList<RollbackSpec> specs)
    {
        foreach (var spec in specs.OrderBy(spec => spec.OperationId.Value, StringComparer.Ordinal))
        {
            builder.Append(spec.OperationId.Value).Append('|')
                .Append(spec.Strategy.Kind).Append('|')
                .Append(spec.Strategy.Description).Append('|');
        }
    }

    private static void AppendValues(StringBuilder builder, IEnumerable<string> values)
    {
        foreach (var value in values.OrderBy(value => value, StringComparer.Ordinal))
        {
            builder.Append(value).Append('|');
        }
    }
}
