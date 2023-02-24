using playhouse_connector_net.network;
using PlayHouseConnector.network.buffer;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PlayHouseConnector.network
{
    class TcpClient : NetCoreServer.TcpClient, IClient
    {
        private ILogger _log = Log.ForContext<TcpClient>();

        private IConnectorListener _connectorListener;

        private PBuffer _buffer = new PBuffer(1024 * 4);
        private PacketParser _packetParser = new PacketParser();
        private bool _stop = false;

        public void DisconnectAndStop()
        {
            _stop = true;
            DisconnectAsync();
            while (base.IsConnected)
                Thread.Yield();
        }


        public TcpClient(string host, int port,Connector connector) : base(host, port)
        {
            _connectorListener = new ConnectorListener(connector, this);
        }

        protected override void OnConnected()
        {
            _log.Information($"Connected id:{Id}");
            Console.WriteLine($"Chat WebSocket client connected a new session with Id {Id}");

            _connectorListener.OnConnected();
            _stop = false;
        }

        protected override void OnDisconnected()
        {

            Console.WriteLine($"Chat TCP client disconnected a session with Id {Id}");
            _connectorListener.OnDisconnected();

            
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                _buffer.Append(buffer, offset, size);
                var packets = _packetParser.Parse(_buffer);
                packets.ForEach(packet => {
                        _connectorListener.OnReceive(packet.ServiceId(), packet.ToPacket());
                });

            }
            catch (Exception ex)
            {
                _log.Error("packet  exception occurred , disconnect connection: {0}", ex.ToString());
                Disconnect();
            }

            Console.WriteLine(Encoding.UTF8.GetString(buffer, (int)offset, (int)size));
        }

        public void ClientConnect()
        {
            base.ConnectAsync();
        }

        public void ClientDisconnect()
        {
            base.DisconnectAsync();
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