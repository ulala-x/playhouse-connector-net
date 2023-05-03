using PlayHouseConnector;
using PlayHouseConnector.network;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace playhouse_connector_net.network
{
    internal class ConnectorListener : IConnectorListener
    {
        private Connector _connector;
        private IClient _client;

        private int _retryCnt = 0;
        private RequestCache _requestCache;
        
        public ConnectorListener(Connector connector,IClient client, RequestCache requestCache)
        {
           _connector = connector;
           _client = client;
            _requestCache = requestCache;
        }

        public void OnConnected()
        {
            AsyncManager.Instance.AddJob(() =>
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
            int reconnectCount = _connector._connectorConfig.ReconnectCount;
            int reconnectDelay = _connector._connectorConfig.ReconnectDelay;
            
            if (!_client.IsStoped() && _retryCnt < reconnectCount)
            {
                _retryCnt++;
                Thread.Sleep(reconnectCount * 1000);
                _client.ClientConnectAsync();
            }
            else
            {
                AsyncManager.Instance.AddJob(() =>
                {
                    _connector.CallDisconnected();
                });
            }
        }

        public void OnReceive(ClientPacket clientPacket)
        {

            AsyncManager.Instance.AddJob(() =>
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
