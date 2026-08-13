using BorealBoost.Core.AgentProtocol;

namespace BorealBoost.Core.Foundation;

public sealed record ApplicationInfo(
    string Name,
    Version Version,
    string Phase,
    ProtocolVersion AgentProtocolVersion);

public interface IApplicationInfoProvider
{
    ApplicationInfo GetApplicationInfo();
}
