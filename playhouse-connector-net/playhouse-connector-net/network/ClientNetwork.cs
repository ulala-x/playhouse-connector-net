
using CommonLib;
using PlayHouse.Utils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PlayHouseConnector.Network
{
    internal class ClientNetwork : IConnectorListener
    {
        private LOG<ClientNetwork> _log = new();
        private IClient? _client;
        private readonly RequestCache _requestCache;
        private readonly AsyncManager _asyncManager = new();
        private readonly IConnectorCallback _connectorCallback;
        private readonly ConnectorConfig _config;
        private readonly Stopwatch _stopwatch = new();
        private DateTime _lastReceivedTime = DateTime.Now;
        private DateTime _lastSendHeartBeatTime = DateTime.Now;
        private Timer _timer;
        private DisconnectType _disconnectType = DisconnectType.None;
        private int _retryCount;
        private ConnectorState _state = ConnectorState.Before_Connected;
        private ConcurrentQueue<Action> _msgQ = new();
        private ConcurrentQueue<Action> _priorityQ = new();
        //private TaskCompletionSource<bool>? _taskOnConnector = null;
        private bool _isReadyOnce = false;

        public ClientNetwork(ConnectorConfig config, IConnectorCallback connectorCallback)
        {
            _connectorCallback = connectorCallback;
            _config = config;
            _requestCache = new RequestCache(_config.RequestTimeoutMs);
            PooledBuffer.Init(1024 * 1024);
            
            _timer = new Timer(TimerCallback, this, 10, 10);
        }

        private void InitSocket()
        {
            if (_config.UseWebsocket)
            {
                _client = new WsClient(_config.Host, _config.Port, this);
            }
            else
            {
                _client = new TcpClient(_config.Host, _config.Port, this);
            }

            _msgQ.Clear();
            _priorityQ.Clear();
        }

        private bool IsIdleState()
        {
            if(_state != ConnectorState.Ready || _config.ConnectionIdleTimeoutMs == 0 || _config.DebugMode)
            {
                return false;
            }

            return GetElapedTime(_lastReceivedTime) > _config.ConnectionIdleTimeoutMs;
        }

        private static void TimerCallback(object? o)
        {
            ClientNetwork network = (ClientNetwork)o!;
            if (network._client!.IsClientConnected())
            {
                if(network._config.DebugMode == false)
                {
                    network._requestCache.CheckExpire();
                }

                network.SendHeartBeat();

                if (network.IsIdleState())
                {
                    network._log.Debug(() => $"Client disconnect cause idle time");
                    network.Disconnect(DisconnectType.System_Disconnect);
                }

                network.ExecutePriorityMessage();
                network.ExecuteMessage();
            }
        }

        private void ExecuteMessage()
        {
            if( _state == ConnectorState.Ready) 
            {
                while (_msgQ.TryPeek(out Action action))
                {   
                    if (IsConnect() == false)
                    {
                        return;
                    }
                    action();
                    _msgQ.TryDequeue(out action);
                }
            }
        }

        private void ExecutePriorityMessage()
        {
            if (_state == ConnectorState.Before_Authenticated || _state == ConnectorState.Ready)
            { 
                while( _priorityQ.TryPeek(out Action action))
                {
                    if (IsConnect() == false)
                    {
                        return;
                    }
                    action();
                    _priorityQ.TryDequeue(out action);
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
                PrioritySend(0, packet, 0);
                UpdateTime(ref _lastSendHeartBeatTime);
            }
        }

    
        private void SendDebugMode()
        {
            Packet packet = new Packet(-2);
            Send(0, packet, 0);
        }

        //internal async Task<bool> ConnectAsync()
        //{
        //    //중복 초기화 방지
        //    if (_client != null && !_client.IsClientConnected())
        //    {
        //        _disconnectType = DisconnectType.None;
        //        InitSocket();
        //        _client!.ClientConnect();
        //        _taskOnConnector = new();
        //        return await _taskOnConnector.Task;
        //    }
        //    else
        //    {
        //        await Task.CompletedTask;
        //        return true;
        //    }
        //}
        internal void Connect()
        {
            //중복 초기화 방지
            if(_client != null && !_client.IsClientConnected())
            {
                _disconnectType = DisconnectType.None;
                InitSocket();
                _client!.ClientConnect();
            }
        }

        internal void Disconnect(DisconnectType disconenctType)
        {
            _disconnectType = disconenctType;
            _client!.ClientDisconnect();
        }

        internal bool IsConnect()
        {
            return _client!.IsClientConnected();
        }

        internal bool IsReady()
        {
            return _state == ConnectorState.Ready;
        }



        private void _Send(ClientPacket packet)
        {
            using(packet)
            {
                _client!.Send(packet);
            }
        }

        private void _Request(ushort serviceId, IPacket packet, int stageKey)
        {
            if (_config.EnableLoggingResponseTime)
            {
                _stopwatch.Reset();
                _stopwatch.Start();
            }

            var clientPacket = ClientPacket.ToServerOf(new TargetId(serviceId, stageKey), packet);
            clientPacket.SetMsgSeq(packet.MsgSeq);
            _Send(clientPacket);
        }

        public void Send(ushort serviceId, IPacket packet, int stageKey)
        {
            _msgQ.Enqueue(() =>
            {
                if(IsReady())
                {
                    var clientPacket = ClientPacket.ToServerOf(new TargetId(serviceId, stageKey), packet);
                    _Send(clientPacket);
                }
                else
                {
                    NotReadyStatusPacketCallback(serviceId, stageKey, packet);
                }
            });
        }
        public async Task SendAsync(ushort serviceId, IPacket packet, int stageKey)
        {
            var deferred = new TaskCompletionSource<IPacket>();
            _msgQ.Enqueue(() =>
            {
                if (IsReady())
                {
                    var clientPacket = ClientPacket.ToServerOf(new TargetId(serviceId, stageKey), packet);
                    _Send(clientPacket);
                }
                else
                {
                    NotReadyStatusPacketException(serviceId, stageKey, packet,deferred);
                }
            });

            await deferred.Task;
        }

        private void PrioritySend(ushort serviceId, IPacket packet, int stageKey)
        {
            _priorityQ.Enqueue(() => 
            {
                var clientPacket = ClientPacket.ToServerOf(new TargetId(serviceId, stageKey), packet);
                _Send(clientPacket);
            });
        }

        private void CallAuthenticate()
        {

            if(_isReadyOnce)
            {
                _connectorCallback.ReconnectedCallback();
            }

            _state = ConnectorState.Ready;
            _connectorCallback.ReadyCallback();
            _isReadyOnce = true;
        }

        public void Authenticate(ushort serviceId, IPacket request, Action<IPacket> callback,int timeoutMs)
        {
            ushort seq = (ushort)_requestCache.GetSequence();
            request.MsgSeq = seq;               
            _priorityQ.Enqueue(() =>
            {
                RequestExec(serviceId, request, callback, 0, true, timeoutMs);
            });
            
        }

        private void NotReadyStatusPacketCallback(ushort serviceId, int stageKey,IPacket request)
        {
            if (_disconnectType == DisconnectType.System_Disconnect)
            {
                _asyncManager.AddJob(() =>
                {
                    if (_config.StageIds.Contains(serviceId))
                    {
                        _connectorCallback.ErrorStageCallback(serviceId, stageKey, (ushort)ConnectorErrorCode.DISCONNECTED, request);
                    }
                    else
                    {
                        _connectorCallback.ErrorApiCallback(serviceId, (ushort)ConnectorErrorCode.DISCONNECTED, request);
                    }
                });
            }
            else if (_disconnectType == DisconnectType.AuthenticateFail_Disconnect)
            {
                _asyncManager.AddJob(() =>
                {
                    if (_config.StageIds.Contains(serviceId))
                    {
                        _connectorCallback.ErrorStageCallback(serviceId, stageKey, (ushort)ConnectorErrorCode.UNAUTHENTICATED, request);
                    }
                    else
                    {
                        _connectorCallback.ErrorApiCallback(serviceId, (ushort)ConnectorErrorCode.UNAUTHENTICATED, request);
                    }
                });
            }
        }
        public void Request(ushort serviceId, IPacket request, Action<IPacket> callback,int stageKey,int timeoutMs)
        {
            ushort seq = (ushort)_requestCache.GetSequence();            
            request.MsgSeq = seq;

            _msgQ.Enqueue(() =>
            {
                if(IsReady())
                {
                    RequestExec(serviceId, request, callback, stageKey, false, timeoutMs);
                }
                else
                {
                    NotReadyStatusPacketCallback(serviceId, stageKey, request);
                }
                    
            });
    }

        private void RequestExec(ushort serviceId, IPacket request, Action<IPacket> callback, int stageKey, bool forAthenticate,int timetoutMs)
        {
            var seq = request.MsgSeq;
            _requestCache.Put(seq, new ReplyObject(seq, timetoutMs ,(errorCode, reply) =>
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
                        if (forAthenticate)
                        {
                            CallAuthenticate();
                        }

                        callback.Invoke(reply);
                    }
                    else
                    {
                        
                        if (_config.StageIds.Contains(serviceId))
                        {
                            _connectorCallback.ErrorStageCallback(serviceId, stageKey, errorCode, request);
                        }
                        else
                        {
                            _connectorCallback.ErrorApiCallback(serviceId, errorCode, request);
                        }

                        if (forAthenticate)
                        {
                            Disconnect(DisconnectType.AuthenticateFail_Disconnect);
                        }
                    }
                });


            }));
            _Request(serviceId, request, stageKey);
        }



        public async Task<IPacket> AuthenticateAsync(ushort serviceId, IPacket request,int timeoutMs)
        {
            ushort seq = (ushort)_requestCache.GetSequence();
            var deferred = new TaskCompletionSource<IPacket>();
            request.MsgSeq = seq;

            _priorityQ.Enqueue(() =>
            {
                RequestExecAsync(serviceId, request, 0, true, deferred, timeoutMs);
            });
            return await deferred.Task;
        }
        private void NotReadyStatusPacketException(ushort serviceId, int stageKey, IPacket request, TaskCompletionSource<IPacket> deferred)
        {
            var seq = request.MsgSeq;
            if (_disconnectType == DisconnectType.System_Disconnect)
            {
                _asyncManager.AddJob(() =>
                {
                    deferred.SetException(new PlayConnectorException.PacketError(serviceId, stageKey, (ushort)ConnectorErrorCode.DISCONNECTED, request, seq));
                });
            }
            else if (_disconnectType == DisconnectType.AuthenticateFail_Disconnect)
            {
                _asyncManager.AddJob(() =>
                {
                    deferred.SetException(new PlayConnectorException.PacketError(serviceId, stageKey, (ushort)ConnectorErrorCode.UNAUTHENTICATED, request, seq));
                });
            }
        }
        public async Task<IPacket> RequestAsync(ushort serviceId, IPacket request,int stageKey, int timeoutMs)
        {
            ushort seq = (ushort)_requestCache.GetSequence();
            var deferred = new TaskCompletionSource<IPacket>();

            _msgQ.Enqueue(() =>
            {
                
                if(IsReady())
                {
                    RequestExecAsync(serviceId, request, stageKey, false, deferred, timeoutMs);
                }
                else
                {
                    NotReadyStatusPacketException(serviceId, stageKey, request, deferred);
                }

            });
            
            return await deferred.Task;
        }

        private void RequestExecAsync(ushort serviceId, IPacket request, int stageKey, bool forAthenticate, TaskCompletionSource<IPacket> deferred,int timeoutMs)
        {
            var seq = request.MsgSeq;
            _requestCache.Put(seq, new ReplyObject(seq, timeoutMs ,(errorCode, reply) =>
            {
                _asyncManager.AddJob(() =>
                {
                    if (errorCode == 0)
                    {
                        if (forAthenticate)
                        {
                            CallAuthenticate();
                        }

                        if (_config.EnableLoggingResponseTime)
                        {
                            _stopwatch.Stop();
                            _log.Debug(() => $"response time - [msgId:{request.MsgId},msgSeq:{seq},elapsedTime:{_stopwatch.ElapsedMilliseconds}]");
                        }

                        _asyncManager.AddJob(() =>
                        {
                            deferred.SetResult(reply);
                        });
                    }
                    else
                    {
                        deferred.SetException(new PlayConnectorException.PacketError(serviceId, stageKey, errorCode, request, seq));

                        if (forAthenticate)
                        {
                            Disconnect(DisconnectType.AuthenticateFail_Disconnect);
                        }
                    }
                });
                
            }));

            _Request(serviceId, request, stageKey);
        }

        public void OnConnected()
        {
            _retryCount = 0;

            Thread.Sleep(TimeSpan.FromMilliseconds(300));

            _state = ConnectorState.Before_Authenticated;

            if (_config.DebugMode)
            {
                SendDebugMode();
            }

            UpdateTime(ref _lastReceivedTime);


            _asyncManager.AddJob(() =>
            {
                _connectorCallback.ConnectCallback();
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
                    if (_config.StageIds.Contains(clientPacket.ServiceId))
                    {
                        _connectorCallback.ReceiveStageCallback(targetId.ServiceId, targetId.StageIndex, packet);
                    }
                    else
                    {
                        _connectorCallback.ReceiveApiCallback(targetId.ServiceId, packet);
                    }
                }
            });
        }

        public void OnDisconnected()
        {

            _state = ConnectorState.Disconnected;

            if(_disconnectType == DisconnectType.None)
            {
                _disconnectType = DisconnectType.System_Disconnect;
            }

            if(_disconnectType == DisconnectType.AuthenticateFail_Disconnect)
            {
                DisconnectProcess();
            }
            else if(_disconnectType == DisconnectType.System_Disconnect)
            {
                if (_retryCount < _config.RetryCount)
                {
                    Connect();
                    ++_retryCount;
                }
                else
                {
                    DisconnectProcess();
                }
            }
            else if(_disconnectType == DisconnectType.Self_Disconnect)
            {
                _priorityQ.Clear();
                _msgQ.Clear();
                _asyncManager.Clear();
            }

            _disconnectType = DisconnectType.System_Disconnect;
        }

        private void DisconnectProcess()
        {
            _priorityQ.Clear();

            while (_msgQ.TryDequeue(out Action action))
            {
                action();
            }

            _asyncManager.AddJob(() =>
            {
                _connectorCallback.DisconnectCallback();
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
            return _config.DebugMode;

        }

     
    }
}
