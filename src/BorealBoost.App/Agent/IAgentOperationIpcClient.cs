using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Common;
using BorealBoost.Core.Optimization;

namespace BorealBoost.App.Agent;

public interface IAgentOperationIpcClient
{
    Task<Result<ValidateOperationResponsePayload>> ValidateOperationAsync(
        OptimizationId optimizationId,
        OperationSpec operation,
        CancellationToken cancellationToken);

    Task<Result<CaptureSnapshotResponsePayload>> CaptureSnapshotAsync(
        OptimizationId optimizationId,
        OperationSpec operation,
        CancellationToken cancellationToken);

    Task<Result<ExecuteOperationResponsePayload>> ExecuteOperationAsync(
        OptimizationId optimizationId,
        OperationSpec operation,
        OperationSnapshotItem snapshotItem,
        CancellationToken cancellationToken);

    Task<Result<VerifyOperationResponsePayload>> VerifyOperationAsync(
        OptimizationId optimizationId,
        OperationSpec operation,
        CancellationToken cancellationToken);

    Task<Result<RollbackOperationResponsePayload>> RollbackOperationAsync(
        OptimizationId optimizationId,
        OperationSpec operation,
        OperationSnapshotItem snapshotItem,
        CancellationToken cancellationToken);
}
