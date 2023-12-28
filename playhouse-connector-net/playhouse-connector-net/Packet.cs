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
        public int MsgSeq { get;set; }
        public IPayload Payload { get; }
        public ReadOnlySpan<byte> Data { get; }
    }

    //
    public class Packet : IPacket
    {
        private int _msgSeq;
        private int _msgId;
        private readonly IPayload _payload;
        
        public IPayload Payload => _payload;

        public int MsgId => _msgId;

        public ReadOnlySpan<byte> Data => _payload!.Data;
        
        public int MsgSeq { get => _msgSeq; set => _msgSeq = value; }

        public Packet(int msgId = 0)
        {
            _msgId = msgId;
            _payload = new EmptyPayload();
            _msgSeq = 0;
        }

        public Packet(int msgId, IPayload payload, int msgSeq = 0) : this(msgId)
        {
            _payload = payload;
            _msgSeq = msgSeq;
        }

        public Packet(IMessage message) : this(message.Descriptor.Index, new ProtoPayload(message),0)
        {
        }


        public void Dispose()
        {
            Payload.Dispose();
        }
    }
}