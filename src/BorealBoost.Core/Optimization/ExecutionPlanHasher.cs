using System.Security.Cryptography;
using System.Text;

namespace BorealBoost.Core.Optimization;

public static class ExecutionPlanHasher
{
    public static string Compute(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var builder = new StringBuilder();
        Append(builder, "schema", plan.SchemaVersion);
        Append(builder, "engine", plan.EngineVersion);
        Append(builder, "catalog", plan.CatalogVersion);
        Append(builder, "planId", plan.PlanId.ToString());
        Append(builder, "sessionId", plan.SessionId.ToString());
        Append(builder, "scanId", plan.ScanId.ToString());
        Append(builder, "analysisId", plan.AnalysisId.ToString());
        Append(builder, "createdAt", plan.CreatedAtUtc.ToUniversalTime().ToString("O"));
        Append(builder, "os", plan.TargetOperatingSystem ?? string.Empty);
        Append(builder, "build", plan.TargetBuild?.ToString(global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        Append(builder, "arch", plan.TargetArchitecture);
        Append(builder, "requiresElevation", plan.RequiresElevation.ToString());
        Append(builder, "requiresRestart", plan.RequiresRestart.ToString());
        Append(builder, "restorePoint", plan.RestorePointRequirement.ToString());
        Append(builder, "estimatedSteps", plan.EstimatedStepCount.ToString(global::System.Globalization.CultureInfo.InvariantCulture));

        AppendIds(builder, "selected", plan.SelectedOptimizationIds.Select(id => id.Value));
        AppendOperations(builder, plan.OrderedOperations);
        AppendDependencies(builder, plan.Dependencies);
        AppendConflicts(builder, plan.Conflicts);
        AppendRisk(builder, plan.RiskSummary);
        AppendSnapshotRequirements(builder, "planSnapshots", plan.SnapshotRequirements);
        AppendValues(builder, "reboot", plan.RebootBoundaries.Select(boundary => boundary.ToString()));
        AppendIssues(builder, "warnings", plan.Warnings);
        AppendIssues(builder, "blockers", plan.Blockers);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    public static bool IsValid(ExecutionPlan plan)
    {
        return IsValidHash(plan.PlanHash) &&
               string.Equals(Compute(plan), plan.PlanHash, StringComparison.Ordinal);
    }

    public static bool IsValidHash(string? hash)
    {
        return !string.IsNullOrWhiteSpace(hash) &&
               hash.Length == 64 &&
               hash.All(character =>
                   character is >= '0' and <= '9' ||
                   character is >= 'A' and <= 'F');
    }

    private static void AppendOperations(StringBuilder builder, IReadOnlyList<OperationSpec> operations)
    {
        Append(builder, "operationCount", operations.Count.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        for (var index = 0; index < operations.Count; index++)
        {
            var operation = operations[index];
            Append(builder, $"operation[{index}].id", operation.OperationId.Value);
            Append(builder, $"operation[{index}].type", operation.OperationType.ToString());
            AppendRegistryValue(builder, $"operation[{index}].registry", operation.RegistryValue);
            Append(builder, $"operation[{index}].timeout", operation.TimeoutPolicy.Timeout.Ticks.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            Append(builder, $"operation[{index}].retryAllowed", operation.RetryPolicy.RetryAllowed.ToString());
            Append(builder, $"operation[{index}].retryMax", operation.RetryPolicy.MaxAttempts.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            Append(builder, $"operation[{index}].retryBackoff", operation.RetryPolicy.Backoff.Ticks.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            AppendValues(builder, $"operation[{index}].retryable", operation.RetryPolicy.RetryableFailures.Select(value => value.ToString()));
            Append(builder, $"operation[{index}].idempotency", operation.Idempotency.ToString());
            Append(builder, $"operation[{index}].reversibility", operation.Reversibility.ToString());
            Append(builder, $"operation[{index}].reboot", operation.RebootBoundary.ToString());
            Append(builder, $"operation[{index}].failure", operation.FailurePolicy.ToString());
            Append(builder, $"operation[{index}].verify.kind", operation.VerificationStrategy.Kind.ToString());
            Append(builder, $"operation[{index}].verify.description", operation.VerificationStrategy.Description);
            Append(builder, $"operation[{index}].rollback.kind", operation.RollbackStrategy.Kind.ToString());
            Append(builder, $"operation[{index}].rollback.description", operation.RollbackStrategy.Description);
            AppendSnapshotRequirements(builder, $"operation[{index}].snapshots", operation.SnapshotRequirements);
        }
    }

    private static void AppendRegistryValue(StringBuilder builder, string prefix, RegistryValueOperationParameters? registryValue)
    {
        if (registryValue is null)
        {
            Append(builder, prefix, "null");
            return;
        }

        Append(builder, $"{prefix}.hive", registryValue.Target.Hive.ToString());
        Append(builder, $"{prefix}.key", registryValue.Target.KeyPath);
        Append(builder, $"{prefix}.valueName", registryValue.Target.ValueName);
        Append(builder, $"{prefix}.view", registryValue.Target.View.ToString());
        AppendRegistryState(builder, $"{prefix}.desired", registryValue.DesiredState);
    }

    private static void AppendRegistryState(StringBuilder builder, string prefix, RegistryValueState state)
    {
        Append(builder, $"{prefix}.exists", state.Exists.ToString());
        Append(builder, $"{prefix}.kind", state.ValueKind.ToString());
        Append(builder, $"{prefix}.string", state.StringValue ?? string.Empty);
        Append(builder, $"{prefix}.dword", state.DWordValue?.ToString(global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        Append(builder, $"{prefix}.qword", state.QWordValue?.ToString(global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        AppendValues(builder, $"{prefix}.multi", state.MultiStringValue ?? []);
        Append(builder, $"{prefix}.binary", state.BinaryValue is null ? string.Empty : Convert.ToHexString(state.BinaryValue));
    }

    private static void AppendDependencies(StringBuilder builder, IReadOnlyList<PlanDependency> dependencies)
    {
        Append(builder, "dependencyCount", dependencies.Count.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        foreach (var dependency in dependencies.OrderBy(value => value.OptimizationId.Value, StringComparer.Ordinal).ThenBy(value => value.DependsOn.Value, StringComparer.Ordinal))
        {
            Append(builder, "dependency", $"{dependency.OptimizationId.Value}->{dependency.DependsOn.Value}");
        }
    }

    private static void AppendConflicts(StringBuilder builder, IReadOnlyList<PlanConflict> conflicts)
    {
        Append(builder, "conflictCount", conflicts.Count.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        foreach (var conflict in conflicts.OrderBy(value => value.OptimizationId.Value, StringComparer.Ordinal).ThenBy(value => value.ConflictsWith.Value, StringComparer.Ordinal))
        {
            Append(builder, "conflict", $"{conflict.OptimizationId.Value}->{conflict.ConflictsWith.Value}");
        }
    }

    private static void AppendRisk(StringBuilder builder, RiskSummary riskSummary)
    {
        Append(builder, "risk.highest", riskSummary.HighestRisk.ToString());
        foreach (var risk in Enum.GetValues<OptimizationRiskLevel>())
        {
            riskSummary.OperationCountByRisk.TryGetValue(risk, out var count);
            Append(builder, $"risk.{risk}", count.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private static void AppendSnapshotRequirements(StringBuilder builder, string prefix, IReadOnlyList<SnapshotRequirement> requirements)
    {
        Append(builder, $"{prefix}.count", requirements.Count.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        for (var index = 0; index < requirements.Count; index++)
        {
            var requirement = requirements[index];
            Append(builder, $"{prefix}[{index}].resource", requirement.ResourceType.ToString());
            Append(builder, $"{prefix}[{index}].kind", requirement.Requirement.ToString());
            Append(builder, $"{prefix}[{index}].method", requirement.CaptureMethod);
            Append(builder, $"{prefix}[{index}].block", requirement.BlockIfUnavailable.ToString());
            Append(builder, $"{prefix}[{index}].classification", requirement.DataClassification);
        }
    }

    private static void AppendIssues(StringBuilder builder, string prefix, IReadOnlyList<OptimizationIssue> issues)
    {
        Append(builder, $"{prefix}.count", issues.Count.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        foreach (var issue in issues.OrderBy(issue => issue.Code, StringComparer.Ordinal).ThenBy(issue => issue.Scope, StringComparer.Ordinal).ThenBy(issue => issue.Message, StringComparer.Ordinal))
        {
            Append(builder, prefix, $"{issue.Code}|{issue.Scope}|{issue.Category}|{issue.Message}");
        }
    }

    private static void AppendIds(StringBuilder builder, string key, IEnumerable<string> values)
    {
        AppendValues(builder, key, values);
    }

    private static void AppendValues(StringBuilder builder, string key, IEnumerable<string> values)
    {
        var valuesArray = values.ToArray();
        Append(builder, key + ".count", valuesArray.Length.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        foreach (var value in valuesArray)
        {
            Append(builder, key, value);
        }
    }

    private static void Append(StringBuilder builder, string key, string value)
    {
        builder
            .Append(key.Length)
            .Append(':')
            .Append(key)
            .Append('=')
            .Append(value.Length)
            .Append(':')
            .Append(value)
            .Append('\n');
    }
}
