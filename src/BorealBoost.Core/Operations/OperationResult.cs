namespace BorealBoost.Core.Operations;

public sealed record OperationResult(
    bool Success,
    string Code,
    string Message,
    TimeSpan Duration,
    bool RequiresRestart,
    string? ErrorType = null,
    string? ErrorMessage = null)
{
    public static OperationResult Completed(string code, string message, TimeSpan duration, bool requiresRestart = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new OperationResult(true, code, message, duration, requiresRestart);
    }

    public static OperationResult Failed(string code, string message, string errorType, string errorMessage, TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorType);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new OperationResult(false, code, message, duration, false, errorType, errorMessage);
    }
}
