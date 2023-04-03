using PlayHouseConnector.network;
using System;
using playhouse_connector_net.network;
using System.Threading.Tasks;
using System.Threading;
using CommonLib;

namespace PlayHouseConnector
{

    public class Connector
    {
        private ClientNetwork? _clientNetwork;        
        private RequestCache _requestCache;
        internal ConnectorConfig _connectorConfig;


        public event Action? OnConnect;
        public event Action<int>? OnReconnect;
        public event Action<short, Packet>? OnReceive;
        public event Action? OnDiconnect;

        
        public void Start()
        {
            AsyncManager.Instance.MainThreadAction();
        }


        public Connector(ConnectorConfig config)
        {            
            _connectorConfig = config;
            _requestCache = new RequestCache(config.ReqestTimeout);

            PooledBuffer.Init(1024 * 1024);
        }
        

        public void Connect(string host,int port)
        {

            if (_connectorConfig.UseWebsocket)
            {
                _clientNetwork = new ClientNetwork(new WsClient(host, port, this, _requestCache));
            }
            else
            {
                _clientNetwork = new ClientNetwork(new TcpClient(host,port,this, _requestCache));                
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
        public void Send(short serviceId,Packet packet) 
        {
            var clientPacket = ClientPacket.ToServerOf(serviceId, packet);
            _clientNetwork!.Send(serviceId, clientPacket);
        }
        public void Request(short serviceId,Packet packet,Action<IReplyPacket> callback) 
        { 
            short seq = (short)_requestCache.GetSequence();
            _requestCache.Put(seq,new ReplyObject(callback));
            var clientPacket = ClientPacket.ToServerOf(serviceId, packet);
            clientPacket.SetMsgSeq(seq);
            _clientNetwork!.Send(serviceId, clientPacket);
            
        }

        public async Task<IReplyPacket> Request(short serviceId, Packet packet)
        {
            short seq = (short)_requestCache.GetSequence(); 
            var deferred = new TaskCompletionSource<ReplyPacket>();
            _requestCache.Put(seq, new ReplyObject(null,deferred));
            var clientPacket = ClientPacket.ToServerOf(serviceId, packet);
            clientPacket.SetMsgSeq(seq);
            _clientNetwork!.Send(serviceId, clientPacket);
            
            return await deferred.Task;
        }

        internal void CallReconnect(int retryCnt)
        {
            OnReconnect?.Invoke(retryCnt);
        }

        internal void CallConnect()
        {
            OnConnect?.Invoke();
        }

        internal void CallReceive(short serviceId, Packet packet)
        {
            OnReceive?.Invoke(serviceId, packet);
        }

        internal void CallDisconnected()
        {
            OnDiconnect?.Invoke();
        }
    }
}