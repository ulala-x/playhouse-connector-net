using System;
using System.Collections.Generic;
using CommonLib;
using PlayHouse;
using PlayHouse.Utils;

namespace PlayHouseConnector.Network
{
    public sealed class PacketParser
    {
        public const int MAX_PACKET_SIZE = 2097152;

        public const int HEADER_SIZE = 14;

        //public const int LENGTH_FIELD_SIZE = 3;
        private readonly LOG<PacketParser> _log = new();

        public List<ClientPacket> Parse(RingBuffer buffer)
        {
            var packets = new List<ClientPacket>();

            while (buffer.Count >= HEADER_SIZE)
            {
                try
                {
                    var bodySize = XBitConverter.ToHostOrder(buffer.PeekInt32(buffer.ReaderIndex));

                    if (bodySize > MAX_PACKET_SIZE)
                    {
                        _log.Error(() => $"Body size over : {bodySize}");
                        throw new IndexOutOfRangeException("BodySizeOver");
                    }

                    // If the remaining buffer is smaller than the expected packet size, wait for more data
                    if (buffer.Count < bodySize + HEADER_SIZE)
                    {
                        break;
                    }

                    buffer.Clear(4);

                    var serviceId = XBitConverter.ToHostOrder(buffer.ReadInt16());
                    var msgId = XBitConverter.ToHostOrder(buffer.ReadInt32());
                    var msgSeq = XBitConverter.ToHostOrder(buffer.ReadInt16());
                    var stageId = XBitConverter.ToHostOrder(buffer.ReadInt64());
                    var errorCode = XBitConverter.ToHostOrder(buffer.ReadInt16());


                    var body = new PooledByteBuffer(bodySize);

                    buffer.Read(body, bodySize);

                    var clientPacket = new ClientPacket(new Header(serviceId, msgId, msgSeq, errorCode, stageId),
                        new PooledByteBufferPayload(body));
                    packets.Add(clientPacket);
                }
                catch (Exception ex)
                {
                    _log.Error(() => $"Exception while parsing packet - [exception:{ex}]");
                }
            }

            return packets;
        }
    }

    /*
     *  2byte  header size
     *  3byte  body size
     *  2byte  serviceId
     *  1byte  msgId size
     *  n byte msgId string
     *  2byte  msgSeq
     *  8byte  stageId
     *  2byte  errorCode
     *  From Header Size = 2+3+2+1+2+8+2+N = 20 + n
     * */

    //    public sealed class PacketParser
    //{
    //    public const int MAX_PACKET_SIZE = 16777215;
    //    public const int MIN_SIZE = 4;
    //    //public const int LENGTH_FIELD_SIZE = 3;
    //    private LOG<PacketParser> _log = new();

    //    public List<ClientPacket> Parse(RingBuffer buffer)
    //    {

    //        var packets = new List<ClientPacket>();

    //        while (buffer.Count >= MIN_SIZE)
    //        {
    //            try {

    //                int headerSize = buffer.PeekInt16(buffer.ReaderIndex);
    //                int bodySize = buffer.PeekInt24(buffer.MoveIndex(buffer.ReaderIndex, 2));

    //                if (bodySize > MAX_PACKET_SIZE)
    //                {
    //                    _log.Error(() => $"Body size over : {bodySize}");
    //                    throw new IndexOutOfRangeException("BodySizeOver");
    //                }

    //                // If the remaining buffer is smaller than the expected packet size, wait for more data
    //                if (buffer.Count < bodySize + headerSize)
    //                {
    //                    break;
    //                }

    //                buffer.Clear(5);

    //                ushort serviceId = buffer.ReadInt16();
    //                byte sizeOfMsgName = buffer.ReadByte();
    //                string msgName = buffer.ReadString(sizeOfMsgName);

    //                ushort msgSeq = buffer.ReadInt16();
    //                long stageId = buffer.ReadInt64();
    //                ushort errorCode = buffer.ReadInt16();

    //                var body = new PooledByteBuffer(bodySize);

    //                buffer.Read(body,bodySize);

    //                var clientPacket = new ClientPacket(new Header(serviceId, msgName, msgSeq,errorCode, stageId),new PooledByteBufferPayload(body));
    //                packets.Add(clientPacket);

    //            }
    //            catch (Exception ex)
    //            {
    //                _log.Error(()=>$"Exception while parsing packet - [exception:{ex}]");
    //            }
    //        }

    //        return packets;
    //    }
    //}
}