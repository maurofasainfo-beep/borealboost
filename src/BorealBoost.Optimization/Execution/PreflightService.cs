using BorealBoost.Core.Optimization;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Optimization.Execution;

public sealed class PreflightService : IPreflightService
{
    private readonly IExecutionPlanValidator _planValidator;
    private readonly IOperationHandlerRegistry _handlerRegistry;

    public PreflightService(IExecutionPlanValidator planValidator, IOperationHandlerRegistry handlerRegistry)
    {
        _planValidator = planValidator;
        _handlerRegistry = handlerRegistry;
    }

    public Task<PreflightResult> CheckAsync(ExecutionPlan plan, SystemSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();

        var issues = new List<OptimizationIssue>();
        var validation = _planValidator.Validate(plan, snapshot);
        issues.AddRange(validation.Issues);

        if (!plan.IsApproved)
        {
            issues.Add(new OptimizationIssue(
                "optimization.preflight.plan_not_approved",
                "ExecutionPlan must be approved before preflight can allow mutation.",
                plan.PlanId.ToString()));
        }

        foreach (var operation in plan.OrderedOperations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_handlerRegistry.TryGetHandler(operation.OperationType, out var handler))
            {
                issues.Add(new OptimizationIssue(
                    "optimization.preflight.handler_missing",
                    $"No handler exists for OperationType '{operation.OperationType}'.",
                    operation.OperationId.ToString()));
                continue;
            }

            var handlerValidation = handler.Validate(operation);
            if (handlerValidation.IsFailure)
            {
                issues.Add(new OptimizationIssue(
                    handlerValidation.ErrorCode ?? "optimization.preflight.operation_invalid",
                    handlerValidation.ErrorMessage ?? "Operation failed handler validation.",
                    operation.OperationId.ToString()));
            }
        }

        if (plan.RestorePointRequirement == RestorePointRequirement.Required &&
            plan.OrderedOperations.Count == 0)
        {
            issues.Add(new OptimizationIssue(
                "optimization.preflight.restore_point_without_operations",
                "Restore point cannot be required for an empty operation plan.",
                plan.PlanId.ToString()));
        }

        var result = new PreflightResult(
            plan,
            issues.Count == 0 && validation.Status == ExecutionPlanValidationStatus.Valid,
            DateTimeOffset.UtcNow,
            issues.DistinctBy(issue => $"{issue.Code}:{issue.Scope}:{issue.Message}").ToArray());

        return Task.FromResult(result);
    }
}
