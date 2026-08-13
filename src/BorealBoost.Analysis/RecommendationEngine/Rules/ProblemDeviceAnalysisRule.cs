using BorealBoost.Core.Analysis;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Analysis.RecommendationEngine.Rules;

public sealed class ProblemDeviceAnalysisRule : AnalysisRuleBase
{
    public ProblemDeviceAnalysisRule()
        : base("BB.DRIVER.002", AnalysisCategory.Drivers, "Problem device evidence", "Detecta dispositivos com problema objetivo no Device Manager.")
    {
    }

    public override AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot)
    {
        if (ProviderUnavailable(snapshot, "Devices"))
        {
            return Result(
                AnalysisRuleStatus.Unknown,
                "Inventario de dispositivos indisponivel",
                "Nao ha base suficiente para detectar dispositivos com problema.",
                "Provider Devices nao concluiu com sucesso utilizavel.",
                RecommendationEvidenceLevel.Unknown,
                [Evidence("Metadata.ProviderResults.Devices", "Unavailable", RecommendationEvidenceLevel.Unknown)]);
        }

        var affected = snapshot.Devices
            .Where(device => device.HealthStatus is DeviceHealthStatus.Problem or DeviceHealthStatus.Disabled)
            .ToArray();
        if (affected.Length == 0)
        {
            return Result(
                AnalysisRuleStatus.Healthy,
                "Nenhum dispositivo com problema objetivo",
                "O snapshot nao trouxe dispositivos com erro ou desabilitados.",
                "DeviceHealthStatus.Problem/Disabled nao apareceu no inventario.",
                RecommendationEvidenceLevel.Strong,
                [Evidence("Devices.ProblemOrDisabledCount", "0")]);
        }

        var evidence = new[]
        {
            Evidence("Devices.ProblemOrDisabledCount", affected.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            Evidence("Devices.ProblemCodes", string.Join(",", affected.Select(device => device.ProblemCode?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown").Distinct()))
        };
        var recommendation = Recommendation(
            "BB.REC.DRIVER.PROBLEM_DEVICE_REVIEW",
            "Revisar dispositivos com problema",
            "Existe evidencia objetiva de dispositivos em erro ou desabilitados.",
            "A recomendacao deriva de DeviceHealthStatus e ProblemCode coletados pelo scanner. A Fase 3 nao instala, atualiza ou remove drivers.",
            RecommendationRiskLevel.Medium,
            RecommendationEvidenceLevel.Strong,
            Compatibility(RecommendationCompatibilityStatus.Conditional, "A causa deve ser confirmada antes de qualquer acao futura."),
            $"{affected.Length} dispositivo(s) com problema ou desabilitado(s)",
            "Diagnostico futuro por Driver Engine oficial e assistido",
            ExpectedImpactLevel.WorkloadDependent,
            ["Estabilidade", "Hardware", "Compatibilidade"],
            ["Qualquer correcao futura de driver pode exigir reboot e rollback planejado."],
            true,
            RecommendationReversibility.Unknown,
            RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
            true,
            evidence);

        return Result(
            AnalysisRuleStatus.Opportunity,
            "Dispositivo com problema detectado",
            "Ha evidencia objetiva de dispositivo em erro ou desabilitado.",
            "A regra nao declara driver desatualizado porque nao existe fonte oficial de versao nova nesta fase.",
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
