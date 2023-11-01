using DotNetty.Transport.Channels;
using VeryTunnel.Core.Contracts;
using VeryTunnel.Core.DotNetty;
using VeryTunnel.Core.Models;

namespace VeryTunnel.Server;

internal class AgentMessageHandler : SimpleChannelInboundHandler<ChannelMessage>, IAgent
{
    private string _agentId = string.Empty;
    public string AgentId => _agentId;


    private readonly AgentManager _agentManager;


    public AgentMessageHandler(AgentManager agentManager)
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
