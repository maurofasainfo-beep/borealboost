using BorealBoost.Core.Analysis;
using BorealBoost.Core.Identity;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Core.Optimization;

public sealed record SupportedWindowsRequirement(
    IReadOnlyList<WindowsCompatibilityStatus> CompatibilityStatuses,
    int? MinimumBuild,
    int? MaximumBuild,
    string Architecture);

public sealed record CompatibilityRequirement(
    string Key,
    string ExpectedValue,
    bool Required);

public sealed record SourceMetadata(
    string CatalogSource,
    string CatalogVersion,
    string? DocumentationUri,
    string? ContentHash);

public sealed record CatalogManifestMetadata(
    string SchemaVersion,
    string CatalogVersion,
    string Publisher,
    string Source,
    string ContentHash,
    DateTimeOffset BuiltAtUtc);

public sealed record TimeoutPolicy(TimeSpan PlanTimeout, TimeSpan OperationTimeout);

public sealed record OperationTimeoutPolicy(TimeSpan Timeout);

public sealed record OperationRetryPolicy(
    bool RetryAllowed,
    int MaxAttempts,
    TimeSpan Backoff,
    IReadOnlyList<OperationErrorCategory> RetryableFailures);

public sealed record OperationVerificationStrategy(
    OperationVerificationKind Kind,
    string Description);

public sealed record OperationRollbackStrategy(
    OperationRollbackKind Kind,
    string Description);

public sealed record SnapshotRequirement(
    OperationResourceType ResourceType,
    SnapshotRequirementKind Requirement,
    string CaptureMethod,
    bool BlockIfUnavailable,
    string DataClassification);

public sealed record RegistryOperationTarget(
    RegistryHiveKind Hive,
    string KeyPath,
    string ValueName,
    RegistryViewKind View);

public sealed record RegistryValueState(
    bool Exists,
    RegistryValueDataKind ValueKind,
    string? StringValue,
    int? DWordValue,
    long? QWordValue = null,
    IReadOnlyList<string>? MultiStringValue = null,
    byte[]? BinaryValue = null);

public sealed record RegistryValueOperationParameters(
    RegistryOperationTarget Target,
    RegistryValueState DesiredState);

public sealed record OperationSpec(
    OperationId OperationId,
    OperationType OperationType,
    RegistryValueOperationParameters? RegistryValue,
    OperationTimeoutPolicy TimeoutPolicy,
    OperationRetryPolicy RetryPolicy,
    OperationIdempotency Idempotency,
    OperationReversibility Reversibility,
    RebootBoundary RebootBoundary,
    OperationFailurePolicy FailurePolicy,
    OperationVerificationStrategy VerificationStrategy,
    OperationRollbackStrategy RollbackStrategy,
    IReadOnlyList<SnapshotRequirement> SnapshotRequirements);

public sealed record VerificationSpec(
    OperationId OperationId,
    OperationVerificationStrategy Strategy);

public sealed record RollbackSpec(
    OperationId OperationId,
    OperationRollbackStrategy Strategy);

public sealed record OptimizationDefinition(
    OptimizationId OptimizationId,
    string Version,
    string Title,
    string Description,
    OptimizationCategory Category,
    OptimizationTechnicalCategory TechnicalCategory,
    OptimizationRiskLevel RiskLevel,
    OptimizationEvidenceLevel EvidenceLevel,
    ConfigurationEvidenceKind ConfigurationEvidence,
    ExpectedImpactLevel ExpectedImpact,
    OptimizationPerformanceRelevance PerformanceRelevance,
    AutomaticPresetSuitability AutomaticPresetSuitability,
    OptimizationUserPreferenceImpact UserPreferenceImpact,
    ConfigurationMechanism ConfigurationMechanism,
    ActivationBoundary ActivationBoundary,
    OptimizationVerificationLevel VerificationLevel,
    RollbackValidationLevel RollbackValidationLevel,
    PlatformValidationLevel Windows10ValidationLevel,
    PlatformValidationLevel Windows11ValidationLevel,
    IReadOnlyList<string> ImpactAreas,
    IReadOnlyList<string> SideEffects,
    IReadOnlyList<string> EvidenceReferences,
    RecommendationPresetEligibility PresetEligibility,
    bool IsSecurityTradeoff,
    bool RequiresUserConfirmation,
    SupportedWindowsRequirement SupportedWindows,
    IReadOnlyList<CompatibilityRequirement> CompatibilityRequirements,
    IReadOnlyList<string> RequiredCapabilities,
    IReadOnlyList<OptimizationId> Conflicts,
    IReadOnlyList<OptimizationId> Dependencies,
    bool RequiresElevation,
    bool RequiresRestart,
    bool SupportsUndo,
    RestorePointRequirement RestorePointRequirement,
    IReadOnlyList<SnapshotRequirement> SnapshotRequirements,
    IReadOnlyList<OperationSpec> OperationSpecs,
    IReadOnlyList<VerificationSpec> VerificationSpecs,
    IReadOnlyList<RollbackSpec> RollbackSpecs,
    OperationFailurePolicy FailurePolicy,
    TimeoutPolicy TimeoutPolicy,
    SourceMetadata SourceMetadata);

public sealed record OptimizationPresetSelectionItem(
    OptimizationId OptimizationId,
    string Title,
    OptimizationCategory Category,
    OptimizationTechnicalCategory TechnicalCategory,
    OptimizationRiskLevel RiskLevel,
    OptimizationEvidenceLevel EvidenceLevel,
    ConfigurationEvidenceKind ConfigurationEvidence,
    ExpectedImpactLevel ExpectedImpact,
    OptimizationPerformanceRelevance PerformanceRelevance,
    AutomaticPresetSuitability AutomaticPresetSuitability,
    OptimizationUserPreferenceImpact UserPreferenceImpact,
    ConfigurationMechanism ConfigurationMechanism,
    ActivationBoundary ActivationBoundary,
    OptimizationVerificationLevel VerificationLevel,
    RollbackValidationLevel RollbackValidationLevel,
    IReadOnlyList<string> ImpactAreas,
    RecommendationPresetEligibility PresetEligibility,
    OptimizationPresetSelectionStatus Status,
    string Reason,
    bool RequiresRestart,
    bool SupportsUndo,
    bool IsSecurityTradeoff);

public sealed record OptimizationPresetSelection(
    RecommendationPreset Preset,
    string CatalogVersion,
    IReadOnlyList<OptimizationPresetSelectionItem> Items)
{
    public IReadOnlyList<OptimizationPresetSelectionItem> SelectedItems =>
        Items.Where(item => item.Status == OptimizationPresetSelectionStatus.Selected).ToArray();

    public IReadOnlyList<OptimizationPresetSelectionItem> BlockedItems =>
        Items.Where(item => item.Status == OptimizationPresetSelectionStatus.Blocked).ToArray();

    public IReadOnlyList<OptimizationPresetSelectionItem> RequiresConfirmationItems =>
        Items.Where(item => item.Status == OptimizationPresetSelectionStatus.RequiresConfirmation).ToArray();
}

public sealed record PlanDependency(OptimizationId OptimizationId, OptimizationId DependsOn);

public sealed record PlanConflict(OptimizationId OptimizationId, OptimizationId ConflictsWith);

public sealed record RiskSummary(
    IReadOnlyDictionary<OptimizationRiskLevel, int> OperationCountByRisk,
    OptimizationRiskLevel HighestRisk);

public sealed record ExecutionPlan(
    ExecutionPlanId PlanId,
    SessionId SessionId,
    ScanId ScanId,
    AnalysisId AnalysisId,
    string SchemaVersion,
    string EngineVersion,
    string CatalogVersion,
    DateTimeOffset CreatedAtUtc,
    string? TargetOperatingSystem,
    int? TargetBuild,
    string TargetArchitecture,
    IReadOnlyList<OptimizationId> SelectedOptimizationIds,
    IReadOnlyList<OperationSpec> OrderedOperations,
    IReadOnlyList<PlanDependency> Dependencies,
    IReadOnlyList<PlanConflict> Conflicts,
    RiskSummary RiskSummary,
    bool RequiresElevation,
    bool RequiresRestart,
    RestorePointRequirement RestorePointRequirement,
    IReadOnlyList<SnapshotRequirement> SnapshotRequirements,
    IReadOnlyList<RebootBoundary> RebootBoundaries,
    int EstimatedStepCount,
    IReadOnlyList<OptimizationIssue> Warnings,
    IReadOnlyList<OptimizationIssue> Blockers,
    string PlanHash,
    bool IsApproved);

public sealed record ExecutionPlanValidationResult(
    ExecutionPlanValidationStatus Status,
    IReadOnlyList<OptimizationIssue> Issues)
{
    public bool CanExecute => Status == ExecutionPlanValidationStatus.Valid && Issues.Count == 0;
}

public sealed record DryRunOperation(
    OperationId OperationId,
    OperationType OperationType,
    string TargetSummary,
    bool WouldChange,
    bool SnapshotRequired,
    bool RequiresRestart,
    OperationReversibility Reversibility);

public sealed record DryRunResult(
    ExecutionPlan Plan,
    ExecutionPlanValidationResult Validation,
    IReadOnlyList<DryRunOperation> Operations,
    IReadOnlyList<OptimizationIssue> Warnings,
    IReadOnlyList<OptimizationIssue> Blockers);

public sealed record PreflightResult(
    ExecutionPlan Plan,
    bool Passed,
    DateTimeOffset CheckedAtUtc,
    IReadOnlyList<OptimizationIssue> Issues);

public sealed record OperationSnapshot(
    string SchemaVersion,
    SessionId SessionId,
    ExecutionPlanId PlanId,
    DateTimeOffset CapturedAtUtc,
    IReadOnlyList<OperationSnapshotItem> Items);

public sealed record OperationSnapshotItem(
    Guid SnapshotItemId,
    OperationId OperationId,
    OperationResourceType ResourceType,
    string ResourceIdentity,
    bool ExistedBefore,
    RegistryOperationTarget? RegistryTarget,
    RegistryValueDataKind? PreviousValueKind,
    string? PreviousStringValue,
    int? PreviousDWordValue,
    string CaptureMethod,
    DateTimeOffset CapturedAtUtc,
    OperationRollbackStrategy RestorationStrategy,
    IReadOnlyList<string> Limitations,
    string VerificationMetadata,
    long? PreviousQWordValue = null,
    IReadOnlyList<string>? PreviousMultiStringValue = null,
    byte[]? PreviousBinaryValue = null,
    string? SnapshotHash = null,
    bool? RegistryKeyExistedBefore = null);

public sealed record RestorePointResult(
    RestorePointStatus Status,
    string Message,
    DateTimeOffset CheckedAtUtc,
    string? RestorePointId = null);

public sealed record OperationExecutionResult(
    OperationId OperationId,
    OperationExecutionStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    TimeSpan Duration,
    bool ChangedState,
    bool RequiresRestart,
    OperationErrorCategory ErrorCategory,
    string SafeMessage);

public sealed record OperationVerificationResult(
    OperationId OperationId,
    OperationExecutionStatus Status,
    DateTimeOffset VerifiedAtUtc,
    bool Verified,
    string SafeMessage);

public sealed record OperationRollbackResult(
    OperationId OperationId,
    OperationExecutionStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    TimeSpan Duration,
    bool RestoredOriginalState,
    OperationErrorCategory ErrorCategory,
    string SafeMessage);

public sealed record OperationJournalEntry(
    OperationId OperationId,
    OperationJournalState State,
    DateTimeOffset TimestampUtc,
    string Action,
    OperationErrorCategory ErrorCategory,
    string SafeMessage);

public sealed record OptimizationFailure(
    OperationErrorCategory Category,
    string Code,
    string SafeMessage,
    OperationId? OperationId = null);

public sealed record OptimizationSession(
    string SchemaVersion,
    SessionId SessionId,
    ExecutionPlan Plan,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    OptimizationSessionState State,
    IReadOnlyList<OptimizationId> SelectedOptimizationIds,
    IReadOnlyList<OperationJournalEntry> Journal,
    OperationSnapshot? Snapshot,
    RestorePointResult? RestorePoint,
    IReadOnlyList<OperationVerificationResult> VerificationResults,
    IReadOnlyList<OperationRollbackResult> RollbackResults,
    bool RebootRequired,
    OptimizationFailure? Failure,
    string AppVersion,
    string EngineVersion);

public sealed record RecoveryCandidate(
    SessionId SessionId,
    ExecutionPlanId PlanId,
    OptimizationSessionState State,
    RecoveryActionKind SuggestedAction,
    string Reason,
    bool IsInvalidArtifact = false,
    string ArtifactId = "");

public sealed record OptimizationSessionArtifact(
    string ArtifactId,
    SessionId SessionId,
    ExecutionPlanId? PlanId,
    OptimizationSession? Session,
    bool IsValid,
    string? ErrorCode,
    string? ErrorMessage);
