using BorealBoost.Core.Analysis;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Analysis.RecommendationEngine.Rules;

public sealed class WindowsCompatibilityAnalysisRule : AnalysisRuleBase
{
    public WindowsCompatibilityAnalysisRule()
        : base("BB.WINDOWS.001", AnalysisCategory.Windows, "Windows compatibility", "Avalia o suporte funcional BorealBoost para a versao do Windows detectada.")
    {
    }

    public override AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot)
    {
        var os = snapshot.OperatingSystem;
        var evidence = new[]
        {
            Evidence("OperatingSystem.Name", os.Name ?? "Unknown"),
            Evidence("OperatingSystem.Build", os.Build?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown"),
            Evidence("OperatingSystem.BorealBoostCompatibility", os.BorealBoostCompatibility.ToString())
        };

        return os.BorealBoostCompatibility switch
        {
            WindowsCompatibilityStatus.Supported => Result(
                AnalysisRuleStatus.Healthy,
                "Windows dentro do target funcional",
                "A versao detectada esta no target funcional V1 do BorealBoost.",
                os.CompatibilityReason ?? "Windows classificado como Supported pelo scanner.",
                RecommendationEvidenceLevel.Strong,
                evidence),

            WindowsCompatibilityStatus.LegacySupported => LegacySupported(evidence, os.CompatibilityReason),
            WindowsCompatibilityStatus.Unsupported => Unsupported(evidence, os.CompatibilityReason),
            _ => Unknown(evidence, os.CompatibilityReason)
        };
    }

    private AnalysisRuleEvaluation LegacySupported(IReadOnlyList<AnalysisEvidence> evidence, string? reason)
    {
        var recommendation = Recommendation(
            "BB.REC.WINDOWS.LEGACY_TARGET",
            "Usar politica de compatibilidade legado",
            "Windows 10 22H2 x64 continua alvo funcional legado, mas deve receber avisos de suporte e regras explicitas.",
            "Compatibilidade funcional BorealBoost nao equivale ao estado de suporte da Microsoft. Otimizacoes futuras devem declarar Windows 10 explicitamente.",
            RecommendationRiskLevel.Safe,
            RecommendationEvidenceLevel.Strong,
            Compatibility(RecommendationCompatibilityStatus.Conditional, "Windows 10 e target legado funcional; recursos Windows 11-only devem ficar bloqueados."),
            "Windows classificado como LegacySupported",
            "Planejamento futuro com regras Windows 10 explicitas e aviso tecnico",
            ExpectedImpactLevel.Unknown,
            ["Compatibilidade", "Transparencia tecnica"],
            ["Alguns recursos futuros podem ficar indisponiveis nesse sistema."],
            false,
            RecommendationReversibility.Full,
            RecommendationPresetEligibility.Basic | RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
            false,
            evidence);

        return Result(
            AnalysisRuleStatus.Warning,
            "Windows legado funcional",
            "O sistema e compatibilidade funcional legado do BorealBoost.",
            reason ?? "Windows classificado como LegacySupported pelo scanner.",
            RecommendationEvidenceLevel.Strong,
            evidence,
            [recommendation]);
    }

    private AnalysisRuleEvaluation Unsupported(IReadOnlyList<AnalysisEvidence> evidence, string? reason)
    {
        var recommendation = Recommendation(
            "BB.REC.WINDOWS.UNSUPPORTED_BLOCK",
            "Bloquear planejamento automatico neste Windows",
            "Nao planejar otimizacoes automaticas quando o Windows esta fora do target funcional V1.",
            "Sistema fora da matriz conhecida torna compatibilidade futura desconhecida. A recomendacao e apenas um bloqueio de planejamento.",
            RecommendationRiskLevel.Advanced,
            RecommendationEvidenceLevel.Strong,
            Compatibility(RecommendationCompatibilityStatus.Incompatible, "Windows fora do target funcional V1."),
            "Windows classificado como Unsupported",
            "Sistema dentro da matriz funcional ou validacao manual documentada",
            ExpectedImpactLevel.Unknown,
            ["Seguranca operacional", "Compatibilidade"],
            ["Recomendacoes de apply futuro devem permanecer indisponiveis."],
            false,
            RecommendationReversibility.Unknown,
            RecommendationPresetEligibility.None,
            true,
            evidence);

        return Result(
            AnalysisRuleStatus.Blocked,
            "Windows nao suportado funcionalmente",
            "A matriz V1 nao cobre este sistema para planejamento automatico.",
            reason ?? "Windows classificado como Unsupported pelo scanner.",
            RecommendationEvidenceLevel.Strong,
            evidence,
            [recommendation]);
    }

    private AnalysisRuleEvaluation Unknown(IReadOnlyList<AnalysisEvidence> evidence, string? reason)
    {
        return Result(
            AnalysisRuleStatus.Unknown,
            "Compatibilidade Windows desconhecida",
            "Dados do sistema operacional nao bastam para declarar oportunidade ou compatibilidade.",
            reason ?? "Windows classificado como Unknown pelo scanner.",
            RecommendationEvidenceLevel.Unknown,
            evidence);
    }
}
