namespace VeryTunnel.Contracts;

public interface ITunnel
{
    public int AgentPort { get; }
    public int ServerPort { get; }
    public IEnumerable<ITunnelSession> Sessions { get; }
    public Task Close();
    public event Action<ITunnel> OnClosed;
    public event Action<ITunnelSession> OnSessionCreated;
    public event Action<ITunnelSession> OnSessionClosed;
}
