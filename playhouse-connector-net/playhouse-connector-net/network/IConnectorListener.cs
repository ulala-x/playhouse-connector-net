using PlayHouseConnector.network;

namespace playhouse_connector_net.network
{
    public interface IConnectorListener
    {
        void OnConnected();
        void OnReceive(ClientPacket clientPacket);
        void OnDisconnected();
        
    }
}
