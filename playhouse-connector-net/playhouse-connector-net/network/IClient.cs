using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouseConnector.network
{
    internal interface IClient
    {
        void ClientConnect();
        void ClientDisconnect();
        bool IsClientConnected();
        void Send(short serviceId, ClientPacket packet);
        bool IsStoped();
    }
}
