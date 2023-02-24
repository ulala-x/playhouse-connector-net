using System.Net.Sockets;

namespace PlayHouseConnector.network
{
    internal class ClientNetwork
    {
        private IClient _client;

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

        internal bool IsConnect()
        {
            return _client.IsClientConnected();
        }

        internal void Send(string serviceId, ClientPacket packet)
        {
            _client.Send(serviceId, packet);
        }

       
    }
}
