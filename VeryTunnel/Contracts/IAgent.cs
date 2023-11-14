namespace VeryTunnel.Contracts;

public interface IAgent
{
    string AgentName { get; }
    Task<ITunnel> CreateTunnel(int agentPort, int serverPort);
    IEnumerable<ITunnel> Tunnels { get; }
}

