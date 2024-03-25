using CommonLib;
using PlayHouse;
using System;

namespace PlayHouseConnector.Network
{
    public class TargetId
    {
        public ushort ServiceId { get; }
        public int StageIndex { get; }

        public TargetId(ushort serviceId, int stageIndex = 0)
        {
            if (stageIndex > byte.MaxValue)
            {
                throw new ArithmeticException("stageIndex overflow");
            }
            ServiceId = serviceId;
            StageIndex = stageIndex;
        }
    }
    public class Header
    {
        public ushort ServiceId { get; set; }
        public int MsgId { get; set; }
        public ushort MsgSeq { get; set; }
        public ushort ErrorCode { get; set; }
        public byte StageIndex { get; set; }

        public  override string ToString()
        {
            return $"ServiceId: {ServiceId}, MsgId: {MsgId}, MsgSeq: {MsgSeq}, ErrorCode: {ErrorCode}, StageIndex: {StageIndex}";
        }


        public Header(ushort serviceId = 0, int msgId =0, ushort msgSeq = 0,ushort errorCode= 0, byte stageIndex = 0)
        {
            MsgId = msgId;
            ErrorCode = errorCode;
            MsgSeq = msgSeq;
            ServiceId = serviceId;
            StageIndex = stageIndex;
        }
    }

    public class PooledByteBufferPayload : IPayload
    {
        private readonly PooledByteBuffer _buffer;

        public PooledByteBufferPayload(PooledByteBuffer buffer)
        {
            _buffer = buffer;
        }


        public void Dispose()
        {
            _buffer.Dispose();
        }

        public PooledByteBuffer GetBuffer()
        {
            return _buffer;
        }

        public ReadOnlySpan<byte> Data => _buffer.AsSpan();
    }


    public class ClientPacket : IBasePacket
    {
        public Header Header { get; set; }
        public IPayload Payload;

        public ClientPacket(Header header, IPayload payload)
        {
            Header = header;
            Payload = payload;
        }

        public void Dispose()
        {
            Payload.Dispose();

        }

        public IPayload MovePayload()
        {
            IPayload temp = Payload;
            Payload = new EmptyPayload();
            return temp;
        }

        public int MsgSeq => Header.MsgSeq;
        public int MsgId=> Header.MsgId;
        public ushort ServiceId => Header.ServiceId;

        public IPacket ToPacket()
        {
            return new Packet(Header.MsgId, MovePayload());
        }

        internal static ClientPacket ToServerOf(TargetId targetId, IPacket packet)
        {
            var header = new Header(serviceId: targetId.ServiceId, msgId: packet.MsgId, stageIndex: (byte)targetId.StageIndex);
            return new ClientPacket(header, packet.Payload);
                                                            
        }

        internal void GetBytes(PooledByteBuffer buffer)
        {
            var body = Payload.Data;
            int bodySize = body.Length;

            if (bodySize > PacketParser.MAX_PACKET_SIZE)
            {
                throw new Exception($"body size is over : {bodySize}");
            }

            buffer.WriteInt16(XBitConverter.ToNetworkOrder((ushort)bodySize));
            buffer.WriteInt16(XBitConverter.ToNetworkOrder(Header.ServiceId));
            buffer.WriteInt32(XBitConverter.ToNetworkOrder(Header.MsgId));
            buffer.WriteInt16(XBitConverter.ToNetworkOrder(Header.MsgSeq));
            buffer.Write(Header.StageIndex);

            buffer.Write(Payload.Data);
        }

        internal void SetMsgSeq(ushort seq)
        {
            Header.MsgSeq = seq;
        }

        internal bool IsHeartBeat()
        {
            return MsgId == -1;
        }
    }
}


