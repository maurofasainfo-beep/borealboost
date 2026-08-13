using BorealBoost.Core.Analysis;

namespace BorealBoost.Analysis.RecommendationEngine;

internal static class RecommendationModelValidator
{
    private const int MaxIdentifierLength = 128;

    public static IReadOnlyList<AnalysisIssue> ValidateEvaluations(IReadOnlyList<AnalysisRuleEvaluation> evaluations)
    {
        var issues = new List<AnalysisIssue>();
        var recommendations = evaluations.SelectMany(evaluation => evaluation.Recommendations).ToArray();
        var recommendationIds = recommendations
            .Where(recommendation => IsValidBorealId(recommendation.RecommendationId))
            .Select(recommendation => recommendation.RecommendationId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var evaluation in evaluations)
        {
            ValidateEvaluation(evaluation, issues);
        }

        foreach (var recommendation in recommendations)
        {
            ValidateRecommendation(recommendation, recommendationIds, issues);
        }

        foreach (var duplicate in recommendations
                     .GroupBy(recommendation => recommendation.RecommendationId, StringComparer.OrdinalIgnoreCase)
                     .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var ruleIds = duplicate
                .Select(recommendation => recommendation.RuleId)
                .Order(StringComparer.Ordinal)
                .ToArray();
            issues.Add(new AnalysisIssue(
                "analysis.recommendation.duplicate_id",
                $"Duplicate RecommendationId '{duplicate.Key}' emitted by rules: {string.Join(", ", ruleIds)}.",
                "AnalysisCatalog"));
        }

        return issues;
    }

    public static IReadOnlyList<AnalysisIssue> ValidatePlan(RecommendationPlan plan)
    {
        var issues = new List<AnalysisIssue>();
        if (string.IsNullOrWhiteSpace(plan.PlanVersion))
        {
            issues.Add(new AnalysisIssue("analysis.plan.version_missing", "RecommendationPlan.PlanVersion is required.", "AnalysisCatalog"));
        }

        var recommendationIds = plan.Recommendations
            .Select(recommendation => recommendation.RecommendationId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var preset in plan.Presets)
        {
            if (!Enum.IsDefined(preset.Preset))
            {
                issues.Add(new AnalysisIssue("analysis.plan.invalid_preset", $"Preset '{preset.Preset}' is not valid.", "AnalysisCatalog"));
            }

            if (preset.EligibleRecommendationCount != preset.RecommendationIds.Count)
            {
                issues.Add(new AnalysisIssue(
                    "analysis.plan.preset_count_mismatch",
                    $"Preset '{preset.Preset}' count does not match its recommendation id list.",
                    "AnalysisCatalog"));
            }

            foreach (var id in preset.RecommendationIds)
            {
                if (!recommendationIds.Contains(id))
                {
                    issues.Add(new AnalysisIssue(
                        "analysis.plan.unknown_recommendation",
                        $"Preset '{preset.Preset}' references unknown RecommendationId '{id}'.",
                        "AnalysisCatalog"));
                }
            }
        }

        return issues;
    }

    private static void ValidateEvaluation(AnalysisRuleEvaluation evaluation, List<AnalysisIssue> issues)
    {
        if (!IsValidBorealId(evaluation.Rule.RuleId))
        {
            issues.Add(new AnalysisIssue("analysis.rule.invalid_id", $"RuleId '{evaluation.Rule.RuleId}' is invalid.", evaluation.Rule.RuleId));
        }

        if (!Enum.IsDefined(evaluation.Rule.Category))
        {
            issues.Add(new AnalysisIssue("analysis.rule.invalid_category", $"Rule '{evaluation.Rule.RuleId}' has an invalid category.", evaluation.Rule.RuleId));
        }

        if (!Enum.IsDefined(evaluation.Status))
        {
            issues.Add(new AnalysisIssue("analysis.rule.invalid_status", $"Rule '{evaluation.Rule.RuleId}' returned an invalid status.", evaluation.Rule.RuleId));
        }

        if ((evaluation.Status is AnalysisRuleStatus.Healthy or AnalysisRuleStatus.Unknown or AnalysisRuleStatus.NotApplicable) &&
            evaluation.Recommendations.Count > 0)
        {
            issues.Add(new AnalysisIssue(
                "analysis.rule.status_recommendation_conflict",
                $"Rule '{evaluation.Rule.RuleId}' returned {evaluation.Status} with recommendations.",
                evaluation.Rule.RuleId));
        }

        foreach (var recommendation in evaluation.Recommendations)
        {
            if (!recommendation.RuleId.Equals(evaluation.Rule.RuleId, StringComparison.Ordinal))
            {
                issues.Add(new AnalysisIssue(
                    "analysis.recommendation.rule_mismatch",
                    $"Recommendation '{recommendation.RecommendationId}' has RuleId '{recommendation.RuleId}' but was emitted by '{evaluation.Rule.RuleId}'.",
                    evaluation.Rule.RuleId));
            }
        }
    }

    private static void ValidateRecommendation(
        Recommendation recommendation,
        IReadOnlySet<string> recommendationIds,
        List<AnalysisIssue> issues)
    {
        var ruleId = string.IsNullOrWhiteSpace(recommendation.RuleId) ? "AnalysisCatalog" : recommendation.RuleId;
        if (!IsValidBorealId(recommendation.RecommendationId))
        {
            issues.Add(new AnalysisIssue(
                "analysis.recommendation.invalid_id",
                $"RecommendationId '{recommendation.RecommendationId}' is invalid.",
                ruleId));
        }

        if (!IsValidBorealId(recommendation.RuleId))
        {
            issues.Add(new AnalysisIssue(
                "analysis.recommendation.invalid_rule_id",
                $"Recommendation '{recommendation.RecommendationId}' has invalid RuleId '{recommendation.RuleId}'.",
                ruleId));
        }

        ValidateRequiredText(recommendation.Title, "Title", recommendation, issues);
        ValidateRequiredText(recommendation.ShortDescription, "ShortDescription", recommendation, issues);
        ValidateRequiredText(recommendation.TechnicalReason, "TechnicalReason", recommendation, issues);
        ValidateRequiredText(recommendation.DetectedState, "DetectedState", recommendation, issues);
        ValidateRequiredText(recommendation.DesiredState, "DesiredState", recommendation, issues);

        if (!Enum.IsDefined(recommendation.Category))
        {
            issues.Add(new AnalysisIssue("analysis.recommendation.invalid_category", $"Recommendation '{recommendation.RecommendationId}' has invalid category.", ruleId));
        }

        if (!Enum.IsDefined(recommendation.RiskLevel))
        {
            issues.Add(new AnalysisIssue("analysis.recommendation.invalid_risk", $"Recommendation '{recommendation.RecommendationId}' has invalid risk level.", ruleId));
        }

        if (!Enum.IsDefined(recommendation.EvidenceLevel))
        {
            issues.Add(new AnalysisIssue("analysis.recommendation.invalid_evidence", $"Recommendation '{recommendation.RecommendationId}' has invalid evidence level.", ruleId));
        }

        if (!Enum.IsDefined(recommendation.ExpectedImpact))
        {
            issues.Add(new AnalysisIssue("analysis.recommendation.invalid_impact", $"Recommendation '{recommendation.RecommendationId}' has invalid expected impact.", ruleId));
        }

        if (!Enum.IsDefined(recommendation.Reversible))
        {
            issues.Add(new AnalysisIssue("analysis.recommendation.invalid_reversibility", $"Recommendation '{recommendation.RecommendationId}' has invalid reversibility.", ruleId));
        }

        if (recommendation.Compatibility is null)
        {
            issues.Add(new AnalysisIssue("analysis.recommendation.compatibility_missing", $"Recommendation '{recommendation.RecommendationId}' has no compatibility metadata.", ruleId));
        }
        else
        {
            if (!Enum.IsDefined(recommendation.Compatibility.Status))
            {
                issues.Add(new AnalysisIssue("analysis.recommendation.invalid_compatibility", $"Recommendation '{recommendation.RecommendationId}' has invalid compatibility.", ruleId));
            }

            if (recommendation.Compatibility.Status is RecommendationCompatibilityStatus.Conditional or RecommendationCompatibilityStatus.Incompatible or RecommendationCompatibilityStatus.Unknown &&
                recommendation.Compatibility.Reasons.Count == 0)
            {
                issues.Add(new AnalysisIssue(
                    "analysis.recommendation.compatibility_reason_missing",
                    $"Recommendation '{recommendation.RecommendationId}' requires compatibility reasons.",
                    ruleId));
            }
        }

        if (recommendation.EvidenceLevel == RecommendationEvidenceLevel.Unknown)
        {
            issues.Add(new AnalysisIssue(
                "analysis.recommendation.unknown_evidence",
                $"Recommendation '{recommendation.RecommendationId}' cannot use Unknown evidence.",
                ruleId));
        }

        if (recommendation.RiskLevel is RecommendationRiskLevel.Advanced or RecommendationRiskLevel.Aggressive &&
            !recommendation.UserConfirmationRequired)
        {
            issues.Add(new AnalysisIssue(
                "analysis.recommendation.confirmation_required",
                $"Recommendation '{recommendation.RecommendationId}' is {recommendation.RiskLevel} and requires future user confirmation.",
                ruleId));
        }

        if (recommendation.RiskLevel is RecommendationRiskLevel.Advanced or RecommendationRiskLevel.Aggressive &&
            HasAnyFlag(recommendation.PresetEligibility, RecommendationPresetEligibility.Basic | RecommendationPresetEligibility.Medium))
        {
            issues.Add(new AnalysisIssue(
                "analysis.recommendation.high_risk_auto_preset",
                $"Recommendation '{recommendation.RecommendationId}' is {recommendation.RiskLevel} and cannot enter Basic/Medium presets.",
                ruleId));
        }

        if (recommendation.EvidenceLevel == RecommendationEvidenceLevel.Experimental &&
            HasAnyFlag(recommendation.PresetEligibility, RecommendationPresetEligibility.Basic | RecommendationPresetEligibility.Medium))
        {
            issues.Add(new AnalysisIssue(
                "analysis.recommendation.experimental_auto_preset",
                $"Recommendation '{recommendation.RecommendationId}' has Experimental evidence and cannot enter Basic/Medium presets.",
                ruleId));
        }

        if (recommendation.Compatibility?.Status is RecommendationCompatibilityStatus.Incompatible or RecommendationCompatibilityStatus.Unknown &&
            recommendation.PresetEligibility != RecommendationPresetEligibility.None)
        {
            issues.Add(new AnalysisIssue(
                "analysis.recommendation.incompatible_preset",
                $"Recommendation '{recommendation.RecommendationId}' is {recommendation.Compatibility.Status} and cannot be preset-eligible.",
                ruleId));
        }

        if (recommendation.Reversible == RecommendationReversibility.None && !recommendation.UserConfirmationRequired)
        {
            issues.Add(new AnalysisIssue(
                "analysis.recommendation.irreversible_without_confirmation",
                $"Recommendation '{recommendation.RecommendationId}' is irreversible and requires future user confirmation.",
                ruleId));
        }

        if (recommendation.ImpactAreas.Count == 0)
        {
            issues.Add(new AnalysisIssue(
                "analysis.recommendation.impact_missing",
                $"Recommendation '{recommendation.RecommendationId}' must declare impact areas.",
                ruleId));
        }

        if (recommendation.Evidence.Count == 0)
        {
            issues.Add(new AnalysisIssue(
                "analysis.recommendation.evidence_missing",
                $"Recommendation '{recommendation.RecommendationId}' must include evidence.",
                ruleId));
        }

        if (recommendation.FutureOptimizationId is { } futureOptimizationId &&
            !IsValidFutureOptimizationId(futureOptimizationId))
        {
            issues.Add(new AnalysisIssue(
                "analysis.recommendation.invalid_future_optimization_id",
                $"Recommendation '{recommendation.RecommendationId}' has invalid FutureOptimizationId '{futureOptimizationId}'.",
                ruleId));
        }

        ValidateRelationIds(recommendation, recommendation.ConflictsWith, "ConflictsWith", "analysis.recommendation.invalid_conflict", recommendationIds, issues);
        ValidateRelationIds(recommendation, recommendation.Requires, "Requires", "analysis.recommendation.invalid_requires", recommendationIds, issues);
    }

    private static void ValidateRequiredText(string value, string fieldName, Recommendation recommendation, List<AnalysisIssue> issues)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        issues.Add(new AnalysisIssue(
            "analysis.recommendation.required_text_missing",
            $"Recommendation '{recommendation.RecommendationId}' requires '{fieldName}'.",
            string.IsNullOrWhiteSpace(recommendation.RuleId) ? "AnalysisCatalog" : recommendation.RuleId));
    }

    private static void ValidateRelationIds(
        Recommendation recommendation,
        IReadOnlyList<string> relationIds,
        string fieldName,
        string issueCode,
        IReadOnlySet<string> recommendationIds,
        List<AnalysisIssue> issues)
    {
        foreach (var relationId in relationIds)
        {
            if (!IsValidBorealId(relationId))
            {
                issues.Add(new AnalysisIssue(issueCode, $"Recommendation '{recommendation.RecommendationId}' has invalid {fieldName} id '{relationId}'.", recommendation.RuleId));
                continue;
            }

            if (relationId.Equals(recommendation.RecommendationId, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new AnalysisIssue(issueCode, $"Recommendation '{recommendation.RecommendationId}' cannot reference itself in {fieldName}.", recommendation.RuleId));
            }

            if (!recommendationIds.Contains(relationId))
            {
                issues.Add(new AnalysisIssue(issueCode, $"Recommendation '{recommendation.RecommendationId}' references unknown {fieldName} id '{relationId}'.", recommendation.RuleId));
            }
        }
    }

    private static bool HasAnyFlag(RecommendationPresetEligibility value, RecommendationPresetEligibility flags)
    {
        return (value & flags) != RecommendationPresetEligibility.None;
    }

    private static bool IsValidFutureOptimizationId(string value)
    {
        return value.StartsWith("BB.OPT.", StringComparison.Ordinal) && IsValidBorealId(value);
    }

    private static bool IsValidBorealId(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > MaxIdentifierLength ||
            !value.StartsWith("BB.", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var character in value)
        {
            var valid = character is '.' or '_' ||
                        character is >= 'A' and <= 'Z' ||
                        character is >= '0' and <= '9';
            if (!valid)
            {
                return false;
            }
        }

        return true;
    }
}
