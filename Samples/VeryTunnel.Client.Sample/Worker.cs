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
            _client.OnClosed += async () =>
            {
                Console.WriteLine("Reconnect After 1 second");
                await Task.Delay(1000);
                //这里会卡死，要处理一下自动重连
                await _client.StartAsync();
            };
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.StopAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}