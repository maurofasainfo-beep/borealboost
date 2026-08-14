namespace BorealBoost.Core.Optimization;

public enum OptimizationCategory
{
    System,
    Performance,
    Gaming,
    Windows,
    Drivers,
    Graphics,
    Storage,
    Power,
    Startup,
    Services,
    Visual,
    Network,
    Privacy,
    Security,
    Memory,
    Maintenance,
    IntegrationTest
}

public enum OptimizationRiskLevel
{
    Safe,
    Medium,
    Advanced,
    Aggressive
}

public enum OptimizationEvidenceLevel
{
    Strong,
    Moderate,
    Experimental,
    Unknown
}

public enum OptimizationTechnicalCategory
{
    Performance,
    Responsiveness,
    GamingPerformance,
    GamingFeaturePreference,
    Privacy,
    UXPreference,
    Security,
    Maintenance,
    SystemBehavior
}

public enum OptimizationPerformanceRelevance
{
    None,
    Low,
    Moderate,
    WorkloadDependent,
    Unknown
}

public enum AutomaticPresetSuitability
{
    Automatic,
    OptIn,
    CustomOnly,
    AdvancedOnly
}

public enum OptimizationUserPreferenceImpact
{
    None,
    Low,
    Medium,
    High
}

public enum ConfigurationMechanism
{
    Policy,
    Preference,
    ImplementationDetail
}

public enum ConfigurationEvidenceKind
{
    DocumentedSupportedMechanism,
    DocumentedPolicy,
    ObservedRegistryBehavior,
    CommunityKnown,
    Experimental
}

public enum ActivationBoundary
{
    Immediate,
    ExplorerRestart,
    ApplicationRestart,
    SignOut,
    PolicyRefresh,
    Reboot,
    Unknown
}

public enum OptimizationVerificationLevel
{
    StateVerified,
    BehaviorVerified,
    RequiresActivationBoundary,
    NotFullyBehaviorVerified
}

public enum RollbackValidationLevel
{
    HandlerValidated,
    OptimizationUnitValidated,
    OptimizationIntegrationValidated,
    OptimizationVMValidated,
    OptimizationHardwareValidated
}

public enum PlatformValidationLevel
{
    NotApplicable,
    SupportedByDesign,
    UnitTested,
    IntegrationValidated,
    VMValidated,
    HardwareValidated,
    UnvalidatedForRelease
}

public enum OperationType
{
    NoOp,
    BorealIntegrationRegistryValue,
    RegistryValue,
    ServiceState,
    PowerPlan,
    DnsConfiguration,
    WindowsFeature,
    FileSystem
}

public enum OperationIdempotency
{
    Idempotent,
    ConditionallyIdempotent,
    NonIdempotent
}

public enum OperationReversibility
{
    Full,
    Partial,
    None
}

public enum RebootBoundary
{
    None,
    AllowedAfterOperation,
    RequiredAfterOperation,
    RequiredBeforeContinue
}

public enum OperationFailurePolicy
{
    StopSession,
    ContinueIfIndependent,
    AttemptRollback,
    MarkManualActionRequired
}

public enum OperationVerificationKind
{
    ExactState,
    Manual,
    NotApplicable
}

public enum OperationRollbackKind
{
    SnapshotRestore,
    InverseOperation,
    ManualOnly,
    None
}

public enum SnapshotRequirementKind
{
    Required,
    Optional,
    NotRequired
}

public enum RegistryHiveKind
{
    CurrentUser,
    LocalMachine
}

public enum RegistryViewKind
{
    Default,
    Registry32,
    Registry64
}

public enum RegistryValueDataKind
{
    Unsupported,
    String,
    ExpandString,
    DWord,
    QWord,
    MultiString,
    Binary
}

public enum OperationResourceType
{
    RegistryValue,
    Service,
    PowerPlan,
    File,
    Unknown
}

public enum RestorePointRequirement
{
    NotRequired,
    BestEffort,
    Required
}

public enum RestorePointStatus
{
    Created,
    RecentRestorePointAvailable,
    Unavailable,
    Disabled,
    Failed,
    NotRequired,
    Unknown
}

public enum ExecutionPlanValidationStatus
{
    Valid,
    Invalid,
    Stale,
    NeedsRevalidation,
    Blocked
}

public enum OptimizationSessionState
{
    Created,
    Planned,
    PreflightPassed,
    Snapshotting,
    Ready,
    Executing,
    Verifying,
    Completed,
    CompletedWithWarnings,
    Failed,
    RollbackPending,
    RollingBack,
    RolledBack,
    RollbackFailed,
    Cancelled,
    Interrupted,
    RecoveryRequired,
    RebootPending,
    ManualActionRequired
}

public enum OperationJournalState
{
    Planned,
    PreflightPassed,
    SnapshotCaptured,
    ApplyStarted,
    ApplyCompleted,
    VerificationPending,
    Verified,
    Failed,
    RollbackStarted,
    RollbackVerified,
    RollbackFailed,
    AlreadySatisfied,
    Skipped,
    UnknownAfterCrash
}

public enum OperationExecutionStatus
{
    AlreadySatisfied,
    Applied,
    Verified,
    Failed,
    FailedVerification,
    RolledBack,
    RollbackFailed,
    OutcomeUnknown,
    Skipped
}

public enum OperationErrorCategory
{
    None,
    AccessDenied,
    NotFound,
    Unsupported,
    ValidationFailed,
    SnapshotFailed,
    ApplyFailed,
    VerificationFailed,
    RollbackFailed,
    Timeout,
    OutcomeUnknown,
    ProtocolRejected,
    RecoveryRequired,
    PersistenceFailed
}

public enum RecoveryActionKind
{
    Inspect,
    Verify,
    Resume,
    Rollback,
    ManualRecovery
}

public enum OptimizationPresetSelectionStatus
{
    Selected,
    Excluded,
    Blocked,
    NotApplicable,
    RequiresConfirmation
}

public sealed record OptimizationIssue(
    string Code,
    string Message,
    string Scope,
    OperationErrorCategory Category = OperationErrorCategory.ValidationFailed);
