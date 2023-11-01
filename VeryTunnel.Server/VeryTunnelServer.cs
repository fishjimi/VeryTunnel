using DotNetty.Codecs.Protobuf;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using VeryTunnel.Contracts;
using VeryTunnel.DotNetty;

namespace VeryTunnel.Server;

public class VeryTunnelServer : ITunnelServer
{
    private readonly IAgentManager _agentManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<VeryTunnelServer> _logger;

    //private IChannel boundChannel;
    //private readonly MultithreadEventLoopGroup bossGroup = new(1);
    //private readonly MultithreadEventLoopGroup workerGroup = new();
    //private readonly ServerBootstrap bootstrap = new();


    public VeryTunnelServer(IAgentManager agentManager, ILoggerFactory loggerFactory)
    {
        _agentManager = agentManager;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<VeryTunnelServer>();
    }

    public event Func<IAgent, Task> OnAgentConnected;
    public bool TryGet(string Id, out IAgent agent) => _agentManager.TryGet(Id, out agent);

    public async Task Start()
    {
        _logger.LogInformation("TunnelServer S");


        //InternalLoggerFactory.DefaultFactory.AddProvider(_loggerProvider);
        //bossGroup = new MultithreadEventLoopGroup(1);
        //workerGroup = new MultithreadEventLoopGroup();
        //bootstrap = new ServerBootstrap();

        //bootstrap
        //    .Group(bossGroup, workerGroup)
        //    .Channel<TcpServerSocketChannel>()
        //    .ChildOption(ChannelOption.TcpNodelay, true)
        //    .ChildOption(ChannelOption.SoKeepalive, true)
        //    .ChildOption(ChannelOption.SoReuseaddr, true)
        //    .Option(ChannelOption.SoReuseport, true)
        //    .Option(ChannelOption.SoBacklog, 1000)
        //    //.Handler(new LoggingHandler("SRV-LSTN", LogLevel.INFO))
        //    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
        //    {
        //        //var scope = _services.CreateScope();
        //        //channel.CloseCompletion.ContinueWith(_ => scope.Dispose());
        //        channel.Pipeline.AddLast(new ProtobufVarint32FrameDecoder());
        //        channel.Pipeline.AddLast(new ProtobufVarint32LengthFieldPrepender());
        //        channel.Pipeline.AddLast(new MessageDecoder());
        //        channel.Pipeline.AddLast(new MessageEncoder());
        //        channel.Pipeline.AddLast(new HeartBeatReadIdleHandler(40));
        //        channel.Pipeline.AddLast(new AgentMessageHandler(_agentManager));
        //    }));
        //boundChannel = await bootstrap.BindAsync(2000);
        //_logger.LogInformation("DotNetty Server started");
    }

}
