namespace VeryTunnel.Client.Sample
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly VeryTunnelClient _client;

        public Worker(ILogger<Worker> logger, VeryTunnelClient client)
        {
            _logger = logger;
            _client = client;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _client.StartAsync();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.StopAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}