using BorealBoost.Core.Common;

namespace BorealBoost.Optimization.Execution;

public interface IOptimizationSessionLock
{
    Task<Result<IAsyncDisposable>> TryAcquireAsync(CancellationToken cancellationToken);
}

public sealed class CrossProcessOptimizationSessionLock : IOptimizationSessionLock
{
    private const string LockFileName = "optimization-session.lock";
    private readonly string _lockFilePath;

    public CrossProcessOptimizationSessionLock()
        : this(CreateDefaultLockPath())
    {
    }

    public CrossProcessOptimizationSessionLock(string lockFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockFilePath);
        _lockFilePath = Path.GetFullPath(lockFilePath);
    }

    public Task<Result<IAsyncDisposable>> TryAcquireAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var directory = Path.GetDirectoryName(_lockFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var stream = new FileStream(
                _lockFilePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);

            var payload = System.Text.Encoding.UTF8.GetBytes($"{Environment.ProcessId}|{DateTimeOffset.UtcNow:O}");
            stream.SetLength(0);
            stream.Write(payload);
            stream.Flush(flushToDisk: true);

            return Task.FromResult(Result<IAsyncDisposable>.Success(new Lease(stream)));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(Result<IAsyncDisposable>.Failure(
                "optimization.session.already_running",
                "Another optimization session is already running."));
        }
    }

    private static string CreateDefaultLockPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "BorealBoost", "Locks", LockFileName);
    }

    private sealed class Lease : IAsyncDisposable
    {
        private readonly FileStream _stream;

        public Lease(FileStream stream)
        {
            _stream = stream;
        }

        public ValueTask DisposeAsync()
        {
            return _stream.DisposeAsync();
        }
    }
}
