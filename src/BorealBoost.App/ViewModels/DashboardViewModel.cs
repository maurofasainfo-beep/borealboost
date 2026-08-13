using BorealBoost.Core.Foundation;
using BorealBoost.App.Agent;
using BorealBoost.Core.Scanner;
using BorealBoost.Infrastructure.Configuration;

namespace BorealBoost.App.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly IAgentBootstrapService _agentBootstrapService;
    private readonly bool _agentProbeEnabled;
    private string _agentStatus = "Agent: probe desabilitado";

    public DashboardViewModel(
        IApplicationInfoProvider applicationInfoProvider,
        IAdminStatusProvider adminStatusProvider,
        IBasicSystemInfoProvider systemInfoProvider,
        IAgentBootstrapService agentBootstrapService,
        ApplicationSettings settings,
        ISystemSnapshotStore snapshotStore)
    {
        _agentBootstrapService = agentBootstrapService;
        _agentProbeEnabled = settings.EnableAgentHandshakeProbe;
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
        MachineName = "Nome da maquina oculto por privacidade.";

        var snapshot = snapshotStore.Current;
        ScanStatus = snapshot is null
            ? "Analise ainda nao realizada."
            : $"Ultima analise concluida em {snapshot.Metadata.CompletedAtUtc.LocalDateTime:g}.";
        HardwareSummary = snapshot is null
            ? "Execute o scanner para preencher fatos reais."
            : FormatHardwareSummary(snapshot);
        DriverStatusSummary = snapshot is null
            ? "Status de dispositivos indisponivel ate o scan."
            : FormatDriverStatus(snapshot.Devices);
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

    public string ScanStatus { get; }

    public string HardwareSummary { get; }

    public string DriverStatusSummary { get; }

    public async Task ProbeAgentAsync(CancellationToken cancellationToken)
    {
        if (!_agentProbeEnabled)
        {
            AgentStatus = "Agent: probe desabilitado";
            return;
        }

        try
        {
            var result = await _agentBootstrapService.ProbeAsync(cancellationToken);
            AgentStatus = result.Success
                ? $"{result.DisplayStatus} v{result.AgentVersion}"
                : $"{result.DisplayStatus} ({result.ErrorCode})";
        }
        catch (OperationCanceledException)
        {
            AgentStatus = "Agent: probe cancelado";
        }
    }

    private static string FormatHardwareSummary(SystemSnapshot snapshot)
    {
        var cpu = snapshot.Processors.FirstOrDefault()?.Name ?? "CPU Unknown";
        var gpu = snapshot.Graphics.FirstOrDefault()?.Name ?? "GPU Unknown";
        var ram = snapshot.Memory.InstalledPhysicalBytes ?? snapshot.Memory.VisiblePhysicalBytes;
        var ramSummary = ram is { } bytes
            ? $"{bytes / 1024d / 1024d / 1024d:N1} GB RAM"
            : "RAM Unknown";
        var windows = snapshot.OperatingSystem.Name ?? "Windows Unknown";

        return $"{windows} | {cpu} | {gpu} | {ramSummary}";
    }

    private static string FormatDriverStatus(IReadOnlyList<DeviceSnapshot> devices)
    {
        var problemCount = devices.Count(device => device.HealthStatus is DeviceHealthStatus.MissingDriver or DeviceHealthStatus.Problem or DeviceHealthStatus.Disabled);
        return problemCount == 0
            ? "Nenhum problema objetivo de dispositivo detectado no ultimo scan."
            : $"{problemCount} dispositivo(s) apresentam problema objetivo.";
    }
}
