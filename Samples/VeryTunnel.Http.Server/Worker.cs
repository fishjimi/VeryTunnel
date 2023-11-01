using VeryTunnel.Contracts;
using VeryTunnel.Server;

namespace VeryTunnel.Http.Server
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ITunnelServer _tunnelServer;

        public Worker(ILogger<Worker> logger, ITunnelServer tunnelServer)
        {
            _logger = logger;
            _tunnelServer = tunnelServer;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _tunnelServer.OnAgentConnected += TunnelServer_OnAgentConnected;
            await _tunnelServer.Start();
        }

        private async Task TunnelServer_OnAgentConnected(IAgent agent)
        {
            _logger.LogInformation($"{agent.Id} 设备上线");
            var tunnel = await agent.CreateTunnel(80, 0);
            _logger.LogInformation($"{agent.Id} 开启隧道 ServerPort:{tunnel.ServerPort} <-> AgentPort:{tunnel.AgentPort}");
        }
    }
}