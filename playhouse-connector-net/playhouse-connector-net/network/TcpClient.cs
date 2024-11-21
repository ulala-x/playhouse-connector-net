using System;
using System.Collections.Generic;
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
        private readonly RingBuffer _recvBuffer = new(PacketConst.MaxBodySize);
        private readonly PooledByteBuffer _sendBuffer = new(PacketConst.MaxBodySize);
        private bool _stop;
        private readonly bool _turnOnTrace;
        private long _sid => Socket.Handle.ToInt64();

        public TcpClient(string host, int port, ClientNetwork clientNetwork, bool turnOnTrace) : base(host, port)
        {
            _clientNetwork = clientNetwork;

            OptionNoDelay = true;
            OptionKeepAlive = true;

            OptionSendBufferSize = 1024 * 64;
            OptionReceiveBufferSize = 1024 * 256;

            _turnOnTrace = turnOnTrace;
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
                if (_turnOnTrace)
                {
                    _log.Info(() => $"Send Packet : [{clientPacket.Header}]");
                }

                _sendBuffer.Clear();
                clientPacket.GetBytes(_sendBuffer);
                base.Send(_sendBuffer.Buffer(), 0, _sendBuffer.Count);
            }
        }

        public bool IsStopped()
        {
            return _stop;
        }

        protected override void OnConnected()
        {
            _log.Info(() => $"Tcp Socket client connected  - [sid:{_sid}]");

            _clientNetwork.OnConnected();
            _stop = false;
        }

        protected override void OnDisconnected()
        {
            _log.Info(() => $"TCP client disconnected  - [sid:{_sid}]");
            _clientNetwork.OnDisconnected();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                List<ClientPacket> packets;
                lock (_recvBuffer)
                {
                    _recvBuffer.Write(buffer, offset, size);
                    packets = _packetParser.Parse(_recvBuffer);
                }
                packets.ForEach(packet =>
                {
                    if (_turnOnTrace)
                    {
                        _log.Info(() => $"Received Packet : [{packet.Header}]");
                    }
                    _clientNetwork.OnReceive(packet);
                });
            }
            catch (Exception ex)
            {
                _log.Error(() => $"OnReceived Exception - [exception:{ex}]");
                Disconnect();
            }
        }
    }
}