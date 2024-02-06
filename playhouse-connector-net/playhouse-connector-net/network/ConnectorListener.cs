// namespace PlayHouseConnector.Network
// {
//     internal class ConnectorListener : IConnectorListener
//     {
//         private readonly ClientNetwork _clientNetwork;
//         private IClient _client;
//
//         private RequestCache _requestCache;
//         private AsyncManager _asyncManager;
//         
//         public ConnectorListener(ClientNetwork clientNetwork, IClient client, RequestCache requestCache,
//             AsyncManager asyncManager)
//         {
//            _clientNetwork = clientNetwork;
//            _client = client;
//             _requestCache = requestCache;
//             _asyncManager = asyncManager;
//         }
//
//         public void OnConnected()
//         {
//             _asyncManager.AddJob(() =>
//             {
//                 _clientNetwork.CallConnect();
//             });
//         }
//   
//         public void OnDisconnected()
//         {
//             _asyncManager.AddJob(() =>
//             {
//                 _clientNetwork.CallDisconnected();
//             });
//         }
//
//         public void OnApiReceive(ClientPacket clientPacket)
//         {
//             _asyncManager.AddJob(() =>
//             {
//                 if (clientPacket.MsgSeq > 0)
//                 {
//                     _requestCache.OnReply(clientPacket);
//                 }
//                 else
//                 {
//                     _clientNetwork.CallReceive(new TargetId(clientPacket.ServiceId,clientPacket.Header.StageIndex), clientPacket.ToPacket());
//                 }
//                 
//             });
//         }
//     }
// }
