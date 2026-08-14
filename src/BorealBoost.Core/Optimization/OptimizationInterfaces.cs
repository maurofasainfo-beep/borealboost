using BorealBoost.Core.Analysis;
using BorealBoost.Core.Common;
using BorealBoost.Core.Identity;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Core.Optimization;

public interface IOptimizationCatalog
{
    string SchemaVersion { get; }

    string CatalogVersion { get; }

    CatalogManifestMetadata Manifest { get; }

    IReadOnlyList<OptimizationDefinition> GetDefinitions();

    OptimizationDefinition? Find(OptimizationId optimizationId);
}

public interface IOptimizationDefinitionValidator
{
    IReadOnlyList<OptimizationIssue> Validate(OptimizationDefinition definition);
}

public interface IExecutionPlanner
{
    Result<ExecutionPlan> CreatePlan(
        SystemSnapshot snapshot,
        AnalysisResult analysis,
        RecommendationPlan recommendationPlan,
        IReadOnlyList<OptimizationId> selectedOptimizationIds);
}

public interface IExecutionPlanValidator
{
    ExecutionPlanValidationResult Validate(ExecutionPlan plan, SystemSnapshot snapshot);
}

public interface IOptimizationPresetEngine
{
    OptimizationPresetSelection Preview(
        SystemSnapshot snapshot,
        AnalysisResult analysis,
        RecommendationPreset preset);
}

public interface IDryRunService
{
    Task<Result<DryRunResult>> DryRunAsync(
        SystemSnapshot snapshot,
        AnalysisResult analysis,
        IReadOnlyList<OptimizationId> selectedOptimizationIds,
        CancellationToken cancellationToken);
}

public interface IPreflightService
{
    Task<PreflightResult> CheckAsync(ExecutionPlan plan, SystemSnapshot snapshot, CancellationToken cancellationToken);
}

public interface IOptimizationSessionStore
{
    Task<Result> SaveAsync(OptimizationSession session, CancellationToken cancellationToken);

    Task<Result<OptimizationSession>> LoadAsync(SessionId sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OptimizationSession>> ListAsync(CancellationToken cancellationToken);
}

public interface IOptimizationSessionArtifactStore
{
    Task<IReadOnlyList<OptimizationSessionArtifact>> ListArtifactsAsync(CancellationToken cancellationToken);
}

public interface IOperationHandler
{
    OperationType OperationType { get; }

    Result Validate(OperationSpec operation);

    Task<Result<OperationSnapshotItem>> CaptureSnapshotAsync(OperationSpec operation, CancellationToken cancellationToken);

    Task<Result<OperationExecutionResult>> ApplyAsync(OperationSpec operation, OperationSnapshotItem snapshot, CancellationToken cancellationToken);

    Task<Result<OperationVerificationResult>> VerifyAsync(OperationSpec operation, CancellationToken cancellationToken);

    Task<Result<OperationRollbackResult>> RollbackAsync(OperationSpec operation, OperationSnapshotItem snapshot, CancellationToken cancellationToken);
}

public interface IOperationHandlerRegistry
{
    bool TryGetHandler(OperationType operationType, out IOperationHandler handler);

    IReadOnlyList<OperationType> SupportedOperationTypes { get; }
}

public interface IRestorePointService
{
    Task<RestorePointResult> PrepareAsync(ExecutionPlan plan, CancellationToken cancellationToken);
}

public interface IRollbackEngine
{
    Task<Result<OptimizationSession>> RollbackAsync(OptimizationSession session, CancellationToken cancellationToken);
}

public interface IOptimizationSessionService
{
    OptimizationSessionState State { get; }

    OptimizationSession? Current { get; }

    Task<Result<OptimizationSession>> ExecuteAsync(ExecutionPlan plan, SystemSnapshot snapshot, CancellationToken cancellationToken);

    Task<Result<OptimizationSession>> RollbackAsync(SessionId sessionId, CancellationToken cancellationToken);
}

public interface IRecoveryService
{
    Task<IReadOnlyList<RecoveryCandidate>> DetectAsync(CancellationToken cancellationToken);
}
