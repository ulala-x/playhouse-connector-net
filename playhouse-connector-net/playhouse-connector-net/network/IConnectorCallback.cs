namespace PlayHouseConnector.Network
{
    public interface IConnectorCallback
    {
        public void ConnectCallback(bool result) ;
        public void ReceiveCallback(ushort serviceId, IPacket packet);
        public void ReceiveStageCallback(ushort serviceId, long stageId, IPacket packet);
        //public void CommonReplyCallback(ushort serviceId, IPacket request, IPacket reply);
        //public void CommonReplyExCallback(ushort serviceId, long stageId, IPacket request, IPacket reply);
        public void ErrorCallback(ushort serviceId, ushort errorCode, IPacket request);
        public void ErrorStageCallback(ushort serviceId, long stageId, ushort errorCode, IPacket request);
        public void DisconnectCallback();

    }
}