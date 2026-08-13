namespace BorealBoost.Tests.System;

internal sealed class ControlledRegistryTestScope : IDisposable
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);
    private readonly FileStream _lockStream;

    private ControlledRegistryTestScope(FileStream lockStream)
    {
        _lockStream = lockStream;
    }

    public static ControlledRegistryTestScope Acquire()
    {
        var directory = Path.Combine(Path.GetTempPath(), "BorealBoostTests");
        Directory.CreateDirectory(directory);
        var lockPath = Path.Combine(directory, "controlled-registry.lock");
        var deadline = DateTimeOffset.UtcNow + WaitTimeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                var stream = new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.DeleteOnClose);
                return new ControlledRegistryTestScope(stream);
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
        }

        throw new TimeoutException("Timed out waiting for the BorealBoost controlled registry test scope.");
    }

    public void Dispose()
    {
        _lockStream.Dispose();
    }
}
