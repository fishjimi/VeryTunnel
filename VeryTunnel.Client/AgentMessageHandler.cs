using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using VeryTunnel.DotNetty;
using VeryTunnel.Models;

namespace VeryTunnel.Client;

internal class AgentMessageHandler : SimpleChannelInboundHandler<ChannelMessage>
{
    private readonly ILogger<AgentMessageHandler> _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<AgentMessageHandler>();
    private string _agentId = string.Empty;
    public string Id => _agentId;
    private readonly ConcurrentDictionary<(int agentPort, int serverPoint, uint sessionId), Lazy<TunnelSession>> _tunnelSessions = new();
    private readonly ConcurrentDictionary<uint, (ChannelMessage request, TaskCompletionSource<IMessage> responseTask)> _messageDic = new();


    private IChannelHandlerContext _context;
    public override void ChannelActive(IChannelHandlerContext context)
    {
        _context = context;
        _context.WriteAndFlushAsync(new ChannelMessage
        {
            Message = new DeviceConnect
            {
                Id = Environment.MachineName
            }
        });
    }

    public async Task OnLocalBytesReceived(int agentPort, int serverPoint, uint sessionId, byte[] bytes)
    {
        var m = new ChannelMessage
        {
            Message = new TunnelPackage
            {
                AgentPort = agentPort,
                ServerPort = serverPoint,
                SessionId = sessionId,
                Data = ByteString.CopyFrom(bytes)
            }
        };
        await _context.WriteAndFlushAsync(m);
    }

    private Lazy<TunnelSession> CreateTunnelSession((int agentPort, int serverPort, uint sessionId) param)
    {
        return new Lazy<TunnelSession>(() =>
        {
            var tunnelSession = new TunnelSession(param.agentPort, param.serverPort, param.sessionId, this);
            tunnelSession.Start();
            return tunnelSession;
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    protected override async void ChannelRead0(IChannelHandlerContext ctx, ChannelMessage msg)
    {
        switch (msg.Message)
        {
            case OperateTunnelSession message:
                {
                    msg.ResponseId = msg.RequestId;
                    switch (message.Command)
                    {
                        case OperateTunnelSession.Types.Command.Create:
                            {
                                var tunnelSession = _tunnelSessions.GetOrAdd((message.AgentPort, message.ServerPort, message.SessionId), CreateTunnelSession);
                                break;
                            }
                        case OperateTunnelSession.Types.Command.Close:
                            {
                                if (_tunnelSessions.TryRemove((message.AgentPort, message.ServerPort, message.SessionId), out var tunnelSession))
                                {
                                    await tunnelSession.Value.Close();
                                }
                                break;
                            }
                    }
                    await ctx.Channel.WriteAndFlushAsync(msg);
                    break;
                }
            case TunnelPackage message:
                {
                    var tunnelSession = _tunnelSessions.GetOrAdd((message.AgentPort, message.ServerPort, message.SessionId), CreateTunnelSession);
                    var bytes = new byte[message.Data.Length];
                    message.Data.CopyTo(bytes, 0);
                    await tunnelSession.Value.WriteBytesToLocal(bytes);
                    break;
                }
            default:
                {
                    if (_messageDic.TryGetValue(msg.ResponseId, out var tcs))
                    {
                        tcs.responseTask.SetResult(msg.Message);
                    }
                    break;
                }
        }
    }
}