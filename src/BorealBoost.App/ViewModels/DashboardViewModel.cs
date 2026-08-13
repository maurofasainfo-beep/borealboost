using BorealBoost.Core.Foundation;
using BorealBoost.App.Agent;

namespace BorealBoost.App.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly IAgentBootstrapService _agentBootstrapService;
    private string _agentStatus = "Agent: nao validado";

    public DashboardViewModel(
        IApplicationInfoProvider applicationInfoProvider,
        IAdminStatusProvider adminStatusProvider,
        IBasicSystemInfoProvider systemInfoProvider,
        IAgentBootstrapService agentBootstrapService)
    {
        _agentBootstrapService = agentBootstrapService;
        var appInfo = applicationInfoProvider.GetApplicationInfo();
        var adminStatus = adminStatusProvider.GetCurrentStatus();
        var os = systemInfoProvider.GetOperatingSystemSummary();

        ApplicationName = appInfo.Name;
        Phase = appInfo.Phase;
        Version = appInfo.Version.ToString(3);
        ProtocolVersion = appInfo.AgentProtocolVersion.ToString();
        AdminStatus = adminStatus.DisplayText;
        AdminStatusKind = adminStatus.Kind;
        OperatingSystem = $"{os.Description} ({os.Architecture})";
        MachineName = os.MachineName;
    }

    public string ApplicationName { get; }

    public string Phase { get; }

    public string Version { get; }

    public string ProtocolVersion { get; }

    public string AdminStatus { get; }

    public AdminStatusKind AdminStatusKind { get; }

    public string AgentStatus
    {
        get => _agentStatus;
        private set => SetProperty(ref _agentStatus, value);
    }

    public string OperatingSystem { get; }

    public string MachineName { get; }

    public async Task ProbeAgentAsync(CancellationToken cancellationToken)
    {
        var result = await _agentBootstrapService.ProbeAsync(cancellationToken);
        AgentStatus = result.Success
            ? $"{result.DisplayStatus} v{result.AgentVersion}"
            : $"{result.DisplayStatus} ({result.ErrorCode})";
    }
}
