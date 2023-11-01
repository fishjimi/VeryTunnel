namespace VeryTunnel.Contracts;

public interface ITunnelSession
{
    public uint SessionId { get; set; }
    public Task Close();
}
