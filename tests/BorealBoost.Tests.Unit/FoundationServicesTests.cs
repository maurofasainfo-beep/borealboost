using BorealBoost.Core.Foundation;
using BorealBoost.Infrastructure.Paths;
using BorealBoost.System;

namespace BorealBoost.Tests.Unit;

public sealed class FoundationServicesTests
{
    [Fact]
    public void Path_service_keeps_mutable_logs_out_of_program_files()
    {
        var paths = new ApplicationPathService().GetPaths();

        Assert.DoesNotContain("Program Files", paths.LogsDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(Path.Combine("BorealBoost", "Logs"), paths.LogsDirectory);
    }

    [Fact]
    public void Admin_status_provider_returns_real_status_shape()
    {
        var status = new WindowsAdminStatusProvider().GetCurrentStatus();

        Assert.True(status.Kind is AdminStatusKind.Active or AdminStatusKind.Required);
        Assert.StartsWith("Administrador:", status.DisplayText);
    }
}
