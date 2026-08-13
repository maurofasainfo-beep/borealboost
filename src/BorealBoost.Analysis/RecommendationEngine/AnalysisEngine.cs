using System.Diagnostics;
using BorealBoost.Core.Analysis;
using BorealBoost.Core.Common;
using BorealBoost.Core.Scanner;
using Microsoft.Extensions.Logging;

namespace BorealBoost.Analysis.RecommendationEngine;

public sealed class AnalysisEngine : IAnalysisEngine
{
    public const string EngineVersion = "3.0.0";
    public const string RuleCatalogVersion = "3.0.0-code-first";
    private const string PlanVersion = "3.0.0-preview";

    private readonly IReadOnlyList<IAnalysisRule> _rules;
    private readonly ILogger<AnalysisEngine> _logger;

    public AnalysisEngine(IEnumerable<IAnalysisRule> rules, ILogger<AnalysisEngine> logger)
    {
        _rules = rules
            .OrderBy(rule => rule.Metadata.RuleId, StringComparer.Ordinal)
            .ToArray();
        _logger = logger;
    }

    public Task<Result<AnalysisResult>> AnalyzeAsync(SystemSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var analysisId = AnalysisId.New();
        var startedAtUtc = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var evaluations = new List<AnalysisRuleEvaluation>();
        var warnings = new List<AnalysisIssue>();

        _logger.LogInformation(
            "Analysis started. AnalysisId={AnalysisId}; ScanId={ScanId}; EngineVersion={EngineVersion}; RuleCatalogVersion={RuleCatalogVersion}; RuleCount={RuleCount}",
            analysisId,
            snapshot.Metadata.ScanId,
            EngineVersion,
            RuleCatalogVersion,
            _rules.Count);

        foreach (var rule in _rules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var evaluation = rule.Evaluate(snapshot);
                evaluations.Add(evaluation);
                warnings.AddRange(evaluation.Issues);
            }
            catch (Exception exception)
            {
                var issue = new AnalysisIssue("analysis.rule.failed", exception.Message, rule.Metadata.RuleId);
                warnings.Add(issue);
                evaluations.Add(new AnalysisRuleEvaluation(
                    rule.Metadata,
                    AnalysisRuleStatus.Unknown,
                    new AnalysisFinding(
                        $"{rule.Metadata.RuleId}.failure",
                        rule.Metadata.RuleId,
                        rule.Metadata.Category,
                        AnalysisRuleStatus.Unknown,
                        "Regra de analise indisponivel",
                        "A regra falhou e nao gerou recomendacao.",
                        "A falha foi isolada para evitar recomendacao falsa.",
                        RecommendationEvidenceLevel.Unknown,
                        [],
                        []),
                    [],
                    [issue]));

                _logger.LogError(exception, "Analysis rule failed. AnalysisId={AnalysisId}; RuleId={RuleId}", analysisId, rule.Metadata.RuleId);
            }
        }

        stopwatch.Stop();
        var completedAtUtc = DateTimeOffset.UtcNow;
        var validationIssues = RecommendationModelValidator.ValidateEvaluations(evaluations);
        if (validationIssues.Count > 0)
        {
            _logger.LogError(
                "Analysis recommendation validation failed. AnalysisId={AnalysisId}; ScanId={ScanId}; Issues={Issues}",
                analysisId,
                snapshot.Metadata.ScanId,
                FormatValidationIssues(validationIssues));
            return Task.FromResult(Result<AnalysisResult>.Failure("analysis.validation.failed", "Recommendation model validation failed."));
        }

        var findings = evaluations
            .Select(evaluation => evaluation.Finding)
            .OfType<AnalysisFinding>()
            .OrderBy(finding => finding.RuleId, StringComparer.Ordinal)
            .ToArray();
        var recommendations = DeduplicateRecommendations(evaluations);
        var plan = BuildRecommendationPlan(recommendations);
        validationIssues = RecommendationModelValidator.ValidatePlan(plan);
        if (validationIssues.Count > 0)
        {
            _logger.LogError(
                "Analysis plan validation failed. AnalysisId={AnalysisId}; ScanId={ScanId}; Issues={Issues}",
                analysisId,
                snapshot.Metadata.ScanId,
                FormatValidationIssues(validationIssues));
            return Task.FromResult(Result<AnalysisResult>.Failure("analysis.validation.failed", "Recommendation plan validation failed."));
        }

        var summary = BuildSummary(evaluations, recommendations);

        var result = new AnalysisResult(
            analysisId,
            snapshot.Metadata.ScanId,
            startedAtUtc,
            completedAtUtc,
            stopwatch.Elapsed,
            EngineVersion,
            RuleCatalogVersion,
            evaluations,
            findings,
            recommendations,
            plan,
            summary,
            warnings);

        _logger.LogInformation(
            "Analysis completed. AnalysisId={AnalysisId}; ScanId={ScanId}; DurationMs={DurationMs}; Rules={Rules}; Opportunities={Opportunities}; Warnings={Warnings}; Blocked={Blocked}; Unknown={Unknown}; Recommendations={Recommendations}",
            analysisId,
            snapshot.Metadata.ScanId,
            stopwatch.Elapsed.TotalMilliseconds,
            summary.RulesEvaluated,
            summary.OpportunityCount,
            summary.WarningCount,
            summary.BlockedCount,
            summary.UnknownCount,
            summary.RecommendationCount);

        return Task.FromResult(Result<AnalysisResult>.Success(result));
    }

    private static Recommendation[] DeduplicateRecommendations(IReadOnlyList<AnalysisRuleEvaluation> evaluations)
    {
        return evaluations
            .SelectMany(evaluation => evaluation.Recommendations)
            .OrderBy(recommendation => recommendation.RecommendationId, StringComparer.Ordinal)
            .ToArray();
    }

    private static string FormatValidationIssues(IReadOnlyList<AnalysisIssue> issues)
    {
        return string.Join(
            " | ",
            issues.Select(issue => $"{issue.Code}:{issue.RuleId}:{issue.Message}"));
    }

    private static AnalysisSummary BuildSummary(
        IReadOnlyList<AnalysisRuleEvaluation> evaluations,
        IReadOnlyList<Recommendation> recommendations)
    {
        var riskDistribution = Enum.GetValues<RecommendationRiskLevel>()
            .ToDictionary(
                risk => risk,
                risk => recommendations.Count(recommendation => recommendation.RiskLevel == risk));

        return new AnalysisSummary(
            evaluations.Count,
            evaluations.Count(evaluation => evaluation.Status == AnalysisRuleStatus.Healthy),
            evaluations.Count(evaluation => evaluation.Status == AnalysisRuleStatus.Opportunity),
            evaluations.Count(evaluation => evaluation.Status == AnalysisRuleStatus.Warning),
            evaluations.Count(evaluation => evaluation.Status == AnalysisRuleStatus.Blocked),
            evaluations.Count(evaluation => evaluation.Status == AnalysisRuleStatus.Unknown),
            evaluations.Count(evaluation => evaluation.Status == AnalysisRuleStatus.NotApplicable),
            recommendations.Count,
            riskDistribution);
    }

    private static RecommendationPlan BuildRecommendationPlan(IReadOnlyList<Recommendation> recommendations)
    {
        var presets = Enum.GetValues<RecommendationPreset>()
            .Select(preset =>
            {
                var flag = ToEligibilityFlag(preset);
                var eligible = recommendations
                    .Where(recommendation => recommendation.PresetEligibility.HasFlag(flag))
                    .OrderBy(recommendation => recommendation.RecommendationId, StringComparer.Ordinal)
                    .ToArray();
                var riskDistribution = Enum.GetValues<RecommendationRiskLevel>()
                    .ToDictionary(
                        risk => risk,
                        risk => eligible.Count(recommendation => recommendation.RiskLevel == risk));

                return new PresetPreview(
                    preset,
                    eligible.Length,
                    eligible.Select(recommendation => recommendation.RecommendationId).ToArray(),
                    riskDistribution);
            })
            .ToArray();

        return new RecommendationPlan(PlanVersion, recommendations, presets);
    }

    private static RecommendationPresetEligibility ToEligibilityFlag(RecommendationPreset preset)
    {
        return preset switch
        {
            RecommendationPreset.Basic => RecommendationPresetEligibility.Basic,
            RecommendationPreset.Medium => RecommendationPresetEligibility.Medium,
            RecommendationPreset.Advanced => RecommendationPresetEligibility.Advanced,
            RecommendationPreset.Custom => RecommendationPresetEligibility.Custom,
            _ => RecommendationPresetEligibility.None
        };
    }
}
