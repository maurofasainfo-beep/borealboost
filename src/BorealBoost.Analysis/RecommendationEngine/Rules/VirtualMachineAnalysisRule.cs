using BorealBoost.Core.Analysis;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Analysis.RecommendationEngine.Rules;

public sealed class VirtualMachineAnalysisRule : AnalysisRuleBase
{
    public VirtualMachineAnalysisRule()
        : base("BB.SYSTEM.002", AnalysisCategory.System, "Virtual machine guard", "Limita recomendacoes dependentes de hardware fisico quando o snapshot indica VM.")
    {
    }

    public override AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot)
    {
        if (snapshot.Hardware.IsVirtualMachine || snapshot.Hardware.FormFactor == MachineFormFactor.VirtualMachine)
        {
            var evidence = new[]
            {
                Evidence("Hardware.FormFactor", snapshot.Hardware.FormFactor.ToString()),
                Evidence("Hardware.VirtualizationPlatform", snapshot.Hardware.VirtualizationPlatform ?? "Unknown")
            };
            var recommendation = Recommendation(
                "BB.REC.SYSTEM.VM_CONSERVATIVE_MODE",
                "Usar analise conservadora em maquina virtual",
                "Recomendacoes dependentes de GPU, storage ou energia fisica devem ficar limitadas em VM.",
                "Maqinas virtuais podem expor hardware sintetico; uma regra fisica universal seria fraca.",
                RecommendationRiskLevel.Safe,
                RecommendationEvidenceLevel.Strong,
                Compatibility(RecommendationCompatibilityStatus.Conditional, "Hardware virtual exige regras futuras especificas ou bloqueio."),
                "Virtual machine detectada",
                "Planejamento futuro limitado a facts compataveis com VM",
                ExpectedImpactLevel.Unknown,
                ["Compatibilidade", "Seguranca operacional"],
                ["Menos recomendacoes automaticas em ambiente virtual."],
                false,
                RecommendationReversibility.Full,
                RecommendationPresetEligibility.Basic | RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
                false,
                evidence);

            return Result(
                AnalysisRuleStatus.Blocked,
                "Maquina virtual detectada",
                "Acoes futuras dependentes de hardware fisico devem ser bloqueadas ou condicionais.",
                "A regra usa Hardware.IsVirtualMachine/FormFactor do SystemSnapshot.",
                RecommendationEvidenceLevel.Strong,
                evidence,
                [recommendation]);
        }

        if (snapshot.Hardware.FormFactor == MachineFormFactor.Unknown)
        {
            return Result(
                AnalysisRuleStatus.Unknown,
                "Tipo de maquina desconhecido",
                "Nao foi possivel confirmar se o ambiente e fisico ou virtual.",
                "Unknown nao e tratado como oportunidade.",
                RecommendationEvidenceLevel.Unknown,
                [Evidence("Hardware.FormFactor", "Unknown", RecommendationEvidenceLevel.Unknown)]);
        }

        return Result(
            AnalysisRuleStatus.Healthy,
            "Maquina fisica ou form factor conhecido",
            "O snapshot nao indica VM.",
            "Regras futuras ainda devem validar hardware por recomendacao.",
            RecommendationEvidenceLevel.Moderate,
            [Evidence("Hardware.FormFactor", snapshot.Hardware.FormFactor.ToString(), RecommendationEvidenceLevel.Moderate)]);
    }
}
