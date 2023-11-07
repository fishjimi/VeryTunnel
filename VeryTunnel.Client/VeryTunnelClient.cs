using DotNetty.Codecs.Protobuf;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using VeryTunnel.DotNetty;
using LogLevel = DotNetty.Handlers.Logging.LogLevel;

namespace VeryTunnel.Client
{
    public class VeryTunnelClient
    {
        private readonly ILogger<VeryTunnelClient> _logger;

        public event Func<Task> OnClosed;

        private IChannel clientChannel;
        private readonly MultithreadEventLoopGroup group = new();
        private readonly Bootstrap bootstrap = new();

        public VeryTunnelClient(ILoggerFactory loggerFactory)
        {
            InternalLoggerFactory.DefaultFactory = loggerFactory;
            _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<VeryTunnelClient>();

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
                    channel.Pipeline.AddLast(new AgentMessageHandler());
                }));
        }

        public async Task StartAsync()
        {

            clientChannel = await bootstrap.ConnectAsync("127.0.0.1", 2000).ConfigureAwait(false);
            _ = clientChannel.CloseCompletion.ContinueWith(t => OnClosed?.Invoke() ?? Task.CompletedTask);

            _logger.LogInformation("TunnelClient started");
        }

        public async Task StopAsync()
        {
            await (clientChannel?.CloseAsync() ?? Task.CompletedTask);
            await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)); ;
            _logger.LogInformation("TunnelClient stopped");
        }
    }
}
