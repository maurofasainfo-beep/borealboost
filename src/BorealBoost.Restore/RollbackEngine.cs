using BorealBoost.Core.Common;
using BorealBoost.Core.Optimization;

namespace BorealBoost.Restore;

public sealed class RollbackEngine : IRollbackEngine
{
    private readonly IOperationHandlerRegistry _handlerRegistry;

    public RollbackEngine(IOperationHandlerRegistry handlerRegistry)
    {
        _handlerRegistry = handlerRegistry;
    }

    public async Task<Result<OptimizationSession>> RollbackAsync(OptimizationSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        if (session.Snapshot is null)
        {
            return Result<OptimizationSession>.Failure("rollback.snapshot_missing", "Rollback requires a trusted OperationSnapshot.");
        }

        var rollbackResults = new List<OperationRollbackResult>(session.RollbackResults);
        var workingSession = session with { State = OptimizationSessionState.RollingBack };

        foreach (var operation in session.Plan.OrderedOperations.Reverse())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_handlerRegistry.TryGetHandler(operation.OperationType, out var handler))
            {
                return Result<OptimizationSession>.Failure("rollback.handler_missing", $"No rollback handler exists for OperationType '{operation.OperationType}'.");
            }

            var snapshotItem = session.Snapshot.Items.SingleOrDefault(item => item.OperationId == operation.OperationId);
            if (snapshotItem is null)
            {
                return Result<OptimizationSession>.Failure("rollback.snapshot_item_missing", $"Snapshot item for '{operation.OperationId}' is missing.");
            }

            var rollback = await handler.RollbackAsync(operation, snapshotItem, cancellationToken).ConfigureAwait(false);
            if (rollback.IsFailure || rollback.Value is null || !rollback.Value.RestoredOriginalState)
            {
                var failed = rollback.Value ?? new OperationRollbackResult(
                    operation.OperationId,
                    OperationExecutionStatus.RollbackFailed,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    TimeSpan.Zero,
                    RestoredOriginalState: false,
                    OperationErrorCategory.RollbackFailed,
                    rollback.ErrorMessage ?? "Rollback failed.");

                rollbackResults.Add(failed);
                return Result<OptimizationSession>.Failure("rollback.failed", failed.SafeMessage);
            }

            rollbackResults.Add(rollback.Value);
        }

        return Result<OptimizationSession>.Success(workingSession with
        {
            State = OptimizationSessionState.RolledBack,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            RollbackResults = rollbackResults.ToArray()
        });
    }
}
