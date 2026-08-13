namespace BorealBoost.App.Agent;

public interface IAgentBootstrapService
{
    Task<AgentBootstrapResult> ProbeAsync(CancellationToken cancellationToken);
}
