using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using System.Buffers;
using System.Linq.Expressions;
using VeryTunnel.Models;

namespace VeryTunnel.DotNetty
{
    public class MessageEncoder : MessageToByteEncoder<ChannelMessage>
    {
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
                RequestId = channelMessage.RequestId
            };
            SetInnerMessage(wrappedMessage, channelMessage.Message);
            byte[] bytes = ArrayPool<byte>.Shared.Rent(wrappedMessage.CalculateSize());
            wrappedMessage.WriteTo(bytes);
            output.WriteBytes(bytes);
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }
}
