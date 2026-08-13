using BorealBoost.Core.Optimization;

namespace BorealBoost.Optimization.Execution;

public static class OptimizationSessionStateMachine
{
    private static readonly IReadOnlyDictionary<OptimizationSessionState, OptimizationSessionState[]> AllowedTransitions =
        new Dictionary<OptimizationSessionState, OptimizationSessionState[]>
        {
            [OptimizationSessionState.Created] = [OptimizationSessionState.Planned, OptimizationSessionState.Cancelled, OptimizationSessionState.Failed],
            [OptimizationSessionState.Planned] = [OptimizationSessionState.PreflightPassed, OptimizationSessionState.Failed, OptimizationSessionState.Cancelled, OptimizationSessionState.Interrupted],
            [OptimizationSessionState.PreflightPassed] = [OptimizationSessionState.Snapshotting, OptimizationSessionState.Failed, OptimizationSessionState.Cancelled, OptimizationSessionState.Interrupted],
            [OptimizationSessionState.Snapshotting] = [OptimizationSessionState.Ready, OptimizationSessionState.Failed, OptimizationSessionState.RollbackPending, OptimizationSessionState.Interrupted],
            [OptimizationSessionState.Ready] = [OptimizationSessionState.Executing, OptimizationSessionState.Cancelled, OptimizationSessionState.Interrupted],
            [OptimizationSessionState.Executing] = [OptimizationSessionState.Verifying, OptimizationSessionState.RollbackPending, OptimizationSessionState.Failed, OptimizationSessionState.RebootPending, OptimizationSessionState.Interrupted, OptimizationSessionState.RecoveryRequired],
            [OptimizationSessionState.Verifying] = [OptimizationSessionState.Executing, OptimizationSessionState.Completed, OptimizationSessionState.CompletedWithWarnings, OptimizationSessionState.RollbackPending, OptimizationSessionState.Failed, OptimizationSessionState.Interrupted],
            [OptimizationSessionState.RollbackPending] = [OptimizationSessionState.RollingBack, OptimizationSessionState.ManualActionRequired],
            [OptimizationSessionState.RollingBack] = [OptimizationSessionState.RolledBack, OptimizationSessionState.RollbackFailed, OptimizationSessionState.ManualActionRequired, OptimizationSessionState.Interrupted],
            [OptimizationSessionState.Interrupted] = [OptimizationSessionState.RecoveryRequired],
            [OptimizationSessionState.RecoveryRequired] = [OptimizationSessionState.RollbackPending, OptimizationSessionState.ManualActionRequired],
            [OptimizationSessionState.RebootPending] = [OptimizationSessionState.RecoveryRequired, OptimizationSessionState.CompletedWithWarnings],
            [OptimizationSessionState.Completed] = [OptimizationSessionState.RollbackPending],
            [OptimizationSessionState.CompletedWithWarnings] = [OptimizationSessionState.RollbackPending],
            [OptimizationSessionState.Failed] = [OptimizationSessionState.RollbackPending, OptimizationSessionState.ManualActionRequired],
            [OptimizationSessionState.RolledBack] = [],
            [OptimizationSessionState.RollbackFailed] = [OptimizationSessionState.ManualActionRequired],
            [OptimizationSessionState.Cancelled] = [],
            [OptimizationSessionState.ManualActionRequired] = []
        };

    public static bool CanTransition(OptimizationSessionState from, OptimizationSessionState to)
    {
        return AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static OptimizationSession Transition(OptimizationSession session, OptimizationSessionState nextState)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.State == nextState)
        {
            return session;
        }

        if (!CanTransition(session.State, nextState))
        {
            throw new InvalidOperationException($"Invalid optimization session transition: {session.State} -> {nextState}.");
        }

        if (nextState is OptimizationSessionState.Completed or OptimizationSessionState.CompletedWithWarnings &&
            !AllOperationsVerified(session))
        {
            throw new InvalidOperationException("Optimization session cannot complete before every operation has verified result.");
        }

        if (nextState == OptimizationSessionState.RolledBack &&
            !AllOperationsRolledBack(session))
        {
            throw new InvalidOperationException("Optimization session cannot be marked rolled back before rollback verification is complete.");
        }

        return session with { State = nextState };
    }

    private static bool AllOperationsVerified(OptimizationSession session)
    {
        var expected = session.Plan.OrderedOperations.Select(operation => operation.OperationId).ToHashSet();
        var verified = session.VerificationResults
            .Where(result => result.Verified && result.Status == OperationExecutionStatus.Verified)
            .Select(result => result.OperationId)
            .ToHashSet();

        return expected.Count > 0 && expected.SetEquals(verified);
    }

    private static bool AllOperationsRolledBack(OptimizationSession session)
    {
        var expected = session.Journal
            .Where(entry => entry.State is OperationJournalState.ApplyCompleted or OperationJournalState.Verified)
            .Select(entry => entry.OperationId)
            .ToHashSet();
        if (expected.Count == 0)
        {
            expected = session.Plan.OrderedOperations.Select(operation => operation.OperationId).ToHashSet();
        }

        var rolledBack = session.RollbackResults
            .Where(result => result.RestoredOriginalState && result.Status == OperationExecutionStatus.RolledBack)
            .Select(result => result.OperationId)
            .ToHashSet();

        return expected.Count > 0 && expected.SetEquals(rolledBack);
    }
}
