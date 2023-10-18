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
        //public event Action<ushort, Packet>? OnApiReceive;
        public event Action<ushort,int, Packet>? OnReceive;
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

            _clientNetwork.ConnectAsync();
        }

      
        public bool Reconnect()
        {
            return _clientNetwork!.Reconnect();
        }

        public void Disconnect() 
        {
            _clientNetwork!.DisconnectAsync();
        }
      
        public bool IsConnect() 
        {
            return _clientNetwork!.IsConnect();
        }

        public void SendToApi(ushort serviceId,Packet packet)
        {
            SendToStage(serviceId,0,packet);
        }
        public void RequestToApi(ushort serviceId,  Packet packet, Action<IReplyPacket> callback)
        {
            RequestToStage(serviceId,0,packet,callback);
        }

        public async Task<IReplyPacket> RequestToApi(ushort serviceId, Packet packet)
        {
            return await RequestToStage(serviceId,0,packet);
        }
        public void SendToStage(ushort serviceId,int stageindex,Packet packet) 
        {
            var clientPacket = ClientPacket.ToServerOf(new TargetId(serviceId,stageindex), packet);
            _clientNetwork!.Send(clientPacket);
        }
        public void RequestToStage(ushort serviceId, int stageindex, Packet packet,Action<IReplyPacket> callback) 
        { 
            ushort seq = (ushort)_requestCache.GetSequence();
            _requestCache.Put(seq,new ReplyObject(callback));
            var clientPacket = ClientPacket.ToServerOf(new TargetId(serviceId, stageindex), packet);
            clientPacket.SetMsgSeq(seq);
            _clientNetwork!.Send(clientPacket);
            
        }

        public async Task<IReplyPacket> RequestToStage(ushort serviceId, int stageindex, Packet packet)
        {
            ushort seq = (ushort)_requestCache.GetSequence(); 
            var deferred = new TaskCompletionSource<ReplyPacket>();
            _requestCache.Put(seq, new ReplyObject(null,deferred));
            var clientPacket = ClientPacket.ToServerOf(new TargetId(serviceId, stageindex), packet);
            clientPacket.SetMsgSeq(seq);
            _clientNetwork!.Send(clientPacket);
            
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

        internal void CallReceive(TargetId targetId, Packet packet)
        {
            OnReceive(targetId.ServiceId, targetId.StageIndex, packet);
            // if(targetId.ServiceId == 0)
            // {
            //     OnApiReceive?.Invoke(targetId.ServiceId, packet);
            // }
            // else
            // {
            //     OnStageReceive?.Invoke(targetId.ServiceId,targetId.StageIndex, packet);
            // }

        }

        internal void CallDisconnected()
        {
            OnDiconnect?.Invoke();
        }




    }
}