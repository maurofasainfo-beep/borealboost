using System.Collections.ObjectModel;
using BorealBoost.Core.Analysis;
using BorealBoost.Core.Scanner;
using Microsoft.Extensions.Logging;

namespace BorealBoost.App.ViewModels;

public sealed class AnalysisViewModel : ObservableObject
{
    private readonly IAnalysisSessionService _analysisSessionService;
    private readonly ISystemSnapshotStore _snapshotStore;
    private readonly IAnalysisResultStore _analysisResultStore;
    private readonly ILogger<AnalysisViewModel> _logger;
    private AnalysisResult? _currentResult;
    private RecommendationPreset? _presetFilter;
    private AnalysisCategory? _categoryFilter;
    private RecommendationRiskLevel? _riskFilter;
    private bool _isAnalyzing;
    private string _statusText = "Execute o scanner antes de analisar recomendacoes.";
    private string _summaryText = "Nenhum snapshot disponivel.";
    private string _rulesSummary = "Rules 0 | Opportunities 0 | Warnings 0 | Blocked 0 | Unknown 0";
    private string _recommendationSummary = "Recommendations 0";
    private string _riskDistribution = "Safe 0 | Medium 0 | Advanced 0 | Aggressive 0";
    private string _durationSummary = "Analysis duration: n/a";
    private string _filterSummary = "Filtro: todos";

    public AnalysisViewModel(
        IAnalysisSessionService analysisSessionService,
        ISystemSnapshotStore snapshotStore,
        IAnalysisResultStore analysisResultStore,
        ILogger<AnalysisViewModel> logger)
    {
        _analysisSessionService = analysisSessionService;
        _snapshotStore = snapshotStore;
        _analysisResultStore = analysisResultStore;
        _logger = logger;

        if (_analysisResultStore.Current is { } result)
        {
            ApplyResult(result);
        }
    }

    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        private set
        {
            if (SetProperty(ref _isAnalyzing, value))
            {
                OnPropertyChanged(nameof(CanAnalyze));
            }
        }
    }

    public bool CanAnalyze => !IsAnalyzing;

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    public string RulesSummary
    {
        get => _rulesSummary;
        private set => SetProperty(ref _rulesSummary, value);
    }

    public string RecommendationSummary
    {
        get => _recommendationSummary;
        private set => SetProperty(ref _recommendationSummary, value);
    }

    public string RiskDistribution
    {
        get => _riskDistribution;
        private set => SetProperty(ref _riskDistribution, value);
    }

    public string DurationSummary
    {
        get => _durationSummary;
        private set => SetProperty(ref _durationSummary, value);
    }

    public string FilterSummary
    {
        get => _filterSummary;
        private set => SetProperty(ref _filterSummary, value);
    }

    public ObservableCollection<AnalysisFindingItem> Findings { get; } = [];

    public ObservableCollection<RecommendationCardItem> Recommendations { get; } = [];

    public ObservableCollection<PresetPreviewItem> Presets { get; } = [];

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        RefreshSessionState();

        if (_snapshotStore.Current is null)
        {
            StatusText = "Execute o scanner para gerar um snapshot antes da analise.";
            SummaryText = "Sem snapshot real.";
            return;
        }

        if (_currentResult is null || _currentResult.ScanId != _snapshotStore.Current.Metadata.ScanId)
        {
            await AnalyzeCurrentSnapshotAsync(cancellationToken);
        }
    }

    public async Task AnalyzeCurrentSnapshotAsync(CancellationToken cancellationToken)
    {
        RefreshSessionState();
        if (_analysisSessionService.State is AnalysisSessionState.Running or AnalysisSessionState.Cancelling)
        {
            StatusText = "Analise ja em andamento.";
            return;
        }

        var snapshot = _snapshotStore.Current;
        if (snapshot is null)
        {
            StatusText = "Execute o scanner antes de analisar.";
            SummaryText = "Sem snapshot real.";
            return;
        }

        IsAnalyzing = true;
        StatusText = "Analisando snapshot existente.";

        try
        {
            var result = await _analysisSessionService.AnalyzeCurrentSnapshotAsync(cancellationToken);
            if (result.IsFailure || result.Value is null)
            {
                HandleAnalysisFailure(result.ErrorCode);
                return;
            }

            ApplyResult(result.Value);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Analise cancelada.";
            SummaryText = "A operacao foi interrompida antes de publicar novo resultado.";
        }
        catch (Exception exception)
        {
            ReportUnexpectedException(exception, "analysis.view_model.analyze_failed");
        }
        finally
        {
            RefreshSessionState();
        }
    }

    public void CancelActiveAnalysis()
    {
        _analysisSessionService.Cancel();
        RefreshSessionState();
    }

    public void ReportUnexpectedException(Exception exception, string context)
    {
        _logger.LogError(exception, "Analysis UI operation failed. Context={Context}", context);
        StatusText = "Nao foi possivel atualizar a analise.";
        SummaryText = "Detalhes tecnicos foram registrados nos logs.";
        IsAnalyzing = false;
    }

    public void SetPresetFilter(RecommendationPreset? preset)
    {
        _presetFilter = preset;
        ApplyFilters();
    }

    public void SetCategoryFilter(string? category)
    {
        _categoryFilter = Enum.TryParse<AnalysisCategory>(category, ignoreCase: true, out var parsed) ? parsed : null;
        ApplyFilters();
    }

    public void SetRiskFilter(string? risk)
    {
        _riskFilter = Enum.TryParse<RecommendationRiskLevel>(risk, ignoreCase: true, out var parsed) ? parsed : null;
        ApplyFilters();
    }

    private void ApplyResult(AnalysisResult result)
    {
        _currentResult = result;
        var summary = result.Summary;
        StatusText = "Analise concluida sem aplicar alteracoes.";
        SummaryText = $"Snapshot {result.ScanId} analisado. Oportunidades={summary.OpportunityCount}; Avisos={summary.WarningCount}; Bloqueios={summary.BlockedCount}.";
        RulesSummary = $"Rules {summary.RulesEvaluated} | Healthy {summary.HealthyCount} | Opportunities {summary.OpportunityCount} | Warnings {summary.WarningCount} | Blocked {summary.BlockedCount} | Unknown {summary.UnknownCount}";
        RecommendationSummary = $"Recommendations {summary.RecommendationCount}";
        RiskDistribution = FormatRiskDistribution(summary.RiskDistribution);
        DurationSummary = $"Analysis duration: {result.Duration.TotalMilliseconds:N0} ms";

        Presets.Clear();
        foreach (var preset in result.RecommendationPlan.Presets)
        {
            Presets.Add(new PresetPreviewItem(
                preset.Preset.ToString(),
                preset.EligibleRecommendationCount.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                FormatRiskDistribution(preset.RiskDistribution)));
        }

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        Recommendations.Clear();
        Findings.Clear();

        if (_currentResult is null)
        {
            return;
        }

        var recommendations = _currentResult.Recommendations.AsEnumerable();
        if (_presetFilter.HasValue)
        {
            var flag = ToEligibilityFlag(_presetFilter.Value);
            recommendations = recommendations.Where(recommendation => recommendation.PresetEligibility.HasFlag(flag));
        }

        if (_categoryFilter.HasValue)
        {
            recommendations = recommendations.Where(recommendation => recommendation.Category == _categoryFilter.Value);
        }

        if (_riskFilter.HasValue)
        {
            recommendations = recommendations.Where(recommendation => recommendation.RiskLevel == _riskFilter.Value);
        }

        var findings = _currentResult.Findings.AsEnumerable();
        if (_categoryFilter.HasValue)
        {
            findings = findings.Where(finding => finding.Category == _categoryFilter.Value);
        }

        foreach (var finding in findings.OrderBy(finding => finding.RuleId, StringComparer.Ordinal))
        {
            Findings.Add(new AnalysisFindingItem(
                finding.RuleId,
                finding.Category.ToString(),
                finding.Status.ToString(),
                finding.Title,
                finding.Summary));
        }

        foreach (var recommendation in recommendations.OrderBy(recommendation => recommendation.RecommendationId, StringComparer.Ordinal))
        {
            Recommendations.Add(new RecommendationCardItem(
                recommendation.RecommendationId,
                recommendation.Title,
                recommendation.ShortDescription,
                recommendation.Category.ToString(),
                recommendation.RiskLevel.ToString(),
                recommendation.EvidenceLevel.ToString(),
                recommendation.ExpectedImpact.ToString(),
                recommendation.Compatibility.Status.ToString(),
                recommendation.TechnicalReason,
                recommendation.DetectedState,
                recommendation.DesiredState,
                FormatList("Razoes de compatibilidade", recommendation.Compatibility.Reasons),
                FormatList("Impactos", recommendation.ImpactAreas),
                FormatList("Efeitos colaterais", recommendation.SideEffects),
                $"Reboot: {(recommendation.RebootRequired ? "Pode exigir" : "Nao indicado nesta fase")} | Reversibilidade futura: {recommendation.Reversible}",
                FormatList("Conflitos", recommendation.ConflictsWith),
                FormatList("Requisitos", recommendation.Requires),
                recommendation.UserConfirmationRequired ? "Confirmacao futura exigida" : "Sem confirmacao nesta fase read-only",
                recommendation.RiskLevel is RecommendationRiskLevel.Advanced or RecommendationRiskLevel.Aggressive
                    ? "Esta recomendacao exige cuidado por poder afetar compatibilidade, seguranca ou comportamento se virar otimizacao futura."
                    : string.Empty));
        }

        FilterSummary = $"Filtro: preset={_presetFilter?.ToString() ?? "Todos"}; categoria={_categoryFilter?.ToString() ?? "Todas"}; risco={_riskFilter?.ToString() ?? "Todos"}";
    }

    private void HandleAnalysisFailure(string? errorCode)
    {
        _logger.LogWarning("Analysis did not complete. ErrorCode={ErrorCode}", errorCode ?? "unknown");

        switch (errorCode)
        {
            case "analysis.already_running":
                StatusText = "Analise ja em andamento.";
                SummaryText = "Aguarde a sessao atual terminar antes de iniciar outra.";
                break;
            case "analysis.snapshot_missing":
                StatusText = "Execute o scanner antes de analisar.";
                SummaryText = "Sem snapshot real.";
                break;
            case "analysis.snapshot_changed":
                StatusText = "Snapshot alterado durante a analise.";
                SummaryText = "Execute a analise novamente sobre o snapshot atual.";
                break;
            case "analysis.canceled":
                StatusText = "Analise cancelada.";
                SummaryText = "Nenhum novo resultado foi publicado.";
                break;
            case "analysis.validation.failed":
                StatusText = "Catalogo de recomendacoes invalido.";
                SummaryText = "A analise foi bloqueada para evitar plano ambiguo.";
                break;
            default:
                StatusText = "Nao foi possivel concluir a analise.";
                SummaryText = "Detalhes tecnicos foram registrados nos logs.";
                break;
        }
    }

    private void RefreshSessionState()
    {
        IsAnalyzing = _analysisSessionService.State is AnalysisSessionState.Running or AnalysisSessionState.Cancelling;
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

    private static string FormatRiskDistribution(IReadOnlyDictionary<RecommendationRiskLevel, int> riskDistribution)
    {
        return $"Safe {Get(riskDistribution, RecommendationRiskLevel.Safe)} | Medium {Get(riskDistribution, RecommendationRiskLevel.Medium)} | Advanced {Get(riskDistribution, RecommendationRiskLevel.Advanced)} | Aggressive {Get(riskDistribution, RecommendationRiskLevel.Aggressive)}";
    }

    private static string FormatList(string label, IReadOnlyList<string> values)
    {
        return values.Count == 0 ? $"{label}: nenhum" : $"{label}: {string.Join("; ", values)}";
    }

    private static int Get(IReadOnlyDictionary<RecommendationRiskLevel, int> values, RecommendationRiskLevel risk)
    {
        return values.TryGetValue(risk, out var count) ? count : 0;
    }
}

public sealed record AnalysisFindingItem(
    string RuleId,
    string Category,
    string Status,
    string Title,
    string Summary);

public sealed record RecommendationCardItem(
    string RecommendationId,
    string Title,
    string ShortDescription,
    string Category,
    string Risk,
    string Evidence,
    string ExpectedImpact,
    string Compatibility,
    string TechnicalReason,
    string DetectedState,
    string DesiredState,
    string CompatibilityReasons,
    string ImpactAreas,
    string SideEffects,
    string OperationalMetadata,
    string Conflicts,
    string Requires,
    string Confirmation,
    string RiskWarning);

public sealed record PresetPreviewItem(
    string Preset,
    string EligibleCount,
    string RiskDistribution);
