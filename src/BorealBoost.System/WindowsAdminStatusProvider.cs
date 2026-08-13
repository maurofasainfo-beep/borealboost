using System.Security.Principal;
using BorealBoost.Core.Foundation;

namespace BorealBoost.System;

public sealed class WindowsAdminStatusProvider : IAdminStatusProvider
{
    public AdminStatus GetCurrentStatus()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);

        return principal.IsInRole(WindowsBuiltInRole.Administrator)
            ? AdminStatus.Active()
            : AdminStatus.Required();
    }
}
