using playhouse_connector_net.network;
using PlayHouseConnector;
using Serilog;
using System.Collections.Concurrent;

namespace playhouse_connector_net_sample
{
    class ClientConnectorListener : IConnectorListener
    {
        private ConcurrentQueue<Packet> _packetQueue = new ConcurrentQueue<Packet>();


        public void OnConnected()
        {
            Log.Information($"onConnected");
        }

        public void OnDisconnected()
        {
            Log.Information($"onConnected");
        }

        public void OnReceive(string serviceId, Packet packet)
        {
            _packetQueue.Enqueue(packet);
        }
    }
}
