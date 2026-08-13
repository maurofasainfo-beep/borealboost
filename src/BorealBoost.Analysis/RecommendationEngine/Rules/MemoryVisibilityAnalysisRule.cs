using BorealBoost.Core.Analysis;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Analysis.RecommendationEngine.Rules;

public sealed class MemoryVisibilityAnalysisRule : AnalysisRuleBase
{
    public const ulong VisibleMemoryGapThresholdBytes = 512UL * 1024UL * 1024UL;

    public MemoryVisibilityAnalysisRule()
        : base("BB.MEMORY.001", AnalysisCategory.Memory, "Installed versus visible memory", "Compara memoria fisica instalada com memoria visivel ao Windows.")
    {
    }

    public override AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot)
    {
        if (snapshot.Memory.InstalledPhysicalBytes is null || snapshot.Memory.VisiblePhysicalBytes is null)
        {
            return Result(
                AnalysisRuleStatus.Unknown,
                "Memoria instalada ou visivel desconhecida",
                "Nao ha dados suficientes para comparar memoria fisica instalada e visivel.",
                "Unknown nao e tratado como oportunidade.",
                RecommendationEvidenceLevel.Unknown,
                [Evidence("Memory.InstalledPhysicalBytes/VisiblePhysicalBytes", "Unknown", RecommendationEvidenceLevel.Unknown)]);
        }

        var installed = snapshot.Memory.InstalledPhysicalBytes.Value;
        var visible = snapshot.Memory.VisiblePhysicalBytes.Value;
        if (installed <= visible || installed - visible <= VisibleMemoryGapThresholdBytes)
        {
            return Result(
                AnalysisRuleStatus.Healthy,
                "Memoria visivel coerente com a memoria instalada",
                "A diferenca entre memoria instalada e visivel esta dentro do threshold conservador.",
                "A regra nao recomenda upgrade de RAM por capacidade isolada.",
                RecommendationEvidenceLevel.Moderate,
                BuildEvidence(installed, visible));
        }

        var gapBytes = installed - visible;
        var recommendation = Recommendation(
            "BB.REC.MEMORY.VISIBLE_GAP_REVIEW",
            "Registrar diferenca entre RAM instalada e visivel",
            "A memoria visivel pelo Windows e menor que a memoria fisica instalada em margem relevante.",
            "A diferenca pode vir de reserva de hardware, firmware, iGPU ou configuracao do sistema. A Fase 3 nao infere problema de RAM nem sugere XMP/EXPO.",
            RecommendationRiskLevel.Safe,
            RecommendationEvidenceLevel.Moderate,
            Compatibility(RecommendationCompatibilityStatus.Compatible, "A recomendacao e observacional e nao altera configuracao."),
            $"Instalada={FormatBytes(installed)}; Visivel={FormatBytes(visible)}; Diferenca={FormatBytes(gapBytes)}",
            "Diferenca explicada ao tecnico antes de qualquer relatorio ou plano futuro",
            ExpectedImpactLevel.Unknown,
            ["Memoria", "Transparencia tecnica"],
            [],
            false,
            RecommendationReversibility.Full,
            RecommendationPresetEligibility.Basic | RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
            false,
            BuildEvidence(installed, visible));

        return Result(
            AnalysisRuleStatus.Warning,
            "Diferenca relevante de memoria visivel",
            "RAM instalada e RAM visivel ao Windows nao devem ser misturadas no diagnostico.",
            "A regra preserva a distincao aprovada na Fase 2.",
            RecommendationEvidenceLevel.Moderate,
            BuildEvidence(installed, visible),
            [recommendation]);
    }

    private static AnalysisEvidence[] BuildEvidence(ulong installed, ulong visible)
    {
        return
        [
            new AnalysisEvidence("SystemSnapshot", "Memory.InstalledPhysicalBytes", installed.ToString(System.Globalization.CultureInfo.InvariantCulture), RecommendationEvidenceLevel.Moderate),
            new AnalysisEvidence("SystemSnapshot", "Memory.VisiblePhysicalBytes", visible.ToString(System.Globalization.CultureInfo.InvariantCulture), RecommendationEvidenceLevel.Moderate)
        ];
    }

    private static string FormatBytes(ulong bytes)
    {
        return $"{bytes / 1024d / 1024d / 1024d:N1} GiB";
    }
}
