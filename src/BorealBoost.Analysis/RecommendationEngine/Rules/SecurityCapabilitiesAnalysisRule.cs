using BorealBoost.Core.Analysis;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Analysis.RecommendationEngine.Rules;

public sealed class SecurityCapabilitiesAnalysisRule : AnalysisRuleBase
{
    public SecurityCapabilitiesAnalysisRule()
        : base("BB.SECURITY.001", AnalysisCategory.Security, "Critical security guard", "Avalia capabilities de seguranca sem recomendar reducao de protecoes.")
    {
    }

    public override AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot)
    {
        var secureBootAvailable = Capability(snapshot, "SecureBootAvailable");
        var secureBootEnabled = Capability(snapshot, "SecureBootEnabled");
        if (secureBootAvailable?.Status == DetectionStatus.NotSupported)
        {
            return Result(
                AnalysisRuleStatus.NotApplicable,
                "Secure Boot nao suportado neste firmware",
                "A capability nao se aplica ao ambiente detectado.",
                "A regra nao cria oportunidade quando a capability nao e suportada.",
                RecommendationEvidenceLevel.Strong,
                [Evidence("Capabilities.SecureBootAvailable", "NotSupported")]);
        }

        if (secureBootEnabled?.Status == DetectionStatus.Known && secureBootEnabled.IsPresent == false)
        {
            var evidence = new[]
            {
                Evidence("Capabilities.SecureBootEnabled", "False"),
                Evidence("Firmware.FirmwareType", snapshot.Firmware.FirmwareType ?? "Unknown", RecommendationEvidenceLevel.Moderate)
            };
            var recommendation = Recommendation(
                "BB.REC.SECURITY.SECURE_BOOT_REVIEW",
                "Revisar estado do Secure Boot",
                "Secure Boot aparece desativado. Isso e um aviso de seguranca, nao uma promessa de performance.",
                "A Fase 3 nao altera firmware nem reduz protecoes. Qualquer ajuste futuro de seguranca exige consentimento e documentacao especifica.",
                RecommendationRiskLevel.Advanced,
                RecommendationEvidenceLevel.Strong,
                Compatibility(RecommendationCompatibilityStatus.Conditional, "Mudancas de boot podem afetar compatibilidade e exigem acao manual ou fluxo futuro proprio."),
                "Secure Boot desativado",
                "Estado revisado pelo tecnico conforme objetivo do cliente",
                ExpectedImpactLevel.Unknown,
                ["Seguranca", "Integridade do sistema"],
                ["Alteracoes futuras podem afetar boot, dual-boot ou drivers legados."],
                true,
                RecommendationReversibility.Unknown,
                RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
                true,
                evidence);

            return Result(
                AnalysisRuleStatus.Warning,
                "Secure Boot desativado",
                "Ha aviso de seguranca a revisar antes de qualquer estrategia agressiva.",
                "A regra nao recomenda desativar Defender, Firewall, VBS ou Memory Integrity.",
                RecommendationEvidenceLevel.Strong,
                evidence,
                [recommendation]);
        }

        if (secureBootEnabled?.Status == DetectionStatus.Known && secureBootEnabled.IsPresent == true)
        {
            return Result(
                AnalysisRuleStatus.Healthy,
                "Protecao critica preservada",
                "Secure Boot aparece ativo no snapshot.",
                "Capabilities de seguranca diferidas permanecem fora de recomendacoes automaticas.",
                RecommendationEvidenceLevel.Strong,
                [Evidence("Capabilities.SecureBootEnabled", "True")]);
        }

        return Result(
            AnalysisRuleStatus.Unknown,
            "Capabilities de seguranca incompletas",
            "Dados de seguranca insuficientes nao geram oportunidade automatica.",
            "Unknown/Deferred para seguranca nunca entra automaticamente em preset de performance.",
            RecommendationEvidenceLevel.Unknown,
            [Evidence("Capabilities.SecureBootEnabled", secureBootEnabled?.Status.ToString() ?? "Unknown", RecommendationEvidenceLevel.Unknown)]);
    }

    private static SystemCapabilitySnapshot? Capability(SystemSnapshot snapshot, string key)
    {
        return snapshot.Capabilities.FirstOrDefault(capability => capability.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }
}
