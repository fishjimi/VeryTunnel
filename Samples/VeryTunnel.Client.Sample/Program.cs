using VeryTunnel.Client.Sample;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddVeryTunnelClient();
        //services.AddVeryTunnelClient(c => c.AgentName = "AgentName");
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
