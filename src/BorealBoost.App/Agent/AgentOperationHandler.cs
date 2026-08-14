using BorealBoost.Core.Common;
using BorealBoost.Core.Optimization;
using BorealBoost.Optimization.Catalog;

namespace BorealBoost.App.Agent;

public sealed class AgentOperationHandler : IOperationHandler
{
    private readonly IAgentOperationIpcClient _agentClient;
    private readonly IOptimizationCatalog _catalog;
    private readonly AgentOperationSecurityValidator _localValidator = new();
    private readonly OperationType _operationType;

    public AgentOperationHandler(
        IAgentOperationIpcClient agentClient,
        IOptimizationCatalog catalog,
        OperationType operationType)
    {
        _agentClient = agentClient;
        _catalog = catalog;
        _operationType = operationType;
    }

    public OperationType OperationType => _operationType;

    public Result Validate(OperationSpec operation)
    {
        return _localValidator.Validate(operation);
    }

    public async Task<Result<OperationSnapshotItem>> CaptureSnapshotAsync(OperationSpec operation, CancellationToken cancellationToken)
    {
        var optimizationId = ResolveOptimizationId(operation);
        if (optimizationId is null)
        {
            return Result<OperationSnapshotItem>.Failure("agent.operation.optimization_not_found", "Operation is not present in the trusted catalog.");
        }

        var response = await _agentClient.CaptureSnapshotAsync(optimizationId.Value, operation, cancellationToken).ConfigureAwait(false);
        if (response.IsFailure || response.Value is null)
        {
            return Result<OperationSnapshotItem>.Failure(response.ErrorCode ?? "agent.snapshot.failed", response.ErrorMessage ?? "Agent snapshot request failed.");
        }

        if (!response.Value.Captured || response.Value.SnapshotItem is null)
        {
            return Result<OperationSnapshotItem>.Failure("agent.snapshot.rejected", FormatIssues(response.Value.Issues));
        }

        return Result<OperationSnapshotItem>.Success(response.Value.SnapshotItem);
    }

    public async Task<Result<OperationExecutionResult>> ApplyAsync(OperationSpec operation, OperationSnapshotItem snapshot, CancellationToken cancellationToken)
    {
        var optimizationId = ResolveOptimizationId(operation);
        if (optimizationId is null)
        {
            return Result<OperationExecutionResult>.Failure("agent.operation.optimization_not_found", "Operation is not present in the trusted catalog.");
        }

        var response = await _agentClient.ExecuteOperationAsync(optimizationId.Value, operation, snapshot, cancellationToken).ConfigureAwait(false);
        if (response.IsFailure || response.Value is null)
        {
            return Result<OperationExecutionResult>.Failure(response.ErrorCode ?? "agent.apply.failed", response.ErrorMessage ?? "Agent apply request failed.");
        }

        return response.Value.Result is not null
            ? Result<OperationExecutionResult>.Success(response.Value.Result)
            : Result<OperationExecutionResult>.Failure("agent.apply.rejected", FormatIssues(response.Value.Issues));
    }

    public async Task<Result<OperationVerificationResult>> VerifyAsync(OperationSpec operation, CancellationToken cancellationToken)
    {
        var optimizationId = ResolveOptimizationId(operation);
        if (optimizationId is null)
        {
            return Result<OperationVerificationResult>.Failure("agent.operation.optimization_not_found", "Operation is not present in the trusted catalog.");
        }

        var response = await _agentClient.VerifyOperationAsync(optimizationId.Value, operation, cancellationToken).ConfigureAwait(false);
        if (response.IsFailure || response.Value is null)
        {
            return Result<OperationVerificationResult>.Failure(response.ErrorCode ?? "agent.verify.failed", response.ErrorMessage ?? "Agent verify request failed.");
        }

        return response.Value.Result is not null
            ? Result<OperationVerificationResult>.Success(response.Value.Result)
            : Result<OperationVerificationResult>.Failure("agent.verify.rejected", FormatIssues(response.Value.Issues));
    }

    public async Task<Result<OperationRollbackResult>> RollbackAsync(OperationSpec operation, OperationSnapshotItem snapshot, CancellationToken cancellationToken)
    {
        var optimizationId = ResolveOptimizationId(operation);
        if (optimizationId is null)
        {
            return Result<OperationRollbackResult>.Failure("agent.operation.optimization_not_found", "Operation is not present in the trusted catalog.");
        }

        var response = await _agentClient.RollbackOperationAsync(optimizationId.Value, operation, snapshot, cancellationToken).ConfigureAwait(false);
        if (response.IsFailure || response.Value is null)
        {
            return Result<OperationRollbackResult>.Failure(response.ErrorCode ?? "agent.rollback.failed", response.ErrorMessage ?? "Agent rollback request failed.");
        }

        return response.Value.Result is not null
            ? Result<OperationRollbackResult>.Success(response.Value.Result)
            : Result<OperationRollbackResult>.Failure("agent.rollback.rejected", FormatIssues(response.Value.Issues));
    }

    private static string FormatIssues(IReadOnlyList<OptimizationIssue> issues)
    {
        return issues.Count == 0
            ? "Agent rejected the operation."
            : string.Join("; ", issues.Select(issue => $"{issue.Code}:{issue.Message}"));
    }

    private OptimizationId? ResolveOptimizationId(OperationSpec operation)
    {
        return _catalog.GetDefinitions()
            .Where(definition => definition.OperationSpecs.Any(candidate => candidate.OperationId == operation.OperationId))
            .Select(definition => definition.OptimizationId)
            .Cast<OptimizationId?>()
            .FirstOrDefault();
    }
}
