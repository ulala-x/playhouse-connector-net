using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommonLib;
using PlayHouse.Utils;

namespace PlayHouseConnector.Network
{
    internal class ClientNetwork : IConnectorListener
    {
        private readonly AsyncManager _asyncManager = new();
        private readonly IClient _client;
        private readonly ConnectorConfig _config;
        private readonly IConnectorCallback _connectorCallback;
        private readonly LOG<ClientNetwork> _log = new();
        private readonly RequestCache _requestCache;
        private readonly Stopwatch _stopwatch = new();
        private bool _connectChecker;
        private bool _debugMode;
        private bool _isAuthenticate;
        private readonly Stopwatch _lastReceivedTime = new();
        private readonly Stopwatch _lastSendHeartBeatTime = new();
        private TaskCompletionSource<bool>? _taskOnConnector;
        private readonly ConcurrentQueue<ClientPacket> _sendQueue =new();
        private readonly AtomicBoolean _isSending = new(false);
        private readonly object _lockObject = new(); // 잠금 객체

        //private Timer _timer;


        public ClientNetwork(ConnectorConfig config, IConnectorCallback connectorCallback)
        {
            _connectorCallback = connectorCallback;
            _config = config;
            _requestCache = new RequestCache(_config.RequestTimeoutMs);
            PooledBuffer.Init(1024 * 1024);

            if (_config.UseWebsocket)
            {
                _client = new WsClient(_config.Host, _config.Port, this,_config.TurnOnTrace);
            }
            else
            {
                _client = new TcpClient(_config.Host, _config.Port, this, _config.TurnOnTrace);
            }

            //_timer = new Timer(TimerCallback, this, 100, 100);
            _lastSendHeartBeatTime.Start();
        }

        public void OnConnected()
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(300));

            if (_debugMode)
            {
                SendDebugMode();
            }

            //UpdateTime(ref _lastReceivedTime);
            _lastReceivedTime.Restart();
            _lastSendHeartBeatTime.Restart();

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
             _lastReceivedTime.Restart();

            if (clientPacket.IsHeartBeat())
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
                    var serviceId = clientPacket.ServiceId;
                    var stageId = clientPacket.StageId;
                    var packet = clientPacket.ToPacket();
                    if (stageId > 0)
                    {
                        _connectorCallback.ReceiveStageCallback(serviceId, stageId, packet);
                    }
                    else
                    {
                        _connectorCallback.ReceiveCallback(serviceId, packet);
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
            _requestCache.Clear();
        }

        private bool IsIdleState()
        {
            if (_isAuthenticate == false || _config.ConnectionIdleTimeoutMs == 0)
            {
                return false;
            }

            return _lastReceivedTime.ElapsedMilliseconds > _config.ConnectionIdleTimeoutMs;
        }

        private void UpdateClientConnection()
        {
            //var network = (ClientNetwork)o!;
            if (_client.IsClientConnected())
            {
                if (_debugMode == false)
                {
                    _requestCache.CheckExpire();
                    SendHeartBeat();

                    if (IsIdleState())
                    {
                        _log.Debug(() => $"Client disconnect cause idle time");
                        Disconnect();
                    }
                }
            }
        }

        //private void UpdateTime(ref DateTime time)
        //{
        //    time = DateTime.Now;
        //}

        //private long GetElapsedTime(DateTime lastTime)
        //{
        //    var timeDifference = DateTime.UtcNow - lastTime;
        //    return (long)timeDifference.TotalMilliseconds;
        //}

        private void SendHeartBeat()
        {
            if (_config.HeartBeatIntervalMs == 0)
            {
                return;
            }

            if (_lastSendHeartBeatTime.ElapsedMilliseconds > _config.HeartBeatIntervalMs)
            {
                var packet = new Packet(PacketConst.HeartBeat);
                Send(0, packet, 0);
                _lastSendHeartBeatTime.Restart();
            }
        }

        private void SendDebugMode()
        {
            var packet = new Packet(PacketConst.Debug);
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
            _taskOnConnector = new TaskCompletionSource<bool>();
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

            _sendQueue.Enqueue(packet);
            if (_isSending.CompareAndSet(false, true))
            {
                while (_sendQueue.TryDequeue(out var sendPacket))
                {
                    _client.Send(sendPacket);
                }
                _isSending.Set(false);
                
            }
        }

        private void _Request(ushort serviceId, IPacket packet, long stageId, ushort seq)
        {
            if (_config.EnableLoggingResponseTime)
            {
                _stopwatch.Reset();
                _stopwatch.Start();
            }

            var clientPacket = ClientPacket.ToServerOf(new TargetId(serviceId, stageId), packet);
            clientPacket.SetMsgSeq(seq);
            _Send(clientPacket);
        }

        public void Send(ushort serviceId, IPacket packet, long stageId)
        {
            var clientPacket = ClientPacket.ToServerOf(new TargetId(serviceId, stageId), packet);
            _Send(clientPacket);
        }

        public void Request(ushort serviceId, IPacket request, Action<IPacket> callback, long stageId,
            bool forSystem = false)
        {
            var seq = (ushort)_requestCache.GetSequence();


            _requestCache.Put(seq, new ReplyObject(seq, (errorCode, reply) =>
            {
                if (_config.EnableLoggingResponseTime)
                {
                    _stopwatch.Stop();
                    _log.Debug(() =>
                        $"response time - [msgId:{request.MsgId},msgSeq:{seq},elapsedTime:{_stopwatch.ElapsedMilliseconds}]");
                }


                _asyncManager.AddJob(() =>
                {
                    if (errorCode == 0)
                    {
                        if (forSystem)
                        {
                            _isAuthenticate = true;
                        }

                        //if (stageId > 0)
                        //{
                        //    _connectorCallback.CommonReplyExCallback(serviceId, stageId, request, reply);
                        //}
                        //else
                        //{
                        //    _connectorCallback.CommonReplyCallback(serviceId, request, reply);

                        //}

                        callback.Invoke(reply);
                    }
                    else
                    {
                        if (stageId > 0)
                        {
                            _connectorCallback.ErrorStageCallback(serviceId, stageId, errorCode, request);
                        }
                        else
                        {
                            _connectorCallback.ErrorCallback(serviceId, errorCode, request);
                        }
                    }
                });
            }));

            _Request(serviceId, request, stageId, seq);
        }

        public async Task<IPacket> RequestAsync(ushort serviceId, IPacket request, long stageId,
            bool forAuthenticate = false)
        {
            var deferred = new TaskCompletionSource<IPacket>();
            lock (_lockObject)
            {
                var seq = (ushort)_requestCache.GetSequence();

                _requestCache.Put(seq, new ReplyObject(seq, (errorCode, reply) =>
                {
                    if (errorCode == 0)
                    {
                        if (forAuthenticate)
                        {
                            _isAuthenticate = true;
                        }

                        if (_config.EnableLoggingResponseTime)
                        {
                            _stopwatch.Stop();
                            _log.Debug(() =>
                                $"response time - [msgId:{request.MsgId},msgSeq:{seq},elapsedTime:{_stopwatch.ElapsedMilliseconds}]");
                        }

                        _asyncManager.AddJob(() => { deferred.SetResult(reply); });
                    }
                    else
                    {
                        _asyncManager.AddJob(() =>
                        {
                            deferred.SetException(new PlayConnectorException(serviceId, stageId, errorCode, request,
                                seq));
                        });
                    }
                }));

                _Request(serviceId, request, stageId, seq);
            }
            return await deferred.Task;
        }

        public void MainThreadAction()
        {
            UpdateClientConnection();
            _asyncManager.MainThreadAction();
        }

        public IEnumerator MainCoroutineAction()
        {
            UpdateClientConnection();

            return _asyncManager.MainCoroutineAction();
        }


        internal bool IsDebugMode()
        {
            return _debugMode;
        }
    }
}