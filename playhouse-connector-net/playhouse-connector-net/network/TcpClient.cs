using CommonLib;
using playhouse_connector_net.network;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace PlayHouseConnector.network
{
    class TcpClient : NetCoreServer.TcpClient, IClient
    {
        private IConnectorListener _connectorListener;
        private PacketParser _packetParser = new PacketParser();
        private RingBuffer _recvBuffer = new RingBuffer(1024 * 1024);
        private static RingBuffer _sendBuffer = new RingBuffer(1024 * 1024);
        private RingBufferStream _stream;
        private bool _stop = false;

        public void DisconnectAndStop()
        {
            _stop = true;
            DisconnectAsync();
            while (base.IsConnected)
                Thread.Yield();
        }

        public TcpClient(string host, int port,Connector connector, RequestCache requestCache) : base(host, port)
        {
            _connectorListener = new ConnectorListener(connector, this, requestCache);
            _stream = new RingBufferStream(_recvBuffer);

            OptionNoDelay = true;
            OptionKeepAlive = true;

            OptionReceiveBufferSize = 64 * 1024;
            OptionSendBufferSize = 64 * 1024;
        }

        protected override void OnConnected()
        {
            LOG.Info($"Connected id:{Id}",GetType());
            LOG.Info($"Tcp Socket client connected a new session with Id {Id}",GetType());

            _connectorListener.OnConnected();
            _stop = false;
        }

        protected override void OnDisconnected()
        {

            LOG.Info($"TCP client disconnected a session with Id {Id}", GetType());
            _connectorListener.OnDisconnected();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                _stream.Write(buffer, (int)offset, (int)size);
                var packets = _packetParser.Parse(_recvBuffer);
                packets.ForEach(packet => {
                        _connectorListener.OnReceive(packet);
                });

            }
            catch (Exception ex)
            {
                LOG.Error($"packet  exception occurred , disconnect connection: {ex}",GetType(),ex);
                Disconnect();
            }

            //Console.WriteLine(Encoding.UTF8.GetString(buffer, (int)offset, (int)size));
        }

        public void ClientConnectAsync()
        {
            base.ConnectAsync();
        }

        public void ClientDisconnectAsync()
        {
            base.DisconnectAsync();
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

        public void Send(ClientPacket clientPacket)
        {
            using (clientPacket)
            {
                _sendBuffer.Clear();
                clientPacket.GetBytes(_sendBuffer);
                base.Send(_sendBuffer.Buffer(), 0, _sendBuffer.Count);
            }
        }
        public bool IsStoped()
        {
            return _stop;
        }

        public bool ClientReconnect()
        {
            return base.ReconnectAsync();
        }
    }
}