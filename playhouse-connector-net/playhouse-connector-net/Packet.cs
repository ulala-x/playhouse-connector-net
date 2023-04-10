using Google.Protobuf;
using playhouse_connector_net;
using System;
using System.IO;

namespace PlayHouseConnector
{
    public interface IBasePacket : IDisposable
    {
    }

    public class Packet : IBasePacket
    {
        public short MsgId;
        public IPayload Payload;

        public Packet(short msgId = 0)
        {
            this.MsgId = msgId;
            this.Payload = new EmptyPayload();
        }

        public Packet(short msgId, IPayload payload) : this(msgId)
        {
            Payload = payload;
        }

        public Packet(IMessage message) : this((short)message.Descriptor.Index, new ProtoPayload(message)) { }


        public void Dispose()
        {
            Payload.Dispose();
        }
    }

    public interface IReplyPacket : IBasePacket
    {
        public short ErrorCode { get; }
        public short MsgId { get;}
        public bool IsSuccess();

        //public Stream GetStream();

        public ReadOnlySpan<byte> Data { get; }

    }

   



}