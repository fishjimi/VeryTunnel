using Google.Protobuf;

namespace VeryTunnel.DotNetty
{
    public class ChannelMessage
    {
        public uint RequestId { get; set; }
        public IMessage Message { get; set; }
    }
}
