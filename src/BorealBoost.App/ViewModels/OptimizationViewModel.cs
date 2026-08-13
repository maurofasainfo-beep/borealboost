using System.Collections.ObjectModel;
using BorealBoost.Core.Analysis;
using BorealBoost.Core.Optimization;
using BorealBoost.Core.Scanner;
using BorealBoost.Optimization.Catalog;
using Microsoft.Extensions.Logging;

namespace BorealBoost.App.ViewModels;

public sealed class OptimizationViewModel : ObservableObject
{
    private readonly IOptimizationCatalog _catalog;
    private readonly IDryRunService _dryRunService;
    private readonly IOptimizationSessionService _sessionService;
    private readonly ISystemSnapshotStore _snapshotStore;
    private readonly IAnalysisResultStore _analysisResultStore;
    private readonly ILogger<OptimizationViewModel> _logger;
    private DryRunResult? _lastDryRun;
    private bool _isBusy;
    private string _statusText = "Execute Scanner e Analise antes de revisar um plano.";
    private string _planSummary = "Nenhum plano criado.";
    private string _safetySummary = "Dry Run nao executado.";
    private string _executionSummary = "Nenhuma operacao modificadora executada.";

    public OptimizationViewModel(
        IOptimizationCatalog catalog,
        IDryRunService dryRunService,
        IOptimizationSessionService sessionService,
        ISystemSnapshotStore snapshotStore,
        IAnalysisResultStore analysisResultStore,
        ILogger<OptimizationViewModel> logger)
    {
        _catalog = catalog;
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
                OnPropertyChanged(nameof(CanDryRun));
                OnPropertyChanged(nameof(CanExecuteControlledOperation));
            }
        }
    }

    public bool CanDryRun => !IsBusy && _snapshotStore.Current is not null && _analysisResultStore.Current is not null;

    public bool CanExecuteControlledOperation => !IsBusy && _lastDryRun?.Validation.CanExecute == true && _lastDryRun.Blockers.Count == 0;

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

    public ObservableCollection<OptimizationOperationItem> Operations { get; } = [];

    public ObservableCollection<OptimizationIssueItem> Issues { get; } = [];

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RefreshAvailability();
        if (_snapshotStore.Current is null)
        {
            StatusText = "Sem SystemSnapshot. Execute o Scanner.";
            return Task.CompletedTask;
        }

        if (_analysisResultStore.Current is null)
        {
            StatusText = "Sem AnalysisResult. Execute a Analise.";
            return Task.CompletedTask;
        }

        StatusText = "Pronto para Dry Run do motor de otimizacao.";
        PlanSummary = "Fase 4 expõe apenas a prova controlada do motor; catalogo real fica para a Fase 5.";
        return Task.CompletedTask;
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
            var selected = new[] { BuiltInOptimizationCatalog.IntegrationProofOptimizationId };
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
            StatusText = "Execute um Dry Run valido antes da prova controlada.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _sessionService.ExecuteAsync(_lastDryRun.Plan, snapshot, cancellationToken).ConfigureAwait(true);
            if (result.IsFailure || result.Value is null)
            {
                ExecutionSummary = result.ErrorMessage ?? "Execucao controlada falhou.";
                StatusText = "Execucao controlada nao concluida.";
                return;
            }

            ExecutionSummary = $"Sessao {result.Value.SessionId} finalizada em estado {result.Value.State}. Rollback disponivel quando snapshot existir.";
            StatusText = "Prova controlada do motor concluida sem otimizar performance.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Execucao controlada cancelada.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Controlled optimization execution failed.");
            StatusText = "Nao foi possivel executar a prova controlada.";
            ExecutionSummary = "Detalhes tecnicos foram registrados nos logs.";
        }
        finally
        {
            IsBusy = false;
            RefreshAvailability();
        }
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

        PlanSummary = $"PlanId={result.Plan.PlanId}; Operations={result.Plan.OrderedOperations.Count}; Risk={result.Plan.RiskSummary.HighestRisk}; Reboot={(result.Plan.RequiresRestart ? "sim" : "nao")}; RestorePoint={result.Plan.RestorePointRequirement}.";
        SafetySummary = result.Validation.CanExecute && result.Blockers.Count == 0
            ? "Dry Run validado. Snapshot e verification sao obrigatorios antes de commit."
            : $"Dry Run bloqueado. Blockers={result.Blockers.Count}; Issues={result.Validation.Issues.Count}.";
        StatusText = "Dry Run concluido sem modificar Windows.";
    }

    private void RefreshAvailability()
    {
        OnPropertyChanged(nameof(CanDryRun));
        OnPropertyChanged(nameof(CanExecuteControlledOperation));
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
