using System.Reflection;
using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Foundation;

namespace BorealBoost.Infrastructure.Metadata;

public sealed class AssemblyApplicationInfoProvider : IApplicationInfoProvider
{
    public Core.Foundation.ApplicationInfo GetApplicationInfo()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 1, 0, 0);

        return new Core.Foundation.ApplicationInfo(
            Name: "BorealBoost",
            Version: version,
            Phase: "Fase 1 - Foundation",
            AgentProtocolVersion: ProtocolVersion.Current);
    }
}
