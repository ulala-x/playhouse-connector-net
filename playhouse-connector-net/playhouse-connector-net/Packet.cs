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
        public string MsgId { get; }
        public IPayload Payload { get; }
        public ReadOnlyMemory<byte> Data { get; }
        public ReadOnlySpan<byte> DataSpan => Data.Span;

    }

    //
    public class Packet : IPacket
    {
        private string _msgId;
        private readonly IPayload _payload;
        
        public IPayload Payload => _payload;

        public string MsgId => _msgId;

        public ReadOnlyMemory<byte> Data => _payload.Data;
        

        public Packet(string msgId = "")
        {
            _msgId = msgId;
            _payload = new EmptyPayload();
        }

        public Packet(string msgId, IPayload payload) : this(msgId)
        {
            _payload = payload;
        }

        public Packet(IMessage message) : this(message.Descriptor.Name, new ProtoPayload(message))
        {
        }


        public void Dispose()
        {
            Payload.Dispose();
        }
    }
}