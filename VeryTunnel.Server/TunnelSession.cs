using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System.Buffers;
using VeryTunnel.Contracts;

namespace VeryTunnel.Server;

internal class TunnelSession : ChannelHandlerAdapter, ITunnelSession
{
    private readonly uint _sessionId;
    private IChannelHandlerContext _context;
    private readonly Tunnel _tunnel;

    public uint SessionId => _sessionId;

    public TunnelSession(uint sessionId, Tunnel tunnel)
    {
        _sessionId = sessionId;
        _tunnel = tunnel;
    }

    public override void ChannelRead(IChannelHandlerContext context, object message)
    {
        Task.Run(async () =>
        {
            IByteBuffer buf = message as IByteBuffer;
            var bytes = new byte[buf.ReadableBytes];
            buf.ReadBytes(bytes);
            await _tunnel.OnSessionBytesReceived(_sessionId, bytes);
        });
    }

    public override void ChannelActive(IChannelHandlerContext context)
    {
        _context = context;
        Task.Run(async () => await _tunnel.OnSessionCreated(_sessionId));
        base.ChannelActive(context);
    }

    public async Task WriteBytes(byte[] bytes)
    {
        IByteBuffer buf = Unpooled.WrappedBuffer(bytes);
        await _context.Channel.WriteAndFlushAsync(buf);
    }

    public Task Close()
    {
        return _context.CloseAsync();
    }
}
