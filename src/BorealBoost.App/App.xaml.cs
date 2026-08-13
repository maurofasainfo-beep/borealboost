using System.Diagnostics;
using BorealBoost.App.Agent;
using BorealBoost.App.Navigation;
using BorealBoost.App.Pages;
using BorealBoost.App.ViewModels;
using BorealBoost.Core.Foundation;
using BorealBoost.Infrastructure.DependencyInjection;
using BorealBoost.Infrastructure.Logging;
using BorealBoost.Infrastructure.Paths;
using BorealBoost.System;
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

                services.AddSingleton<INavigationService>(provider => new NavigationService(
                    () => provider.GetRequiredService<DashboardPage>(),
                    () => provider.GetRequiredService<PlaceholderPage>()));
                services.AddSingleton<MainViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<PlaceholderViewModel>();
                services.AddTransient<MainWindow>();
                services.AddTransient<DashboardPage>();
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
