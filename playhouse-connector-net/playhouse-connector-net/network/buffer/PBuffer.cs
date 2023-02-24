using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlayHouseConnector.network.buffer
{
    public class PBuffer :  DynamicBuffer
    {

        public PBuffer() :base(){ }
        public PBuffer(long capacity) : base(capacity) { }
        public PBuffer(byte[] data) : base(data) { }

        public static new void Init(long maxBufferPoolSize)
        {
            DynamicBuffer.Init(maxBufferPoolSize);
        }       
    }
}
