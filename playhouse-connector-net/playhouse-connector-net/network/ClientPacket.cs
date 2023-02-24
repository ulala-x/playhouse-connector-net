using PlayHouseConnector.network.buffer;
using System;
using System.Net;

namespace PlayHouseConnector.network
{
    public class Header
    {
        public String MsgName { get; set; }
        public int ErrorCode { get; set; }
        public int MsgSeq{ get; set; }
        public String ServiceId { get; set; }

        public Header(String serviceId="",String msgName="", int msgSeq=0,int errorCode=0)
        {
            MsgName = msgName;
            ErrorCode = errorCode;
            MsgSeq = msgSeq;
            ServiceId = serviceId;
        }

        public static Header Of(HeaderMsg headerMsg)
        {
            return new Header(headerMsg.ServiceId, headerMsg.MsgName, headerMsg.MsgSeq, headerMsg.ErrorCode);
        }

        public HeaderMsg ToMsg()
        {
            var headerMsg = new HeaderMsg();
            headerMsg.MsgName = MsgName;
            headerMsg.MsgSeq = MsgSeq;                
            headerMsg.ErrorCode = ErrorCode;
            return headerMsg;

        }
    }
    public class ClientPacket : IDisposable
    {
        private Header _header;
        private PBuffer? _buffer ;

        public ClientPacket(Header header, PBuffer buffer)
        {
            this._header = header;
            this._buffer = buffer;  
        }

        public String ServiceId()
        {
            return _header.ServiceId;
        }

        public String MsgName()
        {
            return _header.MsgName;

        }

        public Packet ToPacket()
        {
            return new Packet(MsgName(),_buffer!);
        }

        public static ClientPacket ToServerOf(string serviceId, Packet packet)
        {
            return new ClientPacket(new Header(serviceId, packet.MsgName),packet.MoveBuffer());
        }

        internal PBuffer ToByteBuffer()
        {
            var headerMsg = this._header.ToMsg();
            short headerSize = (short)headerMsg.CalculateSize();
            var packetSize = 1+2+headerSize+_buffer!.Size;
            
            var buffer = new PBuffer(packetSize);
           
            buffer.Append((byte)headerSize);
            buffer.Append(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(headerSize)));
            buffer.Append(this._buffer);
                                    
            return buffer;            
        }

        public void Dispose()
        {
            if(_buffer != null)
            {
               this._buffer.Dispose();
            }
        }

        private PBuffer? MoveBuffer()
        {
            if (_buffer == null) return null;

            var temp = _buffer;
            _buffer = null;
            return temp!;
        }

        public ReplyPacket ToReplyPacket()
        {
            return new ReplyPacket(_header.ErrorCode, _header.MsgName, MoveBuffer());
        }



        public int GetMsgSeq()
        {
            return _header.MsgSeq;
        }

        internal int GetErrorCode()
        {
            return _header.ErrorCode; 
        }

        internal void SetMsgSeq(int seq)
        {
            _header.MsgSeq = seq;
        }

        internal static ReplyPacket OfErrorPacket(int errorCode)
        {
            return new ReplyPacket(errorCode, "", null);
                
        }
    }
}
