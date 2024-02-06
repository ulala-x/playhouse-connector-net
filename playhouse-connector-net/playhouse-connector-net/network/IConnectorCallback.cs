namespace PlayHouseConnector.Network
{
    public interface IConnectorCallback
    {

        public void ConnectCallback() ;
        public void ReadyCallback();
        public void ReceiveApiCallback(ushort serviceId, IPacket packet);
        public void ReceiveStageCallback(ushort serviceId, int stageKey, IPacket packet);
        //public void CommonReplyCallback(ushort serviceId, IPacket request, IPacket reply);
        //public void CommonReplyExCallback(ushort serviceId, int stageKey, IPacket request, IPacket reply);
        public void ErrorApiCallback(ushort serviceId, ushort errorCode, IPacket request);
        public void ErrorStageCallback(ushort serviceId, int stageKey, ushort errorCode, IPacket request);
        public void DisconnectCallback();
        public void ReconnectedCallback();
        //public void AuthenticateCallback();
    }
}