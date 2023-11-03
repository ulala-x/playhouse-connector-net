using Google.Protobuf;
using System;
using System.IO;

namespace PlayHouseConnector
{
    public interface IBasePacket : IDisposable
    {
    }

    public interface IPacket : IBasePacket
    {
        public int MsgId { get; }
        public IPayload Payload { get; }
        public ReadOnlySpan<byte> Data { get; }
    }

    //
    public class Packet : IPacket
    {
        public IPayload Payload => _payload;
        private readonly IPayload _payload;

        public int MsgId { get; }
        public ReadOnlySpan<byte> Data => _payload!.Data;

        public Packet(int msgId = 0)
        {
            this.MsgId = msgId;
            this._payload = new EmptyPayload();
        }

        public Packet(int msgId, IPayload payload) : this(msgId)
        {
            _payload = payload;
        }

        public Packet(IMessage message) : this(message.Descriptor.Index, new ProtoPayload(message))
        {
        }


        public void Dispose()
        {
            Payload.Dispose();
        }
    }
}