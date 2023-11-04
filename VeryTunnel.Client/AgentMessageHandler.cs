using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;
using VeryTunnel.Contracts;
using VeryTunnel.DotNetty;
using VeryTunnel.Models;

namespace VeryTunnel.Client;

internal class AgentMessageHandler : SimpleChannelInboundHandler<ChannelMessage>
{
    private readonly ILogger<AgentMessageHandler> _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<AgentMessageHandler>();
    private string _agentId = string.Empty;
    public string Id => _agentId;
    private readonly ConcurrentDictionary<(int agentPort, int serverPoint, uint sessionId), TunnelSession> _tunnelSessions = new();
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
        _logger.LogInformation($"发送 {m}");
        await _context.WriteAndFlushAsync(m);
    }

    protected override void ChannelRead0(IChannelHandlerContext ctx, ChannelMessage msg)
    {
        _logger.LogInformation($"ChannelRead0 {msg.Message}");
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
                                    var tunnelSession = new TunnelSession(message.AgentPort, message.ServerPort, message.SessionId, this);
                                    _tunnelSessions.TryAdd((message.AgentPort, message.ServerPort, message.SessionId), tunnelSession);
                                    _logger.LogInformation($"TunnelSession Created {message.AgentPort}, {message.ServerPort}, {message.SessionId}");
                                    await tunnelSession.StartAsync();
                                    break;
                                }
                            case OperateTunnelSession.Types.Command.Close:
                                {
                                    if (_tunnelSessions.TryRemove((message.AgentPort, message.ServerPort, message.SessionId), out var tunnelSession))
                                    {
                                        await tunnelSession.Close();
                                        _logger.LogInformation($"TunnelSession Closed {message.AgentPort}, {message.ServerPort}, {message.SessionId}");
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
                    if (_tunnelSessions.TryGetValue((message.AgentPort, message.ServerPort, message.SessionId), out var tunnelSession))
                    {
                        Task.Run(async () =>
                        {
                            var bytes = new byte[message.Data.Length];
                            message.Data.CopyTo(bytes, 0);
                            await tunnelSession.WriteBytesToLocal(bytes);
                        });
                    }
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
