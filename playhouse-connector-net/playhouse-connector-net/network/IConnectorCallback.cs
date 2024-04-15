namespace PlayHouseConnector.Network
{
    public interface IConnectorCallback
    {
        public void ConnectCallback(bool result) ;
        public void ReceiveCallback(ushort serviceId, IPacket packet);
        public void ReceiveExCallback(ushort serviceId, long stageId, IPacket packet);
        public void CommonReplyCallback(ushort serviceId, IPacket request, IPacket reply);
        public void CommonReplyExCallback(ushort serviceId, long stageId, IPacket request, IPacket reply);
        public void ErrorCallback(ushort serviceId, ushort errorCode, IPacket request);
        public void ErrorExCallback(ushort serviceId, long stageId, ushort errorCode, IPacket request);
        public void DisconnectCallback();

    }
}