using System.Collections.Concurrent;
using VeryTunnel.Contracts;

namespace VeryTunnel.Server;

internal class DefaultAgentManager : IAgentManager
{
    private readonly ConcurrentDictionary<string, IAgent> _agents = new();

    public void Add(IAgent agent)
    {
        _agents.AddOrUpdate(agent.Id, agent, (agentId, oldAgent) => agent);
    }

    public void Remove(string agentId)
    {
        _agents.Remove(agentId, out _);
    }

    public bool TryGet(string Id, out IAgent agent)
    {
        return _agents.TryGetValue(Id, out agent);
    }

}
