using BorealBoost.Analysis.SystemScanner;
using BorealBoost.Core.Common;
using BorealBoost.Core.Scanner;
using Microsoft.Extensions.Logging;

namespace BorealBoost.Tests.Unit;

public sealed class SystemScanSessionServiceTests
{
    [Fact]
    public async Task Start_rejects_second_scan_while_first_is_running()
    {
        var scanner = new BlockingScanner();
        var store = new InMemorySystemSnapshotStore();
        var service = CreateService(scanner, store);

        var first = service.StartAsync(null, CancellationToken.None);
        await scanner.WaitForInvocationAsync();

        var second = await service.StartAsync(null, CancellationToken.None);

        Assert.True(second.IsFailure);
        Assert.Equal("scanner.already_running", second.ErrorCode);
        Assert.Equal(ScanSessionState.Running, service.State);

        scanner.Complete(Result<SystemSnapshot>.Success(CreateSnapshot()));
        var firstResult = await first;

        Assert.True(firstResult.IsSuccess);
        Assert.Equal(ScanSessionState.Completed, service.State);
        Assert.NotNull(store.Current);
        Assert.Equal(1, scanner.InvocationCount);
    }

    [Fact]
    public async Task Cancel_moves_running_session_to_cancelled()
    {
        var scanner = new BlockingScanner();
        var store = new InMemorySystemSnapshotStore();
        var service = CreateService(scanner, store);

        var task = service.StartAsync(null, CancellationToken.None);
        await scanner.WaitForInvocationAsync();

        service.Cancel();
        scanner.Complete(Result<SystemSnapshot>.Failure("scanner.canceled", "System scan was canceled."));
        var result = await task;

        Assert.True(result.IsFailure);
        Assert.Equal("scanner.canceled", result.ErrorCode);
        Assert.Equal(ScanSessionState.Cancelled, service.State);
        Assert.Null(store.Current);
    }

    [Fact]
    public async Task Start_allows_new_scan_after_completion()
    {
        var scanner = new ImmediateScanner();
        var store = new InMemorySystemSnapshotStore();
        var service = CreateService(scanner, store);

        var first = await service.StartAsync(null, CancellationToken.None);
        var second = await service.StartAsync(null, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, scanner.InvocationCount);
        Assert.Equal(ScanSessionState.Completed, service.State);
    }

    [Fact]
    public async Task Start_allows_new_scan_after_cancellation()
    {
        var scanner = new CancelThenSuccessScanner();
        var store = new InMemorySystemSnapshotStore();
        var service = CreateService(scanner, store);

        var first = await service.StartAsync(null, CancellationToken.None);
        var second = await service.StartAsync(null, CancellationToken.None);

        Assert.True(first.IsFailure);
        Assert.Equal("scanner.canceled", first.ErrorCode);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, scanner.InvocationCount);
        Assert.Equal(ScanSessionState.Completed, service.State);
    }

    private static SystemScanSessionService CreateService(ISystemScanner scanner, ISystemSnapshotStore store)
    {
        return new SystemScanSessionService(scanner, store, new NoopLogger<SystemScanSessionService>());
    }

    private static SystemSnapshot CreateSnapshot()
    {
        return new SystemSnapshot(
            new ScanMetadata(
                ScanId.New(),
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds(1),
                "2.0.0",
                "2.0.0",
                "X64",
                [],
                false,
                [],
                []),
            new OperatingSystemSnapshot("Windows", null, null, 26200, null, null, "x64", WindowsCompatibilityStatus.Supported, null, DataSourceKind.Unknown),
            new HardwareSnapshot(null, null, MachineFormFactor.Unknown, false, null, DataSourceKind.Unknown),
            [],
            [],
            new MemorySnapshot(null, null, 0, [], DataSourceKind.Unknown),
            new StorageSnapshot([], [], DataSourceKind.Unknown),
            new MotherboardSnapshot(null, null, null, DataSourceKind.Unknown),
            new FirmwareSnapshot(null, null, null, null, null, DataSourceKind.Unknown),
            [],
            [],
            [],
            [],
            new PowerSnapshot(null, null, null, PowerSourceKind.Unknown, null, DataSourceKind.Unknown),
            [],
            [],
            [],
            []);
    }

    private sealed class BlockingScanner : ISystemScanner
    {
        private readonly TaskCompletionSource _invoked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<Result<SystemSnapshot>> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int InvocationCount { get; private set; }

        public Task WaitForInvocationAsync()
        {
            return _invoked.Task.WaitAsync(TimeSpan.FromSeconds(2));
        }

        public void Complete(Result<SystemSnapshot> result)
        {
            _completion.SetResult(result);
        }

        public async Task<Result<SystemSnapshot>> ScanAsync(IProgress<ScanProgressUpdate>? progress, CancellationToken cancellationToken)
        {
            InvocationCount++;
            _invoked.SetResult();
            return await _completion.Task.ConfigureAwait(false);
        }
    }

    private sealed class ImmediateScanner : ISystemScanner
    {
        public int InvocationCount { get; private set; }

        public Task<Result<SystemSnapshot>> ScanAsync(IProgress<ScanProgressUpdate>? progress, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(Result<SystemSnapshot>.Success(CreateSnapshot()));
        }
    }

    private sealed class CancelThenSuccessScanner : ISystemScanner
    {
        public int InvocationCount { get; private set; }

        public Task<Result<SystemSnapshot>> ScanAsync(IProgress<ScanProgressUpdate>? progress, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(InvocationCount == 1
                ? Result<SystemSnapshot>.Failure("scanner.canceled", "System scan was canceled.")
                : Result<SystemSnapshot>.Success(CreateSnapshot()));
        }
    }

    private sealed class NoopLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }
}
