using PlayHouseConnector.Network;
using System;
using System.Collections;
using System.Threading.Tasks;
using PlayHouse.Utils;

namespace PlayHouseConnector
{
    public class Connector : IConnectorCallback
    {
        public event Action? OnAuthenticate;
        public event Action? OnReady; //result
        public event Action? OnReconnected;
        public event Action<ushort, IPacket>? OnApiReceive ; //(serviceId, packet) 
        public event Action<ushort, ushort, IPacket>? OnApiError; // (serviceId, errorCode, request)
        public event Action<ushort, int, IPacket>? OnStageReceive; //(serviceId, stageKey, packet)
        public event Action<ushort, int, ushort, IPacket>? OnStageError; //(serviceId,stageKey,errorCode,request)
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
        
        public void Connect()
        {
            _clientNetwork!.Connect();
        }
        //public async Task ConnectAsync()
        //{
        //    await _clientNetwork!.ConnectAsync();
        //}

        public bool IsDebugMode()
        {
            return _clientNetwork!.IsDebugMode();
        }
   
        public void Disconnect() 
        {
            _clientNetwork!.Disconnect(DisconnectType.Self_Disconnect);
        }
      
        public bool IsConnect() 
        {
            return _clientNetwork!.IsConnect();
        }

        public async Task SendToApiAsync(ushort serviceId, IPacket packet)
        {
            await _clientNetwork!.SendAsync(serviceId, packet, 0);
        }
        public async Task SendToStageAsync(ushort serviceId, int stageKey, IPacket packet)
        {

            await _clientNetwork!.SendAsync(serviceId, packet, stageKey);
        }

        public void SendToApi(ushort serviceId,IPacket packet)
        {
            _clientNetwork!.Send(serviceId, packet, 0);
        }
        public void SendToStage(ushort serviceId,int stageKey,IPacket packet)
        {
            
            _clientNetwork!.Send(serviceId, packet, stageKey);
        }

        public void Authenticate(ushort serviceId, IPacket request, Action<IPacket> callback, int timeoutMs = 0)
        {
            if(timeoutMs == 0)
            {
                timeoutMs = ConnectorConfig.RequestTimeoutMs;
            }
            _clientNetwork!.Authenticate(serviceId, request, callback, timeoutMs);
        }
        public void RequestToApi(ushort serviceId,  IPacket request, Action<IPacket> callback, int timeoutMs = 0)
        {
            if (timeoutMs == 0)
            {
                timeoutMs = ConnectorConfig.RequestTimeoutMs;
            }
            _clientNetwork!.Request(serviceId,request,callback,0, timeoutMs);
        }
        public void RequestToStage(ushort serviceId, IPacket request, Action<IPacket> callback,int stageKey,int timeoutMs = 0)
        {
            if (timeoutMs == 0)
            {
                timeoutMs = ConnectorConfig.RequestTimeoutMs;
            }
            _clientNetwork!.Request(serviceId,request,callback,stageKey, timeoutMs);
        }

        public async Task<IPacket> AuthenticateAsync(ushort serviceId, IPacket request,int timeoutMs=0)
        {
            if (timeoutMs == 0)
            {
                timeoutMs = ConnectorConfig.RequestTimeoutMs;
            }
            return await _clientNetwork!.AuthenticateAsync(serviceId, request, timeoutMs);
        }
        public async Task<IPacket> RequestToApiAsync(ushort serviceId, IPacket request, int timeoutMs = 0)
        {
            if (timeoutMs == 0)
            {
                timeoutMs = ConnectorConfig.RequestTimeoutMs;
            }
            return await _clientNetwork!.RequestAsync(serviceId, request, 0, timeoutMs);
        }
        public async Task<IPacket> RequestToStageAsync(ushort serviceId, IPacket request,int stageKey,int timeoutMs = 0)
        {
            if (timeoutMs == 0)
            {
                timeoutMs = ConnectorConfig.RequestTimeoutMs;
            }
            return await _clientNetwork!.RequestAsync(serviceId, request, stageKey, timeoutMs);
        }

        public bool IsAuthenticated()
        {
            return _clientNetwork!.IsReady();
        }
        public void ConnectCallback()
        {
            if (OnAuthenticate != null)
            {
                OnAuthenticate.Invoke();
            }
            else
            {
                _log.Error(() => "OnAuthenticate is not initialized");
            }
            //OnConnect?.Invoke(result);
        }

        public void ReceiveApiCallback(ushort serviceId, IPacket packet)
        {
            if(OnApiReceive != null)
            {
                OnApiReceive.Invoke(serviceId, packet);
            }
            else
            {
                _log.Error(()=>"OnApiReceive is not initialized");
            }
        }

        public void ReceiveStageCallback(ushort serviceId, int stageKey, IPacket packet)
        {
            if (OnStageReceive != null)
            {
                OnStageReceive.Invoke(serviceId, stageKey, packet);
            }
            else
            {
                _log.Error(() => "CallReceiveEx is not initialized");
            }
        }

        public void ErrorApiCallback(ushort serviceId, ushort errorCode, IPacket request)
        {
            if (OnApiError != null)
            {
                OnApiError.Invoke(serviceId, errorCode, request);
            }
            else
            {
                _log.Error(() => "OnApiError is not initialized");
            }
        }
        public void ErrorStageCallback(ushort serviceId,int stageKey, ushort errorCode, IPacket request)
        {
            if (OnStageError != null)
            {
                OnStageError.Invoke(serviceId, stageKey, errorCode, request);
            }
            else
            {
                _log.Error(() => "OnStageError is not initialized");
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

        public void ReadyCallback()
        {
            if(OnReady != null)
            {
                OnReady.Invoke();
            }
            else
            {
                _log.Error(() => "OnReady is not initialized");
            }
        }

        public void ReconnectedCallback()
        {
            if(OnReconnected != null)
            {
                OnReconnected.Invoke();
            }
            else
            {
                _log.Error(() => "OnReconnected is not initialized");
            }
        }
    }
}