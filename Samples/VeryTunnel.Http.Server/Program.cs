using VeryTunnel.Http.Server;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddVeryTunnelServer();
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
