using BorealBoost.Core.Analysis;

namespace BorealBoost.Analysis.RecommendationEngine;

public sealed class InMemoryAnalysisResultStore : IAnalysisResultStore
{
    private readonly object _syncRoot = new();
    private AnalysisResult? _current;

    public AnalysisResult? Current
    {
        get
        {
            lock (_syncRoot)
            {
                return _current;
            }
        }
    }

    public void Set(AnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        lock (_syncRoot)
        {
            _current = result;
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
