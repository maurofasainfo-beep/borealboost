using BorealBoost.Core.Analysis;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Analysis.RecommendationEngine.Rules;

public sealed class PartialScanAnalysisRule : AnalysisRuleBase
{
    public PartialScanAnalysisRule()
        : base("BB.SYSTEM.001", AnalysisCategory.System, "Partial scan guard", "Detecta snapshots parciais antes de recomendar acoes futuras.")
    {
    }

    public override AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot)
    {
        if (!snapshot.Metadata.PartialScan)
        {
            return Result(
                AnalysisRuleStatus.Healthy,
                "Scan completo",
                "Todos os providers obrigatorios finalizaram sem erro bloqueante.",
                "PartialScan=false no metadata do snapshot.",
                RecommendationEvidenceLevel.Strong,
                [Evidence("Metadata.PartialScan", "False")]);
        }

        var failedProviders = snapshot.Metadata.ProviderResults
            .Where(provider => provider.Status is ProviderResultStatus.Partial or ProviderResultStatus.Failed or ProviderResultStatus.NotSupported or ProviderResultStatus.TimedOut or ProviderResultStatus.Canceled)
            .Select(provider => $"{provider.ProviderName}:{provider.Status}")
            .Order(StringComparer.Ordinal)
            .ToArray();
        var observed = failedProviders.Length == 0 ? "PartialScan=True" : string.Join("; ", failedProviders);
        var evidence = new[] { Evidence("Metadata.ProviderResults", observed) };
        var recommendation = Recommendation(
            "BB.REC.SYSTEM.RESCAN.PARTIAL",
            "Tratar a analise como parcial",
            "Reexecute o scanner ou revise os providers incompletos antes de planejar qualquer otimizacao.",
            "Uma sessao parcial pode esconder fatos de compatibilidade. A recomendacao nao aplica mudanca no Windows.",
            RecommendationRiskLevel.Safe,
            RecommendationEvidenceLevel.Strong,
            Compatibility(RecommendationCompatibilityStatus.Conditional, "Snapshot parcial: recomendacoes futuras devem ser conservadoras."),
            "Snapshot parcial",
            "Snapshot completo ou aceite explicito do tecnico para continuar em modo conservador",
            ExpectedImpactLevel.Unknown,
            ["Confiabilidade do diagnostico"],
            [],
            false,
            RecommendationReversibility.Full,
            RecommendationPresetEligibility.Basic | RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
            false,
            evidence);

        return Result(
            AnalysisRuleStatus.Warning,
            "Scan parcial detectado",
            "Alguns fatos podem estar ausentes ou incompletos.",
            "PartialScan=true; providers incompletos: " + observed,
            RecommendationEvidenceLevel.Strong,
            evidence,
            [recommendation]);
    }
}
