using DotNetty.Codecs.Protobuf;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VeryTunnel.Contracts;
using VeryTunnel.DotNetty;
using LogLevel = DotNetty.Handlers.Logging.LogLevel;

namespace VeryTunnel.Server;

public class VeryTunnelServer : ITunnelServer
{
    private readonly IAgentManager _agentManager;
    private readonly ILogger<VeryTunnelServer> _logger;
    private readonly IOptions<VeryTunnelServerOptions> _options;


    private IChannel boundChannel;
    private MultithreadEventLoopGroup bossGroup;
    private MultithreadEventLoopGroup workerGroup;
    private ServerBootstrap bootstrap;


    public VeryTunnelServer(IAgentManager agentManager, IOptions<VeryTunnelServerOptions> options, ILoggerFactory loggerFactory)
    {
        _agentManager = agentManager;
        InternalLoggerFactory.DefaultFactory = loggerFactory;
        _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<VeryTunnelServer>();
        _options = options;
    }

    public event Func<IAgent, Task> OnAgentConnected;
    private Task TrigerOnAgentConnected(IAgent agent) => OnAgentConnected?.Invoke(agent) ?? Task.CompletedTask;

    public event Func<IAgent, Task> OnAgentDisConnected;
    private Task TrigerOnAgentDisConnected(IAgent agent) => OnAgentDisConnected?.Invoke(agent) ?? Task.CompletedTask;

    public bool TryGet(string Id, out IAgent agent) => _agentManager.TryGet(Id, out agent);
    public IEnumerable<IAgent> Agents => _agentManager.Agents;

    public async Task StartAsync()
    {
        bossGroup = new MultithreadEventLoopGroup(1);
        workerGroup = new MultithreadEventLoopGroup();
        bootstrap = new ServerBootstrap();
        bootstrap
            .Group(bossGroup, workerGroup)
            .Channel<TcpServerSocketChannel>()
            .ChildOption(ChannelOption.TcpNodelay, true)
            .ChildOption(ChannelOption.SoKeepalive, true)
            .ChildOption(ChannelOption.SoReuseaddr, true)
            .Option(ChannelOption.SoReuseport, true)
            .Option(ChannelOption.SoBacklog, 1000)
            .Handler(new LoggingHandler("SRV-LSTN", LogLevel.INFO))
            .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
            {
                channel.Pipeline.AddLast(new ProtobufVarint32FrameDecoder());
                channel.Pipeline.AddLast(new ProtobufVarint32LengthFieldPrepender());
                channel.Pipeline.AddLast(new MessageDecoder());
                channel.Pipeline.AddLast(new MessageEncoder());
                channel.Pipeline.AddLast(new HeartBeatReadIdleHandler(40));
                channel.Pipeline.AddLast(new AgentMessageHandler(_agentManager, TrigerOnAgentConnected, TrigerOnAgentDisConnected));
            }));
        boundChannel = await bootstrap.BindAsync(_options.Value.ServerPort);
        _logger.LogInformation("TunnelServer started");
    }

    public async Task StopAsync()
    {
        await boundChannel.CloseAsync();
        await Task.WhenAll(
            bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
            workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
        _logger.LogInformation("TunnelServer stopped");
    }

}
