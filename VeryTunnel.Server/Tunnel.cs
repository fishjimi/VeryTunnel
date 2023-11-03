using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System.Net;
using VeryTunnel.Contracts;

namespace VeryTunnel.Server;

internal class Tunnel : ITunnel
{
    private readonly int _agentPort;
    private int _serverPort;
    private readonly Func<Task> _onClose;

    private IChannel boundChannel;
    private MultithreadEventLoopGroup bossGroup;
    private MultithreadEventLoopGroup workerGroup;
    private ServerBootstrap bootstrap;

    public Tunnel(int agentPort, int serverPort, Func<Task> onClose)
    {
        _agentPort = agentPort;
        _serverPort = serverPort;
        _onClose = onClose;
    }

    public int AgentPort => _agentPort;
    public int ServerPort => _serverPort;

    public IList<ITunnelSession> Sessions => throw new NotImplementedException();

    internal async Task<int> StartTunnel()
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
            //.Handler(new LoggingHandler("SRV-LSTN", LogLevel.INFO))
            .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
            {
                channel.Pipeline.AddLast(new AgentMessageHandler(_agentManager, TrigerOnAgentConnected));
            }));
        boundChannel = await bootstrap.BindAsync(_serverPort);
        //_logger.LogInformation("TunnelServer started");
        _serverPort = (boundChannel.LocalAddress as IPEndPoint).Port;
        return _serverPort;
    }

    public Task Close()
    {
        return _onClose();
    }
}
