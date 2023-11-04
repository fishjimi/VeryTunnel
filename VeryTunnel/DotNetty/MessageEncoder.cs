using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Linq.Expressions;
using VeryTunnel.Models;

namespace VeryTunnel.DotNetty;

public class MessageEncoder : MessageToByteEncoder<ChannelMessage>
{
    private readonly ILogger<MessageEncoder> _logger = InternalLoggerFactory.DefaultFactory.CreateLogger<MessageEncoder>();
    public static void SetInnerMessage(WrappedMessage instance, IMessage property) => _setInnerMessageFuncs.Value[property.GetType()](instance, property);
    private readonly static Lazy<Dictionary<Type, Action<WrappedMessage, IMessage>>> _setInnerMessageFuncs = new(() =>
    {
        var properties = typeof(WrappedMessage).GetProperties().Where(x => x.PropertyType.IsAssignableTo(typeof(IMessage))).ToList();
        return properties.Select((property) =>
        {
            var objParameterExpr = Expression.Parameter(typeof(WrappedMessage), "instance");
            var propertyParameterExpr = Expression.Parameter(typeof(IMessage), "property");
            var propertyParameterAsExpr = Expression.TypeAs(propertyParameterExpr, property.PropertyType);
            var propertyExp = Expression.Property(objParameterExpr, property);
            var propertyAssignExpr = Expression.Assign(propertyExp, propertyParameterAsExpr);
            var expr = Expression.Lambda<Action<WrappedMessage, IMessage>>(propertyAssignExpr, objParameterExpr, propertyParameterExpr);
            return new KeyValuePair<Type, Action<WrappedMessage, IMessage>>(property.PropertyType, expr.Compile());
        }).ToDictionary(kv => kv.Key, kv => kv.Value);
    });


    protected override void Encode(IChannelHandlerContext context, ChannelMessage channelMessage, IByteBuffer output)
    {
        var wrappedMessage = new WrappedMessage()
        {
            RequestId = channelMessage.RequestId,
            ResponseId = channelMessage.ResponseId
        };
        SetInnerMessage(wrappedMessage, channelMessage.Message);
        byte[] bytes = new byte[wrappedMessage.CalculateSize()];
        wrappedMessage.WriteTo(bytes);


        output.WriteBytes(bytes);


        _logger.LogInformation(ByteBufferUtil.PrettyHexDump(output));
    }
}
