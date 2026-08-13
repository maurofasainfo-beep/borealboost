using BorealBoost.Core.Analysis;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Analysis.RecommendationEngine.Rules;

public sealed class MissingDriverAnalysisRule : AnalysisRuleBase
{
    public MissingDriverAnalysisRule()
        : base("BB.DRIVER.001", AnalysisCategory.Drivers, "Missing driver evidence", "Detecta dispositivos com codigo objetivo de driver ausente.")
    {
    }

    public override AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot)
    {
        if (ProviderUnavailable(snapshot, "Devices"))
        {
            return Result(
                AnalysisRuleStatus.Unknown,
                "Inventario de dispositivos indisponivel",
                "Nao ha base suficiente para detectar driver ausente.",
                "Provider Devices nao concluiu com sucesso utilizavel.",
                RecommendationEvidenceLevel.Unknown,
                [Evidence("Metadata.ProviderResults.Devices", "Unavailable", RecommendationEvidenceLevel.Unknown)]);
        }

        var missing = snapshot.Devices
            .Where(device => device.HealthStatus == DeviceHealthStatus.MissingDriver)
            .ToArray();
        if (missing.Length == 0)
        {
            return Result(
                AnalysisRuleStatus.Healthy,
                "Nenhum driver ausente detectado",
                "O snapshot nao trouxe dispositivo com codigo objetivo de driver ausente.",
                "DeviceHealthStatus.MissingDriver nao apareceu no inventario.",
                RecommendationEvidenceLevel.Strong,
                [Evidence("Devices.MissingDriverCount", "0")]);
        }

        var evidence = new[]
        {
            Evidence("Devices.MissingDriverCount", missing.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            Evidence("Devices.ProblemCodes", string.Join(",", missing.Select(device => device.ProblemCode?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown").Distinct()))
        };
        var recommendation = Recommendation(
            "BB.REC.DRIVER.MISSING_INVESTIGATE",
            "Investigar dispositivo sem driver",
            "Existe evidencia objetiva de dispositivo sem driver. A Fase 3 apenas recomenda investigacao.",
            "O scanner encontrou DeviceHealthStatus.MissingDriver, normalmente associado a ConfigManagerErrorCode 28. Nao ha fonte externa de versao nem instalador nesta fase.",
            RecommendationRiskLevel.Medium,
            RecommendationEvidenceLevel.Strong,
            Compatibility(RecommendationCompatibilityStatus.Conditional, "Driver Engine operacional pertence a fase futura; fonte oficial e assinatura ainda serao exigidas."),
            $"{missing.Length} dispositivo(s) com driver ausente",
            "Dispositivos identificados e avaliados por fonte oficial futura",
            ExpectedImpactLevel.WorkloadDependent,
            ["Estabilidade", "Dispositivos", "Gaming quando GPU/rede/audio forem afetados"],
            ["Instalacao futura de driver pode exigir reboot e validacao de publisher."],
            true,
            RecommendationReversibility.Unknown,
            RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
            true,
            evidence);

        return Result(
            AnalysisRuleStatus.Opportunity,
            "Driver ausente detectado",
            "Ha dispositivo com evidencia objetiva de driver ausente.",
            "A regra usa apenas DeviceHealthStatus do SystemSnapshot e nao infere driver desatualizado.",
            RecommendationEvidenceLevel.Strong,
            evidence,
            [recommendation]);
    }

    private static bool ProviderUnavailable(SystemSnapshot snapshot, string providerName)
    {
        var provider = snapshot.Metadata.ProviderResults.FirstOrDefault(result => result.ProviderName == providerName);
        return provider is null || provider.Status is ProviderResultStatus.Failed or ProviderResultStatus.NotSupported or ProviderResultStatus.TimedOut or ProviderResultStatus.Canceled;
    }
}
