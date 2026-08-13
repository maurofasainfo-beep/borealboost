using BorealBoost.Core.Optimization;

namespace BorealBoost.Optimization.Handlers;

public sealed class OperationHandlerRegistry : IOperationHandlerRegistry
{
    private readonly IReadOnlyDictionary<OperationType, IOperationHandler> _handlers;

    public OperationHandlerRegistry(IEnumerable<IOperationHandler> handlers)
    {
        _handlers = handlers
            .GroupBy(handler => handler.OperationType)
            .ToDictionary(group => group.Key, group => group.First());
    }

    public IReadOnlyList<OperationType> SupportedOperationTypes => _handlers.Keys.Order().ToArray();

    public bool TryGetHandler(OperationType operationType, out IOperationHandler handler)
    {
        return _handlers.TryGetValue(operationType, out handler!);
    }
}
