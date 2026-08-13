using BorealBoost.Core.Optimization;

namespace BorealBoost.Restore;

public sealed class RestorePointService : IRestorePointService
{
    public Task<RestorePointResult> PrepareAsync(ExecutionPlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        cancellationToken.ThrowIfCancellationRequested();

        var result = plan.RestorePointRequirement switch
        {
            RestorePointRequirement.NotRequired => new RestorePointResult(
                RestorePointStatus.NotRequired,
                "Restore point is not required for this controlled Phase 4 operation.",
                DateTimeOffset.UtcNow),
            RestorePointRequirement.BestEffort => new RestorePointResult(
                RestorePointStatus.Unavailable,
                "Real restore point creation is deferred; OperationSnapshot remains mandatory.",
                DateTimeOffset.UtcNow),
            RestorePointRequirement.Required => new RestorePointResult(
                RestorePointStatus.Unavailable,
                "Required restore point creation is unavailable in the Phase 4 controlled implementation.",
                DateTimeOffset.UtcNow),
            _ => new RestorePointResult(
                RestorePointStatus.Unknown,
                "Restore point policy is unknown.",
                DateTimeOffset.UtcNow)
        };

        return Task.FromResult(result);
    }
}
