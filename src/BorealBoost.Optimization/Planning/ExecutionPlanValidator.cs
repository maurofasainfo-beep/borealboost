using BorealBoost.Core.Optimization;
using BorealBoost.Core.Scanner;
using BorealBoost.Optimization.Catalog;

namespace BorealBoost.Optimization.Planning;

public sealed class ExecutionPlanValidator : IExecutionPlanValidator
{
    private readonly IOptimizationCatalog _catalog;
    private readonly IOperationHandlerRegistry _handlerRegistry;
    private readonly AgentOperationSecurityValidator _operationValidator = new();
    private readonly CanonicalOperationSpecValidator _canonicalValidator;

    public ExecutionPlanValidator(IOptimizationCatalog catalog, IOperationHandlerRegistry handlerRegistry)
    {
        _catalog = catalog;
        _handlerRegistry = handlerRegistry;
        _canonicalValidator = new CanonicalOperationSpecValidator(catalog);
    }

    public ExecutionPlanValidationResult Validate(ExecutionPlan plan, SystemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(snapshot);

        var issues = new List<OptimizationIssue>();
        var status = ExecutionPlanValidationStatus.Valid;

        if (plan.SchemaVersion != ExecutionPlanner.PlanSchemaVersion)
        {
            issues.Add(Issue("optimization.plan.schema_unsupported", "ExecutionPlan schema version is unsupported.", "ExecutionPlan"));
            status = ExecutionPlanValidationStatus.Invalid;
        }

        if (plan.CatalogVersion != _catalog.CatalogVersion)
        {
            issues.Add(Issue("optimization.plan.catalog_mismatch", "ExecutionPlan catalog version does not match loaded catalog.", "ExecutionPlan"));
            status = ExecutionPlanValidationStatus.NeedsRevalidation;
        }

        if (!ExecutionPlanHasher.IsValidHash(plan.PlanHash) ||
            !string.Equals(ExecutionPlanHasher.Compute(plan), plan.PlanHash, StringComparison.Ordinal))
        {
            issues.Add(Issue("optimization.plan.hash_mismatch", "ExecutionPlan hash does not match the canonical approved plan representation.", "ExecutionPlan"));
            status = ExecutionPlanValidationStatus.Invalid;
        }

        if (plan.ScanId != snapshot.Metadata.ScanId ||
            !string.Equals(plan.TargetArchitecture, snapshot.OperatingSystem.Architecture, StringComparison.OrdinalIgnoreCase) ||
            plan.TargetBuild != snapshot.OperatingSystem.Build)
        {
            issues.Add(Issue("optimization.plan.stale", "ExecutionPlan target snapshot no longer matches current facts.", "ExecutionPlan"));
            status = ExecutionPlanValidationStatus.NeedsRevalidation;
        }

        if (plan.OrderedOperations.Count == 0)
        {
            issues.Add(Issue("optimization.plan.operations_missing", "ExecutionPlan contains no operations.", "ExecutionPlan"));
            status = ExecutionPlanValidationStatus.Invalid;
        }

        foreach (var duplicate in plan.OrderedOperations
                     .GroupBy(operation => operation.OperationId.Value, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            issues.Add(Issue("optimization.plan.operation_duplicate", $"Duplicate OperationId '{duplicate.Key}'.", "ExecutionPlan"));
            status = ExecutionPlanValidationStatus.Invalid;
        }

        var catalogOperationIds = new HashSet<OperationId>();
        foreach (var optimizationId in plan.SelectedOptimizationIds)
        {
            var definition = _catalog.Find(optimizationId);
            if (definition is null)
            {
                issues.Add(Issue("optimization.plan.definition_missing", $"OptimizationDefinition '{optimizationId}' was not found.", optimizationId.ToString()));
                status = ExecutionPlanValidationStatus.Invalid;
                continue;
            }

            if (!definition.SupportedWindows.CompatibilityStatuses.Contains(snapshot.OperatingSystem.BorealBoostCompatibility))
            {
                issues.Add(Issue("optimization.plan.compatibility_blocked", $"Optimization '{optimizationId}' is not compatible with this Windows state.", optimizationId.ToString()));
                status = ExecutionPlanValidationStatus.Blocked;
            }

            foreach (var operation in definition.OperationSpecs)
            {
                catalogOperationIds.Add(operation.OperationId);
            }
        }

        foreach (var operation in plan.OrderedOperations)
        {
            if (!catalogOperationIds.Contains(operation.OperationId))
            {
                issues.Add(Issue("optimization.plan.operation_not_in_catalog", $"OperationId '{operation.OperationId}' is not present in the selected catalog definitions.", operation.OperationId.ToString()));
                status = ExecutionPlanValidationStatus.Invalid;
            }

            var canonicalIssues = _canonicalValidator.Validate(plan.CatalogVersion, ResolveOptimizationId(plan, operation), operation);
            if (canonicalIssues.Count > 0)
            {
                issues.AddRange(canonicalIssues.Select(issue => issue with { Code = issue.Code.Replace("agent.", "optimization.", StringComparison.Ordinal) }));
                status = ExecutionPlanValidationStatus.Invalid;
            }
        }

        var selectedSet = plan.SelectedOptimizationIds.ToHashSet();
        foreach (var dependency in plan.Dependencies)
        {
            if (!selectedSet.Contains(dependency.DependsOn))
            {
                issues.Add(Issue("optimization.plan.dependency_missing", $"Missing dependency '{dependency.DependsOn}'.", dependency.OptimizationId.ToString()));
                status = ExecutionPlanValidationStatus.Invalid;
            }
        }

        foreach (var conflict in plan.Conflicts)
        {
            if (selectedSet.Contains(conflict.ConflictsWith))
            {
                issues.Add(Issue("optimization.plan.conflict_present", $"Conflicting optimization '{conflict.ConflictsWith}' is selected.", conflict.OptimizationId.ToString()));
                status = ExecutionPlanValidationStatus.Invalid;
            }
        }

        foreach (var operation in plan.OrderedOperations)
        {
            if (!_handlerRegistry.TryGetHandler(operation.OperationType, out _))
            {
                issues.Add(Issue("optimization.plan.handler_missing", $"No handler exists for OperationType '{operation.OperationType}'.", operation.OperationId.ToString()));
                status = ExecutionPlanValidationStatus.Invalid;
                continue;
            }

            var validation = _operationValidator.Validate(operation);
            if (validation.IsFailure)
            {
                issues.Add(Issue(validation.ErrorCode ?? "optimization.operation.rejected", validation.ErrorMessage ?? "Operation rejected.", operation.OperationId.ToString()));
                status = ExecutionPlanValidationStatus.Invalid;
            }
        }

        if (plan.Blockers.Count > 0)
        {
            issues.AddRange(plan.Blockers);
            status = status == ExecutionPlanValidationStatus.Valid ? ExecutionPlanValidationStatus.Invalid : status;
        }

        return new ExecutionPlanValidationResult(status, issues);
    }

    private static OptimizationIssue Issue(string code, string message, string scope)
    {
        return new OptimizationIssue(code, message, scope);
    }

    private OptimizationId ResolveOptimizationId(ExecutionPlan plan, OperationSpec operation)
    {
        foreach (var optimizationId in plan.SelectedOptimizationIds)
        {
            var definition = _catalog.Find(optimizationId);
            if (definition?.OperationSpecs.Any(candidate => candidate.OperationId == operation.OperationId) == true)
            {
                return optimizationId;
            }
        }

        return plan.SelectedOptimizationIds.FirstOrDefault();
    }
}
