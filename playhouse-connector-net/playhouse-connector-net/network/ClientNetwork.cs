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
        private DateTime _lastReceivedTime = DateTime.Now;
        private DateTime _lastSendHeartBeatTime = DateTime.Now;
        private TaskCompletionSource<bool>? _taskOnConnector;

        private Timer _timer;


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
                _client = new TcpClient(_config.Host, _config.Port, this);
            }

            _timer = new Timer(TimerCallback, this, 100, 100);
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
        }

        private bool IsIdleState()
        {
            if (_isAuthenticate == false || _config.ConnectionIdleTimeoutMs == 0 || _debugMode)
            {
                return false;
            }

            return GetElapedTime(_lastReceivedTime) > _config.ConnectionIdleTimeoutMs;
        }

        private static void TimerCallback(object? o)
        {
            var network = (ClientNetwork)o!;
            if (network._client.IsClientConnected())
            {
                if (network._debugMode == false)
                {
                    network._requestCache.CheckExpire();
                }

                network.SendHeartBeat();

                if (network.IsIdleState())
                {
                    network._log.Debug(() => "Client disconnect cause idle time");
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
            if (_config.HeartBeatIntervalMs == 0)
            {
                return;
            }

            if (GetElapedTime(_lastSendHeartBeatTime) > _config.HeartBeatIntervalMs)
            {
                var packet = new Packet(-1);
                Send(0, packet, 0);
                UpdateTime(ref _lastSendHeartBeatTime);
            }
        }

        private void SendDebugMode()
        {
            var packet = new Packet(-2);
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
            using (packet)
            {
                _client.Send(packet);
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
            bool forAthenticate = false)
        {
            var seq = (ushort)_requestCache.GetSequence();

            var deferred = new TaskCompletionSource<IPacket>();

            _requestCache.Put(seq, new ReplyObject(seq, (errorCode, reply) =>
            {
                if (errorCode == 0)
                {
                    if (forAthenticate)
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
            return await deferred.Task;
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