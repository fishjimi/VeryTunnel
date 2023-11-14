using VeryTunnel.Server;

namespace Microsoft.Extensions.DependencyInjection;

public static class Extensions
{
    public static IServiceCollection AddVeryTunnelServer(this IServiceCollection services, Action<VeryTunnelServerOptions> configure = null)
    {
        services.AddSingleton<ITunnelServer, VeryTunnelServer>();
        services.AddSingleton<IAgentManager, DefaultAgentManager>();
        if (configure != null)
            services.Configure(configure);
        return services;
    }
}
