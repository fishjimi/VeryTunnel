using VeryTunnel.Server;

namespace VeryTunnel.Http.Server;

public class VeryTunnelBackgroundService : BackgroundService
{
    private readonly ITunnelServer _server;
    public VeryTunnelBackgroundService(ITunnelServer server)
    {
        _server = server;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _server.StartAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _server.StopAsync();
    }
}
