namespace BorealBoost.Core.Identity;

public readonly record struct SessionId(Guid Value)
{
    public static SessionId New() => new(Guid.NewGuid());

    public static bool TryParse(string? value, out SessionId sessionId)
    {
        if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
        {
            sessionId = new SessionId(parsed);
            return true;
        }

        sessionId = default;
        return false;
    }

    public override string ToString() => Value.ToString("D");
}

public readonly record struct CorrelationId(Guid Value)
{
    public static CorrelationId New() => new(Guid.NewGuid());

    public static bool TryParse(string? value, out CorrelationId correlationId)
    {
        if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
        {
            correlationId = new CorrelationId(parsed);
            return true;
        }

        correlationId = default;
        return false;
    }

    public override string ToString() => Value.ToString("D");
}

public readonly record struct RequestId(Guid Value)
{
    public static RequestId New() => new(Guid.NewGuid());

    public static bool TryParse(string? value, out RequestId requestId)
    {
        if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
        {
            requestId = new RequestId(parsed);
            return true;
        }

        requestId = default;
        return false;
    }

    public override string ToString() => Value.ToString("D");
}
