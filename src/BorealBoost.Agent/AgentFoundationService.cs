using BorealBoost.Core.Foundation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BorealBoost.Agent;

public sealed class AgentFoundationService : IHostedService
{
    private readonly AgentBootstrapOptions _options;
    private readonly IApplicationInfoProvider _applicationInfoProvider;
    private readonly AgentIpcSession _ipcSession;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<AgentFoundationService> _logger;
    private Task? _runningTask;

    public AgentFoundationService(
        AgentBootstrapOptions options,
        IApplicationInfoProvider applicationInfoProvider,
        AgentIpcSession ipcSession,
        IHostApplicationLifetime applicationLifetime,
        ILogger<AgentFoundationService> logger)
    {
        _options = options;
        _applicationInfoProvider = applicationInfoProvider;
        _ipcSession = ipcSession;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var appInfo = _applicationInfoProvider.GetApplicationInfo();

        _logger.LogInformation(
            "BorealBoost.Agent foundation started. BootstrapRequested={BootstrapRequested}; Protocol={ProtocolVersion}; PrivilegedOperationHandlers=1; PerformanceTweaks=0",
            _options.IsHandshakeBootstrapRequested,
            _options.ProtocolVersion?.ToString() ?? "none");

        Console.WriteLine($"BorealBoost.Agent {appInfo.Version} foundation");
        Console.WriteLine("Allowlisted operation handlers: 1 controlled integration handler");
        Console.WriteLine("Performance tweak handlers: 0");
        Console.WriteLine("Arbitrary command execution: disabled");

        if (_options.IsHandshakeBootstrapRequested)
        {
            _runningTask = _ipcSession.RunAsync(cancellationToken);
        }
        else
        {
            _applicationLifetime.StopApplication();
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BorealBoost.Agent foundation stopped.");
        if (_runningTask is not null)
        {
            await Task.WhenAny(_runningTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken)).ConfigureAwait(false);
        }
    }
}
