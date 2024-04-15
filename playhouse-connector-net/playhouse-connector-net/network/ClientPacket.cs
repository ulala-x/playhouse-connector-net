using CommonLib;
using PlayHouse;
using System;

namespace PlayHouseConnector.Network
{
    public class TargetId
    {
        public ushort ServiceId { get; }
        public long StageId { get; }

        public TargetId(ushort serviceId, long stageId = 0)
        {
            
            ServiceId = serviceId;
            StageId = stageId;
        }
    }
    public class Header
    {
        public ushort ServiceId { get; set; }
        public string MsgId { get; set; }
        public ushort MsgSeq { get; set; }
        public ushort ErrorCode { get; set; }
        public long StageId { get; set; }

        public  override string ToString()
        {
            return $"ServiceId: {ServiceId}, MsgId: {MsgId}, MsgSeq: {MsgSeq}, ErrorCode: {ErrorCode}, StageId: {StageId}";
        }


        public Header(ushort serviceId = 0, string msgId = "", ushort msgSeq = 0,ushort errorCode= 0, long stageId = 0)
        {
            MsgId = msgId;
            ErrorCode = errorCode;
            MsgSeq = msgSeq;
            ServiceId = serviceId;
            StageId = stageId;
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

        public ReadOnlyMemory<byte> Data => _buffer.AsMemory();
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
        public string MsgId=> Header.MsgId;
        public ushort ServiceId => Header.ServiceId;

        public IPacket ToPacket()
        {
            return new Packet(Header.MsgId, MovePayload());
        }

        internal static ClientPacket ToServerOf(TargetId targetId, IPacket packet)
        {
            var header = new Header(serviceId: targetId.ServiceId, msgId: packet.MsgId, stageId: targetId.StageId);
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

            int headerIdSize = Header.MsgId.Length;
            if(headerIdSize > 256)
            {
                throw new Exception($"MsgId size is over : {headerIdSize}");
            }
            /*
             *  2byte  header size
             *  3byte  body size
             *  2byte  serviceId
             *  1byte  msgId size
             *  n byte msgId string
             *  2byte  msgSeq
             *  8byte  stageId
             *  
             *  ToServer Header Size = 2+3+2+1+2+8+N = 18 + n
             * */
            int headerSize = 18 + headerIdSize;

            buffer.WriteInt16((ushort)headerSize); //header size
            buffer.WriteInt24(bodySize); // body size 3byte
            buffer.WriteInt16(Header.ServiceId); // service id
            buffer.Write((byte)headerIdSize); // msgId size
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
            return MsgId == "-1";
        }
    }
}


