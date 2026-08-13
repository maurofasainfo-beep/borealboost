using BorealBoost.Core.Scanner;

namespace BorealBoost.Analysis.SystemScanner;

public sealed class InMemorySystemSnapshotStore : ISystemSnapshotStore
{
    private readonly object _syncRoot = new();
    private SystemSnapshot? _current;

    public SystemSnapshot? Current
    {
        get
        {
            lock (_syncRoot)
            {
                return _current;
            }
        }
    }

    public void Set(SystemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_syncRoot)
        {
            _current = snapshot;
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _current = null;
        }
    }
}
