using BorealBoost.Core.Analysis;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Analysis.RecommendationEngine.Rules;

public sealed class BasicDisplayAdapterAnalysisRule : AnalysisRuleBase
{
    public BasicDisplayAdapterAnalysisRule()
        : base("BB.GRAPHICS.001", AnalysisCategory.Graphics, "Basic display adapter", "Detecta uso de Microsoft Basic Display Adapter ou GPU virtual generica.")
    {
    }

    public override AnalysisRuleEvaluation Evaluate(SystemSnapshot snapshot)
    {
        var isVirtualMachine = snapshot.Hardware.IsVirtualMachine || snapshot.Hardware.FormFactor == MachineFormFactor.VirtualMachine;
        if (ProviderUnavailable(snapshot, "Graphics") || snapshot.Graphics.Count == 0)
        {
            return Result(
                AnalysisRuleStatus.Unknown,
                "GPU desconhecida",
                "Nao ha GPU confiavel no snapshot para declarar oportunidade grafica.",
                "Unknown nao e tratado como oportunidade.",
                RecommendationEvidenceLevel.Unknown,
                [Evidence("Graphics.Count", snapshot.Graphics.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), RecommendationEvidenceLevel.Unknown)]);
        }

        if (snapshot.Graphics.All(IsUnknownGpu))
        {
            return Result(
                AnalysisRuleStatus.Unknown,
                "GPU desconhecida",
                isVirtualMachine
                    ? "A VM nao expos GPU suficiente para uma recomendacao grafica fisica."
                    : "O snapshot nao possui identificacao confiavel de GPU para declarar oportunidade grafica.",
                "Unknown nao e tratado como oportunidade.",
                RecommendationEvidenceLevel.Unknown,
                [
                    Evidence("Hardware.FormFactor", snapshot.Hardware.FormFactor.ToString(), RecommendationEvidenceLevel.Moderate),
                    Evidence("Graphics.UnknownGpuCount", snapshot.Graphics.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), RecommendationEvidenceLevel.Unknown)
                ]);
        }

        var affected = snapshot.Graphics
            .Where(gpu => gpu.Vendor == HardwareVendor.Microsoft ||
                          gpu.FormFactor == GpuFormFactor.Virtual ||
                          (gpu.Name?.Contains("Basic Display", StringComparison.OrdinalIgnoreCase) == true))
            .ToArray();
        if (isVirtualMachine && affected.Length > 0 && affected.All(IsVirtualOrGenericAdapter))
        {
            var vmEvidence = new[]
            {
                Evidence("Hardware.FormFactor", snapshot.Hardware.FormFactor.ToString(), RecommendationEvidenceLevel.Strong),
                Evidence("Hardware.VirtualizationPlatform", snapshot.Hardware.VirtualizationPlatform ?? "Unknown", RecommendationEvidenceLevel.Moderate),
                Evidence("Graphics.VirtualOrGenericAdapterCount", affected.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), RecommendationEvidenceLevel.Moderate)
            };

            return Result(
                AnalysisRuleStatus.NotApplicable,
                "GPU virtual esperada em VM",
                "Adaptadores graficos virtuais ou genericos em VM nao indicam, por si so, problema de driver para gaming.",
                "A regra limita recomendacoes dependentes de GPU fisica quando Hardware.IsVirtualMachine/FormFactor indica VM.",
                RecommendationEvidenceLevel.Moderate,
                vmEvidence);
        }

        if (affected.Length == 0)
        {
            return Result(
                AnalysisRuleStatus.Healthy,
                "GPU grafica especifica detectada",
                "O snapshot nao indica Microsoft Basic Display Adapter.",
                "A regra nao infere configuracoes de painel NVIDIA/AMD/Intel.",
                RecommendationEvidenceLevel.Strong,
                [Evidence("Graphics.BasicDisplayAdapterCount", "0")]);
        }

        var evidence = new[]
        {
            Evidence("Graphics.BasicDisplayAdapterCount", affected.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            Evidence("Graphics.Names", string.Join(" | ", affected.Select(gpu => gpu.Name ?? "Unknown")))
        };
        var recommendation = Recommendation(
            "BB.REC.GRAPHICS.BASIC_DISPLAY_REVIEW",
            "Investigar driver grafico generico",
            "A GPU aparece como adaptador basico, Microsoft ou virtual; isso pode limitar recursos graficos.",
            "O scanner detectou adaptador grafico generico. A Fase 3 nao consulta fontes externas nem instala driver.",
            RecommendationRiskLevel.Medium,
            RecommendationEvidenceLevel.Strong,
            Compatibility(RecommendationCompatibilityStatus.Conditional, "A origem oficial e o match de hardware ainda precisam ser resolvidos por fase futura."),
            "Adaptador grafico generico ou virtual detectado",
            "Driver grafico especifico validado por fonte oficial futura quando aplicavel",
            ExpectedImpactLevel.WorkloadDependent,
            ["Graphics", "Gaming", "Estabilidade visual"],
            ["Correcao futura de driver pode exigir reboot e validacao de assinatura."],
            true,
            RecommendationReversibility.Unknown,
            RecommendationPresetEligibility.Medium | RecommendationPresetEligibility.Advanced | RecommendationPresetEligibility.Custom,
            true,
            evidence);

        return Result(
            AnalysisRuleStatus.Opportunity,
            "Adaptador grafico generico detectado",
            "Ha evidencia de adaptador grafico generico ou virtual.",
            "A regra trabalha somente sobre Graphics do SystemSnapshot.",
            RecommendationEvidenceLevel.Strong,
            evidence,
            [recommendation]);
    }

    private static bool ProviderUnavailable(SystemSnapshot snapshot, string providerName)
    {
        var provider = snapshot.Metadata.ProviderResults.FirstOrDefault(result => result.ProviderName == providerName);
        return provider is null || provider.Status is ProviderResultStatus.Failed or ProviderResultStatus.NotSupported or ProviderResultStatus.TimedOut or ProviderResultStatus.Canceled;
    }

    private static bool IsUnknownGpu(GpuSnapshot gpu)
    {
        return gpu.Vendor == HardwareVendor.Unknown &&
               gpu.FormFactor == GpuFormFactor.Unknown &&
               string.IsNullOrWhiteSpace(gpu.Name);
    }

    private static bool IsVirtualOrGenericAdapter(GpuSnapshot gpu)
    {
        return gpu.FormFactor == GpuFormFactor.Virtual ||
               gpu.Vendor is HardwareVendor.Microsoft or HardwareVendor.HyperV or HardwareVendor.Vmware or HardwareVendor.VirtualBox ||
               (gpu.Name?.Contains("Basic Display", StringComparison.OrdinalIgnoreCase) == true) ||
               (gpu.Name?.Contains("Virtual", StringComparison.OrdinalIgnoreCase) == true) ||
               (gpu.Name?.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) == true) ||
               (gpu.Name?.Contains("VMware", StringComparison.OrdinalIgnoreCase) == true) ||
               (gpu.Name?.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) == true);
    }
}
