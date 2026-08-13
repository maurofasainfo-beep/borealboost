using BorealBoost.Core.Optimization;

namespace BorealBoost.Optimization.Execution;

public sealed class RecoveryService : IRecoveryService
{
    private readonly IOptimizationSessionStore _store;

    public RecoveryService(IOptimizationSessionStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<RecoveryCandidate>> DetectAsync(CancellationToken cancellationToken)
    {
        if (_store is IOptimizationSessionArtifactStore artifactStore)
        {
            var artifacts = await artifactStore.ListArtifactsAsync(cancellationToken).ConfigureAwait(false);
            return artifacts
                .SelectMany(ToRecoveryCandidates)
                .OrderBy(candidate => candidate.SessionId.ToString(), StringComparer.Ordinal)
                .ThenBy(candidate => candidate.ArtifactId, StringComparer.Ordinal)
                .ToArray();
        }

        var sessions = await _store.ListAsync(cancellationToken).ConfigureAwait(false);
        return sessions
            .Where(IsIncomplete)
            .Select(session => new RecoveryCandidate(
                session.SessionId,
                session.Plan.PlanId,
                session.State is OptimizationSessionState.Completed ? OptimizationSessionState.RecoveryRequired : session.State,
                SuggestedAction(session),
                Reason(session)))
            .OrderBy(candidate => candidate.SessionId.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<RecoveryCandidate> ToRecoveryCandidates(OptimizationSessionArtifact artifact)
    {
        if (!artifact.IsValid)
        {
            yield return new RecoveryCandidate(
                artifact.SessionId,
                artifact.PlanId ?? new ExecutionPlanId(Guid.Empty),
                OptimizationSessionState.ManualActionRequired,
                RecoveryActionKind.ManualRecovery,
                $"Invalid optimization artifact '{artifact.ArtifactId}': {artifact.ErrorCode ?? "unknown"} - {artifact.ErrorMessage ?? "manual recovery required"}.",
                IsInvalidArtifact: true,
                artifact.ArtifactId);
            yield break;
        }

        if (artifact.Session is not null && IsIncomplete(artifact.Session))
        {
            yield return new RecoveryCandidate(
                artifact.Session.SessionId,
                artifact.Session.Plan.PlanId,
                artifact.Session.State is OptimizationSessionState.Completed ? OptimizationSessionState.RecoveryRequired : artifact.Session.State,
                SuggestedAction(artifact.Session),
                Reason(artifact.Session),
                IsInvalidArtifact: false,
                artifact.ArtifactId);
        }
    }

    private static bool IsIncomplete(OptimizationSession session)
    {
        if (session.CompletedAtUtc is null)
        {
            return true;
        }

        return session.State is not OptimizationSessionState.Completed
            and not OptimizationSessionState.CompletedWithWarnings
            and not OptimizationSessionState.RolledBack
            and not OptimizationSessionState.Cancelled;
    }

    private static RecoveryActionKind SuggestedAction(OptimizationSession session)
    {
        return session.State switch
        {
            OptimizationSessionState.Executing => RecoveryActionKind.Verify,
            OptimizationSessionState.Verifying => RecoveryActionKind.Verify,
            OptimizationSessionState.RollbackPending => RecoveryActionKind.Rollback,
            OptimizationSessionState.RollingBack => RecoveryActionKind.ManualRecovery,
            OptimizationSessionState.RollbackFailed => RecoveryActionKind.ManualRecovery,
            OptimizationSessionState.RebootPending => RecoveryActionKind.Verify,
            _ => RecoveryActionKind.Inspect
        };
    }

    private static string Reason(OptimizationSession session)
    {
        if (session.CompletedAtUtc is null)
        {
            return "Session has no durable completion timestamp.";
        }

        return $"Session state '{session.State}' is not a final successful state.";
    }
}
