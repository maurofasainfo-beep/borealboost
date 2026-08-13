using BorealBoost.Core.Analysis;
using BorealBoost.Core.Common;
using BorealBoost.Core.Foundation;
using BorealBoost.Core.Identity;
using BorealBoost.Core.Optimization;
using BorealBoost.Core.Scanner;
using BorealBoost.Infrastructure.Persistence;
using BorealBoost.Optimization.Catalog;
using BorealBoost.Optimization.Execution;
using BorealBoost.Optimization.Handlers;
using BorealBoost.Optimization.Planning;
using BorealBoost.Restore;
using BorealBoost.System.Operations;
using Microsoft.Extensions.Logging.Abstractions;

namespace BorealBoost.Tests.Unit;

public sealed class OptimizationEngineTests
{
    [Fact]
    public void Built_in_definition_declares_transactional_operation_contract()
    {
        var definition = new BuiltInOptimizationCatalog().GetDefinitions().Single();
        var operation = definition.OperationSpecs.Single();

        Assert.Equal(BuiltInOptimizationCatalog.IntegrationProofOptimizationId, definition.OptimizationId);
        Assert.Equal(OperationType.BorealIntegrationRegistryValue, operation.OperationType);
        Assert.Equal(OperationIdempotency.Idempotent, operation.Idempotency);
        Assert.Equal(OperationReversibility.Full, operation.Reversibility);
        Assert.Equal(RebootBoundary.None, operation.RebootBoundary);
        Assert.Equal(OperationFailurePolicy.AttemptRollback, operation.FailurePolicy);
        Assert.Equal(OperationVerificationKind.ExactState, operation.VerificationStrategy.Kind);
        Assert.Equal(OperationRollbackKind.SnapshotRestore, operation.RollbackStrategy.Kind);
        Assert.Contains(operation.SnapshotRequirements, requirement => requirement.Requirement == SnapshotRequirementKind.Required && requirement.BlockIfUnavailable);
        Assert.Equal(RestorePointRequirement.NotRequired, definition.RestorePointRequirement);
    }

    [Fact]
    public void Agent_validator_rejects_unknown_operation_type()
    {
        var operation = BuiltInOperation() with { OperationType = (OperationType)999 };

        var result = new AgentOperationSecurityValidator().Validate(operation);

        Assert.True(result.IsFailure);
        Assert.Equal("agent.operation.type_unknown", result.ErrorCode);
    }

    [Fact]
    public void Agent_validator_rejects_registry_target_outside_allowlist()
    {
        var operation = BuiltInOperation() with
        {
            RegistryValue = BuiltInOperation().RegistryValue! with
            {
                Target = BuiltInOperation().RegistryValue!.Target with { KeyPath = @"Software\OtherProduct" }
            }
        };

        var result = new AgentOperationSecurityValidator().Validate(operation);

        Assert.True(result.IsFailure);
        Assert.Equal("agent.operation.target_not_allowed", result.ErrorCode);
    }

    [Theory]
    [InlineData(@"Software\BorealBoost\IntegrationTest&cmd.exe")]
    [InlineData(@"C:\Windows\System32\cmd.exe")]
    [InlineData(@"Software\BorealBoost\IntegrationTest\..\Run")]
    public void Agent_validator_rejects_command_or_executable_target_injection(string keyPath)
    {
        var baseOperation = BuiltInOperation();
        var operation = baseOperation with
        {
            RegistryValue = baseOperation.RegistryValue! with
            {
                Target = baseOperation.RegistryValue.Target with { KeyPath = keyPath }
            }
        };

        var result = new AgentOperationSecurityValidator().Validate(operation);

        Assert.True(result.IsFailure);
        Assert.Equal("agent.operation.target_not_allowed", result.ErrorCode);
    }

    [Fact]
    public void Operation_definition_validator_rejects_missing_snapshot_for_full_reversibility()
    {
        var definition = new BuiltInOptimizationCatalog().GetDefinitions().Single();
        var invalid = definition with
        {
            OperationSpecs =
            [
                definition.OperationSpecs.Single() with { SnapshotRequirements = [] }
            ]
        };

        var issues = new OptimizationDefinitionValidator().Validate(invalid);

        Assert.Contains(issues, issue => issue.Code == "optimization.operation.snapshot_missing");
    }

    [Fact]
    public void Execution_plan_contains_required_phase_4_metadata()
    {
        var context = CreateContext();
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]);

        Assert.True(plan.IsSuccess, plan.ErrorMessage);
        Assert.Empty(plan.Value!.Blockers);
        Assert.Equal("4.0.0", plan.Value.SchemaVersion);
        Assert.Equal(BuiltInOptimizationCatalog.CurrentCatalogVersion, plan.Value.CatalogVersion);
        Assert.Single(plan.Value.OrderedOperations);
        Assert.NotEmpty(plan.Value.PlanHash);
        Assert.False(plan.Value.RequiresRestart);
        Assert.DoesNotContain("FPS", plan.Value.Warnings.Select(issue => issue.Message));
    }

    [Fact]
    public void Plan_validator_rejects_stale_snapshot()
    {
        var context = CreateContext();
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;
        var staleSnapshot = BuildSnapshot();

        var validation = context.PlanValidator.Validate(plan, staleSnapshot);

        Assert.Equal(ExecutionPlanValidationStatus.NeedsRevalidation, validation.Status);
        Assert.Contains(validation.Issues, issue => issue.Code == "optimization.plan.stale");
    }

    [Fact]
    public void Plan_validator_rejects_plan_hash_mismatch_after_operation_tamper()
    {
        var context = CreateContext();
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;
        var operation = plan.OrderedOperations.Single();
        var tampered = plan with
        {
            IsApproved = true,
            OrderedOperations =
            [
                operation with
                {
                    RegistryValue = operation.RegistryValue! with
                    {
                        Target = operation.RegistryValue.Target with { KeyPath = @"Software\BorealBoost\IntegrationTest\Tampered" }
                    }
                }
            ]
        };

        var validation = context.PlanValidator.Validate(tampered, context.Snapshot);

        Assert.Equal(ExecutionPlanValidationStatus.Invalid, validation.Status);
        Assert.Contains(validation.Issues, issue => issue.Code == "optimization.plan.hash_mismatch");
        Assert.Contains(validation.Issues, issue => issue.Code == "optimization.catalog.operation_mismatch");
    }

    [Fact]
    public void Plan_validator_accepts_intact_approved_plan_hash()
    {
        var context = CreateContext();
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;

        var validation = context.PlanValidator.Validate(plan with { IsApproved = true }, context.Snapshot);

        Assert.True(validation.CanExecute, string.Join("; ", validation.Issues.Select(issue => issue.Code)));
    }

    [Fact]
    public void Plan_validator_rejects_operation_order_tamper_after_approval()
    {
        var operations = new[]
        {
            BuiltInOperation() with { OperationId = new OperationId("BB.OP.TEST.A") },
            BuiltInOperation() with { OperationId = new OperationId("BB.OP.TEST.B") }
        };
        var definition = new BuiltInOptimizationCatalog().GetDefinitions().Single() with { OperationSpecs = operations };
        var context = CreateContext(new StaticCatalog(definition), new OrderedFailureOperationHandler("none"));
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;

        var tampered = plan with { IsApproved = true, OrderedOperations = plan.OrderedOperations.Reverse().ToArray() };
        var validation = context.PlanValidator.Validate(tampered, context.Snapshot);

        Assert.Equal(ExecutionPlanValidationStatus.Invalid, validation.Status);
        Assert.Contains(validation.Issues, issue => issue.Code == "optimization.plan.hash_mismatch");
    }

    [Fact]
    public async Task Preflight_requires_explicit_approved_plan()
    {
        var context = CreateContext();
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;
        var preflight = new PreflightService(context.PlanValidator, context.HandlerRegistry);

        var unapproved = await preflight.CheckAsync(plan, context.Snapshot, CancellationToken.None);
        var approved = await preflight.CheckAsync(plan with { IsApproved = true }, context.Snapshot, CancellationToken.None);

        Assert.False(unapproved.Passed);
        Assert.Contains(unapproved.Issues, issue => issue.Code == "optimization.preflight.plan_not_approved");
        Assert.True(approved.Passed, string.Join("; ", approved.Issues.Select(issue => issue.Code)));
    }

    [Fact]
    public async Task Dry_run_builds_valid_plan_without_mutating()
    {
        var context = CreateContext();

        var result = await context.DryRun.DryRunAsync(
            context.Snapshot,
            context.Analysis,
            [BuiltInOptimizationCatalog.IntegrationProofOptimizationId],
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(result.Value!.Validation.CanExecute);
        Assert.Single(result.Value.Operations);
        Assert.Equal(OperationReversibility.Full, result.Value.Operations.Single().Reversibility);
    }

    [Fact]
    public void State_machine_rejects_invalid_transition_from_completed_to_executing()
    {
        var context = CreateContext();
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;
        var session = CreateSession(plan) with { State = OptimizationSessionState.Completed, CompletedAtUtc = DateTimeOffset.UtcNow };

        Assert.Throws<InvalidOperationException>(() => OptimizationSessionStateMachine.Transition(session, OptimizationSessionState.Executing));
    }

    [Fact]
    public async Task Session_store_rejects_corrupted_session_json()
    {
        var directory = CreateTempDirectory();
        var store = new FileOptimizationSessionStore(directory);
        var sessionId = SessionId.New();
        await File.WriteAllTextAsync(Path.Combine(directory, sessionId + ".json"), "{ truncated", CancellationToken.None);

        var result = await store.LoadAsync(sessionId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("optimization.session.malformed", result.ErrorCode);
    }

    [Fact]
    public async Task Session_store_rejects_integrity_hash_mismatch()
    {
        var directory = CreateTempDirectory();
        var store = new FileOptimizationSessionStore(directory);
        var context = CreateContext(store: store);
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;
        var session = CreateSession(plan);
        Assert.True((await store.SaveAsync(session, CancellationToken.None)).IsSuccess);

        var path = Path.Combine(directory, session.SessionId + ".json");
        var content = await File.ReadAllTextAsync(path);
        await File.WriteAllTextAsync(path, content.Replace("Planned", "Executing", StringComparison.Ordinal));

        var result = await store.LoadAsync(session.SessionId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("optimization.session.integrity_failed", result.ErrorCode);
    }

    [Fact]
    public async Task Recovery_detects_incomplete_session_without_completed_timestamp()
    {
        var directory = CreateTempDirectory();
        var store = new FileOptimizationSessionStore(directory);
        var context = CreateContext(store: store);
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;
        var session = CreateSession(plan) with { State = OptimizationSessionState.Executing, CompletedAtUtc = null };
        Assert.True((await store.SaveAsync(session, CancellationToken.None)).IsSuccess);

        var candidates = await new RecoveryService(store).DetectAsync(CancellationToken.None);

        Assert.Single(candidates);
        Assert.Equal(OptimizationSessionState.Executing, candidates.Single().State);
        Assert.Equal(RecoveryActionKind.Verify, candidates.Single().SuggestedAction);
    }

    [Fact]
    public async Task Recovery_detects_corrupted_session_artifact()
    {
        var directory = CreateTempDirectory();
        var store = new FileOptimizationSessionStore(directory);
        var context = CreateContext(store: store);
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;
        var session = CreateSession(plan);
        Assert.True((await store.SaveAsync(session, CancellationToken.None)).IsSuccess);

        var path = Path.Combine(directory, session.SessionId + ".json");
        var content = await File.ReadAllTextAsync(path);
        await File.WriteAllTextAsync(path, content.Replace("Planned", "Executing", StringComparison.Ordinal));

        var candidates = await new RecoveryService(store).DetectAsync(CancellationToken.None);

        var candidate = Assert.Single(candidates);
        Assert.True(candidate.IsInvalidArtifact);
        Assert.Equal(RecoveryActionKind.ManualRecovery, candidate.SuggestedAction);
        Assert.Contains("integrity", candidate.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Recovery_detects_residual_temp_artifact()
    {
        var directory = CreateTempDirectory();
        var sessionId = SessionId.New();
        await File.WriteAllTextAsync(Path.Combine(directory, sessionId + ".json.tmp"), "partial");
        var store = new FileOptimizationSessionStore(directory);

        var candidates = await new RecoveryService(store).DetectAsync(CancellationToken.None);

        var candidate = Assert.Single(candidates);
        Assert.True(candidate.IsInvalidArtifact);
        Assert.Equal(RecoveryActionKind.ManualRecovery, candidate.SuggestedAction);
        Assert.Contains("temp_artifact", candidate.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Two_optimization_sessions_cannot_run_simultaneously()
    {
        var handler = new BlockingOperationHandler();
        var context = CreateContext(handler: handler);
        var firstPlan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;
        var secondPlan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;

        var first = context.SessionService.ExecuteAsync(firstPlan, context.Snapshot, CancellationToken.None);
        await handler.WaitUntilApplyStartedAsync();
        var second = await context.SessionService.ExecuteAsync(secondPlan, context.Snapshot, CancellationToken.None);
        handler.ReleaseApply();
        _ = await first;

        Assert.True(second.IsFailure);
        Assert.Equal("optimization.session.already_running", second.ErrorCode);
    }

    [Fact]
    public async Task Cross_process_session_lock_rejects_second_holder_and_releases_after_dispose()
    {
        var lockPath = Path.Combine(CreateTempDirectory(), "optimization.lock");
        var firstLock = new CrossProcessOptimizationSessionLock(lockPath);
        var secondLock = new CrossProcessOptimizationSessionLock(lockPath);

        var first = await firstLock.TryAcquireAsync(CancellationToken.None);
        Assert.True(first.IsSuccess, first.ErrorMessage);
        await using (first.Value!)
        {
            var second = await secondLock.TryAcquireAsync(CancellationToken.None);
            Assert.True(second.IsFailure);
            Assert.Equal("optimization.session.already_running", second.ErrorCode);
        }

        var afterRelease = await secondLock.TryAcquireAsync(CancellationToken.None);
        Assert.True(afterRelease.IsSuccess, afterRelease.ErrorMessage);
        await afterRelease.Value!.DisposeAsync();
    }

    [Fact]
    public async Task Failure_policy_attempt_rollback_runs_applied_operations_in_reverse_order()
    {
        var operations = new[]
        {
            BuiltInOperation() with { OperationId = new OperationId("BB.OP.TEST.A") },
            BuiltInOperation() with { OperationId = new OperationId("BB.OP.TEST.B") },
            BuiltInOperation() with { OperationId = new OperationId("BB.OP.TEST.C") }
        };
        var definition = new BuiltInOptimizationCatalog().GetDefinitions().Single() with { OperationSpecs = operations };
        var handler = new OrderedFailureOperationHandler("BB.OP.TEST.C");
        var context = CreateContext(new StaticCatalog(definition), handler);
        var plan = CreateManualPlan(context, operations);

        var result = await context.SessionService.ExecuteAsync(plan, context.Snapshot, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(["BB.OP.TEST.B", "BB.OP.TEST.A"], handler.RollbackOrder);
        Assert.Equal(OptimizationSessionState.RolledBack, context.SessionService.Current!.State);
    }

    [Fact]
    public async Task Partial_rollback_failure_does_not_end_as_rolled_back()
    {
        var operations = new[]
        {
            BuiltInOperation() with { OperationId = new OperationId("BB.OP.TEST.A") },
            BuiltInOperation() with { OperationId = new OperationId("BB.OP.TEST.B") },
            BuiltInOperation() with { OperationId = new OperationId("BB.OP.TEST.C") }
        };
        var definition = new BuiltInOptimizationCatalog().GetDefinitions().Single() with { OperationSpecs = operations };
        var handler = new PartialRollbackFailureOperationHandler("BB.OP.TEST.C", "BB.OP.TEST.A");
        var context = CreateContext(new StaticCatalog(definition), handler);
        var plan = CreateManualPlan(context, operations);

        var result = await context.SessionService.ExecuteAsync(plan, context.Snapshot, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(["BB.OP.TEST.B", "BB.OP.TEST.A"], handler.RollbackOrder);
        Assert.Equal(OptimizationSessionState.RollbackFailed, context.SessionService.Current!.State);
    }

    [Fact]
    public async Task Timeout_after_apply_start_marks_session_recovery_required()
    {
        var operation = BuiltInOperation() with
        {
            TimeoutPolicy = new OperationTimeoutPolicy(TimeSpan.FromMilliseconds(20)),
            RetryPolicy = new OperationRetryPolicy(false, 1, TimeSpan.Zero, [])
        };
        var definition = new BuiltInOptimizationCatalog().GetDefinitions().Single() with { OperationSpecs = [operation] };
        var handler = new TimeoutAfterApplyStartedHandler();
        var context = CreateContext(new StaticCatalog(definition), handler);
        var plan = CreateManualPlan(context, [operation]);

        var result = await context.SessionService.ExecuteAsync(plan, context.Snapshot, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("optimization.apply.outcome_unknown", result.ErrorCode);
        Assert.Equal(OptimizationSessionState.RecoveryRequired, context.SessionService.Current!.State);
        Assert.Null(context.SessionService.Current.CompletedAtUtc);
        Assert.Contains(context.SessionService.Current.Journal, entry => entry.State == OperationJournalState.UnknownAfterCrash);
    }

    [Fact]
    public async Task Cancellation_before_lock_acquire_returns_structured_cancelled_result()
    {
        var context = CreateContext();
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await context.SessionService.ExecuteAsync(plan, context.Snapshot, cts.Token);

        Assert.True(result.IsFailure);
        Assert.Equal("optimization.cancelled", result.ErrorCode);
        Assert.Null(context.SessionService.Current);
        Assert.Equal(OptimizationSessionState.Created, context.SessionService.State);
    }

    [Fact]
    public async Task Cancellation_before_apply_marks_session_cancelled_without_apply_started()
    {
        using var cts = new CancellationTokenSource();
        var handler = new CancellingAfterCaptureHandler(cts);
        var context = CreateContext(handler: handler);
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;

        var result = await context.SessionService.ExecuteAsync(plan, context.Snapshot, cts.Token);

        Assert.True(result.IsFailure);
        Assert.Equal("optimization.cancelled", result.ErrorCode);
        Assert.Equal(OptimizationSessionState.Cancelled, context.SessionService.Current!.State);
        Assert.DoesNotContain(context.SessionService.Current.Journal, entry => entry.State == OperationJournalState.ApplyStarted);
    }

    [Fact]
    public async Task Cancellation_during_apply_does_not_interrupt_critical_mutation_boundary()
    {
        using var cts = new CancellationTokenSource();
        var handler = new SignalingApplyHandler();
        var context = CreateContext(handler: handler, store: new FileOptimizationSessionStore(CreateTempDirectory()));
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;

        var execution = context.SessionService.ExecuteAsync(plan, context.Snapshot, cts.Token);
        await handler.WaitUntilApplyStartedAsync();
        await cts.CancelAsync();
        handler.ReleaseApply();
        var result = await execution;

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(OptimizationSessionState.Completed, result.Value!.State);
        Assert.Contains(result.Value.Journal, entry => entry.State == OperationJournalState.ApplyCompleted);
        Assert.Contains(result.Value.Journal, entry => entry.State == OperationJournalState.Verified);
    }

    [Fact]
    public async Task Cancellation_after_apply_before_verify_still_persists_verified_completion()
    {
        using var cts = new CancellationTokenSource();
        var handler = new CancellingAfterApplyHandler(cts);
        var context = CreateContext(handler: handler, store: new FileOptimizationSessionStore(CreateTempDirectory()));
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;

        var result = await context.SessionService.ExecuteAsync(plan, context.Snapshot, cts.Token);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(OptimizationSessionState.Completed, result.Value!.State);
        Assert.Contains(result.Value.VerificationResults, verification => verification.Verified);
    }

    [Fact]
    public async Task Cancellation_during_rollback_does_not_report_rolled_back()
    {
        using var cts = new CancellationTokenSource();
        var store = new InMemoryOptimizationSessionStore(session =>
        {
            if (session.State == OptimizationSessionState.RollingBack)
            {
                cts.Cancel();
            }
        });
        var context = CreateContext(store: store);
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;
        var operation = plan.OrderedOperations.Single();
        var session = CreateSession(plan) with
        {
            State = OptimizationSessionState.Completed,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            VerificationResults =
            [
                new OperationVerificationResult(operation.OperationId, OperationExecutionStatus.Verified, DateTimeOffset.UtcNow, true, "verified")
            ],
            Snapshot = new OperationSnapshot("4.0.0", plan.SessionId, plan.PlanId, DateTimeOffset.UtcNow, [SnapshotFor(operation)])
        };
        Assert.True((await store.SaveAsync(session, CancellationToken.None)).IsSuccess);

        var result = await context.SessionService.RollbackAsync(session.SessionId, cts.Token);

        Assert.True(result.IsFailure);
        Assert.Equal("optimization.rollback.cancelled", result.ErrorCode);
        Assert.Equal(OptimizationSessionState.ManualActionRequired, context.SessionService.Current!.State);
        Assert.NotEqual(OptimizationSessionState.RolledBack, context.SessionService.Current.State);
    }

    [Fact]
    public async Task Rollback_rejects_snapshot_from_another_session()
    {
        var directory = CreateTempDirectory();
        var store = new FileOptimizationSessionStore(directory);
        var context = CreateContext(store: store);
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;
        var operation = plan.OrderedOperations.Single();
        var session = CreateSession(plan) with
        {
            State = OptimizationSessionState.Completed,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            VerificationResults =
            [
                new OperationVerificationResult(operation.OperationId, OperationExecutionStatus.Verified, DateTimeOffset.UtcNow, true, "verified")
            ],
            Snapshot = new OperationSnapshot(
                "4.0.0",
                SessionId.New(),
                plan.PlanId,
                DateTimeOffset.UtcNow,
                [SnapshotFor(operation)])
        };
        Assert.True((await store.SaveAsync(session, CancellationToken.None)).IsSuccess);

        var result = await context.SessionService.RollbackAsync(session.SessionId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("optimization.rollback.snapshot_session_mismatch", result.ErrorCode);
    }

    [Fact]
    public async Task Rollback_rejects_snapshot_item_value_tamper()
    {
        var directory = CreateTempDirectory();
        var store = new FileOptimizationSessionStore(directory);
        var context = CreateContext(store: store);
        var plan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;
        var operation = plan.OrderedOperations.Single();
        var trustedSnapshot = SnapshotFor(operation);
        var tamperedSnapshot = trustedSnapshot with { PreviousStringValue = "tampered" };
        var session = CreateSession(plan) with
        {
            State = OptimizationSessionState.Completed,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            VerificationResults =
            [
                new OperationVerificationResult(operation.OperationId, OperationExecutionStatus.Verified, DateTimeOffset.UtcNow, true, "verified")
            ],
            Snapshot = new OperationSnapshot("4.0.0", plan.SessionId, plan.PlanId, DateTimeOffset.UtcNow, [tamperedSnapshot])
        };
        Assert.True((await store.SaveAsync(session, CancellationToken.None)).IsSuccess);

        var result = await context.SessionService.RollbackAsync(session.SessionId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("optimization.rollback.snapshot_integrity_failed", result.ErrorCode);
    }

    private static TestContext CreateContext(
        IOptimizationCatalog? catalog = null,
        IOperationHandler? handler = null,
        IOptimizationSessionStore? store = null,
        IOptimizationSessionLock? sessionLock = null)
    {
        catalog ??= new BuiltInOptimizationCatalog();
        handler ??= new BorealIntegrationRegistryOperationHandler();
        store ??= new InMemoryOptimizationSessionStore();
        sessionLock ??= new CrossProcessOptimizationSessionLock(Path.Combine(CreateTempDirectory(), "optimization.lock"));

        var definitionValidator = new OptimizationDefinitionValidator();
        var registry = new OperationHandlerRegistry([handler]);
        var planner = new ExecutionPlanner(catalog, definitionValidator);
        var validator = new ExecutionPlanValidator(catalog, registry);
        var preflight = new PreflightService(validator, registry);
        var dryRun = new DryRunService(planner, validator, registry);
        var sessionService = new OptimizationSessionService(
            preflight,
            registry,
            store,
            new RestorePointService(),
            NullLogger<OptimizationSessionService>.Instance,
            sessionLock);
        var snapshot = BuildSnapshot();
        var analysis = BuildAnalysis(snapshot.Metadata.ScanId);
        return new TestContext(catalog, registry, planner, validator, dryRun, sessionService, snapshot, analysis);
    }

    private static OperationSpec BuiltInOperation()
    {
        return new BuiltInOptimizationCatalog().GetDefinitions().Single().OperationSpecs.Single();
    }

    private static ExecutionPlan CreateManualPlan(TestContext context, IReadOnlyList<OperationSpec> operations)
    {
        var basePlan = context.Planner.CreatePlan(context.Snapshot, context.Analysis, context.Analysis.RecommendationPlan, [BuiltInOptimizationCatalog.IntegrationProofOptimizationId]).Value!;
        return Rehash(basePlan with
        {
            OrderedOperations = operations,
            EstimatedStepCount = operations.Count * 3,
            Blockers = [],
            Warnings = []
        });
    }

    private static ExecutionPlan Rehash(ExecutionPlan plan)
    {
        return plan with { PlanHash = ExecutionPlanHasher.Compute(plan) };
    }

    private static OptimizationSession CreateSession(ExecutionPlan plan)
    {
        return new OptimizationSession(
            "4.0.0",
            plan.SessionId,
            plan,
            DateTimeOffset.UtcNow,
            null,
            OptimizationSessionState.Planned,
            plan.SelectedOptimizationIds,
            [],
            null,
            null,
            [],
            [],
            false,
            null,
            "BorealBoost",
            "4.0.0");
    }

    private static OperationSnapshotItem SnapshotFor(OperationSpec operation)
    {
        return OperationSnapshotHasher.Stamp(new OperationSnapshotItem(
            Guid.NewGuid(),
            operation.OperationId,
            OperationResourceType.RegistryValue,
            $"{operation.RegistryValue!.Target.Hive}\\{operation.RegistryValue.Target.KeyPath}\\{operation.RegistryValue.Target.ValueName}",
            true,
            operation.RegistryValue.Target,
            RegistryValueDataKind.String,
            "before",
            null,
            "test",
            DateTimeOffset.UtcNow,
            operation.RollbackStrategy,
            [],
            "test"));
    }

    private static SystemSnapshot BuildSnapshot()
    {
        var started = DateTimeOffset.UtcNow.AddMilliseconds(-10);
        var completed = DateTimeOffset.UtcNow;
        return new SystemSnapshot(
            new ScanMetadata(
                ScanId.New(),
                started,
                completed,
                completed - started,
                "test",
                "2.0.0",
                "X64",
                [ProviderResult.Succeeded("OperatingSystem", DataSourceKind.Composite, TimeSpan.FromMilliseconds(1))],
                PartialScan: false,
                [],
                []),
            new OperatingSystemSnapshot("Microsoft Windows 11 Pro", "Pro", "10.0", 26200, 0, "25H2", "X64", WindowsCompatibilityStatus.Supported, "test", DataSourceKind.Composite),
            new HardwareSnapshot("Vendor", "Model", MachineFormFactor.Desktop, false, null, DataSourceKind.Wmi),
            [new CpuSnapshot("AMD", "AMD Ryzen Test", HardwareVendor.Amd, "X64", 12, 6, 1, null, null, null, null, true, DataSourceKind.Wmi)],
            [new GpuSnapshot("NVIDIA Test", HardwareVendor.Nvidia, "1234", null, "1.0", null, null, VramDetectionStatus.Unknown, "OK", GpuFormFactor.Dedicated, DataSourceKind.Wmi)],
            new MemorySnapshot(16UL * 1024 * 1024 * 1024, 16UL * 1024 * 1024 * 1024, 2, [], DataSourceKind.Wmi),
            new StorageSnapshot([], [new StorageVolumeSnapshot("C:\\", "System", "Fixed", 256L * 1024 * 1024 * 1024, 128L * 1024 * 1024 * 1024, true, DataSourceKind.DriveInfo)], DataSourceKind.Composite),
            new MotherboardSnapshot("Vendor", "Board", null, DataSourceKind.Wmi),
            new FirmwareSnapshot("Vendor", "1.0", null, "UEFI", true, DataSourceKind.Composite),
            [],
            [],
            [],
            [new DisplaySnapshot("\\\\.\\DISPLAY1", "Display", 1920, 1080, 144, 96, true, DataSourceKind.WindowsApi)],
            new PowerSnapshot(false, true, null, PowerSourceKind.AC, "Balanced", DataSourceKind.Composite),
            [],
            [],
            [],
            [new SystemCapabilitySnapshot("SecureBootEnabled", DetectionStatus.Known, true, "True", DataSourceKind.Composite)]);
    }

    private static AnalysisResult BuildAnalysis(ScanId scanId)
    {
        var now = DateTimeOffset.UtcNow;
        return new AnalysisResult(
            AnalysisId.New(),
            scanId,
            now,
            now,
            TimeSpan.Zero,
            "3.0.0",
            "3.0.0-code-first",
            [],
            [],
            [],
            new RecommendationPlan("3.0.0-preview", [], []),
            new AnalysisSummary(0, 0, 0, 0, 0, 0, 0, 0, Enum.GetValues<RecommendationRiskLevel>().ToDictionary(risk => risk, _ => 0)),
            []);
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "BorealBoostTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed record TestContext(
        IOptimizationCatalog Catalog,
        IOperationHandlerRegistry HandlerRegistry,
        IExecutionPlanner Planner,
        IExecutionPlanValidator PlanValidator,
        IDryRunService DryRun,
        IOptimizationSessionService SessionService,
        SystemSnapshot Snapshot,
        AnalysisResult Analysis);

    private sealed class StaticCatalog : IOptimizationCatalog
    {
        private readonly OptimizationDefinition _definition;

        public StaticCatalog(OptimizationDefinition definition)
        {
            _definition = definition;
        }

        public string SchemaVersion => "4.0.0";

        public string CatalogVersion => BuiltInOptimizationCatalog.CurrentCatalogVersion;

        public IReadOnlyList<OptimizationDefinition> GetDefinitions() => [_definition];

        public OptimizationDefinition? Find(OptimizationId optimizationId)
        {
            return optimizationId == _definition.OptimizationId ? _definition : null;
        }
    }

    private sealed class InMemoryOptimizationSessionStore : IOptimizationSessionStore
    {
        private readonly Dictionary<SessionId, OptimizationSession> _sessions = [];
        private readonly Action<OptimizationSession>? _onSave;

        public InMemoryOptimizationSessionStore(Action<OptimizationSession>? onSave = null)
        {
            _onSave = onSave;
        }

        public Task<Result> SaveAsync(OptimizationSession session, CancellationToken cancellationToken)
        {
            _sessions[session.SessionId] = session;
            _onSave?.Invoke(session);
            return Task.FromResult(Result.Success());
        }

        public Task<Result<OptimizationSession>> LoadAsync(SessionId sessionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_sessions.TryGetValue(sessionId, out var session)
                ? Result<OptimizationSession>.Success(session)
                : Result<OptimizationSession>.Failure("missing", "missing"));
        }

        public Task<IReadOnlyList<OptimizationSession>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<OptimizationSession>>(_sessions.Values.ToArray());
        }
    }

    private class OrderedFailureOperationHandler : IOperationHandler
    {
        private readonly string _failOperationId;

        public OrderedFailureOperationHandler(string failOperationId)
        {
            _failOperationId = failOperationId;
        }

        public List<string> RollbackOrder { get; } = [];

        public OperationType OperationType => OperationType.BorealIntegrationRegistryValue;

        public Result Validate(OperationSpec operation) => Result.Success();

        public virtual Task<Result<OperationSnapshotItem>> CaptureSnapshotAsync(OperationSpec operation, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<OperationSnapshotItem>.Success(OperationSnapshotHasher.Stamp(new OperationSnapshotItem(
                Guid.NewGuid(),
                operation.OperationId,
                OperationResourceType.RegistryValue,
                operation.OperationId.ToString(),
                true,
                operation.RegistryValue!.Target,
                RegistryValueDataKind.String,
                "before",
                null,
                "test",
                DateTimeOffset.UtcNow,
                operation.RollbackStrategy,
                [],
                "test"))));
        }

        public virtual Task<Result<OperationExecutionResult>> ApplyAsync(OperationSpec operation, OperationSnapshotItem snapshot, CancellationToken cancellationToken)
        {
            var status = operation.OperationId.Value == _failOperationId ? OperationExecutionStatus.Failed : OperationExecutionStatus.Applied;
            var category = status == OperationExecutionStatus.Failed ? OperationErrorCategory.ApplyFailed : OperationErrorCategory.None;
            return Task.FromResult(Result<OperationExecutionResult>.Success(new OperationExecutionResult(
                operation.OperationId,
                status,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                TimeSpan.Zero,
                ChangedState: status == OperationExecutionStatus.Applied,
                RequiresRestart: false,
                category,
                status == OperationExecutionStatus.Applied ? "applied" : "failed")));
        }

        public Task<Result<OperationVerificationResult>> VerifyAsync(OperationSpec operation, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<OperationVerificationResult>.Success(new OperationVerificationResult(
                operation.OperationId,
                OperationExecutionStatus.Verified,
                DateTimeOffset.UtcNow,
                true,
                "verified")));
        }

        public virtual Task<Result<OperationRollbackResult>> RollbackAsync(OperationSpec operation, OperationSnapshotItem snapshot, CancellationToken cancellationToken)
        {
            RollbackOrder.Add(operation.OperationId.Value);
            return Task.FromResult(Result<OperationRollbackResult>.Success(new OperationRollbackResult(
                operation.OperationId,
                OperationExecutionStatus.RolledBack,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                TimeSpan.Zero,
                true,
                OperationErrorCategory.None,
                "rolled back")));
        }
    }

    private sealed class PartialRollbackFailureOperationHandler : OrderedFailureOperationHandler
    {
        private readonly string _rollbackFailOperationId;

        public PartialRollbackFailureOperationHandler(string applyFailOperationId, string rollbackFailOperationId)
            : base(applyFailOperationId)
        {
            _rollbackFailOperationId = rollbackFailOperationId;
        }

        public override Task<Result<OperationRollbackResult>> RollbackAsync(OperationSpec operation, OperationSnapshotItem snapshot, CancellationToken cancellationToken)
        {
            RollbackOrder.Add(operation.OperationId.Value);
            var failed = operation.OperationId.Value == _rollbackFailOperationId;
            return Task.FromResult(Result<OperationRollbackResult>.Success(new OperationRollbackResult(
                operation.OperationId,
                failed ? OperationExecutionStatus.RollbackFailed : OperationExecutionStatus.RolledBack,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                TimeSpan.Zero,
                RestoredOriginalState: !failed,
                failed ? OperationErrorCategory.RollbackFailed : OperationErrorCategory.None,
                failed ? "rollback failed" : "rolled back")));
        }
    }

    private sealed class TimeoutAfterApplyStartedHandler : OrderedFailureOperationHandler
    {
        public TimeoutAfterApplyStartedHandler()
            : base("none")
        {
        }

        public override async Task<Result<OperationExecutionResult>> ApplyAsync(OperationSpec operation, OperationSnapshotItem snapshot, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return await base.ApplyAsync(operation, snapshot, cancellationToken);
        }
    }

    private sealed class CancellingAfterCaptureHandler : OrderedFailureOperationHandler
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public CancellingAfterCaptureHandler(CancellationTokenSource cancellationTokenSource)
            : base("none")
        {
            _cancellationTokenSource = cancellationTokenSource;
        }

        public override Task<Result<OperationSnapshotItem>> CaptureSnapshotAsync(OperationSpec operation, CancellationToken cancellationToken)
        {
            var result = base.CaptureSnapshotAsync(operation, cancellationToken);
            _cancellationTokenSource.Cancel();
            return result;
        }
    }

    private sealed class CancellingAfterApplyHandler : OrderedFailureOperationHandler
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public CancellingAfterApplyHandler(CancellationTokenSource cancellationTokenSource)
            : base("none")
        {
            _cancellationTokenSource = cancellationTokenSource;
        }

        public override async Task<Result<OperationExecutionResult>> ApplyAsync(OperationSpec operation, OperationSnapshotItem snapshot, CancellationToken cancellationToken)
        {
            var result = await base.ApplyAsync(operation, snapshot, cancellationToken);
            _cancellationTokenSource.Cancel();
            return result;
        }
    }

    private sealed class SignalingApplyHandler : OrderedFailureOperationHandler
    {
        private readonly TaskCompletionSource _applyStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseApply = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public SignalingApplyHandler()
            : base("none")
        {
        }

        public Task WaitUntilApplyStartedAsync() => _applyStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public void ReleaseApply() => _releaseApply.TrySetResult();

        public override async Task<Result<OperationExecutionResult>> ApplyAsync(OperationSpec operation, OperationSnapshotItem snapshot, CancellationToken cancellationToken)
        {
            _applyStarted.TrySetResult();
            await _releaseApply.Task.WaitAsync(cancellationToken);
            return await base.ApplyAsync(operation, snapshot, cancellationToken);
        }
    }

    private sealed class BlockingOperationHandler : OrderedFailureOperationHandler
    {
        private readonly TaskCompletionSource _applyStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseApply = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BlockingOperationHandler()
            : base("none")
        {
        }

        public Task WaitUntilApplyStartedAsync() => _applyStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public void ReleaseApply() => _releaseApply.TrySetResult();

        public override async Task<Result<OperationExecutionResult>> ApplyAsync(OperationSpec operation, OperationSnapshotItem snapshot, CancellationToken cancellationToken)
        {
            _applyStarted.TrySetResult();
            await _releaseApply.Task.WaitAsync(cancellationToken);
            return await base.ApplyAsync(operation, snapshot, cancellationToken);
        }
    }
}
