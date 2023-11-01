using Google.Protobuf;

namespace VeryTunnel.Core.DotNetty
{
    public class ChannelMessage
    {
        public uint RequestId { get; set; }
        public IMessage Message { get; set; }
    }
}
