using BorealBoost.Core.Analysis;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Analysis.RecommendationEngine.Rules;

public sealed class LowSystemDriveSpaceAnalysisRule : AnalysisRuleBase
{
    public const double LowFreeSpacePercentThreshold = 10d;
    public const long LowFreeSpaceBytesThreshold = 20L * 1024L * 1024L * 1024L;

    public LowSystemDriveSpaceAnalysisRule()
        : base("BB.STORAGE.001", AnalysisCategory.Storage, "System drive free space", "Detecta espaco criticamente baixo no volume do sistema.")
    {
    }

    public override AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot)
    {
        if (ProviderUnavailable(snapshot, "Storage"))
        {
            return Result(
                AnalysisRuleStatus.Unknown,
                "Storage indisponivel",
                "Nao ha dados suficientes para avaliar espaco livre do sistema.",
                "Provider Storage nao concluiu com sucesso utilizavel.",
                RecommendationEvidenceLevel.Unknown,
                [Evidence("Metadata.ProviderResults.Storage", "Unavailable", RecommendationEvidenceLevel.Unknown)]);
        }

        var systemVolume = snapshot.Storage.Volumes.FirstOrDefault(volume => volume.IsSystemDrive);
        if (systemVolume is null || systemVolume.TotalBytes is null || systemVolume.FreeBytes is null || systemVolume.TotalBytes <= 0)
        {
            return Result(
                AnalysisRuleStatus.Unknown,
                "Volume do sistema desconhecido",
                "O snapshot nao possui capacidade e espaco livre confiaveis para o volume do sistema.",
                "Unknown nao e tratado como oportunidade.",
                RecommendationEvidenceLevel.Unknown,
                [Evidence("Storage.SystemVolume", "Unknown", RecommendationEvidenceLevel.Unknown)]);
        }

        var freePercent = systemVolume.FreeBytes.Value * 100d / systemVolume.TotalBytes.Value;
        var isLow = freePercent < LowFreeSpacePercentThreshold || systemVolume.FreeBytes.Value < LowFreeSpaceBytesThreshold;
        var evidence = new[]
        {
            Evidence("Storage.SystemVolume.Name", systemVolume.Name),
            Evidence("Storage.SystemVolume.FreeBytes", systemVolume.FreeBytes.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            Evidence("Storage.SystemVolume.FreePercent", freePercent.ToString("N1", System.Globalization.CultureInfo.InvariantCulture))
        };

        if (!isLow)
        {
            return Result(
                AnalysisRuleStatus.Healthy,
                "Espaco livre do sistema adequado",
                "O volume do sistema nao esta abaixo dos thresholds conservadores da Fase 3.",
                $"Thresholds: < {LowFreeSpacePercentThreshold:N0}% ou < 20 GiB livres.",
                RecommendationEvidenceLevel.Strong,
                evidence);
        }

        var recommendation = Recommendation(
            "BB.REC.STORAGE.SYSTEM_DRIVE_SPACE",
            "Revisar espaco livre do volume do sistema",
            "O volume do sistema esta abaixo de threshold conservador de espaco livre.",
            "Pouco espaco pode afetar updates, cache, paginacao e estabilidade operacional. A Fase 3 nao executa limpeza.",
            RecommendationRiskLevel.Safe,
            RecommendationEvidenceLevel.Strong,
            Compatibility(RecommendationCompatibilityStatus.Compatible, "A recomendacao e apenas revisao; limpeza automatica pertence a fase futura com escopo e rollback."),
            $"{systemVolume.FreeBytes.Value / 1024d / 1024d / 1024d:N1} GiB livres ({freePercent:N1}%)",
            "Volume do sistema com margem suficiente antes de aplicar otimizacoes futuras",
            ExpectedImpactLevel.WorkloadDependent,
            ["Storage", "Responsividade", "Manutencao"],
            ["Limpeza futura deve preservar dados pessoais e usar escopo seguro."],
            false,
            RecommendationReversibility.Full,
            RecommendationPresetEligibility.Basic | RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
            false,
            evidence);

        return Result(
            AnalysisRuleStatus.Opportunity,
            "Pouco espaco no volume do sistema",
            "Ha uma oportunidade segura de revisao de espaco antes de qualquer apply futuro.",
            "A regra usa apenas DriveInfo normalizado no SystemSnapshot.",
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
