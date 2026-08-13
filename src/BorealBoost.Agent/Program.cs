using BorealBoost.Agent;
using BorealBoost.Infrastructure.DependencyInjection;
using BorealBoost.Infrastructure.Logging;
using BorealBoost.Infrastructure.Paths;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var parseResult = AgentBootstrapOptionsParser.Parse(args);
if (parseResult.IsFailure || parseResult.Value is null)
{
    Console.Error.WriteLine(parseResult.ErrorMessage);
    return 2;
}

var pathService = new ApplicationPathService();
pathService.EnsureUserWritableDirectories();
var paths = pathService.GetPaths();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddProvider(new JsonFileLoggerProvider(paths.LogsDirectory, "agent"));
    })
    .ConfigureServices((context, services) =>
    {
        services.AddBorealBoostInfrastructure(context.Configuration);
        services.AddSingleton(parseResult.Value);
        services.AddSingleton<AgentIpcSession>();
        services.AddHostedService<AgentFoundationService>();
    })
    .Build();

await host.RunAsync();
return 0;
