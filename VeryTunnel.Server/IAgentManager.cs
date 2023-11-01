using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VeryTunnel.Contracts;

namespace VeryTunnel.Server
{
    public interface IAgentManager
    {
        public void Add(IAgent agent);
        public void Remove(string agentId);
        public bool TryGet(string Id, out IAgent agent);
    }
}
