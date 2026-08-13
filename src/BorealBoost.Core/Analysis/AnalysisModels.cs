using BorealBoost.Core.Scanner;

namespace BorealBoost.Core.Analysis;

public sealed record AnalysisRuleMetadata(
    string RuleId,
    AnalysisCategory Category,
    string Name,
    string Description,
    string Version);

public sealed record AnalysisEvidence(
    string Source,
    string FieldPath,
    string ObservedValue,
    RecommendationEvidenceLevel EvidenceLevel);

public sealed record RecommendationCompatibility(
    RecommendationCompatibilityStatus Status,
    IReadOnlyList<string> Reasons);

public sealed record Recommendation(
    string RecommendationId,
    string RuleId,
    string Title,
    string ShortDescription,
    string TechnicalReason,
    AnalysisCategory Category,
    RecommendationRiskLevel RiskLevel,
    RecommendationEvidenceLevel EvidenceLevel,
    RecommendationCompatibility Compatibility,
    string DetectedState,
    string DesiredState,
    ExpectedImpactLevel ExpectedImpact,
    IReadOnlyList<string> ImpactAreas,
    IReadOnlyList<string> SideEffects,
    bool RebootRequired,
    RecommendationReversibility Reversible,
    RecommendationPresetEligibility PresetEligibility,
    bool UserConfirmationRequired,
    string? FutureOptimizationId,
    IReadOnlyList<AnalysisEvidence> Evidence,
    IReadOnlyList<string> ConflictsWith,
    IReadOnlyList<string> Requires);

public sealed record AnalysisFinding(
    string FindingId,
    string RuleId,
    AnalysisCategory Category,
    AnalysisRuleStatus Status,
    string Title,
    string Summary,
    string TechnicalDetails,
    RecommendationEvidenceLevel EvidenceLevel,
    IReadOnlyList<AnalysisEvidence> Evidence,
    IReadOnlyList<string> RelatedRecommendationIds);

public sealed record AnalysisIssue(
    string Code,
    string Message,
    string RuleId);

public sealed record AnalysisRuleEvaluation(
    AnalysisRuleMetadata Rule,
    AnalysisRuleStatus Status,
    AnalysisFinding? Finding,
    IReadOnlyList<Recommendation> Recommendations,
    IReadOnlyList<AnalysisIssue> Issues);

public sealed record AnalysisSummary(
    int RulesEvaluated,
    int HealthyCount,
    int OpportunityCount,
    int WarningCount,
    int BlockedCount,
    int UnknownCount,
    int NotApplicableCount,
    int RecommendationCount,
    IReadOnlyDictionary<RecommendationRiskLevel, int> RiskDistribution);

public sealed record PresetPreview(
    RecommendationPreset Preset,
    int EligibleRecommendationCount,
    IReadOnlyList<string> RecommendationIds,
    IReadOnlyDictionary<RecommendationRiskLevel, int> RiskDistribution);

public sealed record RecommendationPlan(
    string PlanVersion,
    IReadOnlyList<Recommendation> Recommendations,
    IReadOnlyList<PresetPreview> Presets);

public sealed record AnalysisResult(
    AnalysisId AnalysisId,
    ScanId ScanId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    TimeSpan Duration,
    string EngineVersion,
    string RuleCatalogVersion,
    IReadOnlyList<AnalysisRuleEvaluation> RuleResults,
    IReadOnlyList<AnalysisFinding> Findings,
    IReadOnlyList<Recommendation> Recommendations,
    RecommendationPlan RecommendationPlan,
    AnalysisSummary Summary,
    IReadOnlyList<AnalysisIssue> Warnings);
