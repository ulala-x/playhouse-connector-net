using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
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
        private bool _connectChecker;
        private bool _debugMode;
        private bool _isAuthenticate;
        private readonly Stopwatch _lastReceivedTime = new();
        private readonly Stopwatch _lastSendHeartBeatTime = new();
        private TaskCompletionSource<bool>? _taskOnConnector;
        private readonly ConcurrentQueue<ClientPacket> _sendQueue =new();
        private readonly object _lockObject = new(); // 잠금 객체


        public ClientNetwork(ConnectorConfig config, IConnectorCallback connectorCallback)
        {
            _connectorCallback = connectorCallback;
            _config = config;
            _requestCache = new RequestCache(_config.RequestTimeoutMs,_config.EnableLoggingResponseTime);
            PooledBuffer.Init(1024 * 1024);

            if (_config.UseWebsocket)
            {
                _client = new WsClient(_config.Host, _config.Port, this,_config.TurnOnTrace);
            }
            else
            {
                _client = new TcpClient(_config.Host, _config.Port, this, _config.TurnOnTrace);
            }

            _lastSendHeartBeatTime.Start();
        }

        //private void SendProcess()
        //{
            
        //    while(IsConnect())
        //    {
        //        int count = 0;
        //        while (_sendQueue.TryDequeue(out var sendPacket))
        //        {
        //            _client.Send(sendPacket);
        //            count++;
        //            if (count > 10) //flow control max 100 / sec
        //            {
        //                Thread.Sleep(100);
        //                count = 0;
        //            }
        //        }
        //        Thread.Sleep(10);
        //    }
        //}

        private void SendInQueue()
        {
            try
            {
                if (IsConnect())
                {
                    while (_sendQueue.TryDequeue(out var sendPacket))
                    {
                        _client.Send(sendPacket);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(() => $"{ex.StackTrace }");
            }

        }

        public void OnConnected()
        {
            //Thread.Sleep(TimeSpan.FromMilliseconds(300));
            //var sendThread = new Thread(SendProcess);
            //sendThread.Start();

            if (_debugMode)
            {
                SendDebugMode();
            }

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

            if (clientPacket.MsgSeq > 0)
            {
                _requestCache.TouchReceive(clientPacket.MsgSeq);
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
        }

        private void _Request(ushort serviceId, IPacket packet, long stageId, ushort seq)
        {
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
                _asyncManager.AddJob(() =>
                {
                    if (errorCode == 0)
                    {
                        if (forSystem)
                        {
                            _isAuthenticate = true;
                        }

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
            var seq = (ushort)_requestCache.GetSequence();
            return await RequestAsync(serviceId, seq, request, stageId, forAuthenticate);
        }

        public async Task<IPacket> RequestAsync(ushort serviceId,ushort seq, IPacket request, long stageId,
            bool forAuthenticate = false)
        {
            var deferred = new TaskCompletionSource<IPacket>();
            lock (_lockObject)
            {
                //var seq = (ushort)_requestCache.GetSequence();

                _requestCache.Put(seq, new ReplyObject(seq, (errorCode, reply) =>
                {
                    if (errorCode == 0)
                    {
                        if (forAuthenticate)
                        {
                            _isAuthenticate = true;
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
            SendInQueue();
            _asyncManager.MainThreadAction();
        }

        public IEnumerator MainCoroutineAction()
        {
            UpdateClientConnection();
            SendInQueue();
            return _asyncManager.MainCoroutineAction();
        }


        internal bool IsDebugMode()
        {
            return _debugMode;
        }
    }
}