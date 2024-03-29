﻿using CommonLib;
using System;
using System.Threading;
using PlayHouse.Utils;
using PlayHouse;

namespace PlayHouseConnector.Network
{
    class TcpClient : NetCoreServer.TcpClient, IClient
    {
        private readonly PacketParser _packetParser = new PacketParser();
        private readonly RingBuffer _recvBuffer = new RingBuffer(1024 * 1024);
        private readonly PooledByteBuffer _sendBuffer = new PooledByteBuffer(1024 * 1024);
        private readonly RingBufferStream _stream;
        private bool _stop = false;
        private LOG<TcpClient> _log = new();
        private readonly ClientNetwork _clientNetwork;

        public void DisconnectAndStop()
        {
            _stop = true;
            DisconnectAsync();
            while (base.IsConnected)
                Thread.Yield();
        }

        public TcpClient(string host, int port, ClientNetwork clientNetwork) : base(host, port)
        {
            _clientNetwork = clientNetwork;
            _stream = new RingBufferStream(_recvBuffer);

            OptionNoDelay = true;
            OptionKeepAlive = true;

            OptionReceiveBufferSize = 64 * 1024;
            OptionSendBufferSize = 64 * 1024;
        }

        protected override void OnConnected()
        {
            _log.Info(()=>$"Tcp Socket client connected  - [sid:{Id}]");

            _clientNetwork.OnConnected();
            _stop = false;
        }

        protected override void OnDisconnected()
        {
            _log.Info(()=>$"TCP client disconnected  - [sid:{Id}]");
            _clientNetwork.OnDisconnected();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                _stream.Write(buffer, (int)offset, (int)size);
                var packets = _packetParser.Parse(_recvBuffer);
                packets.ForEach(packet => { _clientNetwork.OnReceive(packet); });

            }
            catch (Exception ex)
            {
                _log.Error(()=>$"OnReceived Exception - [exception:{ex}]");
                Disconnect();
            }
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
    }
}