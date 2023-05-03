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

        void ClientConnectAsync();
        void ClientDisconnectAsync();
        bool IsClientConnected();
        void Send(ClientPacket packet);
        bool IsStoped();
        bool ClientReconnect();
    }
}
