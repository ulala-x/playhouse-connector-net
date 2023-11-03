namespace PlayHouseConnector.Network
{
    internal class ConnectorListener : IConnectorListener
    {
        private Connector _connector;
        private IClient _client;

        private RequestCache _requestCache;
        private AsyncManager _asyncManager;
        
        public ConnectorListener(Connector connector, IClient client, RequestCache requestCache,
            AsyncManager asyncManager)
        {
           _connector = connector;
           _client = client;
            _requestCache = requestCache;
            _asyncManager = asyncManager;
        }

        public void OnConnected()
        {
            _asyncManager.AddJob(() =>
            {
                _connector.CallConnect();
            });
        }
  
        public void OnDisconnected()
        {
            _asyncManager.AddJob(() =>
            {
                _connector.CallDisconnected();
            });
        }

        public void OnReceive(ClientPacket clientPacket)
        {
            _asyncManager.AddJob(() =>
            {
                if (clientPacket.MsgSeq > 0)
                {
                    _requestCache.OnReply(clientPacket);
                }
                else
                {
                    _connector.CallReceive(new TargetId(clientPacket.ServiceId,clientPacket.Header.StageIndex), clientPacket.ToPacket());
                }
                
            });
        }
    }
}
