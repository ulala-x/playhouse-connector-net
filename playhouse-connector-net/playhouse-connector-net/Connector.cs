using PlayHouseConnector.network;
using System;
using playhouse_connector_net.network;
using System.Collections;

namespace PlayHouseConnector
{

    public class Connector
    {
        private ClientNetwork? _clientNetwork;        
        private RequestCache _requestCache;
        internal ConnectorConfig _connectorConfig;


        public event Action? OnConnect;
        public event Action<int>? OnReconnect;
        public event Action<string,Packet>? OnReceive;
        public event Action? OnDiconnect;


        public IEnumerator StartForUnity()
        {
            return AsyncManager.Instance.MainThreadActionCoroutine();
        }

        public void Start()
        {
            AsyncManager.Instance.MainThreadAction();
        }


        public Connector(ConnectorConfig config)
        {            
            _connectorConfig = config;
            _requestCache = new RequestCache(config.ReqestTimeout);
        }
        

        public void Connect(string host,int port)
        {

            if (_connectorConfig.UseWebsocket)
            {
                _clientNetwork = new ClientNetwork(new network.WsClient(host, port, this));
            }
            else
            {
                _clientNetwork = new ClientNetwork(new network.TcpClient(host,port,this));                
            }

            _clientNetwork.Connect();
        }

        public void Disconnect() 
        {
            _clientNetwork!.Disconnect();
        }
        public bool IsConnect() 
        {
            return _clientNetwork!.IsConnect();
        }
        public void Send(string serviceId,Packet packet) 
        {
            using (var clientPacket = ClientPacket.ToServerOf(serviceId, packet))
            {
                _clientNetwork!.Send(serviceId, clientPacket);
            }   
        }
        public void Request(string serviceId,Packet packet,Action<ReplyPacket> callback) 
        { 
            int seq = _requestCache.GetSequence();
            _requestCache.Put(seq,new ReplyObject(callback));
            using (var clientPacket = ClientPacket.ToServerOf(serviceId, packet))
            {
                clientPacket.SetMsgSeq(seq);
                _clientNetwork!.Send(serviceId, clientPacket);
            }
                
        }

        internal void CallReconnect(int retryCnt)
        {
            OnReconnect?.Invoke(retryCnt);
        }

        internal void CallConnect()
        {
            OnConnect?.Invoke();
        }

        internal void CallReceive(string serviceId, Packet packet)
        {
            OnReceive?.Invoke(serviceId, packet);
        }

        internal void CallDisconnected()
        {
            OnDiconnect?.Invoke();
        }
    }
}