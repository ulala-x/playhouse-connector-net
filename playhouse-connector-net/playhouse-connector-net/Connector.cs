using System;
using System.Collections;
using System.Threading.Tasks;
using PlayHouse.Utils;
using PlayHouseConnector.Network;

namespace PlayHouseConnector
{
    public class Connector : IConnectorCallback
    {
        private ClientNetwork? _clientNetwork;
        private readonly LOG<Connector> _log = new();

        public ConnectorConfig ConnectorConfig { get; private set; } = new();

        public void ConnectCallback(bool result)
        {
            OnConnect?.Invoke(result);
        }

        public void ReceiveCallback(ushort serviceId, IPacket packet)
        {
            if (OnReceive != null)
            {
                OnReceive.Invoke(serviceId, packet);
            }
            else
            {
                _log.Error(() => "OnReceive is not initialized");
            }
        }

        public void ReceiveStageCallback(ushort serviceId, long stageId, IPacket packet)
        {
            if (OnReceiveStage != null)
            {
                OnReceiveStage.Invoke(serviceId, stageId, packet);
            }
            else
            {
                _log.Error(() => "CallReceiveEx is not initialized");
            }
        }

        //public void CommonReplyCallback(ushort serviceId, IPacket request, IPacket reply)
        //{
        //    if (OnCommonReply != null)
        //    {
        //        OnCommonReply.Invoke(serviceId, request, reply);
        //    }
        //}

        //public void CommonReplyExCallback(ushort serviceId,long stageId, IPacket request, IPacket reply)
        //{

        //    if (OnCommonReplyEx != null)
        //    {
        //        OnCommonReplyEx.Invoke(serviceId, stageId, request, reply);
        //    }
        //}

        public void ErrorCallback(ushort serviceId, ushort errorCode, IPacket request)
        {
            if (OnError != null)
            {
                OnError.Invoke(serviceId, errorCode, request);
            }
            else
            {
                _log.Error(() => "OnError is not initialized");
            }
        }

        public void ErrorStageCallback(ushort serviceId, long stageId, ushort errorCode, IPacket request)
        {
            if (OnErrorStage != null)
            {
                OnErrorStage.Invoke(serviceId, stageId, errorCode, request);
            }
            else
            {
                _log.Error(() => "OnErrorStage is not initialized");
            }
        }

        public void DisconnectCallback()
        {
            if (OnDisconnect != null)
            {
                OnDisconnect.Invoke();
            }
            else
            {
                _log.Error(() => "OnDisconnect is not initialized");
            }
        }

        public event Action<bool>? OnConnect; //result
        public event Action<ushort, IPacket>? OnReceive; //(serviceId, packet) 

        public event Action<ushort, long, IPacket>? OnReceiveStage; //(serviceId, stageId, packet)

        //public event Action<ushort, IPacket, IPacket>? OnCommonReply; //(serviceId, request, reply)
        //public event Action<ushort, long, IPacket, IPacket>? OnCommonReplyEx;// (serviceId, stageId, request, reply)
        public event Action<ushort, ushort, IPacket>? OnError; // (serviceId, errorCode, request)
        public event Action<ushort, long, ushort, IPacket>? OnErrorStage; //(serviceId,stageId,errorCode,request)
        public event Action? OnDisconnect; //


        public void MainThreadAction()
        {
            _clientNetwork!.MainThreadAction();
        }

        public IEnumerator MainCoroutineAction()
        {
            return _clientNetwork!.MainCoroutineAction();
        }

        public void Init(ConnectorConfig config)
        {
            ConnectorConfig = config;
            _clientNetwork = new ClientNetwork(config, this);
        }

        public void Connect(bool debugMode = false)
        {
            _clientNetwork!.Connect(debugMode);
        }

        public async Task<bool> ConnectAsync(bool debugMode = false)
        {
            return await _clientNetwork!.ConnectAsync(debugMode);
        }

        public bool IsDebugMode()
        {
            return _clientNetwork!.IsDebugMode();
        }

        public void Disconnect()
        {
            _clientNetwork!.DisconnectAsync();
        }

        public bool IsConnect()
        {
            return _clientNetwork!.IsConnect();
        }

        public void Send(ushort serviceId, IPacket packet)
        {
            if (IsConnect() == false)
            {
                if (OnError != null)
                {
                    OnError(serviceId, (ushort)ConnectorErrorCode.DISCONNECTED, packet);
                }
                else
                {
                    throw new PlayConnectorException(serviceId, 0, (ushort)ConnectorErrorCode.DISCONNECTED, packet, 0);
                }

                return;
            }

            if (_clientNetwork!.IsAuthenticated() == false)
            {
                if (OnError != null)
                {
                    OnError(serviceId, (ushort)ConnectorErrorCode.UNAUTHENTICATED, packet);
                }
                else
                {
                    throw new PlayConnectorException(serviceId, 0, (ushort)ConnectorErrorCode.UNAUTHENTICATED, packet,
                        0);
                }

                return;
            }

            _clientNetwork!.Send(serviceId, packet, 0);
        }

        public void Send(ushort serviceId, long stageId, IPacket packet)
        {
            if (IsConnect() == false)
            {
                if (OnErrorStage != null)
                {
                    OnErrorStage(serviceId, stageId, (ushort)ConnectorErrorCode.DISCONNECTED, packet);
                }
                else
                {
                    throw new PlayConnectorException(serviceId, stageId, (ushort)ConnectorErrorCode.DISCONNECTED,
                        packet, 0);
                }

                return;
            }

            if (_clientNetwork!.IsAuthenticated() == false)
            {
                if (OnErrorStage != null)
                {
                    OnErrorStage(serviceId, stageId, (ushort)ConnectorErrorCode.UNAUTHENTICATED, packet);
                }
                else
                {
                    throw new PlayConnectorException(serviceId, stageId, (ushort)ConnectorErrorCode.UNAUTHENTICATED,
                        packet, 0);
                }

                return;
            }

            _clientNetwork!.Send(serviceId, packet, stageId);
        }

        public void Authenticate(ushort serviceId, IPacket request, Action<IPacket> callback)
        {
            _clientNetwork!.Request(serviceId, request, callback, 0, true);
        }

        public void Request(ushort serviceId, IPacket request, Action<IPacket> callback)
        {
            if (IsConnect() == false)
            {
                ErrorCallback(serviceId, (ushort)ConnectorErrorCode.DISCONNECTED, request);
                return;
            }

            if (_clientNetwork!.IsAuthenticated() == false)
            {
                ErrorCallback(serviceId, (ushort)ConnectorErrorCode.UNAUTHENTICATED, request);
                return;
            }

            _clientNetwork!.Request(serviceId, request, callback, 0);
        }

        public void Request(ushort serviceId, long stageId, IPacket request, Action<IPacket> callback)
        {
            if (IsConnect() == false)
            {
                ErrorStageCallback(serviceId, stageId, (ushort)ConnectorErrorCode.DISCONNECTED, request);
                return;
            }

            if (_clientNetwork!.IsAuthenticated() == false)
            {
                ErrorStageCallback(serviceId, stageId, (ushort)ConnectorErrorCode.UNAUTHENTICATED, request);
                return;
            }

            _clientNetwork!.Request(serviceId, request, callback, stageId);
        }

        public async Task<IPacket> AuthenticateAsync(ushort serviceId, IPacket request)
        {
            if (IsConnect() == false)
            {
                throw new PlayConnectorException(serviceId, 0, (ushort)ConnectorErrorCode.DISCONNECTED, request, 0);
            }

            return await _clientNetwork!.RequestAsync(serviceId, request, 0, true);
        }

        public async Task<IPacket> RequestAsync(ushort serviceId, IPacket request)
        {
            if (IsConnect() == false)
            {
                throw new PlayConnectorException(serviceId, 0, (ushort)ConnectorErrorCode.DISCONNECTED, request, 0);
            }

            if (_clientNetwork!.IsAuthenticated() == false)
            {
                throw new PlayConnectorException(serviceId, 0, (ushort)ConnectorErrorCode.UNAUTHENTICATED, request, 0);
            }

            return await _clientNetwork!.RequestAsync(serviceId, request, 0);
        }

        public async Task<IPacket> RequestAsync(ushort serviceId, long stageId, IPacket request)
        {
            if (IsConnect() == false)
            {
                throw new PlayConnectorException(serviceId, stageId, (ushort)ConnectorErrorCode.DISCONNECTED, request,
                    0);
            }

            if (_clientNetwork!.IsAuthenticated() == false)
            {
                throw new PlayConnectorException(serviceId, stageId, (ushort)ConnectorErrorCode.UNAUTHENTICATED,
                    request, 0);
            }


            return await _clientNetwork!.RequestAsync(serviceId, request, stageId);
        }

        public bool IsAuthenticated()
        {
            return _clientNetwork!.IsAuthenticated();
        }
    }
}