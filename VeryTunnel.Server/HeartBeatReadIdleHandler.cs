using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;

namespace VeryTunnel.Server;

internal class HeartBeatReadIdleHandler : IdleStateHandler
{
    private readonly ILogger<HeartBeatReadIdleHandler> _logger;
    public HeartBeatReadIdleHandler(int idleTimeSeconds) : base(0, 0, idleTimeSeconds)
    {
        _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<HeartBeatReadIdleHandler>();
    }

    protected override void ChannelIdle(IChannelHandlerContext context, IdleStateEvent stateEvent)
    {
        _logger.LogDebug($"{context.Channel.Id}:{context.Channel.RemoteAddress} {stateEvent.State}:心跳超时");
        context.Channel.CloseAsync();
    }
}
