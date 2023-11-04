using VeryTunnel.Contracts;

namespace VeryTunnel.Server
{
    public interface IAgentManager
    {
        public void Add(IAgent agent);
        public void Remove(string agentId);
        public bool TryGet(string Id, out IAgent agent);
        public IEnumerable<IAgent> Agents { get; }
    }
}
