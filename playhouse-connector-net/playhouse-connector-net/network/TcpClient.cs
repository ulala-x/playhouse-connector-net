using System;
using System.Threading;
using CommonLib;
using PlayHouse;
using PlayHouse.Utils;

namespace PlayHouseConnector.Network
{
    internal class TcpClient : NetCoreServer.TcpClient, IClient
    {
        private readonly ClientNetwork _clientNetwork;
        private readonly LOG<TcpClient> _log = new();
        private readonly PacketParser _packetParser = new();
        private readonly RingBuffer _recvBuffer = new(PacketConst.MaxPacketSize);
        private readonly PooledByteBuffer _sendBuffer = new(PacketConst.MaxPacketSize);
        private bool _stop;

        public TcpClient(string host, int port, ClientNetwork clientNetwork) : base(host, port)
        {
            _clientNetwork = clientNetwork;

            OptionNoDelay = true;
            OptionKeepAlive = true;

            OptionReceiveBufferSize = 64 * 1024;
            OptionSendBufferSize = 64 * 1024;
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
            base.ConnectAsync();
        }

        public void ClientDisconnect()
        {
            base.Disconnect();
        }

        public bool IsClientConnected()
        {
            return IsConnected;
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

        public void DisconnectAndStop()
        {
            _stop = true;
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnConnected()
        {
            _log.Info(() => $"Tcp Socket client connected  - [sid:{Id}]");

            _clientNetwork.OnConnected();
            _stop = false;
        }

        protected override void OnDisconnected()
        {
            _log.Info(() => $"TCP client disconnected  - [sid:{Id}]");
            _clientNetwork.OnDisconnected();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                _recvBuffer.Write(buffer, offset, size);
                var packets = _packetParser.Parse(_recvBuffer);
                packets.ForEach(packet => { _clientNetwork.OnReceive(packet); });
            }
            catch (Exception ex)
            {
                _log.Error(() => $"OnReceived Exception - [exception:{ex}]");
                Disconnect();
            }
        }
    }
}