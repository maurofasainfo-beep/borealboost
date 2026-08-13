using System.Diagnostics;
using BorealBoost.Analysis.RecommendationEngine;
using BorealBoost.Analysis.RecommendationEngine.Rules;
using BorealBoost.Analysis.SystemScanner;
using BorealBoost.App.Agent;
using BorealBoost.App.Navigation;
using BorealBoost.App.Pages;
using BorealBoost.App.ViewModels;
using BorealBoost.Core.Analysis;
using BorealBoost.Core.Foundation;
using BorealBoost.Core.Scanner;
using BorealBoost.Infrastructure.DependencyInjection;
using BorealBoost.Infrastructure.Logging;
using BorealBoost.Infrastructure.Paths;
using BorealBoost.System;
using BorealBoost.System.Registry;
using BorealBoost.System.Scanner;
using BorealBoost.System.Wmi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace BorealBoost.App;

public partial class App : Application
{
    private readonly IHost _host;
    private bool _shutdownStarted;

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        _host = CreateHostBuilder().Build();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            await _host.StartAsync();

            var window = _host.Services.GetRequiredService<MainWindow>();
            window.Closed += OnMainWindowClosed;
            window.Activate();
        }
        catch (Exception exception)
        {
            LogStartupException(exception);
            Exit();
        }
    }

    private static IHostBuilder CreateHostBuilder()
    {
        var pathService = new ApplicationPathService();
        pathService.EnsureUserWritableDirectories();
        var paths = pathService.GetPaths();

        return Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new JsonFileLoggerProvider(paths.LogsDirectory, "app"));
            })
            .ConfigureServices((context, services) =>
            {
                services.AddBorealBoostInfrastructure(context.Configuration);
                services.AddSingleton<IAdminStatusProvider, WindowsAdminStatusProvider>();
                services.AddSingleton<IBasicSystemInfoProvider, BasicSystemInfoProvider>();
                services.AddSingleton<IAgentBootstrapService, AgentBootstrapService>();
                services.AddSingleton<ISystemSnapshotStore, InMemorySystemSnapshotStore>();
                services.AddSingleton<ISystemScanner, SystemScanner>();
                services.AddSingleton<ISystemScanSessionService, SystemScanSessionService>();
                services.AddSingleton<IAnalysisResultStore, InMemoryAnalysisResultStore>();
                services.AddSingleton<IAnalysisEngine, AnalysisEngine>();
                services.AddSingleton<IAnalysisSessionService, AnalysisSessionService>();
                services.AddSingleton<IAnalysisRule, PartialScanAnalysisRule>();
                services.AddSingleton<IAnalysisRule, WindowsCompatibilityAnalysisRule>();
                services.AddSingleton<IAnalysisRule, MissingDriverAnalysisRule>();
                services.AddSingleton<IAnalysisRule, ProblemDeviceAnalysisRule>();
                services.AddSingleton<IAnalysisRule, BasicDisplayAdapterAnalysisRule>();
                services.AddSingleton<IAnalysisRule, LowSystemDriveSpaceAnalysisRule>();
                services.AddSingleton<IAnalysisRule, VirtualMachineAnalysisRule>();
                services.AddSingleton<IAnalysisRule, PowerContextAnalysisRule>();
                services.AddSingleton<IAnalysisRule, StartupVolumeAnalysisRule>();
                services.AddSingleton<IAnalysisRule, SecurityCapabilitiesAnalysisRule>();
                services.AddSingleton<IAnalysisRule, MemoryVisibilityAnalysisRule>();
                services.AddSingleton<WmiQueryService>();
                services.AddSingleton<ReadOnlyRegistryReader>();
                services.AddSingleton<ISystemScanProvider, OperatingSystemScanProvider>();
                services.AddSingleton<ISystemScanProvider, CpuScanProvider>();
                services.AddSingleton<ISystemScanProvider, GraphicsScanProvider>();
                services.AddSingleton<ISystemScanProvider, MemoryScanProvider>();
                services.AddSingleton<ISystemScanProvider, StorageScanProvider>();
                services.AddSingleton<ISystemScanProvider, HardwareFirmwareScanProvider>();
                services.AddSingleton<ISystemScanProvider, DisplayScanProvider>();
                services.AddSingleton<ISystemScanProvider, NetworkScanProvider>();
                services.AddSingleton<ISystemScanProvider, DevicesScanProvider>();
                services.AddSingleton<ISystemScanProvider, DriverInventoryScanProvider>();
                services.AddSingleton<ISystemScanProvider, PowerScanProvider>();
                services.AddSingleton<ISystemScanProvider, ServicesScanProvider>();
                services.AddSingleton<ISystemScanProvider, ProcessesScanProvider>();
                services.AddSingleton<ISystemScanProvider, StartupScanProvider>();
                services.AddSingleton<ISystemScanProvider, SecurityCapabilitiesScanProvider>();

                services.AddSingleton<INavigationService>(provider => new NavigationService(
                    () => provider.GetRequiredService<DashboardPage>(),
                    () => provider.GetRequiredService<ScannerPage>(),
                    () => provider.GetRequiredService<AnalysisPage>(),
                    () => provider.GetRequiredService<PlaceholderPage>()));
                services.AddSingleton<MainViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<ScannerViewModel>();
                services.AddSingleton<AnalysisViewModel>();
                services.AddTransient<PlaceholderViewModel>();
                services.AddTransient<MainWindow>();
                services.AddTransient<DashboardPage>();
                services.AddTransient<ScannerPage>();
                services.AddTransient<AnalysisPage>();
                services.AddTransient<PlaceholderPage>();
            });
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
    {
        try
        {
            var logger = _host.Services.GetService<ILogger<App>>();
            logger?.LogError(args.Exception, "Unhandled UI exception.");
            args.Handled = false;
        }
        catch (Exception loggingException)
        {
            Debug.WriteLine(loggingException);
        }
    }

    private async void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        if (_shutdownStarted)
        {
            return;
        }

        _shutdownStarted = true;
        try
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
        }
    }

    private void LogStartupException(Exception exception)
    {
        try
        {
            var logger = _host.Services.GetService<ILogger<App>>();
            logger?.LogCritical(exception, "BorealBoost startup failed.");
        }
        catch (Exception loggingException)
        {
            Debug.WriteLine(loggingException);
        }
    }
}
