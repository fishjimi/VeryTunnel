using DotNetty.Codecs.Protobuf;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using VeryTunnel.DotNetty;

namespace VeryTunnel.Server;

public class VeryTunnelServer
{
    private readonly ILoggerProvider _loggerProvider;
    private readonly IServiceProvider _services;
    private readonly ILogger<VeryTunnelServer> _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<VeryTunnelServer>();

    private IChannel boundChannel;
    private readonly MultithreadEventLoopGroup bossGroup = new(1);
    private readonly MultithreadEventLoopGroup workerGroup = new();
    private readonly ServerBootstrap bootstrap = new();

    private readonly AgentManager _agentManager;

    public VeryTunnelServer(AgentManager agentManager)
    {
        _agentManager = agentManager;
    }

    public async Task Start()
    {
        //InternalLoggerFactory.DefaultFactory.AddProvider(_loggerProvider);
        //bossGroup = new MultithreadEventLoopGroup(1);
        //workerGroup = new MultithreadEventLoopGroup();
        //bootstrap = new ServerBootstrap();

        bootstrap
            .Group(bossGroup, workerGroup)
            .Channel<TcpServerSocketChannel>()
            .ChildOption(ChannelOption.TcpNodelay, true)
            .ChildOption(ChannelOption.SoKeepalive, true)
            .ChildOption(ChannelOption.SoReuseaddr, true)
            .Option(ChannelOption.SoReuseport, true)
            .Option(ChannelOption.SoBacklog, 1000)
            //.Handler(new LoggingHandler("SRV-LSTN", LogLevel.INFO))
            .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
            {
                //var scope = _services.CreateScope();
                //channel.CloseCompletion.ContinueWith(_ => scope.Dispose());
                channel.Pipeline.AddLast(new ProtobufVarint32FrameDecoder());
                channel.Pipeline.AddLast(new ProtobufVarint32LengthFieldPrepender());
                channel.Pipeline.AddLast(new MessageDecoder());
                channel.Pipeline.AddLast(new MessageEncoder());
                channel.Pipeline.AddLast(new HeartBeatReadIdleHandler(40));
                channel.Pipeline.AddLast(new AgentMessageHandler(_agentManager));
            }));
        boundChannel = await bootstrap.BindAsync(2000);
        _logger.LogInformation("DotNetty Server started");
    }
}
