using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;
using VeryTunnel.Contracts;
using VeryTunnel.DotNetty;
using VeryTunnel.Models;

namespace VeryTunnel.Server;

internal class AgentMessageHandler : SimpleChannelInboundHandler<ChannelMessage>, IAgent
{
    private readonly ILogger<AgentMessageHandler> _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<AgentMessageHandler>();
    private readonly IAgentManager _agentManager;
    private readonly Func<IAgent, Task> _trigerOnAgentConnected;
    private string _agentId = string.Empty;
    public string Id => _agentId;
    private readonly ConcurrentDictionary<(int agentPort, int serverPoint), Tunnel> _tunnels = new();
    public IEnumerable<ITunnel> Tunnels => _tunnels.Values;
    private readonly ConcurrentDictionary<uint, (ChannelMessage request, TaskCompletionSource<IMessage> responseTask)> _messageDic = new();
    private uint requestId = 0;
    private uint NextRequestID => Interlocked.Increment(ref requestId);

    public AgentMessageHandler(IAgentManager agentManager, Func<IAgent, Task> trigerOnAgentConnected)
    {
        _agentManager = agentManager;
        _trigerOnAgentConnected = trigerOnAgentConnected;
    }

    private IChannelHandlerContext _context;
    protected override void ChannelRead0(IChannelHandlerContext ctx, ChannelMessage msg)
    {
        _logger.LogInformation($"ChannelRead0 {msg.Message}");
        switch (msg.Message)
        {
            case DeviceConnect message:
                {
                    _agentId = message.Id;
                    _agentManager.Add(this);
                    _trigerOnAgentConnected?.Invoke(this);
                    break;
                }
            case HeartBeat message:
                {
                    ctx.Channel.WriteAndFlushAsync(message);
                    break;
                }
            case TunnelPackage message:
                {
                    if (_tunnels.TryGetValue((message.AgentPort, message.ServerPort), out var tunnel))
                    {
                        Task.Run(async () =>
                        {
                            var bytes = new byte[message.Data.Length];
                            message.Data.CopyTo(bytes, 0);
                            await tunnel.WriteBytesToSession(message.SessionId, bytes);
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

    public override void ChannelActive(IChannelHandlerContext context)
    {
        _context = context;
        base.ChannelActive(context);
    }

    public override void ChannelInactive(IChannelHandlerContext context)
    {
        _agentManager.Remove(_agentId);
    }


    private async Task SendAsync(IMessage message)
    {
        var request = new ChannelMessage { RequestId = 0, Message = message };
        await _context.Channel.WriteAndFlushAsync(request);
    }
    private async Task<IMessage> SendAndReceiveAsync(IMessage message)
    {
        var request = new ChannelMessage { RequestId = NextRequestID, Message = message };
        var tcs = new TaskCompletionSource<IMessage>();
        if (_messageDic.TryAdd(request.RequestId, (request, tcs)))
        {
            await _context.Channel.WriteAndFlushAsync(request);
            try
            {
                var response = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                return response;
            }
            finally
            {
                _messageDic.Remove(request.RequestId, out _);
            }
        }
        else
        {
            throw new InvalidOperationException();
        }
    }


    internal async Task OnTunnelSessionCreated(int agentPort, int serverPort, uint sessionId)
    {
        _logger.LogInformation($"OnTunnelSessionCreated {agentPort} {serverPort} {sessionId}");
        await SendAndReceiveAsync(new OperateTunnelSession
        {
            AgentPort = agentPort,
            ServerPort = serverPort,
            SessionId = sessionId,
            Command = OperateTunnelSession.Types.Command.Create
        });
    }

    internal async Task OnTunnelSessionBytesReceived(int agentPort, int serverPort, uint sessionId, byte[] bytes)
    {
        var m = new TunnelPackage
        {
            AgentPort = agentPort,
            ServerPort = serverPort,
            SessionId = sessionId,
            Data = ByteString.CopyFrom(bytes)
        };
        _logger.LogInformation($"监听收到 {m}");
        await SendAsync(m);
    }
    internal async Task OnTunnelSessionClose(int agentPort, int serverPort, uint sessionId)
    {
        _logger.LogInformation($"OnTunnelSessionClose {agentPort} {serverPort} {sessionId}");
        await SendAndReceiveAsync(new OperateTunnelSession
        {
            AgentPort = agentPort,
            ServerPort = serverPort,
            SessionId = sessionId,
            Command = OperateTunnelSession.Types.Command.Close
        });
    }

    public async Task<ITunnel> CreateTunnel(int agentPort, int serverPort)
    {
        var tunnel = new Tunnel(agentPort, serverPort, this);
        serverPort = await tunnel.StartTunnel();
        _tunnels.TryAdd((agentPort, serverPort), tunnel);
        return tunnel;
    }
}
