using System.Collections.Concurrent;
using VeryTunnel.Contracts;

namespace VeryTunnel.Server;

internal class DefaultAgentManager : IAgentManager
{
    private readonly ConcurrentDictionary<string, IAgent> _agents = new();

    public void Add(IAgent agent)
    {
        _agents.AddOrUpdate(agent.AgentName, agent, (agentId, oldAgent) => agent);
    }

    public bool TryRemove(string agentId, out IAgent agent)
    {
#if NET472 || NETSTANDARD2_0
        return _agents.TryRemove(agentId, out agent);
#else
        return _agents.Remove(agentId, out agent);
#endif
    }

    public bool TryGet(string Id, out IAgent agent)
    {
        return _agents.TryGetValue(Id, out agent);
    }
    public IEnumerable<IAgent> Agents => _agents.Values;
}
