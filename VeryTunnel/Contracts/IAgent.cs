namespace VeryTunnel.Contracts;

public interface IAgent
{
    public string Id { get; }
    public Task<ITunnel> CreateTunnel(int agentPort, int serverPort);
    public IEnumerable<ITunnel> Tunnels { get; }
}
