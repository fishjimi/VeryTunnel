using DotNetty.Buffers;
using DotNetty.Codecs.Protobuf;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using System.Buffers;
using VeryTunnel.Contracts;
using VeryTunnel.DotNetty;

namespace VeryTunnel.Client
{
    internal class TunnelSession : ChannelHandlerAdapter, ITunnelSession
    {
        public int AgentPort { get; }
        private int _serverPort;
        public int ServerPort => _serverPort;
        public uint SessionId { get; }


        private readonly ILogger<TunnelSession> _logger;
        private readonly AgentMessageHandler _agent;

        private IChannel clientChannel;
        private readonly MultithreadEventLoopGroup group = new();
        private readonly Bootstrap bootstrap = new();

        public TunnelSession(int agentPort, int serverPort, uint sessionId, AgentMessageHandler agent)
        {
            AgentPort = agentPort;
            _serverPort = serverPort;
            SessionId = sessionId;
            _agent = agent;
            _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<TunnelSession>();
        }

        public async Task StartAsync()
        {
            bootstrap
                .Group(group)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    channel.Pipeline.AddLast(this);
                }));

            clientChannel = await bootstrap.ConnectAsync("127.0.0.1", AgentPort);

            _logger.LogInformation("TunnelSession started");
        }

        public async Task Close()
        {
            await (clientChannel?.CloseAsync() ?? Task.CompletedTask);
            await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)); ;
            _logger.LogInformation("TunnelSession stopped");
        }


        public async Task WriteBytesToLocal(byte[] bytes)
        {
            IByteBuffer buf = Unpooled.WrappedBuffer(bytes);
            await clientChannel.WriteAndFlushAsync(buf);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            Task.Run(async () =>
            {
                IByteBuffer buf = message as IByteBuffer;
                var bytes = new byte[buf.ReadableBytes];
                buf.ReadBytes(bytes);
                await _agent.OnLocalBytesReceived(AgentPort, _serverPort, SessionId, bytes);
            });
        }
    }
}
