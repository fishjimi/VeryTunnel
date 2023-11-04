using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using VeryTunnel.DotNetty;
using VeryTunnel.Models;

namespace VeryTunnel.Client;

internal class HeartBeatReadIdleHandler : IdleStateHandler
{
    private readonly ILogger<HeartBeatReadIdleHandler> _logger;
    public HeartBeatReadIdleHandler(int idleTimeSeconds) : base(0, idleTimeSeconds, 0)
    {
        _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<HeartBeatReadIdleHandler>();
    }

    protected override void ChannelIdle(IChannelHandlerContext context, IdleStateEvent stateEvent)
    {
        context.Channel.WriteAndFlushAsync(new ChannelMessage { Message = new HeartBeat() });
    }
}
