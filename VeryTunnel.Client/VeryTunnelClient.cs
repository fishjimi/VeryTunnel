using DotNetty.Codecs.Protobuf;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VeryTunnel.DotNetty;
using LogLevel = DotNetty.Handlers.Logging.LogLevel;

namespace VeryTunnel.Client;

public class VeryTunnelClient
{
    private readonly ILogger<VeryTunnelClient> _logger;
    private readonly IOptions<VeryTunnelClientOptions> _options;

    public event Func<Task> OnClosed;

    private IChannel clientChannel;
    private readonly MultithreadEventLoopGroup group = new();
    private readonly Bootstrap bootstrap = new();

    public VeryTunnelClient(ILoggerFactory loggerFactory, IOptions<VeryTunnelClientOptions> options)
    {
        InternalLoggerFactory.DefaultFactory = loggerFactory;
        _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<VeryTunnelClient>();
        _options = options;

        bootstrap
            .Group(group)
            .Channel<TcpSocketChannel>()
            .Option(ChannelOption.TcpNodelay, true)
            .Handler(new LoggingHandler("SRV-LSTN", LogLevel.INFO))
            .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
            {
                channel.Pipeline.AddLast(new ProtobufVarint32FrameDecoder());
                channel.Pipeline.AddLast(new ProtobufVarint32LengthFieldPrepender());
                channel.Pipeline.AddLast(new MessageDecoder());
                channel.Pipeline.AddLast(new MessageEncoder());
                channel.Pipeline.AddLast(new HeartBeatReadIdleHandler(30));
                channel.Pipeline.AddLast(new AgentMessageHandler(_options.Value.AgentName));
            }));
    }

    public async Task<Task> StartAsync(string serverAddress, int serverPort = 4000)
    {
        clientChannel = await bootstrap.ConnectAsync(serverAddress, serverPort).ConfigureAwait(false);
        _logger.LogInformation("TunnelClient started");
        return clientChannel.CloseCompletion;
    }

    public async Task StopAsync()
    {
        await (clientChannel?.CloseAsync() ?? Task.CompletedTask);
        await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)); ;
        _logger.LogInformation("TunnelClient stopped");
    }
}
