using BorealBoost.Core.Analysis;
using BorealBoost.Core.Common;
using BorealBoost.Core.Identity;
using BorealBoost.Core.Optimization;
using BorealBoost.Core.Scanner;
using BorealBoost.Optimization.Catalog;

namespace BorealBoost.Optimization.Planning;

public sealed class ExecutionPlanner : IExecutionPlanner
{
    public const string EngineVersion = "4.0.0";
    public const string PlanSchemaVersion = "4.0.0";

    private readonly IOptimizationCatalog _catalog;
    private readonly IOptimizationDefinitionValidator _definitionValidator;

    public ExecutionPlanner(IOptimizationCatalog catalog, IOptimizationDefinitionValidator definitionValidator)
    {
        _catalog = catalog;
        _definitionValidator = definitionValidator;
    }

    public Result<ExecutionPlan> CreatePlan(
        SystemSnapshot snapshot,
        AnalysisResult analysis,
        RecommendationPlan recommendationPlan,
        IReadOnlyList<OptimizationId> selectedOptimizationIds)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(recommendationPlan);
        ArgumentNullException.ThrowIfNull(selectedOptimizationIds);

        if (analysis.ScanId != snapshot.Metadata.ScanId)
        {
            return Result<ExecutionPlan>.Failure("optimization.plan.scan_mismatch", "AnalysisResult does not match the selected SystemSnapshot.");
        }

        var warnings = new List<OptimizationIssue>();
        var blockers = new List<OptimizationIssue>();
        var selected = selectedOptimizationIds
            .Distinct()
            .OrderBy(id => id.Value, StringComparer.Ordinal)
            .ToArray();

        if (selected.Length == 0)
        {
            blockers.Add(new OptimizationIssue("optimization.plan.selection_empty", "No optimization was selected.", "ExecutionPlan"));
        }

        var definitions = new List<OptimizationDefinition>();
        foreach (var id in selected)
        {
            var definition = _catalog.Find(id);
            if (definition is null)
            {
                blockers.Add(new OptimizationIssue("optimization.plan.definition_missing", $"OptimizationDefinition '{id}' was not found.", id.ToString()));
                continue;
            }

            definitions.Add(definition);
            blockers.AddRange(_definitionValidator.Validate(definition));
        }

        var selectedSet = selected.ToHashSet();
        foreach (var definition in definitions)
        {
            foreach (var dependency in definition.Dependencies)
            {
                if (!selectedSet.Contains(dependency))
                {
                    blockers.Add(new OptimizationIssue("optimization.plan.dependency_missing", $"Optimization '{definition.OptimizationId}' requires '{dependency}'.", definition.OptimizationId.ToString()));
                }
            }

            foreach (var conflict in definition.Conflicts)
            {
                if (selectedSet.Contains(conflict))
                {
                    blockers.Add(new OptimizationIssue("optimization.plan.conflict_present", $"Optimization '{definition.OptimizationId}' conflicts with '{conflict}'.", definition.OptimizationId.ToString()));
                }
            }

            if (!IsSupported(definition, snapshot))
            {
                blockers.Add(new OptimizationIssue("optimization.plan.compatibility_blocked", $"Optimization '{definition.OptimizationId}' is not compatible with the current Windows snapshot.", definition.OptimizationId.ToString()));
            }
        }

        var operations = definitions
            .OrderBy(definition => definition.OptimizationId.Value, StringComparer.Ordinal)
            .SelectMany(definition => definition.OperationSpecs.OrderBy(operation => operation.OperationId.Value, StringComparer.Ordinal))
            .ToArray();

        foreach (var duplicate in operations
                     .GroupBy(operation => operation.OperationId.Value, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            blockers.Add(new OptimizationIssue("optimization.plan.operation_duplicate", $"Duplicate OperationId '{duplicate.Key}' in plan.", "ExecutionPlan"));
        }

        var dependencies = definitions
            .SelectMany(definition => definition.Dependencies.Select(dependsOn => new PlanDependency(definition.OptimizationId, dependsOn)))
            .ToArray();
        var conflicts = definitions
            .SelectMany(definition => definition.Conflicts.Select(conflict => new PlanConflict(definition.OptimizationId, conflict)))
            .ToArray();
        var snapshotRequirements = operations
            .SelectMany(operation => operation.SnapshotRequirements)
            .ToArray();
        var rebootBoundaries = operations
            .Select(operation => operation.RebootBoundary)
            .Distinct()
            .Order()
            .ToArray();
        var riskSummary = BuildRiskSummary(definitions);
        var requiresRestart = definitions.Any(definition => definition.RequiresRestart) ||
                              operations.Any(operation => operation.RebootBoundary != RebootBoundary.None);
        var restorePointRequirement = definitions.Any(definition => definition.RestorePointRequirement == RestorePointRequirement.Required)
            ? RestorePointRequirement.Required
            : definitions.Any(definition => definition.RestorePointRequirement == RestorePointRequirement.BestEffort)
                ? RestorePointRequirement.BestEffort
                : RestorePointRequirement.NotRequired;

        var planId = ExecutionPlanId.New();
        var sessionId = SessionId.New();
        var plan = new ExecutionPlan(
            planId,
            sessionId,
            snapshot.Metadata.ScanId,
            analysis.AnalysisId,
            PlanSchemaVersion,
            EngineVersion,
            _catalog.CatalogVersion,
            DateTimeOffset.UtcNow,
            snapshot.OperatingSystem.Name,
            snapshot.OperatingSystem.Build,
            snapshot.OperatingSystem.Architecture,
            selected,
            operations,
            dependencies,
            conflicts,
            riskSummary,
            definitions.Any(definition => definition.RequiresElevation),
            requiresRestart,
            restorePointRequirement,
            snapshotRequirements,
            rebootBoundaries,
            Math.Max(1, operations.Length * 3),
            warnings,
            blockers,
            string.Empty,
            IsApproved: false);

        return Result<ExecutionPlan>.Success(plan with { PlanHash = ExecutionPlanHasher.Compute(plan) });
    }

    private static bool IsSupported(OptimizationDefinition definition, SystemSnapshot snapshot)
    {
        if (!definition.SupportedWindows.CompatibilityStatuses.Contains(snapshot.OperatingSystem.BorealBoostCompatibility))
        {
            return false;
        }

        if (definition.SupportedWindows.MinimumBuild is { } min && snapshot.OperatingSystem.Build < min)
        {
            return false;
        }

        if (definition.SupportedWindows.MaximumBuild is { } max && snapshot.OperatingSystem.Build > max)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(definition.SupportedWindows.Architecture) ||
               string.Equals(definition.SupportedWindows.Architecture, snapshot.OperatingSystem.Architecture, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(definition.SupportedWindows.Architecture, snapshot.Metadata.MachineArchitecture, StringComparison.OrdinalIgnoreCase);
    }

    private static RiskSummary BuildRiskSummary(IReadOnlyList<OptimizationDefinition> definitions)
    {
        var counts = Enum.GetValues<OptimizationRiskLevel>()
            .ToDictionary(risk => risk, risk => definitions.Count(definition => definition.RiskLevel == risk));
        var highest = definitions.Count == 0
            ? OptimizationRiskLevel.Safe
            : definitions.Max(definition => definition.RiskLevel);
        return new RiskSummary(counts, highest);
    }

}
