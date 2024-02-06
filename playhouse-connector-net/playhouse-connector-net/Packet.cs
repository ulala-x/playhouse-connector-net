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

        public ushort MsgSeq { get; set; }
    }

    //
    public class Packet : IPacket
    {
        private int _msgId;
        private readonly IPayload _payload;
        private ushort _seq;
        public IPayload Payload => _payload;

        public int MsgId => _msgId;
        

        public ReadOnlySpan<byte> Data => _payload!.Data;

        public ushort MsgSeq { get => _seq; set => _seq = value; }

        public Packet(int msgId = 0)
        {
            _msgId = msgId;
            _payload = new EmptyPayload();
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