using System.Collections.ObjectModel;
using BorealBoost.Core.Optimization;
using Microsoft.Extensions.Logging;

namespace BorealBoost.App.ViewModels;

public sealed class RestoreViewModel : ObservableObject
{
    private readonly IOptimizationSessionStore _sessionStore;
    private readonly IRecoveryService _recoveryService;
    private readonly ILogger<RestoreViewModel> _logger;
    private string _statusText = "Nenhuma sessao carregada.";

    public RestoreViewModel(
        IOptimizationSessionStore sessionStore,
        IRecoveryService recoveryService,
        ILogger<RestoreViewModel> logger)
    {
        _sessionStore = sessionStore;
        _recoveryService = recoveryService;
        _logger = logger;
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public ObservableCollection<RestoreSessionItem> Sessions { get; } = [];

    public ObservableCollection<RecoveryCandidateItem> RecoveryCandidates { get; } = [];

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            Sessions.Clear();
            RecoveryCandidates.Clear();

            var sessions = await _sessionStore.ListAsync(cancellationToken).ConfigureAwait(true);
            foreach (var session in sessions.OrderByDescending(session => session.StartedAtUtc))
            {
                Sessions.Add(new RestoreSessionItem(
                    session.SessionId.ToString(),
                    session.State.ToString(),
                    session.StartedAtUtc.ToString("u"),
                    session.CompletedAtUtc?.ToString("u") ?? "incompleta",
                    session.Snapshot?.Items.Count.ToString(global::System.Globalization.CultureInfo.InvariantCulture) ?? "0",
                    RollbackStatus(session)));
            }

            var candidates = await _recoveryService.DetectAsync(cancellationToken).ConfigureAwait(true);
            foreach (var candidate in candidates)
            {
                RecoveryCandidates.Add(new RecoveryCandidateItem(
                    candidate.SessionId.ToString(),
                    candidate.State.ToString(),
                    candidate.SuggestedAction.ToString(),
                    candidate.IsInvalidArtifact ? $"Artefato invalido: {candidate.Reason}" : candidate.Reason));
            }

            StatusText = $"Sessoes={Sessions.Count}; Recovery={RecoveryCandidates.Count}.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Atualizacao cancelada.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Restore foundation refresh failed.");
            StatusText = "Nao foi possivel carregar sessoes de restauracao.";
        }
    }

    private static string RollbackStatus(OptimizationSession session)
    {
        if (session.Snapshot is null || session.Snapshot.Items.Count == 0)
        {
            return "Rollback indisponivel: snapshot ausente.";
        }

        if (session.Snapshot.SessionId != session.SessionId || session.Snapshot.PlanId != session.Plan.PlanId)
        {
            return "Rollback bloqueado: snapshot nao pertence a sessao.";
        }

        return session.State switch
        {
            OptimizationSessionState.Completed or OptimizationSessionState.CompletedWithWarnings => "Rollback disponivel: snapshot valido sera revalidado antes da reversao.",
            OptimizationSessionState.RolledBack => "Sessao ja revertida.",
            OptimizationSessionState.RollbackFailed => "Rollback falhou: acao manual requerida.",
            OptimizationSessionState.RecoveryRequired or OptimizationSessionState.ManualActionRequired => "Recovery/inspecao requerida antes de qualquer rollback.",
            _ => "Rollback nao disponivel neste estado."
        };
    }
}

public sealed record RestoreSessionItem(
    string SessionId,
    string State,
    string StartedAtUtc,
    string CompletedAtUtc,
    string SnapshotItems,
    string RollbackStatus);

public sealed record RecoveryCandidateItem(
    string SessionId,
    string State,
    string SuggestedAction,
    string Reason);
