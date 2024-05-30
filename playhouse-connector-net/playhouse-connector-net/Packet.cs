using System;
using Google.Protobuf;

namespace PlayHouseConnector
{
    public interface IBasePacket : IDisposable
    {
    }

    public interface IPacket : IBasePacket
    {
        public string MsgId { get; }
        public IPayload Payload { get; }
        public ReadOnlyMemory<byte> Data { get; }
        public ReadOnlySpan<byte> DataSpan => Data.Span;
    }

    //
    public class Packet : IPacket
    {
        public Packet(string msgId = "")
        {
            MsgId = msgId;
            Payload = new EmptyPayload();
        }

        public Packet(string msgId, IPayload payload) : this(msgId)
        {
            Payload = payload;
        }

        public Packet(IMessage message) : this(message.Descriptor.Name, new ProtoPayload(message))
        {
        }

        public IPayload Payload { get; }

        public string MsgId { get; }

        public ReadOnlyMemory<byte> Data => Payload.Data;


        public void Dispose()
        {
            Payload.Dispose();
        }
    }
}