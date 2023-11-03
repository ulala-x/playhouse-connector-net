using CommonLib;
using System;
using System.Collections.Generic;
using PlayHouse.Utils;

namespace PlayHouseConnector.Network
{
    public sealed class PacketParser
    {
        public const int MAX_PACKET_SIZE = 65535;
        public const int HEADER_SIZE = 13;
        //public const int LENGTH_FIELD_SIZE = 3;
        private LOG<PacketParser> _log = new();

        public List<ClientPacket> Parse(RingBuffer buffer)
        {
            
            var packets = new List<ClientPacket>();
                        
            while (buffer.Count >= HEADER_SIZE)
            {
                try { 
                    
                    int bodySize = XBitConverter.ToHostOrder(buffer.PeekInt16(buffer.ReaderIndex));

                    if (bodySize > MAX_PACKET_SIZE)
                    {
                        _log.Error(()=>$"Body size over : {bodySize}");
                        throw new IndexOutOfRangeException("BodySizeOver");
                    }

                    // If the remaining buffer is smaller than the expected packet size, wait for more data
                    if (buffer.Count < bodySize + HEADER_SIZE)
                    {
                        break;
                    }

                    buffer.Clear(2);

                    ushort serviceId = XBitConverter.ToHostOrder(buffer.ReadInt16());
                    int msgId = XBitConverter.ToHostOrder(buffer.ReadInt32());
                    ushort msgSeq = XBitConverter.ToHostOrder(buffer.ReadInt16());
                    byte stageIndex = buffer.ReadByte();
                    ushort errorCode = XBitConverter.ToHostOrder(buffer.ReadInt16());
                    

                    var body = new PooledBuffer(bodySize);

                    buffer.Read(body,bodySize);

                    var clientPacket = new ClientPacket(new Header(serviceId,msgId,msgSeq,errorCode, stageIndex),new PooledBufferPayload(body));
                    packets.Add(clientPacket);

                }
                catch (Exception ex)
                {
                    _log.Error(()=>$"Exception while parsing packet - [exception:{ex}]");
                }
            }

            return packets;
        }
    }
}
