using playhouse_connector_net;
using PlayHouseConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace playhouse_connector_net.network
{
    public interface IConnectorListener
    {
        void OnConnected();
        void OnReceive(string serviceId, Packet packet);
        void OnDisconnected();
        
    }
}
