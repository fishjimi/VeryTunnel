using System.Collections.Concurrent;
using VeryTunnel.Core.Contracts;

namespace VeryTunnel.Server;

public class AgentManager
{
    private readonly ConcurrentDictionary<string, IAgent> _agents = new();



    internal void Add(IAgent agent)
    {
        _agents.AddOrUpdate(agent.AgentId, agent, (agentId, oldAgent) => agent);
    }

    internal void Remove(string agentId)
    {
        if (_agents.Remove(agentId, out var agent))
        {

        }
    }
}
