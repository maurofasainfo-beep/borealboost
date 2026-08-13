using BorealBoost.Core.Analysis;
using BorealBoost.Core.Common;
using BorealBoost.Core.Scanner;
using Microsoft.Extensions.Logging;

namespace BorealBoost.Analysis.RecommendationEngine;

public sealed class AnalysisSessionService : IAnalysisSessionService
{
    private readonly object _syncRoot = new();
    private readonly IAnalysisEngine _analysisEngine;
    private readonly ISystemSnapshotStore _snapshotStore;
    private readonly IAnalysisResultStore _analysisResultStore;
    private readonly ILogger<AnalysisSessionService> _logger;
    private CancellationTokenSource? _activeCancellation;
    private AnalysisSessionState _state = AnalysisSessionState.Idle;

    public AnalysisSessionService(
        IAnalysisEngine analysisEngine,
        ISystemSnapshotStore snapshotStore,
        IAnalysisResultStore analysisResultStore,
        ILogger<AnalysisSessionService> logger)
    {
        _analysisEngine = analysisEngine;
        _snapshotStore = snapshotStore;
        _analysisResultStore = analysisResultStore;
        _logger = logger;
    }

    public AnalysisSessionState State
    {
        get
        {
            lock (_syncRoot)
            {
                return _state;
            }
        }
    }

    public AnalysisResult? Current => _analysisResultStore.Current;

    public async Task<Result<AnalysisResult>> AnalyzeCurrentSnapshotAsync(CancellationToken cancellationToken)
    {
        CancellationTokenSource sessionCancellation;
        SystemSnapshot snapshot;

        lock (_syncRoot)
        {
            if (_state is AnalysisSessionState.Running or AnalysisSessionState.Cancelling)
            {
                return Result<AnalysisResult>.Failure("analysis.already_running", "An analysis session is already running.");
            }

            if (_snapshotStore.Current is not { } currentSnapshot)
            {
                return Result<AnalysisResult>.Failure("analysis.snapshot_missing", "No system snapshot is available for analysis.");
            }

            snapshot = currentSnapshot;
            _state = AnalysisSessionState.Running;
            _activeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            sessionCancellation = _activeCancellation;
        }

        try
        {
            _logger.LogInformation("Analysis session started. ScanId={ScanId}", snapshot.Metadata.ScanId);

            var result = await _analysisEngine.AnalyzeAsync(snapshot, sessionCancellation.Token).ConfigureAwait(false);
            if (result.IsFailure || result.Value is null)
            {
                SetState(AnalysisSessionState.Failed);
                return result;
            }

            if (_snapshotStore.Current?.Metadata.ScanId != snapshot.Metadata.ScanId)
            {
                SetState(AnalysisSessionState.Failed);
                _logger.LogWarning(
                    "Analysis session discarded because snapshot changed during analysis. OriginalScanId={OriginalScanId}; CurrentScanId={CurrentScanId}",
                    snapshot.Metadata.ScanId,
                    _snapshotStore.Current?.Metadata.ScanId.ToString() ?? "None");
                return Result<AnalysisResult>.Failure("analysis.snapshot_changed", "System snapshot changed while analysis was running.");
            }

            _analysisResultStore.Set(result.Value);
            SetState(AnalysisSessionState.Completed);
            _logger.LogInformation("Analysis session completed. AnalysisId={AnalysisId}; ScanId={ScanId}", result.Value.AnalysisId, result.Value.ScanId);
            return result;
        }
        catch (OperationCanceledException)
        {
            SetState(AnalysisSessionState.Cancelled);
            _logger.LogInformation("Analysis session canceled. ScanId={ScanId}", snapshot.Metadata.ScanId);
            return Result<AnalysisResult>.Failure("analysis.canceled", "Analysis was canceled.");
        }
        catch (Exception exception)
        {
            SetState(AnalysisSessionState.Failed);
            _logger.LogError(exception, "Analysis session failed unexpectedly. ScanId={ScanId}", snapshot.Metadata.ScanId);
            return Result<AnalysisResult>.Failure("analysis.failed", "Analysis failed unexpectedly.");
        }
        finally
        {
            lock (_syncRoot)
            {
                if (ReferenceEquals(_activeCancellation, sessionCancellation))
                {
                    _activeCancellation = null;
                }
            }

            sessionCancellation.Dispose();
        }
    }

    public void Cancel()
    {
        CancellationTokenSource? cancellation;
        lock (_syncRoot)
        {
            if (_state != AnalysisSessionState.Running)
            {
                return;
            }

            _state = AnalysisSessionState.Cancelling;
            cancellation = _activeCancellation;
        }

        _logger.LogInformation("Analysis session cancellation requested.");
        cancellation?.Cancel();
    }

    private void SetState(AnalysisSessionState state)
    {
        lock (_syncRoot)
        {
            _state = state;
        }
    }
}
