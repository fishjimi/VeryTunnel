namespace VeryTunnel.Contracts;

public interface ITunnel
{
    int AgentPort { get; }
    int ServerPort { get; }
    IEnumerable<ITunnelSession> Sessions { get; }
    Task Close();
    event Action<ITunnel> OnClosed;
    event Action<ITunnelSession> OnSessionCreated;
    event Action<ITunnelSession> OnSessionClosed;
}
