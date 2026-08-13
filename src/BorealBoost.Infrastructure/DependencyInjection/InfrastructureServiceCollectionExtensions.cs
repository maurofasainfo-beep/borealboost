using BorealBoost.Core.Foundation;
using BorealBoost.Infrastructure.Configuration;
using BorealBoost.Infrastructure.Metadata;
using BorealBoost.Infrastructure.Paths;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BorealBoost.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddBorealBoostInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(ApplicationSettings.FromConfiguration(configuration));
        services.AddSingleton<IApplicationPathService, ApplicationPathService>();
        services.AddSingleton<IApplicationInfoProvider, AssemblyApplicationInfoProvider>();

        return services;
    }
}
