using VeryTunnel.Contracts;

namespace VeryTunnel.Server;

public interface ITunnelServer
{
    public Task Start();
    public bool TryGet(string Id, out IAgent agent);
    public event Func<IAgent, Task> OnAgentConnected;
}
