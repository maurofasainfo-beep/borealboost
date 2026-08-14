using System.Collections.ObjectModel;
using BorealBoost.Core.Analysis;
using BorealBoost.Core.Optimization;
using BorealBoost.Core.Scanner;
using Microsoft.Extensions.Logging;

namespace BorealBoost.App.ViewModels;

public sealed class OptimizationViewModel : ObservableObject
{
    private readonly IOptimizationCatalog _catalog;
    private readonly IOptimizationPresetEngine _presetEngine;
    private readonly IDryRunService _dryRunService;
    private readonly IOptimizationSessionService _sessionService;
    private readonly ISystemSnapshotStore _snapshotStore;
    private readonly IAnalysisResultStore _analysisResultStore;
    private readonly ILogger<OptimizationViewModel> _logger;
    private DryRunResult? _lastDryRun;
    private RecommendationPreset _selectedPreset = RecommendationPreset.Basic;
    private bool _isBusy;
    private string _statusText = "Execute Scanner e Analise antes de revisar um plano.";
    private string _planSummary = "Nenhum plano criado.";
    private string _safetySummary = "Dry Run nao executado.";
    private string _executionSummary = "Nenhuma operacao modificadora executada.";

    public OptimizationViewModel(
        IOptimizationCatalog catalog,
        IOptimizationPresetEngine presetEngine,
        IDryRunService dryRunService,
        IOptimizationSessionService sessionService,
        ISystemSnapshotStore snapshotStore,
        IAnalysisResultStore analysisResultStore,
        ILogger<OptimizationViewModel> logger)
    {
        _catalog = catalog;
        _presetEngine = presetEngine;
        _dryRunService = dryRunService;
        _sessionService = sessionService;
        _snapshotStore = snapshotStore;
        _analysisResultStore = analysisResultStore;
        _logger = logger;
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RefreshAvailability();
            }
        }
    }

    public bool CanDryRun => !IsBusy && _snapshotStore.Current is not null && _analysisResultStore.Current is not null;

    public bool CanExecuteControlledOperation => !IsBusy && _lastDryRun?.Validation.CanExecute == true && _lastDryRun.Blockers.Count == 0;

    public string SelectedPresetText => _selectedPreset.ToString();

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string PlanSummary
    {
        get => _planSummary;
        private set => SetProperty(ref _planSummary, value);
    }

    public string SafetySummary
    {
        get => _safetySummary;
        private set => SetProperty(ref _safetySummary, value);
    }

    public string ExecutionSummary
    {
        get => _executionSummary;
        private set => SetProperty(ref _executionSummary, value);
    }

    public ObservableCollection<OptimizationCatalogItem> PresetItems { get; } = [];

    public ObservableCollection<OptimizationOperationItem> Operations { get; } = [];

    public ObservableCollection<OptimizationIssueItem> Issues { get; } = [];

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RefreshAvailability();
        if (_snapshotStore.Current is null)
        {
            StatusText = "Sem SystemSnapshot. Execute o Scanner.";
            PlanSummary = $"CatalogVersion={_catalog.CatalogVersion}; Definitions={RealDefinitionCount()}.";
            return Task.CompletedTask;
        }

        if (_analysisResultStore.Current is null)
        {
            StatusText = "Sem AnalysisResult. Execute a Analise.";
            PlanSummary = $"CatalogVersion={_catalog.CatalogVersion}; Definitions={RealDefinitionCount()}.";
            return Task.CompletedTask;
        }

        RefreshPresetPreview();
        return Task.CompletedTask;
    }

    public void SelectPreset(RecommendationPreset preset)
    {
        _selectedPreset = preset;
        _lastDryRun = null;
        Operations.Clear();
        Issues.Clear();
        OnPropertyChanged(nameof(SelectedPresetText));
        RefreshPresetPreview();
        RefreshAvailability();
    }

    public async Task RunDryRunAsync(CancellationToken cancellationToken)
    {
        var snapshot = _snapshotStore.Current;
        var analysis = _analysisResultStore.Current;
        if (snapshot is null || analysis is null)
        {
            StatusText = "Scanner e Analise sao obrigatorios antes do Dry Run.";
            RefreshAvailability();
            return;
        }

        IsBusy = true;
        try
        {
            if (PresetItems.Count == 0)
            {
                RefreshPresetPreview();
            }

            var selected = SelectedOptimizationIds();
            if (selected.Count == 0)
            {
                _lastDryRun = null;
                StatusText = "Preset sem itens executaveis automaticamente.";
                SafetySummary = "Itens bloqueados, incompativeis ou que exigem confirmacao nao entram no plano automaticamente.";
                RefreshAvailability();
                return;
            }

            var result = await _dryRunService.DryRunAsync(snapshot, analysis, selected, cancellationToken).ConfigureAwait(true);
            if (result.IsFailure || result.Value is null)
            {
                StatusText = "Dry Run falhou.";
                SafetySummary = result.ErrorMessage ?? "Falha tecnica registrada.";
                return;
            }

            _lastDryRun = result.Value;
            ApplyDryRun(result.Value);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Dry Run cancelado.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Optimization dry run failed.");
            StatusText = "Nao foi possivel concluir o Dry Run.";
            SafetySummary = "Detalhes tecnicos foram registrados nos logs.";
        }
        finally
        {
            IsBusy = false;
            RefreshAvailability();
        }
    }

    public async Task ExecuteControlledOperationAsync(CancellationToken cancellationToken)
    {
        var snapshot = _snapshotStore.Current;
        if (snapshot is null || _lastDryRun is null || !_lastDryRun.Validation.CanExecute)
        {
            StatusText = "Execute um Dry Run valido antes da execucao.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _sessionService.ExecuteAsync(_lastDryRun.Plan, snapshot, cancellationToken).ConfigureAwait(true);
            if (result.IsFailure || result.Value is null)
            {
                ExecutionSummary = result.ErrorMessage ?? "Execucao falhou.";
                StatusText = "Execucao nao concluida.";
                return;
            }

            ExecutionSummary = $"Sessao {result.Value.SessionId} finalizada em estado {result.Value.State}. Rollback disponivel quando snapshot existir.";
            StatusText = "Execucao concluida pelo pipeline seguro com verificacao.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Execucao cancelada.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Optimization execution failed.");
            StatusText = "Nao foi possivel executar o plano.";
            ExecutionSummary = "Detalhes tecnicos foram registrados nos logs.";
        }
        finally
        {
            IsBusy = false;
            RefreshAvailability();
        }
    }

    private void RefreshPresetPreview()
    {
        var snapshot = _snapshotStore.Current;
        var analysis = _analysisResultStore.Current;
        PresetItems.Clear();
        if (snapshot is null || analysis is null)
        {
            PlanSummary = $"CatalogVersion={_catalog.CatalogVersion}; Definitions={RealDefinitionCount()}; execute Scanner e Analise para calcular presets.";
            SafetySummary = "Nenhuma alteracao ocorre sem Dry Run e confirmacao.";
            return;
        }

        var selection = _presetEngine.Preview(snapshot, analysis, _selectedPreset);
        foreach (var item in selection.Items)
        {
            PresetItems.Add(new OptimizationCatalogItem(
                item.OptimizationId.ToString(),
                item.Title,
                item.Category.ToString(),
                item.TechnicalCategory.ToString(),
                item.RiskLevel.ToString(),
                item.EvidenceLevel.ToString(),
                item.ConfigurationEvidence.ToString(),
                item.ExpectedImpact.ToString(),
                item.PerformanceRelevance.ToString(),
                item.AutomaticPresetSuitability.ToString(),
                item.UserPreferenceImpact.ToString(),
                item.ConfigurationMechanism.ToString(),
                item.ActivationBoundary.ToString(),
                item.VerificationLevel.ToString(),
                item.RollbackValidationLevel.ToString(),
                string.Join(", ", item.ImpactAreas),
                item.Status.ToString(),
                item.Reason,
                item.RequiresRestart ? "Reboot requerido" : $"Activation={item.ActivationBoundary}",
                item.SupportsUndo ? $"Rollback={item.RollbackValidationLevel}" : "Sem rollback",
                item.Status == OptimizationPresetSelectionStatus.Selected,
                item.Status == OptimizationPresetSelectionStatus.Selected));
        }

        PlanSummary = $"Preset={_selectedPreset}; CatalogVersion={_catalog.CatalogVersion}; Definitions={RealDefinitionCount()}; Selected={selection.SelectedItems.Count}; RequiresConfirmation={selection.RequiresConfirmationItems.Count}; Blocked={selection.BlockedItems.Count}.";
        SafetySummary = "Dry Run ainda nao executado. Nenhuma alteracao ocorre antes da revisao do plano.";
        StatusText = "Preset calculado a partir do snapshot e da analise atuais.";
    }

    private void ApplyDryRun(DryRunResult result)
    {
        Operations.Clear();
        Issues.Clear();

        foreach (var operation in result.Operations)
        {
            Operations.Add(new OptimizationOperationItem(
                operation.OperationId.ToString(),
                operation.OperationType.ToString(),
                operation.TargetSummary,
                operation.WouldChange ? "Mudaria estado" : "Ja satisfeito",
                operation.SnapshotRequired ? "Snapshot obrigatorio" : "Snapshot nao requerido",
                operation.Reversibility.ToString()));
        }

        foreach (var issue in result.Blockers.Concat(result.Warnings).DistinctBy(issue => $"{issue.Code}:{issue.Scope}:{issue.Message}"))
        {
            Issues.Add(new OptimizationIssueItem(issue.Code, issue.Scope, issue.Message));
        }

        PlanSummary = $"Preset={_selectedPreset}; PlanId={result.Plan.PlanId}; Operations={result.Plan.OrderedOperations.Count}; Risk={result.Plan.RiskSummary.HighestRisk}; Reboot={(result.Plan.RequiresRestart ? "sim" : "nao")}; RestorePoint={result.Plan.RestorePointRequirement}.";
        SafetySummary = result.Validation.CanExecute && result.Blockers.Count == 0
            ? "Dry Run validado. Snapshot e verification sao obrigatorios antes de commit."
            : $"Dry Run bloqueado. Blockers={result.Blockers.Count}; Issues={result.Validation.Issues.Count}.";
        StatusText = "Dry Run concluido sem modificar Windows.";
    }

    private IReadOnlyList<OptimizationId> SelectedOptimizationIds()
    {
        return PresetItems
            .Where(item => item.IsSelected && item.CanSelect)
            .Select(item => new OptimizationId(item.OptimizationId))
            .Distinct()
            .OrderBy(id => id.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private int RealDefinitionCount()
    {
        return _catalog.GetDefinitions().Count(definition => definition.Category != OptimizationCategory.IntegrationTest);
    }

    private void RefreshAvailability()
    {
        OnPropertyChanged(nameof(CanDryRun));
        OnPropertyChanged(nameof(CanExecuteControlledOperation));
    }
}

public sealed class OptimizationCatalogItem : ObservableObject
{
    private bool _isSelected;

    public OptimizationCatalogItem(
        string optimizationId,
        string title,
        string category,
        string technicalCategory,
        string risk,
        string evidence,
        string configurationEvidence,
        string impact,
        string performanceRelevance,
        string automaticPresetSuitability,
        string userPreferenceImpact,
        string configurationMechanism,
        string activationBoundary,
        string verificationLevel,
        string rollbackValidationLevel,
        string impactAreas,
        string status,
        string reason,
        string reboot,
        string rollback,
        bool isSelected,
        bool canSelect)
    {
        OptimizationId = optimizationId;
        Title = title;
        Category = category;
        TechnicalCategory = technicalCategory;
        Risk = risk;
        Evidence = evidence;
        ConfigurationEvidence = configurationEvidence;
        Impact = impact;
        PerformanceRelevance = performanceRelevance;
        AutomaticPresetSuitability = automaticPresetSuitability;
        UserPreferenceImpact = userPreferenceImpact;
        ConfigurationMechanism = configurationMechanism;
        ActivationBoundary = activationBoundary;
        VerificationLevel = verificationLevel;
        RollbackValidationLevel = rollbackValidationLevel;
        ImpactAreas = impactAreas;
        Status = status;
        Reason = reason;
        Reboot = reboot;
        Rollback = rollback;
        _isSelected = isSelected;
        CanSelect = canSelect;
    }

    public string OptimizationId { get; }

    public string Title { get; }

    public string Category { get; }

    public string TechnicalCategory { get; }

    public string Risk { get; }

    public string Evidence { get; }

    public string ConfigurationEvidence { get; }

    public string Impact { get; }

    public string PerformanceRelevance { get; }

    public string AutomaticPresetSuitability { get; }

    public string UserPreferenceImpact { get; }

    public string ConfigurationMechanism { get; }

    public string ActivationBoundary { get; }

    public string VerificationLevel { get; }

    public string RollbackValidationLevel { get; }

    public string ImpactAreas { get; }

    public string Status { get; }

    public string Reason { get; }

    public string Reboot { get; }

    public string Rollback { get; }

    public bool CanSelect { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (CanSelect)
            {
                SetProperty(ref _isSelected, value);
            }
        }
    }
}

public sealed record OptimizationOperationItem(
    string OperationId,
    string OperationType,
    string Target,
    string ChangeStatus,
    string SnapshotStatus,
    string Reversibility);

public sealed record OptimizationIssueItem(
    string Code,
    string Scope,
    string Message);
