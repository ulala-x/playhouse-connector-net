using PlayHouseConnector.network;
using System;
using playhouse_connector_net.network;
using System.Threading.Tasks;
using System.Threading;
using CommonLib;

namespace PlayHouseConnector
{
    public class TargetId
    {
        public short ServiceId { get; }
        public int StageIndex { get; }

        public TargetId(short serviceId, int stageIndex = 0)
        {
            if (stageIndex > byte.MaxValue)
            {
                throw new ArithmeticException("stageIndex overflow");
            }
            ServiceId = serviceId;
            StageIndex = stageIndex;
        }
    }
    public class Connector
    {
        private ClientNetwork? _clientNetwork;        
        private RequestCache _requestCache;
        internal ConnectorConfig _connectorConfig;


        public event Action? OnConnect;
        public event Action<int>? OnReconnect;
        public event Action<TargetId, Packet>? OnReceive;
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
        public void Send(TargetId targetId,Packet packet) 
        {
            var clientPacket = ClientPacket.ToServerOf(targetId, packet);
            _clientNetwork!.Send(clientPacket);
        }
        public void Request(TargetId targetId, Packet packet,Action<IReplyPacket> callback) 
        { 
            short seq = (short)_requestCache.GetSequence();
            _requestCache.Put(seq,new ReplyObject(callback));
            var clientPacket = ClientPacket.ToServerOf(targetId, packet);
            clientPacket.SetMsgSeq(seq);
            _clientNetwork!.Send(clientPacket);
            
        }

        public async Task<IReplyPacket> Request(TargetId targetId, Packet packet)
        {
            short seq = (short)_requestCache.GetSequence(); 
            var deferred = new TaskCompletionSource<ReplyPacket>();
            _requestCache.Put(seq, new ReplyObject(null,deferred));
            var clientPacket = ClientPacket.ToServerOf(targetId, packet);
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
            OnReceive?.Invoke(targetId, packet);
        }

        internal void CallDisconnected()
        {
            OnDiconnect?.Invoke();
        }
    }
}