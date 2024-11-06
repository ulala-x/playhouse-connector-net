using System;
using System.Collections.Generic;
using CommonLib;
using PlayHouse;
using PlayHouse.Utils;

namespace PlayHouseConnector.Network
{
  
    /*
     *  4byte  body size
     *  2byte  serviceId
     *  1byte  msgId size
     *  n byte msgId string
     *  2byte  msgSeq
     *  8byte  stageId
     *  2byte  errorCode
     *  From Header Size = 3+2+1+2+8+2+N = 19 + n
     * */

    public sealed class PacketParser
    {
        
        //public const int LENGTH_FIELD_SIZE = 3;
        private LOG<PacketParser> _log = new();

        public List<ClientPacket> Parse(RingBuffer buffer)
        {

            var packets = new List<ClientPacket>();

            while (buffer.Count >= PacketConst.MinPacketSize)
            {
                
                int bodySize = buffer.PeekInt32(buffer.ReaderIndex);

                if (bodySize > PacketConst.MaxPacketSize)
                {
                    _log.Error(() => $"Body size over : {bodySize}");
                    throw new IndexOutOfRangeException("BodySizeOver");
                }

                int checkSizeOfMsg = buffer.PeekByte(buffer.MoveIndex(buffer.ReaderIndex, 4 + 2));

                // If the remaining buffer is smaller than the expected packet size, wait for more data
                if (buffer.Count < bodySize + checkSizeOfMsg + PacketConst.MinPacketSize)
                {
                    break;
                }

                buffer.Clear(sizeof(int));

                var serviceId = buffer.ReadInt16();
                var sizeOfMsgId = buffer.ReadByte();
                var msgName = buffer.ReadString(sizeOfMsgId);

                var msgSeq = buffer.ReadInt16();
                var stageId = buffer.ReadInt64();
                var errorCode = buffer.ReadInt16();

                var body = new PooledByteBuffer(bodySize);

                buffer.Read(body, bodySize);

                var clientPacket = new ClientPacket(new Header(serviceId, msgName, msgSeq, errorCode, stageId), new PooledByteBufferPayload(body));
                packets.Add(clientPacket);

            
            }

            return packets;
        }
    }
}