
using NetCoreServer;
using playhouse_connector_net.network;
using PlayHouseConnector.network.buffer;
using Serilog;
using System;
using System.Text;
using System.Threading;

namespace PlayHouseConnector.network
{
    class WsClient : NetCoreServer.WsClient, IClient
    {
        private ILogger _log = Log.ForContext<TcpClient>();

        private IConnectorListener _connectorListener;
        private PacketParser _packetParser = new PacketParser();
        private bool _stop;


        public WsClient(string host, int port, Connector connector) : base(host, port)
        {
            _connectorListener = new ConnectorListener(connector, this);

            base.OptionNoDelay = true;
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

            _log.Information($"Connected id:{Id}");
            Console.WriteLine($"Chat WebSocket client connected a new session with Id {Id}");
            _connectorListener.OnConnected();
        }

    
        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {

            Console.WriteLine($"Incoming: {Encoding.UTF8.GetString(buffer, (int)offset, (int)size)}");

            var packetBuffer = new PBuffer((int)size);
            packetBuffer.Append(buffer,offset, size);
            var packets = _packetParser.Parse(packetBuffer);
            packets.ForEach(packet => { _connectorListener.OnReceive(packet.ServiceId(),packet.ToPacket()); });
            
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

        public bool IsClientConnected()
        {
            return base.IsConnected;
        }

        public void Send(string serviceId, ClientPacket clientPacket)
        {
            using (var buffer = clientPacket.ToByteBuffer())
            {
                base.Send(buffer.Data);
            }
        }

        public void SendAsync(string serviceId, ClientPacket clientPacket)
        {
            using (var buffer = clientPacket.ToByteBuffer())
            {
                base.SendAsync(buffer.Data);
            }
        }

        public bool IsStoped()
        {
            return _stop;
        }

        void IClient.ConnectAsync()
        {
            base.ConnectAsync();
        }
    }
}
