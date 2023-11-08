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

#if NET472 || NETSTANDARD2_0
        Task.Run(async () =>
        {
            await _tunnel.RequestAgentCreateSession(_sessionId);
            while (await _downStream.Reader.WaitToReadAsync())
            {
                while (_upStream.Reader.TryRead(out var bytes))
                {
                    IByteBuffer buf = Unpooled.Buffer();
                    buf.WriteBytes(bytes);
                    await _tunnel.SendBytesToAgent(_sessionId, bytes);
                }
            }
        });
        Task.Run(async () =>
        {
            while (await _upStream.Reader.WaitToReadAsync())
            {
                while (_upStream.Reader.TryRead(out var bytes))
                {
                    IByteBuffer buf = Unpooled.WrappedBuffer(bytes);
                    await _context.Channel.WriteAndFlushAsync(buf);
                }
            }
        });
#else
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
                await _context.Channel.WriteAndFlushAsync(buf);
            }
        });
#endif

    }

    public override void ChannelInactive(IChannelHandlerContext context)
    {
        _downStream.Writer.TryComplete();

    }


    //private readonly SemaphoreSlim _semaphore = new(1);
    public override async void ChannelRead(IChannelHandlerContext context, object message)
    {
        IByteBuffer buf = message as IByteBuffer;
        var bytes = new byte[buf.ReadableBytes];
        buf.ReadBytes(bytes);
        await _downStream.Writer.WriteAsync(bytes);
        buf.Release();
    }

    public async Task WriteBytes(byte[] bytes)
    {
        await _upStream.Writer.WriteAsync(bytes);
    }

    public Task Close()
    {
        return _context.CloseAsync();
    }
}
