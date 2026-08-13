using BorealBoost.Core.Common;
using BorealBoost.Core.Scanner;
using Microsoft.Extensions.Logging;

namespace BorealBoost.Analysis.SystemScanner;

public sealed class SystemScanSessionService : ISystemScanSessionService
{
    private readonly object _syncRoot = new();
    private readonly ISystemScanner _scanner;
    private readonly ISystemSnapshotStore _snapshotStore;
    private readonly ILogger<SystemScanSessionService> _logger;
    private CancellationTokenSource? _activeCancellation;
    private ScanSessionState _state = ScanSessionState.Idle;

    public SystemScanSessionService(
        ISystemScanner scanner,
        ISystemSnapshotStore snapshotStore,
        ILogger<SystemScanSessionService> logger)
    {
        _scanner = scanner;
        _snapshotStore = snapshotStore;
        _logger = logger;
    }

    public ScanSessionState State
    {
        get
        {
            lock (_syncRoot)
            {
                return _state;
            }
        }
    }

    public async Task<Result<SystemSnapshot>> StartAsync(IProgress<ScanProgressUpdate>? progress, CancellationToken cancellationToken)
    {
        CancellationTokenSource sessionCancellation;
        lock (_syncRoot)
        {
            if (_state is ScanSessionState.Running or ScanSessionState.Cancelling)
            {
                return Result<SystemSnapshot>.Failure("scanner.already_running", "A system scan is already running.");
            }

            _state = ScanSessionState.Running;
            _activeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            sessionCancellation = _activeCancellation;
        }

        try
        {
            var result = await Task.Run(() => _scanner.ScanAsync(progress, sessionCancellation.Token), CancellationToken.None).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                _snapshotStore.Set(result.Value);
                SetState(ScanSessionState.Completed);
            }
            else if (result.ErrorCode == "scanner.canceled")
            {
                SetState(ScanSessionState.Cancelled);
            }
            else
            {
                SetState(ScanSessionState.Failed);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            SetState(ScanSessionState.Cancelled);
            return Result<SystemSnapshot>.Failure("scanner.canceled", "System scan was canceled.");
        }
        catch (Exception exception)
        {
            SetState(ScanSessionState.Failed);
            _logger.LogError(exception, "System scan session failed unexpectedly.");
            return Result<SystemSnapshot>.Failure("scanner.failed", "System scan failed unexpectedly.");
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
            if (_state != ScanSessionState.Running)
            {
                return;
            }

            _state = ScanSessionState.Cancelling;
            cancellation = _activeCancellation;
        }

        _logger.LogInformation("System scan cancellation requested.");
        cancellation?.Cancel();
    }

    private void SetState(ScanSessionState state)
    {
        lock (_syncRoot)
        {
            _state = state;
        }
    }
}
