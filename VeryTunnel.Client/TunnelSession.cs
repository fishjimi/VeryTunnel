using DotNetty.Buffers;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using VeryTunnel.Contracts;

namespace VeryTunnel.Client
{
    internal class TunnelSession : ChannelHandlerAdapter, ITunnelSession
    {
        public int AgentPort { get; }
        private int _serverPort;
        public int ServerPort => _serverPort;
        public uint SessionId { get; }

        private readonly SemaphoreSlim _semaphore = new(1);


        private readonly ILogger<TunnelSession> _logger;
        private readonly AgentMessageHandler _agent;

        private readonly Channel<byte[]> _upStream = Channel.CreateUnbounded<byte[]>();
        private readonly Channel<byte[]> _downStream = Channel.CreateUnbounded<byte[]>();

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

        public override void ChannelActive(IChannelHandlerContext context)
        {
            Task.Run(async () =>
            {
                await foreach (var bytes in _downStream.Reader.ReadAllAsync())
                {
                    IByteBuffer buf = Unpooled.WrappedBuffer(bytes);
                    await clientChannel.WriteAndFlushAsync(buf);
                }
            });
            Task.Run(async () =>
            {
                await foreach (var bytes in _upStream.Reader.ReadAllAsync())
                {
                    await _agent.OnLocalBytesReceived(AgentPort, _serverPort, SessionId, bytes);
                }
            });
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            _downStream.Writer.TryComplete();
            _upStream.Writer.TryComplete();
        }

        public void Start()
        {
            bootstrap
                .Group(group)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    channel.Pipeline.AddLast(this);
                }));

            clientChannel = bootstrap.ConnectAsync("127.0.0.1", AgentPort).ConfigureAwait(false).GetAwaiter().GetResult();
            //_logger.LogInformation("TunnelSession Started");
        }

        public async Task Close()
        {
            await (clientChannel?.CloseAsync() ?? Task.CompletedTask);
            await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)); ;
            //_logger.LogInformation("TunnelSession stopped");
        }


        public async Task WriteBytesToLocal(byte[] bytes)
        {
            await _downStream.Writer.WriteAsync(bytes);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            //Task.Run(async () =>
            //{
            IByteBuffer buf = message as IByteBuffer;
            var bytes = new byte[buf.ReadableBytes];
            buf.ReadBytes(bytes);
            _upStream.Writer.WriteAsync(bytes).AsTask().Wait();
            buf.Release();
            //});
        }
    }
}
