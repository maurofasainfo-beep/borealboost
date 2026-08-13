using BorealBoost.Core.Analysis;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Analysis.RecommendationEngine.Rules;

public sealed class StartupVolumeAnalysisRule : AnalysisRuleBase
{
    public const int ExcessiveStartupItemThreshold = 30;

    public StartupVolumeAnalysisRule()
        : base("BB.STARTUP.001", AnalysisCategory.Startup, "Startup item volume", "Detecta volume alto de itens de inicializacao sem classificar itens individuais como ruins.")
    {
    }

    public override AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot)
    {
        if (ProviderUnavailable(snapshot, "Startup"))
        {
            return Result(
                AnalysisRuleStatus.Unknown,
                "Inventario de inicializacao indisponivel",
                "Nao ha dados suficientes para avaliar volume de startup.",
                "Provider Startup nao concluiu com sucesso utilizavel.",
                RecommendationEvidenceLevel.Unknown,
                [Evidence("Metadata.ProviderResults.Startup", "Unavailable", RecommendationEvidenceLevel.Unknown)]);
        }

        var count = snapshot.StartupItems.Count;
        var evidence = new[] { Evidence("StartupItems.Count", count.ToString(System.Globalization.CultureInfo.InvariantCulture), RecommendationEvidenceLevel.Experimental) };
        if (count < ExcessiveStartupItemThreshold)
        {
            return Result(
                AnalysisRuleStatus.Healthy,
                "Volume de startup dentro do threshold",
                "A quantidade de itens de inicializacao nao justifica observacao automatica nesta fase.",
                $"Threshold inicial de inventario: {ExcessiveStartupItemThreshold} itens. Nenhum item individual foi classificado.",
                RecommendationEvidenceLevel.Experimental,
                evidence);
        }

        var recommendation = Recommendation(
            "BB.REC.STARTUP.VOLUME_REVIEW",
            "Inventariar startup elevado com cautela",
            "A quantidade de itens de startup e alta, mas contagem isolada nao prova degradacao de desempenho.",
            "A regra considera apenas volume agregado. Ela nao recomenda desabilitar antivirus, drivers, software critico ou itens desconhecidos sem classificacao futura de publisher, criticidade e dependencias.",
            RecommendationRiskLevel.Safe,
            RecommendationEvidenceLevel.Experimental,
            Compatibility(RecommendationCompatibilityStatus.Conditional, "Cada item futuro precisa de classificacao propria, dependencias e rollback."),
            $"{count} itens de inicializacao detectados",
            "Inventario revisado por categoria, publisher e criticidade antes de qualquer ajuste futuro",
            ExpectedImpactLevel.Unknown,
            ["Inventario de inicializacao", "Confiabilidade da analise"],
            ["Desabilitar item errado futuramente pode afetar recursos esperados pelo cliente."],
            false,
            RecommendationReversibility.Unknown,
            RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
            true,
            evidence);

        return Result(
            AnalysisRuleStatus.Warning,
            "Volume alto de startup para inventario",
            "Ha observacao cautelosa para revisao assistida; nao ha prova direta de problema de performance.",
            $"Threshold inicial mantido em {ExcessiveStartupItemThreshold} itens para sinalizar inventario elevado, sem inferir impacto por item.",
            RecommendationEvidenceLevel.Experimental,
            evidence,
            [recommendation]);
    }

    private static bool ProviderUnavailable(SystemSnapshot snapshot, string providerName)
    {
        var provider = snapshot.Metadata.ProviderResults.FirstOrDefault(result => result.ProviderName == providerName);
        return provider is null || provider.Status is ProviderResultStatus.Failed or ProviderResultStatus.NotSupported or ProviderResultStatus.TimedOut or ProviderResultStatus.Canceled;
    }
}
