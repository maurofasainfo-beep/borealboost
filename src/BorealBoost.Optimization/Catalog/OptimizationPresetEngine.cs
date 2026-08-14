using BorealBoost.Core.Analysis;
using BorealBoost.Core.Optimization;
using BorealBoost.Core.Scanner;

namespace BorealBoost.Optimization.Catalog;

public sealed class OptimizationPresetEngine : IOptimizationPresetEngine
{
    private readonly IOptimizationCatalog _catalog;

    public OptimizationPresetEngine(IOptimizationCatalog catalog)
    {
        _catalog = catalog;
    }

    public OptimizationPresetSelection Preview(
        SystemSnapshot snapshot,
        AnalysisResult analysis,
        RecommendationPreset preset)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(analysis);

        var selectedIds = new HashSet<OptimizationId>();
        var items = new List<OptimizationPresetSelectionItem>();
        var analysisMatchesSnapshot = analysis.ScanId == snapshot.Metadata.ScanId;

        foreach (var definition in _catalog.GetDefinitions()
                     .Where(definition => definition.Category != OptimizationCategory.IntegrationTest)
                     .OrderBy(definition => definition.OptimizationId.Value, StringComparer.Ordinal))
        {
            var status = analysisMatchesSnapshot
                ? Classify(snapshot, definition, preset, selectedIds, out var reason)
                : Block("Blocked because AnalysisResult does not match the current SystemSnapshot.", out reason);
            if (status == OptimizationPresetSelectionStatus.Selected)
            {
                selectedIds.Add(definition.OptimizationId);
            }

            items.Add(new OptimizationPresetSelectionItem(
                definition.OptimizationId,
                definition.Title,
                definition.Category,
                definition.TechnicalCategory,
                definition.RiskLevel,
                definition.EvidenceLevel,
                definition.ConfigurationEvidence,
                definition.ExpectedImpact,
                definition.PerformanceRelevance,
                definition.AutomaticPresetSuitability,
                definition.UserPreferenceImpact,
                definition.ConfigurationMechanism,
                definition.ActivationBoundary,
                definition.VerificationLevel,
                definition.RollbackValidationLevel,
                definition.ImpactAreas,
                definition.PresetEligibility,
                status,
                reason,
                definition.RequiresRestart,
                definition.SupportsUndo,
                definition.IsSecurityTradeoff));
        }

        return new OptimizationPresetSelection(preset, _catalog.CatalogVersion, items);
    }

    private static OptimizationPresetSelectionStatus Classify(
        SystemSnapshot snapshot,
        OptimizationDefinition definition,
        RecommendationPreset preset,
        ISet<OptimizationId> selectedIds,
        out string reason)
    {
        var compatibility = ValidateCompatibility(snapshot, definition);
        if (compatibility != OptimizationPresetSelectionStatus.Selected)
        {
            reason = "Blocked by Windows/hardware compatibility or unknown required facts.";
            return compatibility;
        }

        foreach (var dependency in definition.Dependencies)
        {
            if (!selectedIds.Contains(dependency))
            {
                reason = $"Blocked because dependency '{dependency}' is not selected.";
                return OptimizationPresetSelectionStatus.Blocked;
            }
        }

        foreach (var conflict in definition.Conflicts)
        {
            if (selectedIds.Contains(conflict))
            {
                reason = $"Blocked because it conflicts with selected optimization '{conflict}'.";
                return OptimizationPresetSelectionStatus.Blocked;
            }
        }

        var presetFlag = ToEligibilityFlag(preset);
        if (!definition.PresetEligibility.HasFlag(presetFlag))
        {
            reason = "Excluded by preset eligibility.";
            return OptimizationPresetSelectionStatus.Excluded;
        }

        if (definition.EvidenceLevel == OptimizationEvidenceLevel.Unknown)
        {
            reason = "Blocked because unknown evidence cannot be applied automatically.";
            return OptimizationPresetSelectionStatus.Blocked;
        }

        return preset switch
        {
            RecommendationPreset.Basic => ClassifyBasic(definition, out reason),
            RecommendationPreset.Medium => ClassifyMedium(definition, out reason),
            RecommendationPreset.Advanced => ClassifyAdvanced(definition, out reason),
            RecommendationPreset.Custom => ClassifyCustom(definition, out reason),
            _ => Block("Unknown preset.", out reason)
        };
    }

    private static OptimizationPresetSelectionStatus ClassifyBasic(OptimizationDefinition definition, out string reason)
    {
        if (definition.RiskLevel != OptimizationRiskLevel.Safe)
        {
            return Exclude("Basic selects only Safe optimizations.", out reason);
        }

        if (definition.AutomaticPresetSuitability != AutomaticPresetSuitability.Automatic)
        {
            return Exclude("Basic excludes preferences and opt-in items from automatic selection.", out reason);
        }

        if (definition.EvidenceLevel == OptimizationEvidenceLevel.Experimental ||
            definition.IsSecurityTradeoff ||
            !definition.SupportsUndo ||
            definition.RequiresRestart)
        {
            return Exclude("Basic excludes experimental, security-tradeoff, irreversible, and restart-heavy optimizations.", out reason);
        }

        return Select("Selected by Basic automatic optimization policy.", out reason);
    }

    private static OptimizationPresetSelectionStatus ClassifyMedium(OptimizationDefinition definition, out string reason)
    {
        if (definition.RiskLevel > OptimizationRiskLevel.Medium)
        {
            return Exclude("Medium selects only Safe and Medium optimizations.", out reason);
        }

        if (definition.EvidenceLevel == OptimizationEvidenceLevel.Experimental ||
            definition.IsSecurityTradeoff ||
            !definition.SupportsUndo)
        {
            return Exclude("Medium excludes experimental, security-tradeoff, and irreversible optimizations.", out reason);
        }

        return definition.AutomaticPresetSuitability switch
        {
            AutomaticPresetSuitability.Automatic => Select("Selected by Medium automatic optimization policy.", out reason),
            AutomaticPresetSuitability.OptIn => Confirm("Medium exposes this opt-in preference only after explicit confirmation.", out reason),
            AutomaticPresetSuitability.CustomOnly => Exclude("Medium excludes Custom-only user preferences.", out reason),
            AutomaticPresetSuitability.AdvancedOnly => Exclude("Medium excludes Advanced-only optimizations.", out reason),
            _ => Block("Unknown automatic preset suitability.", out reason)
        };
    }

    private static OptimizationPresetSelectionStatus ClassifyAdvanced(OptimizationDefinition definition, out string reason)
    {
        if (definition.AutomaticPresetSuitability == AutomaticPresetSuitability.CustomOnly)
        {
            return Exclude("Advanced excludes Custom-only user preferences.", out reason);
        }

        if (definition.AutomaticPresetSuitability == AutomaticPresetSuitability.OptIn)
        {
            return Confirm("Advanced exposes this opt-in item only after explicit confirmation.", out reason);
        }

        if (definition.RiskLevel >= OptimizationRiskLevel.Advanced ||
            definition.IsSecurityTradeoff ||
            definition.RequiresUserConfirmation ||
            definition.AutomaticPresetSuitability == AutomaticPresetSuitability.AdvancedOnly)
        {
            return Confirm("Advanced exposes this item only after explicit confirmation.", out reason);
        }

        return Select("Selected by Advanced automatic optimization policy.", out reason);
    }

    private static OptimizationPresetSelectionStatus ClassifyCustom(OptimizationDefinition definition, out string reason)
    {
        if (definition.AutomaticPresetSuitability == AutomaticPresetSuitability.CustomOnly ||
            definition.AutomaticPresetSuitability == AutomaticPresetSuitability.OptIn)
        {
            return Select("Selectable by Custom policy after technician review.", out reason);
        }

        if (definition.RiskLevel >= OptimizationRiskLevel.Advanced ||
            definition.IsSecurityTradeoff ||
            definition.RequiresUserConfirmation)
        {
            return Confirm("Custom exposes this item only after explicit confirmation.", out reason);
        }

        return Select("Selectable by Custom policy.", out reason);
    }

    private static OptimizationPresetSelectionStatus ValidateCompatibility(
        SystemSnapshot snapshot,
        OptimizationDefinition definition)
    {
        if (!definition.SupportedWindows.CompatibilityStatuses.Contains(snapshot.OperatingSystem.BorealBoostCompatibility))
        {
            return snapshot.OperatingSystem.BorealBoostCompatibility is WindowsCompatibilityStatus.Unknown or WindowsCompatibilityStatus.Unsupported
                ? OptimizationPresetSelectionStatus.Blocked
                : OptimizationPresetSelectionStatus.NotApplicable;
        }

        if (definition.SupportedWindows.MinimumBuild is { } min &&
            (snapshot.OperatingSystem.Build is null || snapshot.OperatingSystem.Build < min))
        {
            return snapshot.OperatingSystem.Build is null
                ? OptimizationPresetSelectionStatus.Blocked
                : OptimizationPresetSelectionStatus.NotApplicable;
        }

        if (definition.SupportedWindows.MaximumBuild is { } max &&
            (snapshot.OperatingSystem.Build is null || snapshot.OperatingSystem.Build > max))
        {
            return snapshot.OperatingSystem.Build is null
                ? OptimizationPresetSelectionStatus.Blocked
                : OptimizationPresetSelectionStatus.NotApplicable;
        }

        if (!string.IsNullOrWhiteSpace(definition.SupportedWindows.Architecture) &&
            !string.Equals(definition.SupportedWindows.Architecture, snapshot.OperatingSystem.Architecture, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(definition.SupportedWindows.Architecture, snapshot.Metadata.MachineArchitecture, StringComparison.OrdinalIgnoreCase))
        {
            return OptimizationPresetSelectionStatus.NotApplicable;
        }

        foreach (var requirement in definition.CompatibilityRequirements)
        {
            if (!RequirementSatisfied(snapshot, requirement))
            {
                return requirement.Required
                    ? OptimizationPresetSelectionStatus.Blocked
                    : OptimizationPresetSelectionStatus.NotApplicable;
            }
        }

        return OptimizationPresetSelectionStatus.Selected;
    }

    private static bool RequirementSatisfied(SystemSnapshot snapshot, CompatibilityRequirement requirement)
    {
        return requirement.Key switch
        {
            "NotVirtualMachine" => !snapshot.Hardware.IsVirtualMachine &&
                                   snapshot.Hardware.FormFactor != MachineFormFactor.VirtualMachine &&
                                   string.Equals(requirement.ExpectedValue, "true", StringComparison.OrdinalIgnoreCase),
            _ => !requirement.Required
        };
    }

    private static RecommendationPresetEligibility ToEligibilityFlag(RecommendationPreset preset)
    {
        return preset switch
        {
            RecommendationPreset.Basic => RecommendationPresetEligibility.Basic,
            RecommendationPreset.Medium => RecommendationPresetEligibility.Medium,
            RecommendationPreset.Advanced => RecommendationPresetEligibility.Advanced,
            RecommendationPreset.Custom => RecommendationPresetEligibility.Custom,
            _ => RecommendationPresetEligibility.None
        };
    }

    private static OptimizationPresetSelectionStatus Select(string message, out string reason)
    {
        reason = message;
        return OptimizationPresetSelectionStatus.Selected;
    }

    private static OptimizationPresetSelectionStatus Exclude(string message, out string reason)
    {
        reason = message;
        return OptimizationPresetSelectionStatus.Excluded;
    }

    private static OptimizationPresetSelectionStatus Block(string message, out string reason)
    {
        reason = message;
        return OptimizationPresetSelectionStatus.Blocked;
    }

    private static OptimizationPresetSelectionStatus Confirm(string message, out string reason)
    {
        reason = message;
        return OptimizationPresetSelectionStatus.RequiresConfirmation;
    }
}
