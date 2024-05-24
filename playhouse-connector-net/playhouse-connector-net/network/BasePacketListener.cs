namespace PlayHouseConnector.Network
{
    internal interface IBasePacketListener
    {
        void OnConnect();
        void OnDisconnected();
    }
}