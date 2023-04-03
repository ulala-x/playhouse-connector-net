using Google.Protobuf;
using System;
using System.IO;

namespace playhouse_connector_net
{
    public interface IPayload : IDisposable
    {   
        void Output(Stream outputStream);
        (byte[], int) Data { get; }
    }

    public class ProtoPayload : IPayload
    {
        private readonly IMessage _proto;

        public ProtoPayload(IMessage proto)
        {
            _proto = proto;
        }

     
        public void Output(Stream outputStream)
        {
            _proto.WriteTo(outputStream);
        }

        public void Dispose()
        {
        }

        public (byte[], int) Data => (_proto.ToByteArray(), _proto.CalculateSize());
        
    }




    public class EmptyPayload : IPayload
    {
        
        public void Output(Stream outputStream)
        {
        }

        public void Dispose()
        {
        }

        public (byte[], int) Data => (new byte[0],0);
    }

  

}
