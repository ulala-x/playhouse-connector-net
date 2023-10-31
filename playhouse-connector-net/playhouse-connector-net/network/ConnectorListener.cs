using PlayHouseConnector;
using PlayHouseConnector.network;
using System.Threading;

namespace playhouse_connector_net.network
{
    internal class ConnectorListener : IConnectorListener
    {
        private Connector _connector;
        private IClient _client;

        private int _retryCnt = 0;
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
                if (_retryCnt > 0)
                {
                    _connector.CallReconnect(_retryCnt);
                }
                else
                {
                    _connector.CallConnect();
                }
            });

            _retryCnt = 0;

        }

  
        public void OnDisconnected()
        {
            int reconnectCount = _connector.ConnectorConfig.ReconnectCount;
            int reconnectDelay = _connector.ConnectorConfig.ReconnectDelay;
            
            if (!_client.IsStopped() && _retryCnt < reconnectCount)
            {
                _retryCnt++;
                Thread.Sleep(reconnectCount * 1000);
                _client.ClientConnectAsync();
            }
            else
            {
                _asyncManager.AddJob(() =>
                {
                    _connector.CallDisconnected();
                });
            }
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
