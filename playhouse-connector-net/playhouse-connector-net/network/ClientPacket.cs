using CommonLib;
using Google.Protobuf;
using NetCoreServer;
using playhouse_connector_net;
using System;
using System.IO;

namespace PlayHouseConnector.network
{
    public class TargetId
    {
        public short ServiceId { get; }
        public int StageIndex { get; }

        public TargetId(short serviceId, int stageIndex = 0)
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
        public short ServiceId { get; set; }
        public int MsgId { get; set; }
        public short MsgSeq { get; set; }
        public short ErrorCode { get; set; }
        public byte StageIndex { get; set; }


        public Header(short serviceId = 0, int msgId =0, short msgSeq = 0,short errorCode= 0, byte stageIndex = 0)
        {
            MsgId = msgId;
            ErrorCode = errorCode;
            MsgSeq = msgSeq;
            ServiceId = serviceId;
            StageIndex = stageIndex;
        }

        //public static Header Of(HeaderMsg headerMsg)
        //{
        //    return new Header(headerMsg.ServiceId, headerMsg.MsgName, headerMsg.MsgSeq, headerMsg.ErrorCode);
        //}

        //public HeaderMsg ToMsg()
        //{
        //    var headerMsg = new HeaderMsg();
        //    headerMsg.MsgName = MsgName;
        //    headerMsg.MsgSeq = MsgSeq;                
        //    headerMsg.ErrorCode = ErrorCode;
        //    return headerMsg;

        //}
    }

    public class PooledBufferPayload : IPayload
    {
        private readonly PooledBuffer _buffer;

        public PooledBufferPayload(PooledBuffer buffer)
        {
            _buffer = buffer;
        }


        public void Dispose()
        {
            _buffer.Dispose();
        }

        public PooledBuffer GetBuffer()
        {
            return _buffer;
        }

        public ReadOnlySpan<byte> Data => new ReadOnlySpan<byte>(_buffer.Data,0,_buffer.Size);
    }

    public class ReplyPacket : IReplyPacket
    {
        public short ErrorCode { get; private set; }
        public int MsgId { get; private set; }


        private IPayload _payload;

        public ReplyPacket(short errorCode, int msgId, IPayload payload)
        {
            this.ErrorCode = errorCode;
            this.MsgId = msgId;
            this._payload = payload;
        }

        public ReplyPacket(short errorCode = 0, int msgId = 0) : this(errorCode, msgId, new EmptyPayload()) { }

        public bool IsSuccess()
        {
            return ErrorCode == 0;
        }

        public void Dispose()
        {
            _payload.Dispose();
        }

        public ReadOnlySpan<byte> Data =>_payload.Data;

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

        public int GetMsgSeq()
        {
            return Header.MsgSeq;
        }

        public int GetMsgId()
        {
            return Header.MsgId;
        }


        public short ServiceId()
        {
            return Header.ServiceId;
        }

        public Packet ToPacket()
        {
            return new Packet(Header.MsgId, MovePayload());
        }

        internal static ClientPacket ToServerOf(TargetId targetId, Packet packet)
        {
            var header = new Header(serviceId: targetId.ServiceId, msgId: packet.MsgId, stageIndex: (byte)targetId.StageIndex);
            return new ClientPacket(header, packet.Payload);
                                                            
        }

        internal void GetBytes(RingBuffer buffer)
        {
            var body = Payload.Data;
            int bodySize = body.Length;

            if (bodySize > PacketParser.MAX_PACKET_SIZE)
            {
                throw new Exception($"body size is over : {bodySize}");
            }

            buffer.WriteInt16(XBitConverter.ToNetworkOrder((short)bodySize));
            buffer.WriteInt16(XBitConverter.ToNetworkOrder(Header.ServiceId));
            buffer.WriteInt32(XBitConverter.ToNetworkOrder(Header.MsgId));
            buffer.WriteInt16(XBitConverter.ToNetworkOrder(Header.MsgSeq));
            buffer.Write(Header.StageIndex);

            buffer.Write(Payload.Data);
        }

        internal void SetMsgSeq(short seq)
        {
            Header.MsgSeq = seq;
        }

        internal static ReplyPacket OfErrorPacket(short errorCode)
        {
            return new ReplyPacket(errorCode, 0, new EmptyPayload());

        }

        public ReplyPacket ToReplyPacket()
        {
            return new ReplyPacket(Header.ErrorCode, Header.MsgId, MovePayload());
        }

    }
}



//internal void GetBytes(PreAllocByteArrayOutputStream outputStream)
//{
//    var headerMsg = this._header.ToMsg();
//    byte headerSize = (byte)headerMsg.CalculateSize();
//    short bodySize = (short)_buffer!.Size;
//    var packetSize = 1 + 2 + headerSize + bodySize;

//    outputStream.WriteByte(headerSize);
//    outputStream.WriteShort(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bodySize))));


//        }


//        internal Span<byte> ToByteBuffer()
//{
//    var headerMsg = this._header.ToMsg();
//    byte headerSize = (byte)headerMsg.CalculateSize();
//    short bodySize = (short)_buffer!.Size;
//    var packetSize = 1 + 2 + headerSize + bodySize;

//    var buffer = new PooledBuffer(packetSize);

//    buffer.Append(headerSize);
//    buffer.Append();
//    buffer.Append(headerMsg.ToByteArray());
//    buffer.Append(this._buffer);

//    return buffer;
//}