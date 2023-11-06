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
    private uint requestId = 0;
    private uint NextRequestID => Interlocked.Increment(ref requestId);


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


    int num = 0;
    int num1 = 0;
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
        //_logger.LogInformation($"local 端口发送数据 {num++}");
        await _context.WriteAndFlushAsync(m);
    }

    private Lazy<TunnelSession> CreateTunnelSession((int agentPort, int serverPort, uint sessionId) param)
    {
        return new Lazy<TunnelSession>(() =>
        {
            var tunnelSession = new TunnelSession(param.agentPort, param.serverPort, param.sessionId, this);
            tunnelSession.Start();
            //_logger.LogInformation($"TunnelSession Started {param.agentPort}, {param.serverPort}, {param.sessionId}");
            return tunnelSession;
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    protected override void ChannelRead0(IChannelHandlerContext ctx, ChannelMessage msg)
    {
        //_logger.LogInformation($"ChannelRead0 {msg.Message}");
        switch (msg.Message)
        {
            case OperateTunnelSession message:
                {
                    msg.ResponseId = msg.RequestId;
                    Task.Run(async () =>
                    {
                        switch (message.Command)
                        {
                            case OperateTunnelSession.Types.Command.Create:
                                {
                                    //_logger.LogInformation($"OperateTunnelSession");
                                    var tunnelSession = _tunnelSessions.GetOrAdd((message.AgentPort, message.ServerPort, message.SessionId), CreateTunnelSession);
                                    break;
                                }
                            case OperateTunnelSession.Types.Command.Close:
                                {
                                    if (_tunnelSessions.TryRemove((message.AgentPort, message.ServerPort, message.SessionId), out var tunnelSession))
                                    {
                                        await tunnelSession.Value.Close();
                                        //_logger.LogInformation($"TunnelSession Closed {message.AgentPort}, {message.ServerPort}, {message.SessionId}");
                                    }
                                    break;
                                }
                        }
                        await ctx.Channel.WriteAndFlushAsync(msg);
                    });
                    break;
                }
            case TunnelPackage message:
                {
                    //_logger.LogInformation($"TunnelPackage");
                    var tunnelSession = _tunnelSessions.GetOrAdd((message.AgentPort, message.ServerPort, message.SessionId), CreateTunnelSession);
                    //_logger.LogInformation($"TunnelPackage _tunnelSessions.GetOrAdd");
                    Task.Run(async () =>
                    {
                        var bytes = new byte[message.Data.Length];
                        message.Data.CopyTo(bytes, 0);
                        //_logger.LogInformation($"Local 端口接受数据 {num1++}");
                        await tunnelSession.Value.WriteBytesToLocal(bytes);
                    });
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
