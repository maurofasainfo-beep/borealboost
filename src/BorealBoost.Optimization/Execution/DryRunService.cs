using BorealBoost.Core.Analysis;
using BorealBoost.Core.Common;
using BorealBoost.Core.Optimization;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Optimization.Execution;

public sealed class DryRunService : IDryRunService
{
    private readonly IExecutionPlanner _planner;
    private readonly IExecutionPlanValidator _planValidator;
    private readonly IOperationHandlerRegistry _handlerRegistry;

    public DryRunService(
        IExecutionPlanner planner,
        IExecutionPlanValidator planValidator,
        IOperationHandlerRegistry handlerRegistry)
    {
        _planner = planner;
        _planValidator = planValidator;
        _handlerRegistry = handlerRegistry;
    }

    public async Task<Result<DryRunResult>> DryRunAsync(
        SystemSnapshot snapshot,
        AnalysisResult analysis,
        IReadOnlyList<OptimizationId> selectedOptimizationIds,
        CancellationToken cancellationToken)
    {
        var planResult = _planner.CreatePlan(snapshot, analysis, analysis.RecommendationPlan, selectedOptimizationIds);
        if (planResult.IsFailure || planResult.Value is null)
        {
            return Result<DryRunResult>.Failure(planResult.ErrorCode ?? "optimization.dry_run.plan_failed", planResult.ErrorMessage ?? "ExecutionPlan could not be created.");
        }

        var plan = planResult.Value;
        var validation = _planValidator.Validate(plan, snapshot);
        var operations = new List<DryRunOperation>();
        var warnings = new List<OptimizationIssue>(plan.Warnings);
        var blockers = new List<OptimizationIssue>(plan.Blockers);

        foreach (var operation in plan.OrderedOperations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var targetSummary = SummarizeTarget(operation);
            var wouldChange = true;

            if (_handlerRegistry.TryGetHandler(operation.OperationType, out var handler))
            {
                var verify = await handler.VerifyAsync(operation, cancellationToken).ConfigureAwait(false);
                if (verify.IsSuccess && verify.Value is not null)
                {
                    wouldChange = !verify.Value.Verified;
                }
            }
            else
            {
                blockers.Add(new OptimizationIssue(
                    "optimization.dry_run.handler_missing",
                    $"No handler exists for OperationType '{operation.OperationType}'.",
                    operation.OperationId.ToString()));
            }

            operations.Add(new DryRunOperation(
                operation.OperationId,
                operation.OperationType,
                targetSummary,
                wouldChange,
                operation.SnapshotRequirements.Any(requirement => requirement.Requirement == SnapshotRequirementKind.Required),
                operation.RebootBoundary != RebootBoundary.None,
                operation.Reversibility));
        }

        blockers.AddRange(validation.Issues);
        var result = new DryRunResult(plan, validation, operations, warnings, blockers.DistinctBy(issue => $"{issue.Code}:{issue.Scope}:{issue.Message}").ToArray());
        return Result<DryRunResult>.Success(result);
    }

    private static string SummarizeTarget(OperationSpec operation)
    {
        if (operation.RegistryValue is { } registry)
        {
            return $"{registry.Target.Hive}\\{registry.Target.KeyPath}\\{registry.Target.ValueName}";
        }

        return operation.OperationType.ToString();
    }
}
