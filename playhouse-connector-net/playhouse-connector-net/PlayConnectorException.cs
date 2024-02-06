using System;

namespace PlayHouseConnector
{

    public class PlayConnectorException : Exception
    {
        public PlayConnectorException(string message) : base(message){}

        public PlayConnectorException(string message, Exception innerException) : base(message, innerException){}

        public class ConnectError : PlayConnectorException
        {
            public ConnectError(string message) : base(message){}
        }

        public class PacketError : PlayConnectorException
        {
            public ushort ServiceId { get; private set; }
            public int StageKey { get; private set; }
            public IPacket Request { get; private set; }
            public ushort ErrorCode { get; private set; }


            public PacketError(ushort serviceId, int stageKey, ushort errorCode, IPacket request, ushort msgSeq)
                : base($"An error occurred - [serviceId:{serviceId},stageKey:{stageKey},errorCode:{errorCode},req msgId:{request.MsgId},msgSeq:{msgSeq}]")
            {
                ServiceId = serviceId;
                StageKey = stageKey;
                ErrorCode = errorCode;
                Request = request;
            }

            public PacketError(ushort serviceId, int stageKey, ushort errorCode, string message, IPacket request)
                : base(message)
            {
                ServiceId = serviceId;
                StageKey = stageKey;
                ErrorCode = errorCode;
                Request = request;
            }

            public PacketError(ushort serviceId, int stageKey, ushort errorCode, string message, Exception innerException, IPacket request)
                : base(message, innerException)
            {
                ServiceId = serviceId;
                StageKey = stageKey;
                ErrorCode = errorCode;
                Request = request;
            }
        }
        
    }

}