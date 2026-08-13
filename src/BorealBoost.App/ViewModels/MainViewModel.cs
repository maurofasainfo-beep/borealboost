using BorealBoost.Core.Foundation;

namespace BorealBoost.App.ViewModels;

public sealed class MainViewModel
{
    public MainViewModel(IApplicationInfoProvider applicationInfoProvider, IAdminStatusProvider adminStatusProvider)
    {
        var appInfo = applicationInfoProvider.GetApplicationInfo();
        var adminStatus = adminStatusProvider.GetCurrentStatus();

        VersionText = $"{appInfo.Name} v{appInfo.Version.ToString(3)}";
        AdminStatusText = adminStatus.DisplayText;
    }

    public string VersionText { get; }

    public string AdminStatusText { get; }
}
