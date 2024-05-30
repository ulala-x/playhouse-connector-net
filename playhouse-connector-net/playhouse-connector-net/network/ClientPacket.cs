using System;
using CommonLib;
using PlayHouse;

namespace PlayHouseConnector.Network
{
    public class TargetId
    {
        public TargetId(ushort serviceId, long stageId = 0)
        {
            ServiceId = serviceId;
            StageId = stageId;
        }

        public ushort ServiceId { get; }
        public long StageId { get; }
    }

    public class Header
    {
        public Header(ushort serviceId = 0, string msgId = "", ushort msgSeq = 0, ushort errorCode = 0, long stageId = 0)
        {
            MsgId = msgId;
            ErrorCode = errorCode;
            MsgSeq = msgSeq;
            ServiceId = serviceId;
            StageId = stageId;
        }

        public ushort ServiceId { get; set; }
        public string MsgId { get; set; }
        public ushort MsgSeq { get; set; }
        public ushort ErrorCode { get; set; }
        public long StageId { get; set; }

        public override string ToString()
        {
            return
                $"ServiceId: {ServiceId}, MsgId: {MsgId}, MsgSeq: {MsgSeq}, ErrorCode: {ErrorCode}, StageId: {StageId}";
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

        public ReadOnlyMemory<byte> Data => _buffer.AsMemory();

        public PooledByteBuffer GetBuffer()
        {
            return _buffer;
        }
    }


    public class ClientPacket : IBasePacket
    {
        public IPayload Payload;

        public ClientPacket(Header header, IPayload payload)
        {
            Header = header;
            Payload = payload;
        }

        public Header Header { get; set; }
        public long StageId => Header.StageId;

        public int MsgSeq => Header.MsgSeq;
        public string MsgId => Header.MsgId;
        public ushort ServiceId => Header.ServiceId;

        public void Dispose()
        {
            Payload.Dispose();
        }

        public IPayload MovePayload()
        {
            var temp = Payload;
            Payload = new EmptyPayload();
            return temp;
        }

        public IPacket ToPacket()
        {
            return new Packet(Header.MsgId, MovePayload());
        }

        internal static ClientPacket ToServerOf(TargetId targetId, IPacket packet)
        {
            var header = new Header(targetId.ServiceId, packet.MsgId, stageId: targetId.StageId);
            return new ClientPacket(header, packet.Payload);
        }

        //internal void GetBytes(PooledByteBuffer buffer)
        //{
        //    var body = Payload.Data;
        //    var bodySize = body.Length;

        //    if (bodySize > PacketParser.MAX_PACKET_SIZE)
        //    {
        //        throw new Exception($"body size is over : {bodySize}");
        //    }

        //    buffer.WriteInt32(bodySize);
        //    buffer.WriteInt16(Header.ServiceId);
        //    buffer.WriteInt32(Header.MsgId);
        //    buffer.WriteInt16(Header.MsgSeq);
        //    buffer.WriteInt64(Header.StageId);

        //    buffer.Write(Payload.DataSpan);
        //}


        internal void GetBytes(PooledByteBuffer buffer)
        {
            int msgIdLength = Header.MsgId.Length;
            if (msgIdLength > PacketConst.MsgIdLimit)
            {
                throw new Exception($"MsgId size is over : {msgIdLength}");
            }


            var body = Payload.Data;
            int bodySize = body.Length;

            if (bodySize > PacketConst.MaxPacketSize)
            {
                throw new Exception($"body size is over : {bodySize}");
            }

            
            /*
             *  4byte  body size
             *  2byte  serviceId
             *  1byte  msgId size
             *  n byte msgId string
             *  2byte  msgSeq
             *  8byte  stageId
             *  
             *  ToServer Header Size = 4+2+1+2+8+N = 19 + n
             * */

            buffer.WriteInt32(bodySize); // body size 4byte
            buffer.WriteInt16(Header.ServiceId); // service id
            buffer.Write((byte)msgIdLength); // msgId size
            buffer.Write(Header.MsgId); // msgId string
            buffer.WriteInt16(Header.MsgSeq); //msgseq
            buffer.WriteInt64(Header.StageId);

            buffer.Write(Payload.DataSpan);
        }

        internal void SetMsgSeq(ushort seq)
        {
            Header.MsgSeq = seq;
        }

        internal bool IsHeartBeat()
        {
            return MsgId == PacketConst.HeartBeat;
        }
    }
}