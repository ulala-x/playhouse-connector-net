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
        
        public void Connect()
        {
            _clientNetwork!.Connect();
        }

        public async Task<bool> ConnectAsync()
        {
            return await _clientNetwork!.ConnectAsync();
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
            _clientNetwork!.Send(serviceId, packet, 0);
        }
        public void SendEx(ushort serviceId,int stageKey,IPacket packet)
        {
            _clientNetwork!.Send(serviceId, packet, stageKey);
        }
        public void Request(ushort serviceId,  IPacket request, Action<IPacket> callback)
        {
            _clientNetwork!.Request(serviceId,request,callback,0);
        }
        public void RequestEx(ushort serviceId, IPacket request, Action<IPacket> callback,int stageKey)
        {
            _clientNetwork!.Request(serviceId,request,callback,stageKey);
        }
        public async Task<IPacket> RequestAsync(ushort serviceId, IPacket request)
        {
            return await RequestExAsync(serviceId, request, 0);
        }
        public async Task<IPacket> RequestExAsync(ushort serviceId, IPacket request,int stageKey)
        {
            return await _clientNetwork!.RequestExAsync(serviceId, request, stageKey);
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