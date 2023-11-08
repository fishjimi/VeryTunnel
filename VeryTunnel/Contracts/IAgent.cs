namespace VeryTunnel.Contracts;

public interface IAgent
{
    string Id { get; }
    Task<ITunnel> CreateTunnel(int agentPort, int serverPort);
    IEnumerable<ITunnel> Tunnels { get; }
}

