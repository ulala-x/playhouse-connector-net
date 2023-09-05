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
        public IPayload Payload => _payload;

        private IPayload _payload;

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

        public Packet(IMessage message) : this(message.Descriptor.Index, new ProtoPayload(message)) { }

        


        public void Dispose()
        {
            Payload.Dispose();
        }
    }

    public interface IReplyPacket : IBasePacket
    {
        public ushort ErrorCode { get; }
        public int MsgId { get;}
        public bool IsSuccess();

        //public Stream GetStream();

        public ReadOnlySpan<byte> Data { get; }

    }

   



}