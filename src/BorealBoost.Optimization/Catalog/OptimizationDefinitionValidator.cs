using BorealBoost.Core.Analysis;
using BorealBoost.Core.Optimization;

namespace BorealBoost.Optimization.Catalog;

public sealed class OptimizationDefinitionValidator : IOptimizationDefinitionValidator
{
    private readonly AgentOperationSecurityValidator _operationValidator = new();

    public IReadOnlyList<OptimizationIssue> Validate(OptimizationDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var issues = new List<OptimizationIssue>();
        if (!OptimizationId.TryCreate(definition.OptimizationId.Value, out _))
        {
            issues.Add(Issue("optimization.definition.id_invalid", "OptimizationId is invalid.", definition.OptimizationId.ToString()));
        }

        if (string.IsNullOrWhiteSpace(definition.Version))
        {
            issues.Add(Issue("optimization.definition.version_missing", "Version is required.", definition.OptimizationId.ToString()));
        }

        if (!Enum.IsDefined(definition.Category))
        {
            issues.Add(Issue("optimization.definition.category_invalid", "Category is invalid.", definition.OptimizationId.ToString()));
        }

        if (!Enum.IsDefined(definition.TechnicalCategory) ||
            !Enum.IsDefined(definition.ConfigurationEvidence) ||
            !Enum.IsDefined(definition.PerformanceRelevance) ||
            !Enum.IsDefined(definition.AutomaticPresetSuitability) ||
            !Enum.IsDefined(definition.UserPreferenceImpact) ||
            !Enum.IsDefined(definition.ConfigurationMechanism) ||
            !Enum.IsDefined(definition.ActivationBoundary) ||
            !Enum.IsDefined(definition.VerificationLevel) ||
            !Enum.IsDefined(definition.RollbackValidationLevel) ||
            !Enum.IsDefined(definition.Windows10ValidationLevel) ||
            !Enum.IsDefined(definition.Windows11ValidationLevel))
        {
            issues.Add(Issue("optimization.definition.classification_invalid", "One or more catalog classification fields are invalid.", definition.OptimizationId.ToString()));
        }

        if (!Enum.IsDefined(definition.RiskLevel))
        {
            issues.Add(Issue("optimization.definition.risk_invalid", "RiskLevel is invalid.", definition.OptimizationId.ToString()));
        }

        if (!Enum.IsDefined(definition.EvidenceLevel) || definition.EvidenceLevel == OptimizationEvidenceLevel.Unknown)
        {
            issues.Add(Issue("optimization.definition.evidence_invalid", "EvidenceLevel must be known.", definition.OptimizationId.ToString()));
        }

        if (!Enum.IsDefined(definition.ExpectedImpact))
        {
            issues.Add(Issue("optimization.definition.impact_invalid", "ExpectedImpact is invalid.", definition.OptimizationId.ToString()));
        }

        if (definition.Category != OptimizationCategory.IntegrationTest &&
            definition.EvidenceReferences.Count == 0)
        {
            issues.Add(Issue("optimization.definition.evidence_references_missing", "Catalog optimizations require documented evidence references.", definition.OptimizationId.ToString()));
        }

        if (!ValidPresetFlags(definition.PresetEligibility))
        {
            issues.Add(Issue("optimization.definition.preset_invalid", "PresetEligibility contains invalid flags.", definition.OptimizationId.ToString()));
        }

        if (definition.PresetEligibility.HasFlag(RecommendationPresetEligibility.Basic) &&
            (definition.RiskLevel != OptimizationRiskLevel.Safe ||
             definition.AutomaticPresetSuitability != AutomaticPresetSuitability.Automatic ||
             definition.EvidenceLevel == OptimizationEvidenceLevel.Experimental ||
             definition.IsSecurityTradeoff ||
             !definition.SupportsUndo ||
             definition.RequiresRestart))
        {
            issues.Add(Issue("optimization.definition.basic_policy_invalid", "Basic preset eligibility is allowed only for Safe, automatic, reversible, non-security-tradeoff, non-restart optimizations.", definition.OptimizationId.ToString()));
        }

        if (definition.PresetEligibility.HasFlag(RecommendationPresetEligibility.Medium) &&
            (definition.RiskLevel > OptimizationRiskLevel.Medium ||
             definition.AutomaticPresetSuitability is AutomaticPresetSuitability.CustomOnly or AutomaticPresetSuitability.AdvancedOnly ||
             definition.EvidenceLevel == OptimizationEvidenceLevel.Experimental ||
             definition.IsSecurityTradeoff ||
             !definition.SupportsUndo))
        {
            issues.Add(Issue("optimization.definition.medium_policy_invalid", "Medium preset eligibility is allowed only for Safe/Medium, reversible, non-security-tradeoff optimizations with known evidence.", definition.OptimizationId.ToString()));
        }

        if (definition.SupportsUndo && definition.RollbackSpecs.Count == 0)
        {
            issues.Add(Issue("optimization.definition.rollback_missing", "SupportsUndo requires rollback specs.", definition.OptimizationId.ToString()));
        }

        if (definition.OperationSpecs.Count == 0)
        {
            issues.Add(Issue("optimization.definition.operations_missing", "At least one OperationSpec is required.", definition.OptimizationId.ToString()));
        }

        foreach (var duplicate in definition.OperationSpecs
                     .GroupBy(operation => operation.OperationId.Value, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            issues.Add(Issue("optimization.definition.operation_duplicate", $"Duplicate OperationId '{duplicate.Key}'.", definition.OptimizationId.ToString()));
        }

        foreach (var operation in definition.OperationSpecs)
        {
            ValidateOperationSpec(definition, operation, issues);
        }

        var operationIds = definition.OperationSpecs
            .Select(operation => operation.OperationId)
            .ToHashSet();

        foreach (var spec in definition.VerificationSpecs)
        {
            if (!operationIds.Contains(spec.OperationId))
            {
                issues.Add(Issue("optimization.definition.verify_unknown_operation", $"Verification references unknown OperationId '{spec.OperationId}'.", definition.OptimizationId.ToString()));
            }
        }

        foreach (var spec in definition.RollbackSpecs)
        {
            if (!operationIds.Contains(spec.OperationId))
            {
                issues.Add(Issue("optimization.definition.rollback_unknown_operation", $"Rollback references unknown OperationId '{spec.OperationId}'.", definition.OptimizationId.ToString()));
            }
        }

        return issues;
    }

    private void ValidateOperationSpec(
        OptimizationDefinition definition,
        OperationSpec operation,
        List<OptimizationIssue> issues)
    {
        var scope = $"{definition.OptimizationId}:{operation.OperationId}";
        if (!OperationId.TryCreate(operation.OperationId.Value, out _))
        {
            issues.Add(Issue("optimization.operation.id_invalid", "OperationId is invalid.", scope));
        }

        if (!Enum.IsDefined(operation.OperationType))
        {
            issues.Add(Issue("optimization.operation.type_invalid", "OperationType is invalid.", scope));
        }

        if (!Enum.IsDefined(operation.Idempotency) ||
            !Enum.IsDefined(operation.Reversibility) ||
            !Enum.IsDefined(operation.RebootBoundary) ||
            !Enum.IsDefined(operation.FailurePolicy) ||
            !Enum.IsDefined(operation.VerificationStrategy.Kind) ||
            !Enum.IsDefined(operation.RollbackStrategy.Kind))
        {
            issues.Add(Issue("optimization.operation.policy_invalid", "One or more OperationSpec policies are invalid.", scope));
        }

        if (operation.TimeoutPolicy.Timeout <= TimeSpan.Zero)
        {
            issues.Add(Issue("optimization.operation.timeout_invalid", "Operation timeout must be positive.", scope));
        }

        if (operation.RetryPolicy.MaxAttempts < 1)
        {
            issues.Add(Issue("optimization.operation.retry_invalid", "Operation retry policy must allow at least one attempt.", scope));
        }

        if (operation.Reversibility == OperationReversibility.Full &&
            operation.SnapshotRequirements.All(requirement => requirement.Requirement != SnapshotRequirementKind.Required) &&
            operation.RollbackStrategy.Kind != OperationRollbackKind.InverseOperation)
        {
            issues.Add(Issue("optimization.operation.snapshot_missing", "Fully reversible operation requires snapshot or trusted inverse operation.", scope));
        }

        var security = _operationValidator.Validate(operation);
        if (security.IsFailure)
        {
            issues.Add(Issue(security.ErrorCode ?? "optimization.operation.rejected", security.ErrorMessage ?? "Operation failed Agent security validation.", scope));
        }
    }

    private static OptimizationIssue Issue(string code, string message, string scope)
    {
        return new OptimizationIssue(code, message, scope);
    }

    private static bool ValidPresetFlags(RecommendationPresetEligibility value)
    {
        var allowed = RecommendationPresetEligibility.Basic |
                      RecommendationPresetEligibility.Medium |
                      RecommendationPresetEligibility.Advanced |
                      RecommendationPresetEligibility.Custom;
        return (value & ~allowed) == 0;
    }
}
