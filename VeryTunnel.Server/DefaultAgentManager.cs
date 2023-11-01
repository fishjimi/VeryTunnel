using System.Collections.Concurrent;
using VeryTunnel.Contracts;

namespace VeryTunnel.Server;

internal class DefaultAgentManager : IAgentManager
{
    private readonly ConcurrentDictionary<string, IAgent> _agents = new();

    void IAgentManager.Add(IAgent agent)
    {
        _agents.AddOrUpdate(agent.Id, agent, (agentId, oldAgent) => agent);
    }

    void IAgentManager.Remove(string agentId)
    {
        if (_agents.Remove(agentId, out var agent))
        {

        }
    }

    bool IAgentManager.TryGet(string Id, out IAgent agent)
    {
        return _agents.TryGetValue(Id, out agent);
    }
}
