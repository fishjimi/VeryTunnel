namespace VeryTunnel.Contracts;

public interface ITunnelSession
{
    uint SessionId { get; }
    Task Close();
}
