﻿using System;
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
     *  4byte  original body size (if 0 not compressed)
     *  From Header Size = 3+2+1+2+8+2+4+N = 23 + n
     * */

    public sealed class PacketParser
    {
        private LOG<PacketParser> _log = new();
        private Lz4 _lz4 = new();
        public List<ClientPacket> Parse(RingBuffer buffer)
        {
            var packets = new List<ClientPacket>();

            while (buffer.Count >= PacketConst.MinHeaderSize)
            {

                int bodySize = buffer.PeekInt32(buffer.ReaderIndex);

                if (bodySize > PacketConst.MaxBodySize)
                {
                    _log.Error(() => $"Body size over : {bodySize}");
                    throw new IndexOutOfRangeException("BodySizeOver");
                }

                int checkSizeOfMsg = buffer.PeekByte(buffer.MoveIndex(buffer.ReaderIndex, 4 + 2));

                // If the remaining buffer is smaller than the expected packet size, wait for more data
                if (buffer.Count < bodySize + checkSizeOfMsg + PacketConst.MinHeaderSize)
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
                var originalSize = buffer.ReadInt32();

                PooledByteBuffer body;

                if (originalSize > 0)//compressed
                {
                    body = new PooledByteBuffer(originalSize);
                    buffer.Read(body, bodySize);

                    var source = new ReadOnlySpan<byte>(body.Buffer(), 0, bodySize);
                    var decompressed = _lz4.Decompress(source, originalSize);

                    body.Clear();
                    body.Write(decompressed);

                    _log.Debug(()=>$"decompressed - [msgId:{msgName},originalSize:{originalSize},compressedSize:{bodySize}]");
                }
                else
                {
                    body = new PooledByteBuffer(bodySize);
                    buffer.Read(body, bodySize);
                }

                var clientPacket = new ClientPacket(new Header(serviceId, msgName, msgSeq, errorCode, stageId), new PooledByteBufferPayload(body));
                packets.Add(clientPacket);

            }

            return packets;
        }
    }
}