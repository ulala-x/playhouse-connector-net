// using playhouse_connector_net.network;
// using PlayHouseConnector;
// using PlayHouseConnector.network;
// using System.Collections.Concurrent;
//
// namespace playhouse_connector_net_sample
// {
//     class ClientConnectorListener : IConnectorListener
//     {
//         private ConcurrentQueue<Packet> _packetQueue = new ConcurrentQueue<Packet>();
//
//
//         public void OnConnected()
//         {
//             LOG.Info($"onConnected",GetType());
//         }
//
//         public void OnDisconnected()
//         {
//             LOG.Info($"OnDisconnected", GetType());
//         }
//
//         public void OnReceive(ClientPacket clientPacket)
//         {
//             _packetQueue.Enqueue(clientPacket.ToPacket());
//         }
//     }
// }
