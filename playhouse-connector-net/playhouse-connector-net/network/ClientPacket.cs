using CommonLib;
using Google.Protobuf;
using NetCoreServer;
using playhouse_connector_net;
using System;
using System.IO;

namespace PlayHouseConnector.network
{
    public class Header
    {
        public short ServiceId { get; set; }
        public short MsgId { get; set; }
        public short MsgSeq { get; set; }
        public short ErrorCode { get; set; }


        public Header(short serviceId = -1, short msgId =-1, short msgSeq = 0,short errorCode= 0)
        {
            MsgId = msgId;
            ErrorCode = errorCode;
            MsgSeq = msgSeq;
            ServiceId = serviceId;
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


        public void Output(Stream outputStream)
        {
            outputStream.Write(_buffer.Data, 0, _buffer.Data.Length);
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }

        public PooledBuffer GetBuffer()
        {
            return _buffer;
        }

        public (byte[], int) Data => (_buffer.Data, _buffer.Size);
    }

    public class ReplyPacket : IReplyPacket
    {
        public short ErrorCode { get; private set; }
        public short MsgId { get; private set; }


        private IPayload _payload;

        public ReplyPacket(short errorCode, short msgId, IPayload payload)
        {
            this.ErrorCode = errorCode;
            this.MsgId = msgId;
            this._payload = payload;
        }

        public ReplyPacket(short errorCode = 0, short msgId = -1) : this(errorCode, msgId, new EmptyPayload()) { }

        public bool IsSuccess()
        {
            return ErrorCode == 0;
        }

        public void Dispose()
        {
            _payload.Dispose();
        }

        public (byte[],int) Data =>_payload.Data;

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

        public short GetMsgId()
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

        internal static ClientPacket ToServerOf(short serviceId, Packet packet)
        {
            return new ClientPacket(new Header(serviceId, packet.MsgId), packet.Payload);
        }

        internal void GetBytes(RingBuffer buffer)
        {
            int offset = (int)buffer.Count;

            int bodyIndex = buffer.WriteInt16(0);
            buffer.WriteInt16(XBitConverter.ToNetworkOrder(Header.ServiceId));
            buffer.WriteInt16(XBitConverter.ToNetworkOrder(Header.MsgId));
            buffer.WriteInt16(XBitConverter.ToNetworkOrder(Header.MsgSeq));
            
            RingBufferStream stream = new RingBufferStream(buffer);
            Payload.Output(stream);

            int bodySize = buffer.Count - (offset + 8);

            if (bodySize > PacketParser.MAX_PACKET_SIZE)
            {
                throw new Exception($"body size is over : {bodySize}");
            }

            buffer.ReplaceInt16(bodyIndex, XBitConverter.ToNetworkOrder((short)bodySize));
        }

        internal void SetMsgSeq(short seq)
        {
            Header.MsgSeq = seq;
        }

        internal static ReplyPacket OfErrorPacket(short errorCode)
        {
            return new ReplyPacket(errorCode, -1, new EmptyPayload());

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