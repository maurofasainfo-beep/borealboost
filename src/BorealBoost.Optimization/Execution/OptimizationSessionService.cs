using System.Diagnostics;
using BorealBoost.Core.Common;
using BorealBoost.Core.Identity;
using BorealBoost.Core.Optimization;
using BorealBoost.Core.Scanner;
using BorealBoost.Optimization.Planning;
using Microsoft.Extensions.Logging;

namespace BorealBoost.Optimization.Execution;

public sealed class OptimizationSessionService : IOptimizationSessionService
{
    private readonly object _syncRoot = new();
    private readonly IPreflightService _preflightService;
    private readonly IOperationHandlerRegistry _handlerRegistry;
    private readonly IOptimizationSessionStore _store;
    private readonly IRestorePointService _restorePointService;
    private readonly ILogger<OptimizationSessionService> _logger;
    private readonly IOptimizationSessionLock _sessionLock;
    private OptimizationSessionState _state = OptimizationSessionState.Created;
    private OptimizationSession? _current;

    public OptimizationSessionService(
        IPreflightService preflightService,
        IOperationHandlerRegistry handlerRegistry,
        IOptimizationSessionStore store,
        IRestorePointService restorePointService,
        ILogger<OptimizationSessionService> logger)
        : this(preflightService, handlerRegistry, store, restorePointService, logger, new CrossProcessOptimizationSessionLock())
    {
    }

    public OptimizationSessionService(
        IPreflightService preflightService,
        IOperationHandlerRegistry handlerRegistry,
        IOptimizationSessionStore store,
        IRestorePointService restorePointService,
        ILogger<OptimizationSessionService> logger,
        IOptimizationSessionLock sessionLock)
    {
        _preflightService = preflightService;
        _handlerRegistry = handlerRegistry;
        _store = store;
        _restorePointService = restorePointService;
        _logger = logger;
        _sessionLock = sessionLock;
    }

    public OptimizationSessionState State
    {
        get
        {
            lock (_syncRoot)
            {
                return _state;
            }
        }
    }

    public OptimizationSession? Current
    {
        get
        {
            lock (_syncRoot)
            {
                return _current;
            }
        }
    }

    public async Task<Result<OptimizationSession>> ExecuteAsync(ExecutionPlan plan, SystemSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(snapshot);

        Result<IAsyncDisposable> lockResult;
        try
        {
            lockResult = await _sessionLock.TryAcquireAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Result<OptimizationSession>.Failure("optimization.cancelled", "Optimization session was cancelled.");
        }

        if (lockResult.IsFailure || lockResult.Value is null)
        {
            return Result<OptimizationSession>.Failure(lockResult.ErrorCode ?? "optimization.session.already_running", lockResult.ErrorMessage ?? "Another optimization session is already running.");
        }

        await using var sessionLease = lockResult.Value;
        try
        {
            var approvedPlan = plan with { IsApproved = true };
            var session = CreateSession(approvedPlan);
            SetCurrent(session);
            await PersistAsync(session, cancellationToken).ConfigureAwait(false);

            var preflight = await _preflightService.CheckAsync(approvedPlan, snapshot, cancellationToken).ConfigureAwait(false);
            if (!preflight.Passed)
            {
                session = Fail(session, "optimization.preflight.failed", "Preflight failed.", OperationErrorCategory.ValidationFailed, null);
                await PersistAndReturnAsync(session, cancellationToken).ConfigureAwait(false);
                return Result<OptimizationSession>.Failure("optimization.preflight.failed", "Preflight failed.");
            }

                session = AppendJournal(
                    Transition(session, OptimizationSessionState.PreflightPassed),
                    OperationIdOrNone(),
                    OperationJournalState.PreflightPassed,
                    "PreflightPassed",
                    OperationErrorCategory.None,
                    "Preflight passed.");
            await PersistAsync(session, cancellationToken).ConfigureAwait(false);

            session = Transition(session, OptimizationSessionState.Snapshotting);
            await PersistAsync(session, cancellationToken).ConfigureAwait(false);

            var snapshotItems = new List<OperationSnapshotItem>();
            foreach (var operation in approvedPlan.OrderedOperations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var handler = GetHandlerOrThrow(operation);
                var capture = await handler.CaptureSnapshotAsync(operation, cancellationToken).ConfigureAwait(false);
                if (capture.IsFailure || capture.Value is null)
                {
                    session = Fail(session, "optimization.snapshot.failed", capture.ErrorMessage ?? "Snapshot capture failed.", OperationErrorCategory.SnapshotFailed, operation.OperationId);
                    await PersistAsync(session, cancellationToken).ConfigureAwait(false);
                    return Result<OptimizationSession>.Failure("optimization.snapshot.failed", capture.ErrorMessage ?? "Snapshot capture failed.");
                }

                snapshotItems.Add(capture.Value);
                session = session with
                {
                    Snapshot = new OperationSnapshot(
                        ExecutionPlanner.PlanSchemaVersion,
                        approvedPlan.SessionId,
                        approvedPlan.PlanId,
                        DateTimeOffset.UtcNow,
                        snapshotItems.ToArray())
                };
                session = AppendJournal(session, operation.OperationId, OperationJournalState.SnapshotCaptured, "SnapshotCaptured", OperationErrorCategory.None, "Snapshot captured before mutation.");
                await PersistAsync(session, cancellationToken).ConfigureAwait(false);
            }

            var restorePoint = await _restorePointService.PrepareAsync(approvedPlan, cancellationToken).ConfigureAwait(false);
            session = session with { RestorePoint = restorePoint };
            if (approvedPlan.RestorePointRequirement == RestorePointRequirement.Required && restorePoint.Status is not RestorePointStatus.Created and not RestorePointStatus.RecentRestorePointAvailable)
            {
                session = Fail(session, "optimization.restore_point.failed", restorePoint.Message, OperationErrorCategory.ValidationFailed, null);
                await PersistAsync(session, cancellationToken).ConfigureAwait(false);
                return Result<OptimizationSession>.Failure("optimization.restore_point.failed", restorePoint.Message);
            }

            session = Transition(session, OptimizationSessionState.Ready);
            await PersistAsync(session, cancellationToken).ConfigureAwait(false);

            var verificationResults = new List<OperationVerificationResult>();
            foreach (var operation in approvedPlan.OrderedOperations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var handler = GetHandlerOrThrow(operation);
                var item = session.Snapshot?.Items.Single(snapshotItem => snapshotItem.OperationId == operation.OperationId)
                           ?? throw new InvalidOperationException("Operation snapshot missing after capture.");

                session = AppendJournal(
                    Transition(session, OptimizationSessionState.Executing),
                    operation.OperationId,
                    OperationJournalState.ApplyStarted,
                    "ApplyStarted",
                    OperationErrorCategory.None,
                    "Apply started after durable snapshot.");
                await PersistAsync(session, cancellationToken).ConfigureAwait(false);

                var apply = await ExecuteWithRetryAsync(handler, operation, item).ConfigureAwait(false);
                if (apply.IsFailure || apply.Value is null || apply.Value.Status is OperationExecutionStatus.Failed or OperationExecutionStatus.OutcomeUnknown)
                {
                    session = AppendJournal(session, operation.OperationId, OperationJournalState.Failed, "ApplyFailed", apply.Value?.ErrorCategory ?? OperationErrorCategory.ApplyFailed, apply.ErrorMessage ?? apply.Value?.SafeMessage ?? "Apply failed.");
                    if (apply.Value?.Status == OperationExecutionStatus.OutcomeUnknown)
                    {
                        session = MarkRecoveryRequired(session, "optimization.apply.outcome_unknown", apply.Value.SafeMessage, operation.OperationId);
                        await PersistAndReturnAsync(session, CancellationToken.None).ConfigureAwait(false);
                        return Result<OptimizationSession>.Failure("optimization.apply.outcome_unknown", apply.Value.SafeMessage);
                    }

                    session = await RollbackAfterFailureAsync(session, CancellationToken.None).ConfigureAwait(false);
                    return Result<OptimizationSession>.Failure("optimization.apply.failed", apply.ErrorMessage ?? apply.Value?.SafeMessage ?? "Apply failed.");
                }

                session = AppendJournal(session, operation.OperationId, OperationJournalState.ApplyCompleted, "ApplyCompleted", OperationErrorCategory.None, apply.Value.SafeMessage);
                await PersistAsync(session, CancellationToken.None).ConfigureAwait(false);

                session = AppendJournal(
                    Transition(session, OptimizationSessionState.Verifying),
                    operation.OperationId,
                    OperationJournalState.VerificationPending,
                    "VerificationPending",
                    OperationErrorCategory.None,
                    "Verification started.");
                await PersistAsync(session, CancellationToken.None).ConfigureAwait(false);

                var verification = await handler.VerifyAsync(operation, CancellationToken.None).ConfigureAwait(false);
                if (verification.IsFailure || verification.Value is null || !verification.Value.Verified)
                {
                    var failure = verification.Value ?? new OperationVerificationResult(operation.OperationId, OperationExecutionStatus.FailedVerification, DateTimeOffset.UtcNow, false, verification.ErrorMessage ?? "Verification failed.");
                    verificationResults.Add(failure);
                    session = session with { VerificationResults = verificationResults.ToArray() };
                    session = AppendJournal(session, operation.OperationId, OperationJournalState.Failed, "VerificationFailed", OperationErrorCategory.VerificationFailed, failure.SafeMessage);
                    session = await RollbackAfterFailureAsync(session, CancellationToken.None).ConfigureAwait(false);
                    return Result<OptimizationSession>.Failure("optimization.verify.failed", failure.SafeMessage);
                }

                verificationResults.Add(verification.Value);
                session = session with { VerificationResults = verificationResults.ToArray(), RebootRequired = session.RebootRequired || apply.Value.RequiresRestart };
                session = AppendJournal(session, operation.OperationId, OperationJournalState.Verified, "Verified", OperationErrorCategory.None, verification.Value.SafeMessage);
                await PersistAsync(session, CancellationToken.None).ConfigureAwait(false);
            }

            var finalState = session.RebootRequired ? OptimizationSessionState.CompletedWithWarnings : OptimizationSessionState.Completed;
            session = Transition(session, finalState) with { CompletedAtUtc = DateTimeOffset.UtcNow };
            await PersistAndReturnAsync(session, CancellationToken.None).ConfigureAwait(false);
            _logger.LogInformation(
                "Optimization session completed. SessionId={SessionId}; PlanId={PlanId}; State={State}; OperationCount={OperationCount}",
                session.SessionId,
                session.Plan.PlanId,
                session.State,
                session.Plan.OrderedOperations.Count);
            return Result<OptimizationSession>.Success(session);
        }
        catch (OperationCanceledException)
        {
            var current = Current;
            if (current is not null && current.State is not OptimizationSessionState.Completed and not OptimizationSessionState.RolledBack)
            {
                var cancelled = CancellationIsSafe(current)
                    ? current with
                    {
                        State = OptimizationSessionState.Cancelled,
                        CompletedAtUtc = DateTimeOffset.UtcNow,
                        Failure = new OptimizationFailure(OperationErrorCategory.None, "optimization.cancelled", "Optimization session was cancelled.")
                    }
                    : MarkRecoveryRequired(current, "optimization.cancelled.outcome_unknown", "Cancellation occurred after a mutation boundary; recovery verification is required.", null);
                await PersistAndReturnAsync(cancelled, CancellationToken.None).ConfigureAwait(false);
            }

            return Result<OptimizationSession>.Failure("optimization.cancelled", "Optimization session was cancelled.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            _logger.LogError(exception, "Optimization session failed unexpectedly. PlanId={PlanId}", plan.PlanId);
            var current = Current;
            if (current is not null)
            {
                var failed = Fail(current, "optimization.failed", "Optimization session failed unexpectedly.", OperationErrorCategory.OutcomeUnknown, null);
                await PersistAndReturnAsync(failed, CancellationToken.None).ConfigureAwait(false);
            }

            return Result<OptimizationSession>.Failure("optimization.failed", "Optimization session failed unexpectedly.");
        }
    }

    public async Task<Result<OptimizationSession>> RollbackAsync(SessionId sessionId, CancellationToken cancellationToken)
    {
        Result<IAsyncDisposable> lockResult;
        try
        {
            lockResult = await _sessionLock.TryAcquireAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Result<OptimizationSession>.Failure("optimization.rollback.cancelled", "Rollback was cancelled.");
        }

        if (lockResult.IsFailure || lockResult.Value is null)
        {
            return Result<OptimizationSession>.Failure(lockResult.ErrorCode ?? "optimization.session.already_running", lockResult.ErrorMessage ?? "Another optimization session is already running.");
        }

        await using var sessionLease = lockResult.Value;
        try
        {
            var load = await _store.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (load.IsFailure || load.Value is null)
            {
                return Result<OptimizationSession>.Failure(load.ErrorCode ?? "optimization.session.load_failed", load.ErrorMessage ?? "Optimization session could not be loaded.");
            }

            var session = load.Value;
            var snapshotValidation = ValidateSessionSnapshot(session);
            if (snapshotValidation.IsFailure)
            {
                return Result<OptimizationSession>.Failure(snapshotValidation.ErrorCode ?? "optimization.rollback.snapshot_invalid", snapshotValidation.ErrorMessage ?? "Rollback snapshot is invalid.");
            }

            if (session.Snapshot is null || session.Snapshot.Items.Count == 0)
            {
                return Result<OptimizationSession>.Failure("optimization.rollback.snapshot_missing", "Rollback requires a trusted operation snapshot.");
            }

            session = session.State is OptimizationSessionState.RollbackPending
                ? session
                : Transition(session, OptimizationSessionState.RollbackPending);
            session = Transition(session, OptimizationSessionState.RollingBack);
            SetCurrent(session);
            await PersistAsync(session, cancellationToken).ConfigureAwait(false);

            var snapshot = session.Snapshot;
            if (snapshot is null)
            {
                return Result<OptimizationSession>.Failure("optimization.rollback.snapshot_missing", "Rollback requires a trusted operation snapshot.");
            }

            var rollbackResults = new List<OperationRollbackResult>(session.RollbackResults);
            foreach (var operation in session.Plan.OrderedOperations.Reverse())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var snapshotItem = snapshot.Items.SingleOrDefault(item => item.OperationId == operation.OperationId);
                if (snapshotItem is null)
                {
                    session = Fail(session, "optimization.rollback.snapshot_item_missing", "Rollback snapshot item is missing.", OperationErrorCategory.RollbackFailed, operation.OperationId);
                    await PersistAndReturnAsync(session, cancellationToken).ConfigureAwait(false);
                    return Result<OptimizationSession>.Failure("optimization.rollback.snapshot_item_missing", "Rollback snapshot item is missing.");
                }

                var handler = GetHandlerOrThrow(operation);
                session = AppendJournal(session, operation.OperationId, OperationJournalState.RollbackStarted, "RollbackStarted", OperationErrorCategory.None, "Rollback started.");
                await PersistAsync(session, CancellationToken.None).ConfigureAwait(false);

                var result = await handler.RollbackAsync(operation, snapshotItem, CancellationToken.None).ConfigureAwait(false);
                if (result.IsFailure || result.Value is null || !result.Value.RestoredOriginalState)
                {
                    var failed = result.Value ?? new OperationRollbackResult(operation.OperationId, OperationExecutionStatus.RollbackFailed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TimeSpan.Zero, false, OperationErrorCategory.RollbackFailed, result.ErrorMessage ?? "Rollback failed.");
                    rollbackResults.Add(failed);
                    session = session with { RollbackResults = rollbackResults.ToArray(), State = OptimizationSessionState.RollbackFailed };
                    session = AppendJournal(session, operation.OperationId, OperationJournalState.RollbackFailed, "RollbackFailed", failed.ErrorCategory, failed.SafeMessage);
                    await PersistAndReturnAsync(session, CancellationToken.None).ConfigureAwait(false);
                    return Result<OptimizationSession>.Failure("optimization.rollback.failed", failed.SafeMessage);
                }

                rollbackResults.Add(result.Value);
                session = session with { RollbackResults = rollbackResults.ToArray() };
                session = AppendJournal(session, operation.OperationId, OperationJournalState.RollbackVerified, "RollbackVerified", OperationErrorCategory.None, result.Value.SafeMessage);
                await PersistAsync(session, CancellationToken.None).ConfigureAwait(false);
            }

            session = Transition(session, OptimizationSessionState.RolledBack) with { CompletedAtUtc = DateTimeOffset.UtcNow };
            await PersistAndReturnAsync(session, CancellationToken.None).ConfigureAwait(false);
            return Result<OptimizationSession>.Success(session);
        }
        catch (OperationCanceledException)
        {
            var current = Current;
            if (current is not null)
            {
                var recovery = MarkRollbackRecoveryRequired(current, "optimization.rollback.cancelled", "Rollback was cancelled at an unsafe boundary; manual recovery is required.", null);
                await PersistAndReturnAsync(recovery, CancellationToken.None).ConfigureAwait(false);
            }

            return Result<OptimizationSession>.Failure("optimization.rollback.cancelled", "Rollback was cancelled.");
        }
    }

    private static OptimizationSession CreateSession(ExecutionPlan plan)
    {
        var now = DateTimeOffset.UtcNow;
        return new OptimizationSession(
            ExecutionPlanner.PlanSchemaVersion,
            plan.SessionId,
            plan,
            now,
            null,
            OptimizationSessionState.Planned,
            plan.SelectedOptimizationIds,
            [new OperationJournalEntry(OperationIdOrNone(), OperationJournalState.Planned, now, "Planned", OperationErrorCategory.None, "ExecutionPlan persisted before mutation.")],
            null,
            null,
            [],
            [],
            RebootRequired: false,
            null,
            "BorealBoost",
            ExecutionPlanner.EngineVersion);
    }

    private async Task<Result<OperationExecutionResult>> ExecuteWithRetryAsync(
        IOperationHandler handler,
        OperationSpec operation,
        OperationSnapshotItem snapshot)
    {
        var attempts = operation.RetryPolicy.RetryAllowed ? operation.RetryPolicy.MaxAttempts : 1;
        Result<OperationExecutionResult>? last = null;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            using var timeout = new CancellationTokenSource();
            timeout.CancelAfter(operation.TimeoutPolicy.Timeout);
            try
            {
                last = await handler.ApplyAsync(operation, snapshot, timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                last = Result<OperationExecutionResult>.Success(new OperationExecutionResult(
                    operation.OperationId,
                    OperationExecutionStatus.OutcomeUnknown,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    operation.TimeoutPolicy.Timeout,
                    ChangedState: false,
                    RequiresRestart: false,
                    OperationErrorCategory.Timeout,
                    "Operation timed out; outcome is unknown."));
            }

            if (last.IsSuccess && last.Value is { Status: not OperationExecutionStatus.Failed and not OperationExecutionStatus.OutcomeUnknown })
            {
                return last;
            }

            var category = last.Value?.ErrorCategory ?? OperationErrorCategory.ApplyFailed;
            if (!operation.RetryPolicy.RetryableFailures.Contains(category) || attempt == attempts)
            {
                break;
            }

            await Task.Delay(operation.RetryPolicy.Backoff, CancellationToken.None).ConfigureAwait(false);
        }

        return last ?? Result<OperationExecutionResult>.Failure("optimization.apply.failed", "Operation apply failed.");
    }

    private async Task<OptimizationSession> RollbackAfterFailureAsync(OptimizationSession session, CancellationToken cancellationToken)
    {
        var failed = Transition(session, OptimizationSessionState.RollbackPending);
        await PersistAsync(failed, cancellationToken).ConfigureAwait(false);
        if (failed.Plan.OrderedOperations.Count == 0 || failed.Snapshot is null)
        {
            return Fail(failed, "optimization.rollback.not_possible", "Rollback is not possible without snapshots.", OperationErrorCategory.RollbackFailed, null);
        }

        var appliedOperationIds = failed.Journal
            .Where(entry => entry.State is OperationJournalState.ApplyCompleted or OperationJournalState.Verified)
            .Select(entry => entry.OperationId)
            .ToHashSet();
        if (appliedOperationIds.Count == 0)
        {
            var noMutation = failed with
            {
                State = OptimizationSessionState.Failed,
                CompletedAtUtc = DateTimeOffset.UtcNow,
                Failure = new OptimizationFailure(OperationErrorCategory.ApplyFailed, "optimization.apply.failed", "No verified mutation was available for rollback.")
            };
            await PersistAsync(noMutation, cancellationToken).ConfigureAwait(false);
            return noMutation;
        }

        var rolling = Transition(failed, OptimizationSessionState.RollingBack);
        await PersistAsync(rolling, cancellationToken).ConfigureAwait(false);

        var failureSnapshot = rolling.Snapshot ?? throw new InvalidOperationException("Rollback snapshot missing after validation.");
        var rollbackResults = new List<OperationRollbackResult>(rolling.RollbackResults);
        foreach (var operation in rolling.Plan.OrderedOperations.Reverse().Where(operation => appliedOperationIds.Contains(operation.OperationId)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshotItem = failureSnapshot.Items.SingleOrDefault(item => item.OperationId == operation.OperationId);
            if (snapshotItem is null)
            {
                var missing = AppendJournal(rolling, operation.OperationId, OperationJournalState.RollbackFailed, "RollbackFailed", OperationErrorCategory.RollbackFailed, "Rollback snapshot item is missing.") with
                {
                    State = OptimizationSessionState.RollbackFailed,
                    Failure = new OptimizationFailure(OperationErrorCategory.RollbackFailed, "optimization.rollback.snapshot_item_missing", "Rollback snapshot item is missing.", operation.OperationId)
                };
                await PersistAsync(missing, cancellationToken).ConfigureAwait(false);
                return missing;
            }

            var handler = GetHandlerOrThrow(operation);
            rolling = AppendJournal(rolling, operation.OperationId, OperationJournalState.RollbackStarted, "RollbackStarted", OperationErrorCategory.None, "Rollback started after failure.");
            await PersistAsync(rolling, CancellationToken.None).ConfigureAwait(false);

            var rollback = await handler.RollbackAsync(operation, snapshotItem, CancellationToken.None).ConfigureAwait(false);
            if (rollback.IsFailure || rollback.Value is null || !rollback.Value.RestoredOriginalState)
            {
                var failedRollback = rollback.Value ?? new OperationRollbackResult(operation.OperationId, OperationExecutionStatus.RollbackFailed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TimeSpan.Zero, false, OperationErrorCategory.RollbackFailed, rollback.ErrorMessage ?? "Rollback failed.");
                rollbackResults.Add(failedRollback);
                var rollbackFailed = AppendJournal(rolling, operation.OperationId, OperationJournalState.RollbackFailed, "RollbackFailed", failedRollback.ErrorCategory, failedRollback.SafeMessage) with
                {
                    State = OptimizationSessionState.RollbackFailed,
                    RollbackResults = rollbackResults.ToArray(),
                    Failure = new OptimizationFailure(OperationErrorCategory.RollbackFailed, "optimization.rollback.failed", failedRollback.SafeMessage, operation.OperationId)
                };
                await PersistAsync(rollbackFailed, cancellationToken).ConfigureAwait(false);
                return rollbackFailed;
            }

            rollbackResults.Add(rollback.Value);
            rolling = AppendJournal(rolling, operation.OperationId, OperationJournalState.RollbackVerified, "RollbackVerified", OperationErrorCategory.None, rollback.Value.SafeMessage) with
            {
                RollbackResults = rollbackResults.ToArray()
            };
            await PersistAsync(rolling, CancellationToken.None).ConfigureAwait(false);
        }

        var rolledBack = Transition(rolling, OptimizationSessionState.RolledBack) with
        {
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Failure = new OptimizationFailure(OperationErrorCategory.ApplyFailed, "optimization.apply.failed", "Session failed and prior mutations were rolled back.")
        };
        await PersistAsync(rolledBack, cancellationToken).ConfigureAwait(false);
        return rolledBack;
    }

    private IOperationHandler GetHandlerOrThrow(OperationSpec operation)
    {
        if (_handlerRegistry.TryGetHandler(operation.OperationType, out var handler))
        {
            return handler;
        }

        throw new InvalidOperationException($"No handler exists for operation type {operation.OperationType}.");
    }

    private async Task PersistAsync(OptimizationSession session, CancellationToken cancellationToken)
    {
        var save = await _store.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        if (save.IsFailure)
        {
            throw new IOException(save.ErrorMessage ?? "Optimization session could not be persisted.");
        }

        SetCurrent(session);
    }

    private async Task PersistAndReturnAsync(OptimizationSession session, CancellationToken cancellationToken)
    {
        await PersistAsync(session, cancellationToken).ConfigureAwait(false);
        SetState(session.State);
    }

    private void SetCurrent(OptimizationSession session)
    {
        lock (_syncRoot)
        {
            _current = session;
            _state = session.State;
        }
    }

    private void SetState(OptimizationSessionState state)
    {
        lock (_syncRoot)
        {
            _state = state;
        }
    }

    private static OptimizationSession Transition(OptimizationSession session, OptimizationSessionState nextState)
    {
        return OptimizationSessionStateMachine.Transition(session, nextState);
    }

    private OptimizationSession Fail(
        OptimizationSession session,
        string code,
        string safeMessage,
        OperationErrorCategory category,
        OperationId? operationId)
    {
        return AppendJournal(session with
        {
            State = OptimizationSessionState.Failed,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Failure = new OptimizationFailure(category, code, safeMessage, operationId)
        }, operationId ?? OperationIdOrNone(), OperationJournalState.Failed, "Failed", category, safeMessage);
    }

    private OptimizationSession AppendJournal(
        OptimizationSession session,
        OperationId operationId,
        OperationJournalState state,
        string action,
        OperationErrorCategory category,
        string safeMessage)
    {
        var updated = session.AppendJournal(operationId, state, action, category, safeMessage);
        _logger.LogInformation(
            "Optimization journal event. SessionId={SessionId}; PlanId={PlanId}; OperationId={OperationId}; OptimizationIds={OptimizationIds}; Action={Action}; Outcome={Outcome}; ErrorCategory={ErrorCategory}; State={State}; JournalCount={JournalCount}",
            updated.SessionId,
            updated.Plan.PlanId,
            operationId,
            string.Join(",", updated.SelectedOptimizationIds.Select(id => id.Value)),
            action,
            state,
            category,
            updated.State,
            updated.Journal.Count);
        return updated;
    }

    private static bool CancellationIsSafe(OptimizationSession session)
    {
        var last = session.Journal.LastOrDefault();
        var safeState = session.State is OptimizationSessionState.Planned
            or OptimizationSessionState.PreflightPassed
            or OptimizationSessionState.Snapshotting
            or OptimizationSessionState.Ready;
        var unsafeJournal = last?.State is OperationJournalState.ApplyStarted
            or OperationJournalState.ApplyCompleted
            or OperationJournalState.VerificationPending
            or OperationJournalState.RollbackStarted;
        return safeState && !unsafeJournal;
    }

    private OptimizationSession MarkRecoveryRequired(
        OptimizationSession session,
        string code,
        string safeMessage,
        OperationId? operationId)
    {
        return AppendJournal(session with
        {
            State = OptimizationSessionState.RecoveryRequired,
            CompletedAtUtc = null,
            Failure = new OptimizationFailure(OperationErrorCategory.OutcomeUnknown, code, safeMessage, operationId)
        }, operationId ?? OperationIdOrNone(), OperationJournalState.UnknownAfterCrash, "OutcomeUnknown", OperationErrorCategory.OutcomeUnknown, safeMessage);
    }

    private OptimizationSession MarkRollbackRecoveryRequired(
        OptimizationSession session,
        string code,
        string safeMessage,
        OperationId? operationId)
    {
        return AppendJournal(session with
        {
            State = OptimizationSessionState.ManualActionRequired,
            CompletedAtUtc = null,
            Failure = new OptimizationFailure(OperationErrorCategory.RollbackFailed, code, safeMessage, operationId)
        }, operationId ?? OperationIdOrNone(), OperationJournalState.RollbackFailed, "RollbackInterrupted", OperationErrorCategory.RollbackFailed, safeMessage);
    }

    private static Result ValidateSessionSnapshot(OptimizationSession session)
    {
        if (session.Snapshot is null)
        {
            return Result.Failure("optimization.rollback.snapshot_missing", "Rollback requires a trusted operation snapshot.");
        }

        if (session.Snapshot.SchemaVersion != session.SchemaVersion ||
            session.Snapshot.SessionId != session.SessionId ||
            session.Snapshot.PlanId != session.Plan.PlanId)
        {
            return Result.Failure("optimization.rollback.snapshot_session_mismatch", "OperationSnapshot does not belong to the selected session and plan.");
        }

        var operationIds = session.Plan.OrderedOperations.Select(operation => operation.OperationId).ToHashSet();
        foreach (var item in session.Snapshot.Items)
        {
            if (!operationIds.Contains(item.OperationId))
            {
                return Result.Failure("optimization.rollback.snapshot_operation_unknown", "OperationSnapshot references an operation outside the ExecutionPlan.");
            }

            if (!OperationSnapshotHasher.IsValid(item))
            {
                return Result.Failure("optimization.rollback.snapshot_integrity_failed", "OperationSnapshot item integrity hash does not match.");
            }
        }

        return Result.Success();
    }

    private static OperationId OperationIdOrNone()
    {
        return new OperationId("BB.OP.SESSION");
    }
}

internal static class OptimizationSessionExtensions
{
    public static OptimizationSession AppendJournal(
        this OptimizationSession session,
        OperationId operationId,
        OperationJournalState state,
        string action,
        OperationErrorCategory category,
        string safeMessage)
    {
        var journal = session.Journal
            .Concat([new OperationJournalEntry(operationId, state, DateTimeOffset.UtcNow, action, category, safeMessage)])
            .ToArray();
        return session with { Journal = journal };
    }
}
