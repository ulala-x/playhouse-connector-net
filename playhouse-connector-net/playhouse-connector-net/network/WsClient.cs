
using CommonLib;
using NetCoreServer;
using playhouse_connector_net.network;
using System;
using System.Text;
using System.Threading;

namespace PlayHouseConnector.network
{
    class WsClient : NetCoreServer.WsClient, IClient
    {
        private readonly IConnectorListener _connectorListener;
        private readonly PacketParser _packetParser = new();
        private readonly RingBuffer _recvBuffer = new(1024*1024);
        private readonly RingBuffer _sendBuffer = new(1024 * 1024);
        private readonly RingBufferStream _queueStream ;
        private bool _stop;


        public WsClient(string host, int port, Connector connector, RequestCache requestCache,
            AsyncManager asyncManager) : base(host, port)
        {
            _connectorListener = new ConnectorListener(connector, this, requestCache,asyncManager);

            OptionNoDelay = true;
            OptionKeepAlive = true;

            OptionReceiveBufferSize = 64 * 1024;
            OptionSendBufferSize = 64 * 1024;

            _queueStream = new RingBufferStream(_recvBuffer);
        }

        public void DisconnectAndStop()
        {
            _stop = true;
            CloseAsync(1000);
            while (IsConnected)
                Thread.Yield();
        }

        public override void OnWsConnecting(HttpRequest request)
        {
            request.SetBegin("GET", "/");
            request.SetHeader("Host", "localhost");
            request.SetHeader("Origin", "http://localhost");
            request.SetHeader("Upgrade", "websocket");
            request.SetHeader("Connection", "Upgrade");
            request.SetHeader("Sec-WebSocket-Key", Convert.ToBase64String(WsNonce));
            request.SetHeader("Sec-WebSocket-Protocol", "chat, superchat");
            request.SetHeader("Sec-WebSocket-Version", "13");
            request.SetBody();
        }

        public override void OnWsConnected(HttpResponse response)
        {
            _stop = false;

            LOG.Info($"Connected id:{Id}", GetType());
            Console.WriteLine($"Chat WebSocket client connected a new session with Id {Id}");
            _connectorListener.OnConnected();
        }

    
        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {

            Console.WriteLine($"Incoming: {Encoding.UTF8.GetString(buffer, (int)offset, (int)size)}");

            _queueStream.Write(buffer, (int)offset, (int)size);
            var packets = _packetParser.Parse(_recvBuffer);
            packets.ForEach(packet => { 
                _connectorListener.OnReceive(packet); 
            });
            
        }

        protected override void OnDisconnected()
        {
            base.OnDisconnected();

            Console.WriteLine($"Chat WebSocket client disconnected a session with Id {Id}");

            _connectorListener.OnDisconnected();
        }
        
        public void ClientConnect()
        {
            base.Connect();
        }

        public void ClientDisconnect()
        {
            base.Disconnect();
        }

        public void ClientConnectAsync()
        {
            base.ConnectAsync();
        }

        public void ClientDisconnectAsync()
        {
            base.DisconnectAsync();
        }

        public bool IsClientConnected()
        {
            return base.IsConnected;
        }

        public void Send(ClientPacket clientPacket)
        {
            using (clientPacket)
            {
                _sendBuffer.Clear();
                clientPacket.GetBytes(_sendBuffer);
                base.Send(_sendBuffer.Buffer(), 0, _sendBuffer.Count);
            }
        }

        public bool IsStopped()
        {
            return _stop;
        }

        public bool ClientReconnect()
        {
            return base.Reconnect();
        }
    }
}
