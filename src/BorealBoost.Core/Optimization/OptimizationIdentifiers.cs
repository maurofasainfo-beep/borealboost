namespace BorealBoost.Core.Optimization;

public readonly record struct OptimizationId(string Value)
{
    public static bool TryCreate(string? value, out OptimizationId optimizationId)
    {
        if (OptimizationIdentifierValidator.IsValid(value, "BB.OPT."))
        {
            optimizationId = new OptimizationId(value!);
            return true;
        }

        optimizationId = default;
        return false;
    }

    public override string ToString() => Value;
}

public readonly record struct OperationId(string Value)
{
    public static bool TryCreate(string? value, out OperationId operationId)
    {
        if (OptimizationIdentifierValidator.IsValid(value, "BB.OP."))
        {
            operationId = new OperationId(value!);
            return true;
        }

        operationId = default;
        return false;
    }

    public override string ToString() => Value;
}

public readonly record struct ExecutionPlanId(Guid Value)
{
    public static ExecutionPlanId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}

internal static class OptimizationIdentifierValidator
{
    private const int MaxIdentifierLength = 128;

    public static bool IsValid(string? value, string requiredPrefix)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > MaxIdentifierLength ||
            !value.StartsWith(requiredPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var character in value)
        {
            var valid = character is '.' or '_' or '-' ||
                        character is >= 'A' and <= 'Z' ||
                        character is >= '0' and <= '9';
            if (!valid)
            {
                return false;
            }
        }

        return true;
    }
}
