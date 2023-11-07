using DotNetty.Buffers;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using VeryTunnel.Contracts;

namespace VeryTunnel.Server;

internal class TunnelSession : ChannelHandlerAdapter, ITunnelSession
{
    private readonly uint _sessionId;
    private IChannelHandlerContext _context;
    private readonly Tunnel _tunnel;
    private readonly ILogger<TunnelSession> _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<TunnelSession>();
    private readonly Channel<byte[]> _downStream = Channel.CreateUnbounded<byte[]>();
    private readonly Channel<byte[]> _upStream = Channel.CreateUnbounded<byte[]>();

    public uint SessionId => _sessionId;

    public TunnelSession(uint sessionId, Tunnel tunnel)
    {
        _sessionId = sessionId;
        _tunnel = tunnel;
    }

    public override void ChannelActive(IChannelHandlerContext context)
    {
        _context = context;
        _tunnel.TrigerOnSessionCreated(this);
        Task.Run(async () =>
        {
            await _tunnel.RequestAgentCreateSession(_sessionId);
            await foreach (var bytes in _downStream.Reader.ReadAllAsync())
            {
                IByteBuffer buf = Unpooled.Buffer();
                buf.WriteBytes(bytes);
                await _tunnel.SendBytesToAgent(_sessionId, bytes);
            }
        });
        Task.Run(async () =>
        {
            await foreach (var bytes in _upStream.Reader.ReadAllAsync())
            {
                IByteBuffer buf = Unpooled.WrappedBuffer(bytes);
                Console.WriteLine($"_upStream 吐出 {bytes.Length}");
                await _context.Channel.WriteAndFlushAsync(buf);
            }
        });
    }

    public override void ChannelInactive(IChannelHandlerContext context)
    {
        _downStream.Writer.TryComplete();
        //_logger.LogInformation($"连接关闭 sessionId {SessionId} ChannelInactive");

    }


    //private readonly SemaphoreSlim _semaphore = new(1);
    public override async void ChannelRead(IChannelHandlerContext context, object message)
    {
        //Task.Run(async () =>
        //{
        //    IByteBuffer buf = message as IByteBuffer;
        //    var bytes = new byte[buf.ReadableBytes];
        //    buf.ReadBytes(bytes);
        //    await _downStream.Writer.WriteAsync(bytes);
        //});
        try
        {
            //await _semaphore.WaitAsync();

            IByteBuffer buf = message as IByteBuffer;
            var bytes = new byte[buf.ReadableBytes];
            buf.ReadBytes(bytes);
            await _downStream.Writer.WriteAsync(bytes);
            buf.Release();
        }
        finally
        {
            //_semaphore.Release();
        }
    }

    public async Task WriteBytes(byte[] bytes)
    {
        //Console.WriteLine($"_upStream 准备写入 {bytes.Length}");
        await _upStream.Writer.WriteAsync(bytes);
    }

    public Task Close()
    {
        return _context.CloseAsync();
    }
}
