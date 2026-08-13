namespace BorealBoost.App.Agent;

public sealed record AgentBootstrapResult(
    bool Success,
    string DisplayStatus,
    string? AgentVersion,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static AgentBootstrapResult Completed(string agentVersion)
    {
        return new AgentBootstrapResult(true, "Agent: IPC validado", agentVersion);
    }

    public static AgentBootstrapResult Failed(string errorCode, string errorMessage)
    {
        return new AgentBootstrapResult(false, "Agent: IPC nao validado", null, errorCode, errorMessage);
    }
}
