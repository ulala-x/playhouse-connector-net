
namespace PlayHouseConnector.Network
{
    internal class ClientNetwork
    {
        private readonly IClient _client;

        public ClientNetwork(IClient client)
        {
            _client= client;
        }
        
        internal void Connect()
        {
            
            _client.ClientConnect();
        }

        internal void Disconnect()
        {
            _client.ClientDisconnect();
        }

        internal void ConnectAsync()
        {

            _client.ClientConnectAsync();
        }

        internal void DisconnectAsync()
        {
            _client.ClientDisconnectAsync();
        }

        internal bool IsConnect()
        {
            return _client.IsClientConnected();
        }

        internal void Send(ClientPacket packet)
        {
            using(packet)
            {
                _client.Send(packet);
            }
        }
      
    }
}
