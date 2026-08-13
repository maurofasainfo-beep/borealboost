using System.Runtime.InteropServices;
using BorealBoost.Core.Foundation;

namespace BorealBoost.System;

public sealed class BasicSystemInfoProvider : IBasicSystemInfoProvider
{
    public OperatingSystemSummary GetOperatingSystemSummary()
    {
        var description = RuntimeInformation.OSDescription.Trim();
        var architecture = RuntimeInformation.OSArchitecture.ToString();

        return new OperatingSystemSummary(description, architecture, Environment.MachineName);
    }
}
