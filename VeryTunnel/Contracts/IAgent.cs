namespace VeryTunnel.Contracts;

public interface IAgent
{
    public string Id { get; }
    public Task<ITunnel> CreateTunnel(int agentPort, int serverPort, Func<ITunnel, ITunnelSession, Task> OnSessionCreated = null);
}
