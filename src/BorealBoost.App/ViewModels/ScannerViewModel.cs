using System.Collections.ObjectModel;
using BorealBoost.Core.Common;
using BorealBoost.Core.Scanner;

namespace BorealBoost.App.ViewModels;

public sealed class ScannerViewModel : ObservableObject
{
    private readonly ISystemScanSessionService _scanSessionService;
    private readonly ISystemSnapshotStore _snapshotStore;
    private bool _isScanning;
    private int _progressPercent;
    private string _currentStage = "Pronto para analisar.";
    private string _statusText = "Analise ainda nao realizada.";
    private string _windowsSummary = "Unknown";
    private string _cpuSummary = "Unknown";
    private string _gpuSummary = "Unknown";
    private string _memorySummary = "Unknown";
    private string _storageSummary = "Unknown";
    private string _devicesSummary = "Unknown";
    private string _displaysSummary = "Unknown";
    private string _networkSummary = "Unknown";
    private string _servicesSummary = "Unknown";
    private string _processesSummary = "Unknown";
    private string _startupSummary = "Unknown";
    private string _providerSummary = "Success 0 | Partial 0 | Failed 0 | TimedOut 0 | NotSupported 0";
    private string _durationSummary = "Unknown";
    private string _partialScanSummary = "Nenhum scan concluido.";

    public ScannerViewModel(ISystemScanSessionService scanSessionService, ISystemSnapshotStore snapshotStore)
    {
        _scanSessionService = scanSessionService;
        _snapshotStore = snapshotStore;
        if (_snapshotStore.Current is { } snapshot)
        {
            ApplySnapshot(snapshot);
        }

        RefreshSessionState();
    }

    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (SetProperty(ref _isScanning, value))
            {
                OnPropertyChanged(nameof(CanStartScan));
                OnPropertyChanged(nameof(CanCancelScan));
            }
        }
    }

    public bool CanStartScan => !IsScanning && _scanSessionService.State is not ScanSessionState.Running and not ScanSessionState.Cancelling;

    public bool CanCancelScan => IsScanning;

    public int ProgressPercent
    {
        get => _progressPercent;
        private set
        {
            if (SetProperty(ref _progressPercent, value))
            {
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public string ProgressText => $"{ProgressPercent}%";

    public string CurrentStage
    {
        get => _currentStage;
        private set => SetProperty(ref _currentStage, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string WindowsSummary
    {
        get => _windowsSummary;
        private set => SetProperty(ref _windowsSummary, value);
    }

    public string CpuSummary
    {
        get => _cpuSummary;
        private set => SetProperty(ref _cpuSummary, value);
    }

    public string GpuSummary
    {
        get => _gpuSummary;
        private set => SetProperty(ref _gpuSummary, value);
    }

    public string MemorySummary
    {
        get => _memorySummary;
        private set => SetProperty(ref _memorySummary, value);
    }

    public string StorageSummary
    {
        get => _storageSummary;
        private set => SetProperty(ref _storageSummary, value);
    }

    public string DevicesSummary
    {
        get => _devicesSummary;
        private set => SetProperty(ref _devicesSummary, value);
    }

    public string DisplaysSummary
    {
        get => _displaysSummary;
        private set => SetProperty(ref _displaysSummary, value);
    }

    public string NetworkSummary
    {
        get => _networkSummary;
        private set => SetProperty(ref _networkSummary, value);
    }

    public string ServicesSummary
    {
        get => _servicesSummary;
        private set => SetProperty(ref _servicesSummary, value);
    }

    public string ProcessesSummary
    {
        get => _processesSummary;
        private set => SetProperty(ref _processesSummary, value);
    }

    public string StartupSummary
    {
        get => _startupSummary;
        private set => SetProperty(ref _startupSummary, value);
    }

    public string ProviderSummary
    {
        get => _providerSummary;
        private set => SetProperty(ref _providerSummary, value);
    }

    public string DurationSummary
    {
        get => _durationSummary;
        private set => SetProperty(ref _durationSummary, value);
    }

    public string PartialScanSummary
    {
        get => _partialScanSummary;
        private set => SetProperty(ref _partialScanSummary, value);
    }

    public ObservableCollection<ProviderResultItem> ProviderResults { get; } = [];

    public async Task StartScanAsync()
    {
        if (IsScanning)
        {
            StatusText = "Analise ja em andamento.";
            return;
        }

        IsScanning = true;
        ProgressPercent = 0;
        CurrentStage = "Preparando analise";
        StatusText = "Coletando fatos do computador.";

        var progress = new Progress<ScanProgressUpdate>(update =>
        {
            ProgressPercent = update.Percent;
            CurrentStage = update.Stage;
        });

        try
        {
            var result = await _scanSessionService.StartAsync(progress, CancellationToken.None);
            if (result.IsFailure || result.Value is null)
            {
                StatusText = result.ErrorCode switch
                {
                    "scanner.canceled" => "Analise cancelada.",
                    "scanner.already_running" => "Analise ja em andamento.",
                    _ => "Nao foi possivel concluir a analise."
                };
                CurrentStage = "Analise interrompida";
                return;
            }

            ApplySnapshot(result.Value);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Analise cancelada.";
            CurrentStage = "Analise interrompida";
        }
        finally
        {
            RefreshSessionState();
        }
    }

    public void CancelScan()
    {
        _scanSessionService.Cancel();
        if (_scanSessionService.State == ScanSessionState.Cancelling)
        {
            IsScanning = true;
            CurrentStage = "Cancelando analise";
            StatusText = "Cancelamento solicitado.";
        }
    }

    private void ApplySnapshot(SystemSnapshot snapshot)
    {
        ProgressPercent = 100;
        CurrentStage = "Analise concluida";
        StatusText = snapshot.Metadata.PartialScan
            ? "Analise concluida com dados parciais."
            : "Analise concluida.";
        WindowsSummary = FormatWindows(snapshot.OperatingSystem);
        CpuSummary = FormatCpu(snapshot.Processors);
        GpuSummary = $"{snapshot.Graphics.Count} GPU(s): {FirstOrUnknown(snapshot.Graphics.Select(gpu => gpu.Name))}";
        MemorySummary = FormatMemory(snapshot.Memory);
        StorageSummary = $"{snapshot.Storage.Disks.Count} disco(s), {snapshot.Storage.Volumes.Count} volume(s)";
        DevicesSummary = FormatDevices(snapshot.Devices);
        DisplaysSummary = $"{snapshot.Displays.Count} display(s)";
        NetworkSummary = $"{snapshot.Network.Count} adaptador(es)";
        ServicesSummary = $"{snapshot.Services.Count} servico(s)";
        ProcessesSummary = $"{snapshot.Processes.Count} processo(s)";
        StartupSummary = $"{snapshot.StartupItems.Count} item(ns)";
        DurationSummary = $"{snapshot.Metadata.Duration.TotalSeconds:N1}s";
        PartialScanSummary = snapshot.Metadata.PartialScan
            ? "Algumas informacoes nao puderam ser identificadas."
            : "Todos os providers concluiram sem falha.";

        ProviderResults.Clear();
        foreach (var provider in snapshot.Metadata.ProviderResults.OrderBy(result => result.ProviderName, StringComparer.Ordinal))
        {
            ProviderResults.Add(new ProviderResultItem(
                provider.ProviderName,
                provider.Status.ToString(),
                $"{provider.Duration.TotalMilliseconds:N0} ms",
                provider.Errors.Count == 0 ? string.Empty : provider.Errors[0].Code));
        }

        ProviderSummary = FormatProviderSummary(snapshot.Metadata.ProviderResults);
    }

    private static string FormatWindows(OperatingSystemSnapshot os)
    {
        return $"{os.Name ?? "Unknown"} build {os.Build?.ToString() ?? "Unknown"} ({os.BorealBoostCompatibility})";
    }

    private static string FormatCpu(IReadOnlyList<CpuSnapshot> processors)
    {
        if (processors.Count == 0)
        {
            return "Unknown";
        }

        var first = processors[0];
        return $"{first.Name ?? "Unknown"} | {first.LogicalProcessors} threads";
    }

    private static string FormatDevices(IReadOnlyList<DeviceSnapshot> devices)
    {
        if (devices.Count == 0)
        {
            return "0 dispositivo(s)";
        }

        var problems = devices.Count(device => device.HealthStatus is DeviceHealthStatus.MissingDriver or DeviceHealthStatus.Problem or DeviceHealthStatus.Disabled);
        return $"{devices.Count} dispositivo(s), {problems} com problema objetivo";
    }

    private static string FirstOrUnknown(IEnumerable<string?> values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "Unknown";
    }

    private static string FormatBytes(ulong? bytes)
    {
        if (bytes is null)
        {
            return "Unknown";
        }

        var gib = bytes.Value / 1024d / 1024d / 1024d;
        return $"{gib:N1} GB";
    }

    private static string FormatMemory(MemorySnapshot memory)
    {
        var installed = FormatBytes(memory.InstalledPhysicalBytes);
        var visible = FormatBytes(memory.VisiblePhysicalBytes);
        return $"Instalada: {installed} | Visivel ao Windows: {visible} | {memory.ModuleCount} modulo(s)";
    }

    private static string FormatProviderSummary(IReadOnlyList<ProviderResult> providers)
    {
        var success = providers.Count(provider => provider.Status == ProviderResultStatus.Success);
        var partial = providers.Count(provider => provider.Status == ProviderResultStatus.Partial);
        var failed = providers.Count(provider => provider.Status == ProviderResultStatus.Failed);
        var timedOut = providers.Count(provider => provider.Status == ProviderResultStatus.TimedOut);
        var notSupported = providers.Count(provider => provider.Status == ProviderResultStatus.NotSupported);

        return $"Success {success} | Partial {partial} | Failed {failed} | TimedOut {timedOut} | NotSupported {notSupported}";
    }

    private void RefreshSessionState()
    {
        IsScanning = _scanSessionService.State is ScanSessionState.Running or ScanSessionState.Cancelling;
        if (IsScanning)
        {
            CurrentStage = _scanSessionService.State == ScanSessionState.Cancelling ? "Cancelando analise" : "Analise em andamento";
            StatusText = _scanSessionService.State == ScanSessionState.Cancelling ? "Cancelamento solicitado." : "Coletando fatos do computador.";
        }
    }
}

public sealed record ProviderResultItem(string ProviderName, string Status, string Duration, string ErrorCode);
