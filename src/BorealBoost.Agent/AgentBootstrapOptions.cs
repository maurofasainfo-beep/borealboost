using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Identity;

namespace BorealBoost.Agent;

public sealed record AgentBootstrapOptions(
    string? PipeName,
    string? LocalPipeName,
    SessionId? SessionId,
    string? BootstrapNonce,
    ProtocolVersion? ProtocolVersion)
{
    public bool IsHandshakeBootstrapRequested =>
        !string.IsNullOrWhiteSpace(PipeName) ||
        SessionId is not null ||
        !string.IsNullOrWhiteSpace(BootstrapNonce) ||
        ProtocolVersion is not null;
}
