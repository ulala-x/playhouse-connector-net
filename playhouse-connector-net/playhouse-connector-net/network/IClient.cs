namespace PlayHouseConnector.Network
{
    internal interface IClient
    {
        void ClientConnect();
        void ClientDisconnect();

        //void ClientConnectAsync();
        //void ClientDisconnectAsync();
        bool IsClientConnected();
        void Send(ClientPacket packet);
        bool IsStopped();
    }
}
