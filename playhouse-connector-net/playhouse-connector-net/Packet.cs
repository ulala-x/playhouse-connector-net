using System;
using Google.Protobuf;

namespace PlayHouseConnector
{
    public interface IBasePacket : IDisposable
    {
    }

    public interface IPacket : IBasePacket
    {
        public int MsgId { get; }
        public IPayload Payload { get; }
        public ReadOnlyMemory<byte> Data { get; }
        public ReadOnlySpan<byte> DataSpan => Data.Span;
    }

    //
    public class Packet : IPacket
    {
        public Packet(int msgId = 0)
        {
            MsgId = msgId;
            Payload = new EmptyPayload();
        }

        public Packet(int msgId, IPayload payload) : this(msgId)
        {
            Payload = payload;
        }

        public Packet(IMessage message) : this(message.Descriptor.Index, new ProtoPayload(message))
        {
        }

        public IPayload Payload { get; }

        public int MsgId { get; }

        public ReadOnlyMemory<byte> Data => Payload.Data;


        public void Dispose()
        {
            Payload.Dispose();
        }
    }
}