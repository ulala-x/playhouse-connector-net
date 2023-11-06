namespace PlayHouseConnector.Network
{
    public interface IConnectorCallback
    {
        public void ConnectCallback(bool result) ;
        public void ReceiveCallback(ushort serviceId, IPacket packet);
        public void ReceiveExCallback(ushort serviceId, int stageKey, IPacket packet);
        public void CommonReplyCallback(ushort serviceId, IPacket request, IPacket reply);
        public void CommonReplyExCallback(ushort serviceId, int stageKey, IPacket request, IPacket reply);
        public void ErrorCallback(ushort serviceId, ushort errorCode, IPacket request);
        public void ErrorExCallback(ushort serviceId, int stageKey, ushort errorCode, IPacket request);
        public void DisconnectCallback();

    }
}