using BorealBoost.Core.Analysis;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Analysis.RecommendationEngine.Rules;

public sealed class PowerContextAnalysisRule : AnalysisRuleBase
{
    public PowerContextAnalysisRule()
        : base("BB.POWER.001", AnalysisCategory.Power, "Power context guard", "Avalia notebook/bateria para limitar recomendacoes agressivas de energia.")
    {
    }

    public override AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot)
    {
        var formFactor = snapshot.Hardware.FormFactor;
        var isPortable = formFactor is MachineFormFactor.Laptop or MachineFormFactor.Convertible or MachineFormFactor.Tablet ||
                         snapshot.Power.BatteryPresent == true;
        if (isPortable)
        {
            var evidence = new[]
            {
                Evidence("Hardware.FormFactor", formFactor.ToString(), RecommendationEvidenceLevel.Strong),
                Evidence("Power.BatteryPresent", snapshot.Power.BatteryPresent?.ToString() ?? "Unknown", RecommendationEvidenceLevel.Moderate),
                Evidence("Power.PowerSource", snapshot.Power.PowerSource.ToString(), RecommendationEvidenceLevel.Moderate)
            };
            var recommendation = Recommendation(
                "BB.REC.POWER.PORTABLE_GUARD",
                "Evitar energia agressiva automatica em notebook",
                "Dispositivo portatil exige politica conservadora e consentimento antes de ajustes de energia voltados a desempenho.",
                "Notebook pode depender de gerenciamento OEM, bateria, temperatura e recursos de suspensao. Nenhum plano de energia e criado nesta fase.",
                RecommendationRiskLevel.Advanced,
                RecommendationEvidenceLevel.Moderate,
                Compatibility(RecommendationCompatibilityStatus.Conditional, "Portatil detectado; ajustes maximos de energia exigem consentimento e rollback futuro."),
                $"FormFactor={formFactor}; PowerSource={snapshot.Power.PowerSource}",
                "Politica futura condicionada a AC/bateria, OEM e consentimento explicito",
                ExpectedImpactLevel.WorkloadDependent,
                ["Power", "Gaming", "Autonomia"],
                ["Pode afetar bateria, ruido, temperatura e comportamento de suspensao se aplicado futuramente."],
                false,
                RecommendationReversibility.Unknown,
                RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
                true,
                evidence);

            return Result(
                AnalysisRuleStatus.Warning,
                "Contexto portatil detectado",
                "Recomendacoes agressivas de energia devem permanecer condicionais.",
                "A regra nao altera power plan e nao consulta fontes alem do SystemSnapshot.",
                RecommendationEvidenceLevel.Moderate,
                evidence,
                [recommendation]);
        }

        if (formFactor == MachineFormFactor.Unknown && snapshot.Power.BatteryPresent is null)
        {
            return Result(
                AnalysisRuleStatus.Unknown,
                "Contexto de energia desconhecido",
                "Nao ha dados suficientes para declarar oportunidade de energia.",
                "Unknown nao e tratado como oportunidade.",
                RecommendationEvidenceLevel.Unknown,
                [Evidence("Power.BatteryPresent", "Unknown", RecommendationEvidenceLevel.Unknown)]);
        }

        return Result(
            AnalysisRuleStatus.Healthy,
            "Contexto de energia sem bloqueio portatil",
            "O snapshot nao indica notebook em bateria ou form factor portatil.",
            "Regras futuras ainda devem validar plano ativo antes de qualquer apply.",
            RecommendationEvidenceLevel.Moderate,
            [Evidence("Hardware.FormFactor", formFactor.ToString(), RecommendationEvidenceLevel.Moderate)]);
    }
}
