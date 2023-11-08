using VeryTunnel.Contracts;

namespace VeryTunnel.Server;

public interface IAgentManager
{
    public void Add(IAgent agent);
    public bool TryRemove(string agentId, out IAgent agent);
    public bool TryGet(string Id, out IAgent agent);
    public IEnumerable<IAgent> Agents { get; }
}
