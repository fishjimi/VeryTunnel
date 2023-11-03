using DotNetty.Transport.Channels;
using Google.Protobuf;
using System.Collections.Concurrent;
using VeryTunnel.Contracts;
using VeryTunnel.DotNetty;
using VeryTunnel.Models;

namespace VeryTunnel.Server;

internal class AgentMessageHandler : SimpleChannelInboundHandler<ChannelMessage>, IAgent
{
    private readonly IAgentManager _agentManager;
    private readonly Func<IAgent, Task> _trigerOnAgentConnected;
    private string _agentId = string.Empty;
    public string Id => _agentId;
    private readonly ConcurrentDictionary<(int agentPort, int serverPoint), ITunnel> _tunnels = new();
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
        switch (msg.Message)
        {
            case DeviceConnect message:
                {
                    _agentId = message.Id;
                    _agentManager.Add(this);
                    _trigerOnAgentConnected?.Invoke(this);
                    break;
                }
            case TunnelPackage message:
                {
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

    private async Task DestroyTunnel(int agentPort, int serverPort)
    {
        await SendAndReceiveAsync(new OperateTunnel
        {
            AgentPort = agentPort,
            ServerPort = serverPort,
            Command = OperateTunnel.Types.Command.Destory
        });
    }

    public async Task<ITunnel> CreateTunnel(int agentPort, int serverPort, Func<ITunnel, ITunnelSession, Task> OnSessionCreated = null)
    {
        var cts = new CancellationTokenSource();
        var tunnel = new Tunnel(agentPort, serverPort, () => { cts.Cancel(); return DestroyTunnel(agentPort, serverPort); });
        serverPort = await tunnel.StartTunnel();
        try
        {
            await SendAndReceiveAsync(new OperateTunnel
            {
                AgentPort = agentPort,
                ServerPort = serverPort,
                Command = OperateTunnel.Types.Command.Create
            });
            if (_tunnels.TryAdd((agentPort, serverPort), tunnel))
                cts.Token.Register(() => _tunnels.TryRemove((agentPort, serverPort), out _));
            else
                throw new InvalidOperationException();
        }
        catch
        {
            cts.Cancel();
            await tunnel.Close();
            throw;
        }
        return tunnel;
    }
}
