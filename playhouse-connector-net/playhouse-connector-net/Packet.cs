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
        public int MsgId;
        public IPayload Payload;

        public Packet(int msgId = 0)
        {
            this.MsgId = msgId;
            this.Payload = new EmptyPayload();
        }

        public Packet(int msgId, IPayload payload) : this(msgId)
        {
            Payload = payload;
        }

        public Packet(IMessage message) : this(message.Descriptor.Index, new ProtoPayload(message)) { }

        


        public void Dispose()
        {
            Payload.Dispose();
        }
    }

    public interface IReplyPacket : IBasePacket
    {
        public short ErrorCode { get; }
        public int MsgId { get;}
        public bool IsSuccess();

        //public Stream GetStream();

        public ReadOnlySpan<byte> Data { get; }

    }

   



}