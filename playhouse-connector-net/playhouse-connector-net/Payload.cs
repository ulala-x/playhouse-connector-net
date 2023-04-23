using Google.Protobuf;
using System;
using System.IO;

namespace playhouse_connector_net
{
    public interface IPayload : IDisposable
    {   
        ReadOnlySpan<byte> Data { get; }
    }
    public class ProtoPayload : IPayload
    {
        private readonly IMessage _proto;

        public ProtoPayload(IMessage proto)
        {
            _proto = proto;
        }
        public void Dispose()
        {
        }

        public ReadOnlySpan<byte> Data => (_proto.ToByteArray());
    }

    public class EmptyPayload : IPayload
    {
        public void Output(Stream outputStream)
        {
        }
        public void Dispose()
        {
        }
        public ReadOnlySpan<byte> Data => new ReadOnlySpan<byte>();
    }

  

}
