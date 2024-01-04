using PlayHouseConnector.Network;
using System;
using System.Collections;
using System.Threading.Tasks;
using PlayHouse.Utils;

namespace PlayHouseConnector
{
    public class Connector : IConnectorCallback
    {
        public event Action<bool>? OnConnect; //result
        public event Action<ushort, IPacket>? OnReceive ; //(serviceId, packet) 
        public event Action<ushort, int, IPacket>? OnReceiveEx; //(serviceId, stageKey, packet)
        public event Action<ushort, IPacket, IPacket>? OnCommonReply; //(serviceId, request, reply)
        public event Action<ushort, int, IPacket, IPacket>? OnCommonReplyEx;// (serviceId, stageKey, request, reply)
        public event Action<ushort, ushort, IPacket>? OnError; // (serviceId, errorCode, request)
        public event Action<ushort, int, ushort, IPacket>? OnErrorEx; //(serviceId,stageKey,errorCode,request)
        public event Action? OnDisconnect;//

        public ConnectorConfig ConnectorConfig { get; private set; } = new();
        private LOG<Connector> _log = new();
        private ClientNetwork? _clientNetwork = null;
        
       
        public void MainThreadAction()
        {
            _clientNetwork!.MainThreadAction();
        }
        public IEnumerator MainCoroutineAction()
        {
            return _clientNetwork!.MainCoroutineAction();
        }
        
        public Connector()
        {
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
       
        public void Send(ushort serviceId,IPacket packet)
        {
            if (IsConnect() == false)
            {
                if (OnError!=null)
                {
                    OnError(serviceId,(ushort) ConnectorErrorCode.DISCONNECTED, packet);
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
                    OnError(serviceId,(ushort) ConnectorErrorCode.UNAUTHENTICATED, packet);
                }
                else
                {
                    throw new PlayConnectorException(serviceId, 0, (ushort)ConnectorErrorCode.UNAUTHENTICATED, packet, 0);
                }
                return;
            }

            _clientNetwork!.Send(serviceId, packet, 0);
        }
        public void SendEx(ushort serviceId,int stageKey,IPacket packet)
        {
            if (IsConnect() == false)
            {
                if (OnErrorEx != null)
                {
                    OnErrorEx(serviceId,stageKey, (ushort)ConnectorErrorCode.DISCONNECTED, packet);
                }
                else
                {
                    throw new PlayConnectorException(serviceId, stageKey, (ushort)ConnectorErrorCode.DISCONNECTED, packet, 0);
                }

                return;
            }

            if (_clientNetwork!.IsAuthenticated() == false)
            {
                if (OnErrorEx != null)
                {
                    OnErrorEx(serviceId,stageKey, (ushort)ConnectorErrorCode.UNAUTHENTICATED, packet);
                }
                else
                {
                    throw new PlayConnectorException(serviceId, stageKey, (ushort)ConnectorErrorCode.UNAUTHENTICATED, packet, 0);
                }
                return;
            }

            _clientNetwork!.Send(serviceId, packet, stageKey);
        }

        public void Authenticate(ushort serviceId, IPacket request, Action<IPacket> callback)
        {
            _clientNetwork!.Request(serviceId, request, callback, 0,true);
        }
        public void Request(ushort serviceId,  IPacket request, Action<IPacket> callback)
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

            _clientNetwork!.Request(serviceId,request,callback,0);
        }
        public void RequestEx(ushort serviceId, IPacket request, Action<IPacket> callback,int stageKey)
        {
            if (IsConnect() == false)
            {
                ErrorExCallback(serviceId,stageKey,(ushort)ConnectorErrorCode.DISCONNECTED, request);
                return;
            }

            if (_clientNetwork!.IsAuthenticated() == false)
            {
                ErrorExCallback(serviceId, stageKey,(ushort)ConnectorErrorCode.UNAUTHENTICATED, request);
                return;
            }

            _clientNetwork!.Request(serviceId,request,callback,stageKey);
        }

        public async Task<IPacket> AuthenticateAsync(ushort serviceId, IPacket request)
        {
            if (IsConnect() == false)
            {
                throw new PlayConnectorException(serviceId, 0, (ushort)ConnectorErrorCode.DISCONNECTED, request, 0);
            }

            return await _clientNetwork!.RequestAsync(serviceId, request, 0,true);
        }
        public async Task<IPacket> RequestAsync(ushort serviceId, IPacket request)
        {
            if (IsConnect() == false)
            {
                throw new PlayConnectorException(serviceId, 0, (ushort)ConnectorErrorCode.DISCONNECTED, request, 0);
            }

            if(_clientNetwork!.IsAuthenticated() == false)
            {
                throw new PlayConnectorException(serviceId, 0, (ushort)ConnectorErrorCode.UNAUTHENTICATED, request, 0);
            }

            return await _clientNetwork!.RequestAsync(serviceId, request, 0);
        }
        public async Task<IPacket> RequestExAsync(ushort serviceId, IPacket request,int stageKey)
        {
            if (IsConnect() == false)
            {
                throw new PlayConnectorException(serviceId, stageKey, (ushort)ConnectorErrorCode.DISCONNECTED, request, 0);
            }
            if (_clientNetwork!.IsAuthenticated() == false)
            {
                throw new PlayConnectorException(serviceId, stageKey, (ushort)ConnectorErrorCode.UNAUTHENTICATED, request, 0);
            }


            return await _clientNetwork!.RequestAsync(serviceId, request, stageKey);
        }

        public bool IsAuthenticated()
        {
            return _clientNetwork!.IsAuthenticated();
        }
        public void ConnectCallback(bool result)
        {
            OnConnect?.Invoke(result);
        }

        public void ReceiveCallback(ushort serviceId, IPacket packet)
        {
            if(OnReceive != null)
            {
                OnReceive.Invoke(serviceId, packet);
            }
            else
            {
                _log.Error(()=>"OnReceive is not initialized");
            }

        }

        public void ReceiveExCallback(ushort serviceId, int stageKey, IPacket packet)
        {
            if (OnReceiveEx != null)
            {
                OnReceiveEx.Invoke(serviceId, stageKey, packet);
            }
            else
            {
                _log.Error(() => "CallReceiveEx is not initialized");
            }
        }

        public void CommonReplyCallback(ushort serviceId, IPacket request, IPacket reply)
        {
            if (OnCommonReply != null)
            {
                OnCommonReply.Invoke(serviceId, request, reply);
            }
        }

        public void CommonReplyExCallback(ushort serviceId,int stageKey, IPacket request, IPacket reply)
        {

            if (OnCommonReplyEx != null)
            {
                OnCommonReplyEx.Invoke(serviceId, stageKey, request, reply);
            }
        }

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
        public void ErrorExCallback(ushort serviceId,int stageKey, ushort errorCode, IPacket request)
        {
            if (OnErrorEx != null)
            {
                OnErrorEx.Invoke(serviceId, stageKey, errorCode, request);
            }
            else
            {
                _log.Error(() => "OnErrorEx is not initialized");
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

    }
}