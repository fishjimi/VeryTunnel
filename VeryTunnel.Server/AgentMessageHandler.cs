using DotNetty.Transport.Channels;
using VeryTunnel.Contracts;
using VeryTunnel.DotNetty;
using VeryTunnel.Models;

namespace VeryTunnel.Server;

internal class AgentMessageHandler : SimpleChannelInboundHandler<ChannelMessage>, IAgent
{
    private string _agentId = string.Empty;
    public string Id => _agentId;


    private readonly DefaultAgentManager _agentManager;


    public AgentMessageHandler(DefaultAgentManager agentManager)
    {
        _agentManager = agentManager;
    }

    protected override void ChannelRead0(IChannelHandlerContext ctx, ChannelMessage msg)
    {
        switch (msg.Message)
        {
            case DeviceConnect message:
                {
                    _agentId = message.Id;
                    _agentManager.Add(this);
                    break;
                }
        }
    }

    public override void ChannelActive(IChannelHandlerContext context)
    {
        base.ChannelActive(context);
    }
}
