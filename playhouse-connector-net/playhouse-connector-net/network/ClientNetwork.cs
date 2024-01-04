
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
        private readonly IConnectorCallback _connectorCallback;
        private readonly ConnectorConfig _config;
        private readonly Stopwatch _stopwatch = new();
        private bool _connectChecker = false;
        private bool _isAuthenticate = false;
        private DateTime _lastReceivedTime = DateTime.Now;
        private DateTime _lastSendHeartBeatTime = DateTime.Now;

        private Timer _timer;
        private bool _debugMode = false;


        public ClientNetwork(ConnectorConfig config, IConnectorCallback connectorCallback)
        {
            _connectorCallback = connectorCallback;
            _config = config;
            _requestCache = new RequestCache(_config.RequestTimeoutMs);
            PooledBuffer.Init(1024 * 1024);
            
            if (_config.UseWebsocket)
            {
                _client = new WsClient(_config.Host, _config.Port, this);
            }
            else
            {
                _client = new TcpClient(_config.Host, _config.Port,this);                
            }
            _timer = new Timer(TimerCallback, this, 100, 100);
        }

        private bool IsIdleState()
        {
            if(_isAuthenticate == false || _config.ConnectionIdleTimeoutMs == 0 || _debugMode)
            {
                return false;
            }

            return GetElapedTime(_lastReceivedTime) > _config.ConnectionIdleTimeoutMs;
        }

        private static void TimerCallback(object? o)
        {
            ClientNetwork network = (ClientNetwork)o!;
            if (network._client.IsClientConnected())
            {
                if(network._debugMode == false)
                {
                    network._requestCache.CheckExpire();
                }

                network.SendHeartBeat();

                if (network.IsIdleState())
                {
                    network._log.Debug(() => $"Client disconnect cause idle time");
                    network.Disconnect();
            }
            }
        }

        private void UpdateTime(ref DateTime time)
        {
            time = DateTime.Now;
        }
        private long GetElapedTime(DateTime time)
        {
            var timeDifference = DateTime.Now - _lastReceivedTime;
            return (long)timeDifference.TotalMilliseconds;
        }

        private void SendHeartBeat()
        {
            if(_config.HeartBeatIntervalMs == 0)
            {
                return;
            }

            if(GetElapedTime(_lastSendHeartBeatTime) > _config.HeartBeatIntervalMs)
            {
                Packet packet = new Packet(-1);
                Send(0, packet, 0);
                UpdateTime(ref _lastSendHeartBeatTime);
            }
            
        }

        private void SendDebugMode()
        {
            Packet packet = new Packet(-2);
            Send(0, packet, 0);
        }
        internal void Connect(bool debugMode)
        {
            _debugMode = debugMode;               
            _client.ClientConnect();
        }

        internal void Disconnect()
        {
            _client.ClientDisconnect();
        }

        internal async Task<bool> ConnectAsync(bool debugMode)
        {

            Connect(debugMode);
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

        internal bool IsAuthenticated()
        {
            return _isAuthenticate;
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
        
        public void Request(ushort serviceId, IPacket request, Action<IPacket> callback,int stageKey,bool forSystem = false)
        {
            ushort seq = (ushort)_requestCache.GetSequence(); 

      
            _requestCache.Put(seq, new ReplyObject(seq, (errorCode, reply) =>
            {

                if (_config.EnableLoggingResponseTime)
                {
                    _stopwatch.Stop();
                    _log.Debug(() => $"response time - [msgId:{request.MsgId},msgSeq:{seq},elapsedTime:{_stopwatch.ElapsedMilliseconds}]");
                }

   
                _asyncManager.AddJob(() =>
                {
                    if (errorCode == 0)
                    {
                        if(forSystem)
                        {
                            _isAuthenticate = true;
                        }

                        if (_config.UseExtendStage)
                        {
                            _connectorCallback.CommonReplyExCallback(serviceId, stageKey, request, reply);
                        }
                        else
                        {
                            _connectorCallback.CommonReplyCallback(serviceId, request, reply);

                        }

                        callback.Invoke(reply);
                    }
                    else
                    {
                        if (_config.UseExtendStage)
                        {
                            _connectorCallback.ErrorExCallback(serviceId, stageKey, errorCode, request);
                        }
                        else
                        {
                            _connectorCallback.ErrorCallback(serviceId, errorCode, request);
                        }
                    }
                });
                    
                
            }));

            _Request(serviceId,request,stageKey,seq);
        }
        
        public async Task<IPacket> RequestAsync(ushort serviceId, IPacket request,int stageKey,bool forAthenticate = false)
        {
            ushort seq = (ushort)_requestCache.GetSequence(); 
            var deferred = new TaskCompletionSource<IPacket>();

            _requestCache.Put(seq, new ReplyObject(seq, (errorCode, reply) =>
            {

                if (errorCode == 0)
                {
                    if(forAthenticate)
                    {
                        _isAuthenticate = true;
                    }

                    if (_config.EnableLoggingResponseTime)
                    {
                        _stopwatch.Stop();
                        _log.Debug(()=>$"response time - [msgId:{request.MsgId},msgSeq:{seq},elapsedTime:{_stopwatch.ElapsedMilliseconds}]");
                    }
                    
                    _asyncManager.AddJob(() =>
                    {
                        deferred.SetResult(reply);    
                    });
                }
                else
                {
                    _asyncManager.AddJob(() =>
                    {
                        
                        deferred.TrySetException(new PlayConnectorException(serviceId,stageKey,errorCode,request,seq));
                    });
                }
            }));
         
            _Request(serviceId,request,stageKey,seq);
            return await deferred.Task;
        }
        
        public void OnConnected()
        {

            Thread.Sleep(TimeSpan.FromMilliseconds(300));

            if (_debugMode)
            {
                SendDebugMode();
            }

            UpdateTime(ref _lastReceivedTime);


            _asyncManager.AddJob(() =>
            {
                _connectChecker = true;

                if (_taskOnConnector == null)
                {
                    _connectorCallback.ConnectCallback(true);    
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
            UpdateTime(ref _lastReceivedTime);

            if(clientPacket.IsHeartBeat())
            {
                return;
            }

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
                        _connectorCallback.ReceiveCallback(targetId.ServiceId,  packet);
                    }
                    else
                    {
                        _connectorCallback.ReceiveExCallback(targetId.ServiceId, targetId.StageIndex, packet);
                    }
                }
                
            });
        }

        public void OnDisconnected()
        {
            _isAuthenticate = false;
            _asyncManager.AddJob(() =>
            {
                if (_connectChecker == false)
                {
                    _connectorCallback.ConnectCallback(false);
                    _taskOnConnector?.SetResult(false);
                }
                else
                {
                    _connectChecker = false;
                    _connectorCallback.DisconnectCallback();    
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


        internal bool IsDebugMode()
        {
            return _debugMode;

        }
    }
}
