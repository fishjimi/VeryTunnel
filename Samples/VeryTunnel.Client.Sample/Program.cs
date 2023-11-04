using VeryTunnel.Client.Sample;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddVeryTunnelClient();
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
