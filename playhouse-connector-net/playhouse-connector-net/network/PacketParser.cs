using PlayHouseConnector.network.buffer;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel.Channels;

namespace PlayHouseConnector.network
{
    public class PacketParser
    {
        
        public const int MAX_PACKET_SIZE = 65535;
        public const int HEADER_SIZE = 256;
        public const int LENGTH_FIELD_SIZE = 3;

        
        private ILogger _log = Log.ForContext<PacketParser>();
        

        public virtual List<ClientPacket> Parse(PBuffer buffer)
        {
            
            var packets = new List<ClientPacket>();

            while (buffer.Size >= LENGTH_FIELD_SIZE)
            {
                try { 
                    
                    int headerSize = buffer[0];

                    if (headerSize > HEADER_SIZE)
                    {
                        _log.Error("Header size over : {HeaderSize}", headerSize);
                        throw new IndexOutOfRangeException("HeaderSizeOver");
                    }

                    int bodySize = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(new Span<byte>(buffer.Data, 1, 2)));

                    if (bodySize > MAX_PACKET_SIZE)
                    {
                        _log.Error("Body size over : {BodySize}", bodySize);
                        throw new IndexOutOfRangeException("BodySizeOver");
                    }

                    // If the remaining buffer is smaller than the expected packet size, wait for more data
                    if (buffer.Size < bodySize + LENGTH_FIELD_SIZE)
                    {
                        break;
                    }

                    var header = HeaderMsg.Parser.ParseFrom(new Span<byte>(buffer.Data,LENGTH_FIELD_SIZE, headerSize));

                    var body = new PBuffer(bodySize);
                    body.Append(new Span<byte>(buffer.Data, LENGTH_FIELD_SIZE + headerSize, bodySize));

                    //Buffer.BlockCopy(buffer.Data, LENGTH_FIELD_SIZE + headerSize, body, 0, bodySize);

                    var clientPacket = new ClientPacket(Header.Of(header),body);
                    packets.Add(clientPacket);

                    buffer.Remove(0, LENGTH_FIELD_SIZE + headerSize + bodySize);

                }
                catch (Exception e)
                {
                    _log.Error(e, "Exception while parsing packet");
                }
            }

            return packets;
        }
    }
}
