using VeryTunnel.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class Extensions
{
    public static IServiceCollection AddVeryTunnelClient(this IServiceCollection services)
    {
        services.AddSingleton<VeryTunnelClient>();
        return services;
    }
}
