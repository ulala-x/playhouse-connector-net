
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommonLib;
using PlayHouse.Utils;

namespace PlayHouseConnector.Network
{
    internal class ClientNetwork : IConnectorListener
    {
        private LOG<ClientNetwork> _log = new();
        private readonly IClient _client;
        private readonly RequestCache _requestCache;
        private readonly AsyncManager _asyncManager = new();
        TaskCompletionSource<bool>? _taskOnConnector = null;
        private readonly Connector _connector;
        private readonly ConnectorConfig _config;
        private readonly Stopwatch _stopwatch = new();
        private bool _connectChecker = false;
        public ClientNetwork(Connector connector)
        {
            _connector = connector;
            _config = connector.ConnectorConfig;
            _requestCache = new RequestCache(_config.RequestTimeout);
            PooledBuffer.Init(1024 * 1024);
            
            if (_config.UseWebsocket)
            {
                _client = new WsClient(_config.Host, _config.Port, this);
            }
            else
            {
                _client = new TcpClient(_config.Host, _config.Port,this);                
            }
        }
        
        internal void Connect()
        {
            _client.ClientConnect();
        }

        internal void Disconnect()
        {
            _client.ClientDisconnect();
        }

        internal async Task<bool> ConnectAsync()
        {

            Connect();
            _taskOnConnector = new();
            return await _taskOnConnector.Task;
            
        }

        internal void DisconnectAsync()
        {
            _client.ClientDisconnectAsync();
        }

        internal bool IsConnect()
        {
            return _client.IsClientConnected();
        }
        

        private void _Send(ClientPacket packet)
        {
            using(packet)
            {
                _client.Send(packet);
            }
        }

        private void _Request(ushort serviceId, IPacket packet, int stageKey,ushort seq)
        {
            if (_config.EnableLoggingResponseTime)
            {
                _stopwatch.Reset();
                _stopwatch.Start();
            }

            var clientPacket = ClientPacket.ToServerOf(new TargetId(serviceId, stageKey), packet);
            clientPacket.SetMsgSeq(seq);
            _Send(clientPacket);
        }

        public void Send(ushort serviceId, IPacket packet, int stageKey)
        {
            var clientPacket = ClientPacket.ToServerOf(new TargetId(serviceId,stageKey), packet);
            _Send(clientPacket);
        }
        
        public void Request(ushort serviceId,  IPacket request, Action<IPacket> callback)
        {
            ushort seq = (ushort)_requestCache.GetSequence(); 
      
            _requestCache.Put(seq, new ReplyObject(seq,_asyncManager, (errorCode, reply) =>
            {
                if (errorCode == 0)
                {
                    _connector.CallCommonReply(serviceId,request,reply);
                    callback.Invoke(reply);
                }
                else
                {
                    _connector.CallError(serviceId,errorCode,request);    
                }
            }));

            _Request(serviceId,request,0,seq);
        }
        
        public void RequestEx(ushort serviceId, IPacket request, Action<IPacket> callback,int stageKey)
        {
            ushort seq = (ushort)_requestCache.GetSequence(); 
      
            _requestCache.Put(seq, new ReplyObject(seq,_asyncManager, (errorCode, reply) =>
            {
                if (errorCode == 0)
                {
                    if (_config.EnableLoggingResponseTime)
                    {
                        _stopwatch.Stop();
                        _log.Debug(()=>$"response time - [msgId:{request.MsgId},msgSeq:{seq},elapsedTime:{_stopwatch.ElapsedMilliseconds}]");
                    }
                    _connector.CallCommonReplyEx(serviceId,stageKey,request,reply);
                    callback.Invoke(reply);
                }
                else
                {
                    _connector.CallErrorEx(serviceId,stageKey,errorCode,request);    
                }
            }));

            _Request(serviceId,request,stageKey,seq);
        }
        
        public async Task<IPacket> RequestExAsync(ushort serviceId, IPacket request,int stageKey)
        {
            ushort seq = (ushort)_requestCache.GetSequence(); 
            var deferred = new TaskCompletionSource<IPacket>();
      
            _requestCache.Put(seq, new ReplyObject(seq,_asyncManager, (errorCode, reply) =>
            {
                if (errorCode == 0)
                {
                    if (_config.EnableLoggingResponseTime)
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
        
        public void OnConnected()
        {
            _asyncManager.AddJob(() =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(300));
              
                _connectChecker = true;

                if (_taskOnConnector == null)
                {
                    _connector.CallConnect(true);    
                }
                else
                {
                    _taskOnConnector?.SetResult(true);
                    _taskOnConnector = null;   
                }
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
                    var targetId = new TargetId(clientPacket.ServiceId, clientPacket.Header.StageIndex);
                    var packet = clientPacket.ToPacket();
                    if (!_config.UseExtendStage)
                    {
                        _connector.CallReceive(targetId.ServiceId,  packet);
                    }
                    else
                    {
                        _connector.CallReceiveEx(targetId.ServiceId, targetId.StageIndex, packet);
                    }
                }
                
            });
        }

        public void OnDisconnected()
        {
            _asyncManager.AddJob(() =>
            {
                if (_connectChecker == false)
                {
                    _connector.CallConnect(false);
                    _taskOnConnector?.SetResult(false);
                }
                else
                {
                    _connectChecker = false;
                    _connector.CallDisconnect();    
                }
                _taskOnConnector = null;
            });
        }

        public void MainThreadAction()
        {
            _asyncManager.MainThreadAction();
        }
        public IEnumerator MainCoroutineAction()
        {
            return _asyncManager.MainCoroutineAction();
        }
    }
}
