using VeryTunnel.Server;

namespace Microsoft.Extensions.DependencyInjection;

public static class Extensions
{
    public static IServiceCollection AddVeryTunnelServer(this IServiceCollection services)
    {
        services.AddSingleton<ITunnelServer, VeryTunnelServer>();
        services.AddSingleton<IAgentManager, DefaultAgentManager>();
        return services;
    }
}
