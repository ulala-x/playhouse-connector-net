using PlayHouseConnector.Network;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommonLib;
using PlayHouse.Utils;

//using ReplyCallback = System.Action<ushort, PlayHouseConnector.IPacket>;
namespace PlayHouseConnector
{
    public class Connector
    {
        private ClientNetwork? _clientNetwork;        
        private readonly RequestCache _requestCache;
        private readonly ConnectorConfig _connectorConfig;
        private bool _connectChecker = false;

        public event Action<bool>? OnConnect;
        public event Action<ushort,IPacket>? OnReceive; //serviceId,packet
        public event Action<ushort,int, IPacket>? OnReceiveEx; //serviceId,stageKey,packet
        public event Action<ushort,IPacket, IPacket>? OnCommonReply;//serviceId,request,reply
        public event Action<ushort,int,IPacket, IPacket>? OnCommonReplyEx;//serviceId,stageKey,request,reply
        
        public event Action<ushort,ushort, IPacket>? OnError; //serviceId,errorCode,request
        public event Action<ushort,int,ushort, IPacket>? OnErrorEx; //serviceId,stagekey,errorCode,request
        
        public event Action? OnDisconnectAction;
        
        internal readonly AsyncManager AsyncManager = new();
        TaskCompletionSource<bool>? _taskOnConnector = null;

        private readonly Stopwatch _stopwatch = new();
        private LOG<Connector> _log = new();
        
        
        public void MainThreadAction()
        {
            AsyncManager.MainThreadAction();
        }
        
        
        public Connector(ConnectorConfig config)
        {            
            _connectorConfig = config;
            _requestCache = new RequestCache(config.RequestTimeout);

            PooledBuffer.Init(1024 * 1024);
        }
        
        public void Connect(string host,int port)
        {

            if (_connectorConfig.UseWebsocket)
            {
                _clientNetwork = new ClientNetwork(new WsClient(host, port, this, _requestCache,AsyncManager));
            }
            else
            {
                _clientNetwork = new ClientNetwork(new TcpClient(host,port,this, _requestCache,AsyncManager));                
            }

            _clientNetwork.Connect();
        }

        public async Task<bool> ConnectAsync(string host, int port)
        {
            Connect(host, port);
            _taskOnConnector = new();
            return await _taskOnConnector.Task;
        }


   
        public void Disconnect() 
        {
            _clientNetwork!.DisconnectAsync();
        }
      
        public bool IsConnect() 
        {
            return _clientNetwork!.IsConnect();
        }

        private void _Request(ushort serviceId, IPacket packet, int stageKey,ushort seq)
        {
            if (_connectorConfig.EnableLoggingResponseTime)
            {
                _stopwatch.Reset();
                _stopwatch.Start();
            }

            var clientPacket = ClientPacket.ToServerOf(new TargetId(serviceId, stageKey), packet);
            clientPacket.SetMsgSeq(seq);
            _clientNetwork!.Send(clientPacket);
        }

        private void _Send(ushort serviceId, IPacket packet, int stageKey)
        {
            var clientPacket = ClientPacket.ToServerOf(new TargetId(serviceId,stageKey), packet);
            _clientNetwork!.Send(clientPacket);
        }

        public void Send(ushort serviceId,IPacket packet)
        {
            _Send(serviceId, packet, 0);
        }
        public void SendEx(ushort serviceId,int stageKey,IPacket packet)
        {
            _Send(serviceId, packet, stageKey);
        }
        public void Request(ushort serviceId,  IPacket request, Action<IPacket> callback)
        {
            ushort seq = (ushort)_requestCache.GetSequence(); 
      
            _requestCache.Put(seq, new ReplyObject(seq,AsyncManager, (errorCode, reply) =>
            {
                if (errorCode == 0)
                {
                    OnCommonReply?.Invoke(serviceId,request,reply);
                    callback.Invoke(reply);
                }
                else
                {
                    OnError?.Invoke(serviceId,errorCode,request);    
                }
            }));

            _Request(serviceId,request,0,seq);
        }
        public void RequestEx(ushort serviceId, IPacket request, Action<IPacket> callback,int stageKey = 1)
        {
            ushort seq = (ushort)_requestCache.GetSequence(); 
      
            _requestCache.Put(seq, new ReplyObject(seq,AsyncManager, (errorCode, reply) =>
            {
                if (errorCode == 0)
                {
                    if (_connectorConfig.EnableLoggingResponseTime)
                    {
                        _stopwatch.Stop();
                        _log.Debug(()=>$"response time - [msgId:{request.MsgId},msgSeq:{seq},elapsedTime:{_stopwatch.ElapsedMilliseconds}]");
                    }
                    OnCommonReplyEx?.Invoke(serviceId,stageKey,request,reply);
                    callback.Invoke(reply);
                }
                else
                {
                    OnErrorEx?.Invoke(serviceId,stageKey,errorCode,request);    
                }
            }));

            _Request(serviceId,request,stageKey,seq);
        }

        public async Task<IPacket> RequestAsync(ushort serviceId, IPacket request)
        {
            return await RequestExAsync(serviceId, request, 0);
        }
        public async Task<IPacket> RequestExAsync(ushort serviceId, IPacket request,int stageKey = 1)
        {
            ushort seq = (ushort)_requestCache.GetSequence(); 
            var deferred = new TaskCompletionSource<IPacket>();
      
            _requestCache.Put(seq, new ReplyObject(seq,AsyncManager, (errorCode, reply) =>
            {
                if (errorCode == 0)
                {
                    if (_connectorConfig.EnableLoggingResponseTime)
                    {
                        _stopwatch.Stop();
                        _log.Debug(()=>$"response time - [msgId:{request.MsgId},msgSeq:{seq},elapsedTime:{_stopwatch.ElapsedMilliseconds}]");
                    }
                    
                    deferred.SetResult(reply);
                }
                else
                {
                    deferred.SetException(new PlayConnectorException(serviceId,stageKey,errorCode,request,seq));    
                }
            }));
            
         
            _Request(serviceId,request,stageKey,seq);
            return await deferred.Task;
        }

        
        internal void CallConnect()
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(300));
              
            _connectChecker = true;
            OnConnect?.Invoke(true);
            _taskOnConnector?.SetResult(true);
            _taskOnConnector = null;
        }

        internal void CallReceive(TargetId targetId, IPacket packet)
        {
            {
                if (targetId.StageIndex == 0)
                {
                    if (OnReceive == null)
                    {
                        _log.Error(()=>"OnReceive callback not bind");
                    }
                    OnReceive?.Invoke(targetId.ServiceId,  packet);
                }
                else
                {
                    if (OnReceiveEx == null)
                    {
                        _log.Error(()=>"OnReceiveEx callback not bind");
                    }
                    OnReceiveEx?.Invoke(targetId.ServiceId, targetId.StageIndex, packet);
                }
    
            }
        }
        internal void CallDisconnected()
        {
             //connect 의 결과
            if (_connectChecker == false)
            {
                OnConnect?.Invoke(false);
                _taskOnConnector?.SetResult(false);
            }
            else
            {
                _connectChecker = false;
                OnDisconnectAction?.Invoke();    
            }

            _taskOnConnector = null;

        }
    }
}