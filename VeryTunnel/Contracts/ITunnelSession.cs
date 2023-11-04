namespace VeryTunnel.Contracts;

public interface ITunnelSession
{
    public uint SessionId { get; }
    public Task Close();
}
