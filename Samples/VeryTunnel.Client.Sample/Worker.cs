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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var closeCompletion = await _client.StartAsync("127.0.0.1");
                    //var closeCompletion = await _client.StartAsync("server address" , serverPort);
                    await closeCompletion;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
                _logger.LogInformation("Try reconnect after 5 seconds");
                await Task.Delay(5 * 1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.StopAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}