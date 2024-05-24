using System;

namespace PlayHouseConnector
{
    public class PlayConnectorException : Exception
    {
        public PlayConnectorException(ushort serviceId, long stageId, ushort errorCode, IPacket request, ushort msgSeq)
            : base(
                $"An error occurred - [serviceId:{serviceId},stageId:{stageId},errorCode:{errorCode},req msgId:{request.MsgId},msgSeq:{msgSeq}]")
        {
            ServiceId = serviceId;
            StageId = stageId;
            ErrorCode = errorCode;
            Request = request;
        }

        public PlayConnectorException(ushort serviceId, long stageId, ushort errorCode, string message, IPacket request)
            : base(message)
        {
            ServiceId = serviceId;
            StageId = stageId;
            ErrorCode = errorCode;
            Request = request;
        }

        public PlayConnectorException(ushort serviceId, long stageId, ushort errorCode, string message,
            Exception innerException, IPacket request)
            : base(message, innerException)
        {
            ServiceId = serviceId;
            StageId = stageId;
            ErrorCode = errorCode;
            Request = request;
        }

        public ushort ServiceId { get; private set; }
        public long StageId { get; private set; }
        public IPacket Request { get; private set; }
        public ushort ErrorCode { get; private set; }
    }
}