namespace PlayHouseConnector.Network
{
    public interface IConnectorListener
    {
        void OnConnected();
        void OnReceive(ClientPacket clientPacket);
        void OnDisconnected();
        
    }
}
