using VeryTunnel.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class Extensions
{
    public static IServiceCollection AddVeryTunnelClient(this IServiceCollection services, Action<VeryTunnelClientOptions> configure = null)
    {
        services.AddSingleton<VeryTunnelClient>();
        if (configure != null)
            services.Configure(configure);
        return services;
    }
}