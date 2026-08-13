using BorealBoost.Core.Common;
using BorealBoost.Core.Optimization;
using BorealBoost.Optimization.Catalog;

namespace BorealBoost.App.Agent;

public sealed class AgentOperationHandler : IOperationHandler
{
    private readonly IAgentOperationIpcClient _agentClient;
    private readonly AgentOperationSecurityValidator _localValidator = new();

    public AgentOperationHandler(IAgentOperationIpcClient agentClient)
    {
        _agentClient = agentClient;
    }

    public OperationType OperationType => OperationType.BorealIntegrationRegistryValue;

    public Result Validate(OperationSpec operation)
    {
        return _localValidator.Validate(operation);
    }

    public async Task<Result<OperationSnapshotItem>> CaptureSnapshotAsync(OperationSpec operation, CancellationToken cancellationToken)
    {
        var response = await _agentClient.CaptureSnapshotAsync(BuiltInOptimizationCatalog.IntegrationProofOptimizationId, operation, cancellationToken).ConfigureAwait(false);
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
        var response = await _agentClient.ExecuteOperationAsync(BuiltInOptimizationCatalog.IntegrationProofOptimizationId, operation, snapshot, cancellationToken).ConfigureAwait(false);
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
        var response = await _agentClient.VerifyOperationAsync(BuiltInOptimizationCatalog.IntegrationProofOptimizationId, operation, cancellationToken).ConfigureAwait(false);
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
        var response = await _agentClient.RollbackOperationAsync(BuiltInOptimizationCatalog.IntegrationProofOptimizationId, operation, snapshot, cancellationToken).ConfigureAwait(false);
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
}
