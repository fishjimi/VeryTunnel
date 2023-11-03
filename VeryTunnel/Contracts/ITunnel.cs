namespace VeryTunnel.Contracts;

public interface ITunnel
{
    public int AgentPort { get; }
    public int ServerPort { get; }
    public IList<ITunnelSession> Sessions { get; }
    public Task Close();
}
