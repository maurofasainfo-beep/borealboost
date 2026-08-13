using BorealBoost.Core.Analysis;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Analysis.RecommendationEngine;

public abstract class AnalysisRuleBase : IAnalysisRule
{
    protected AnalysisRuleBase(string ruleId, AnalysisCategory category, string name, string description)
    {
        Metadata = new AnalysisRuleMetadata(ruleId, category, name, description, "1.0.0");
    }

    public AnalysisRuleMetadata Metadata { get; }

    public abstract AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot);

    protected AnalysisRuleEvaluation Result(
        AnalysisRuleStatus status,
        string title,
        string summary,
        string technicalDetails,
        RecommendationEvidenceLevel evidenceLevel,
        IReadOnlyList<AnalysisEvidence> evidence,
        IReadOnlyList<Recommendation>? recommendations = null,
        IReadOnlyList<AnalysisIssue>? issues = null)
    {
        var recommendationList = recommendations ?? [];
        var finding = new AnalysisFinding(
            $"{Metadata.RuleId}.finding",
            Metadata.RuleId,
            Metadata.Category,
            status,
            title,
            summary,
            technicalDetails,
            evidenceLevel,
            evidence,
            recommendationList.Select(recommendation => recommendation.RecommendationId).ToArray());

        return new AnalysisRuleEvaluation(
            Metadata,
            status,
            finding,
            recommendationList,
            issues ?? []);
    }

    protected Recommendation Recommendation(
        string recommendationId,
        string title,
        string shortDescription,
        string technicalReason,
        RecommendationRiskLevel riskLevel,
        RecommendationEvidenceLevel evidenceLevel,
        RecommendationCompatibility compatibility,
        string detectedState,
        string desiredState,
        ExpectedImpactLevel expectedImpact,
        IReadOnlyList<string> impactAreas,
        IReadOnlyList<string> sideEffects,
        bool rebootRequired,
        RecommendationReversibility reversible,
        RecommendationPresetEligibility presetEligibility,
        bool userConfirmationRequired,
        IReadOnlyList<AnalysisEvidence> evidence,
        string? futureOptimizationId = null,
        IReadOnlyList<string>? conflictsWith = null,
        IReadOnlyList<string>? requires = null)
    {
        return new Recommendation(
            recommendationId,
            Metadata.RuleId,
            title,
            shortDescription,
            technicalReason,
            Metadata.Category,
            riskLevel,
            evidenceLevel,
            compatibility,
            detectedState,
            desiredState,
            expectedImpact,
            impactAreas,
            sideEffects,
            rebootRequired,
            reversible,
            presetEligibility,
            userConfirmationRequired,
            futureOptimizationId,
            evidence,
            conflictsWith ?? [],
            requires ?? []);
    }

    protected static AnalysisEvidence Evidence(
        string fieldPath,
        string observedValue,
        RecommendationEvidenceLevel evidenceLevel = RecommendationEvidenceLevel.Strong,
        string source = "SystemSnapshot")
    {
        return new AnalysisEvidence(source, fieldPath, observedValue, evidenceLevel);
    }

    protected static RecommendationCompatibility Compatibility(
        RecommendationCompatibilityStatus status,
        params string[] reasons)
    {
        return new RecommendationCompatibility(status, reasons);
    }
}
