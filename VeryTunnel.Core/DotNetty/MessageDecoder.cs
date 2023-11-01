using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using System.Buffers;
using System.Linq.Expressions;
using VeryTunnel.Core.Models;

namespace VeryTunnel.Core.DotNetty;
public class MessageDecoder : ByteToMessageDecoder
{
    public static Func<WrappedMessage, IMessage> GetInnerMessage => getInnerMessage.Value;
    private static readonly Lazy<Func<WrappedMessage, IMessage>> getInnerMessage = new(() =>
    {
        var objParameterExpr = Expression.Parameter(typeof(WrappedMessage), "instance");
        var field = typeof(WrappedMessage).GetField("innerMessage_", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) ?? throw new ArgumentException();
        var fieldExp = Expression.Field(objParameterExpr, field);
        var fieldAsExpr = Expression.TypeAs(fieldExp, typeof(IMessage));
        var expr = Expression.Lambda<Func<WrappedMessage, IMessage>>(fieldAsExpr, objParameterExpr);
        return expr.Compile();
    });


    protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
    {
        byte[] bytes = ArrayPool<byte>.Shared.Rent(input.ReadableBytes);
        input.ReadBytes(bytes);
        var wrappedMessage = WrappedMessage.Parser.ParseFrom(bytes);
        var channelMessage = new ChannelMessage
        {
            RequestId = wrappedMessage.RequestId,
            Message = GetInnerMessage(wrappedMessage),
        };
        output.Add(channelMessage);
        ArrayPool<byte>.Shared.Return(bytes);
    }

}

