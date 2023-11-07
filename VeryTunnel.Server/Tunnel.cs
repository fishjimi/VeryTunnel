using DotNetty.Buffers;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using VeryTunnel.Contracts;

namespace VeryTunnel.Server;

internal class Tunnel : ITunnel
{
    public event Action<ITunnel> OnClosed;
    public event Action<ITunnelSession> OnSessionCreated;
    public event Action<ITunnelSession> OnSessionClosed;

    private readonly int _agentPort;
    private int _serverPort;
    private readonly AgentMessageHandler _agent;

    private IChannel boundChannel;
    private MultithreadEventLoopGroup bossGroup;
    private MultithreadEventLoopGroup workerGroup;
    private ServerBootstrap bootstrap;
    private readonly ILogger<Tunnel> _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<Tunnel>();

    public Tunnel(int agentPort, int serverPort, AgentMessageHandler agent)
    {
        _agentPort = agentPort;
        _serverPort = serverPort;
        _agent = agent;
    }

    public int AgentPort => _agentPort;
    public int ServerPort => _serverPort;

    private ConcurrentDictionary<uint, TunnelSession> _sessions = new();
    public IEnumerable<ITunnelSession> Sessions => _sessions.Values;

    private uint _sessionId;
    private uint NextSessionId => Interlocked.Increment(ref _sessionId);


    internal async Task RequestAgentCreateSession(uint sessionId)
    {
        await _agent.RequestAgentCreateSession(_agentPort, _serverPort, sessionId);
    }
    internal async Task SendBytesToAgent(uint sessionId, byte[] bytes)
    {
        await _agent.SendBytesToAgent(_agentPort, _serverPort, sessionId, bytes);
    }
    internal void TrigerOnSessionCreated(ITunnelSession session) => OnSessionCreated?.Invoke(session);

    private readonly SemaphoreSlim _semaphore = new(1);
    internal async Task WriteBytesToSession(uint sessionId, byte[] bytes)
    {
        try
        {
            await _semaphore.WaitAsync();
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                await session.WriteBytes(bytes);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    internal async Task<int> StartTunnel()
    {
        bossGroup = new MultithreadEventLoopGroup(1);
        workerGroup = new MultithreadEventLoopGroup();
        bootstrap = new ServerBootstrap();
        bootstrap
           .Group(bossGroup, workerGroup)
           .Channel<TcpServerSocketChannel>()
           .ChildOption(ChannelOption.TcpNodelay, true)
           .ChildOption(ChannelOption.SoKeepalive, true)
           .ChildOption(ChannelOption.SoReuseaddr, true)
           .Option(ChannelOption.SoReuseport, true)
           .Option(ChannelOption.SoBacklog, 1000)
           //.Handler(new LoggingHandler("SRV-LSTN", LogLevel.INFO))
           .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
           {
               var sessionId = NextSessionId;
               var session = new TunnelSession(sessionId, this);
               channel.Pipeline.AddLast(session);
               if (_sessions.TryAdd(sessionId, session))
               {
                   channel.CloseCompletion.ContinueWith(async t =>
                   {
                       _logger.LogInformation($"连接关闭 sessionId {sessionId} CloseCompletion");
                       _sessions.TryRemove(sessionId, out _);
                       OnSessionClosed?.Invoke(session);
                       await _agent.OnTunnelSessionClose(_agentPort, _serverPort, sessionId);
                   });
               }
           }));
        boundChannel = await bootstrap.BindAsync(_serverPort);
        _ = boundChannel.CloseCompletion.ContinueWith(_ => AfterClose());
        //_logger.LogInformation("TunnelServer started");
        _serverPort = (boundChannel.LocalAddress as IPEndPoint).Port;
        return _serverPort;
    }

    public async Task Close()
    {
        //CloseCompletion 如果耗时，下面这句话会很快结束吗?
        await (boundChannel?.CloseAsync() ?? Task.CompletedTask);
        await Task.WhenAll(
            bossGroup?.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100)) ?? Task.CompletedTask,
            workerGroup?.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100)) ?? Task.CompletedTask);
        //_logger.LogInformation("TunnelServer stopped");
    }

    private async Task AfterClose()
    {
        OnClosed?.Invoke(this);
        //CloseAllSessions
        foreach (var session in _sessions)
        {
            await session.Value.Close();
        }
    }
}
