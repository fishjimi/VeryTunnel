using VeryTunnel.Contracts;

namespace VeryTunnel.Server;

public interface ITunnelServer
{
    public Task StartAsync();
    public Task StopAsync();
    public event Func<IAgent, Task> OnAgentConnected;
    public bool TryGet(string Id, out IAgent agent);
    public IEnumerable<IAgent> Agents { get; }
}
