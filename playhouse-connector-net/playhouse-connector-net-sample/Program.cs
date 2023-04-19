using Playhouse.Sample;
using playhouse_connector_net;
using PlayHouseConnector;
using PlayHouse;

namespace playhouse_connector_net_sample
{
    internal class Program
    {

        static void Main(string[] args)
        {

            ConnectorConfig config = new ConnectorConfig();
            ClientConnectorListener listener = new ClientConnectorListener();

            Connector connector = new Connector(config);

            connector.Start();

            connector.Connect("127.0.0.1", 10001);
            short serviceId = 2;

            var authenticateReq = new AuthenticateReq();
            authenticateReq.UserId= 1;
            authenticateReq.Token= "test";

            connector.OnConnect += () =>
            {
                Console.WriteLine("OnConnected");
            };

            connector.OnDiconnect += () =>
            {
                Console.WriteLine("OnDisconnected");
            };

            connector.OnReconnect += (int retryCnt) =>
            {
                Console.WriteLine($"OnConnected {retryCnt}");
            };

            connector.OnReceive += (TargetId targetId, Packet packet) =>
            {
                Console.WriteLine($"OnReceive {targetId.ServiceId},{packet.MsgId}");
            };

            //connector.OnApiReceive()
            //connector.OnStageReceive(short serviceId,int stageIndex,Packet packet)




            connector.Request(new TargetId(serviceId), new Packet(authenticateReq), (IReplyPacket replyPacket) =>
            {
                using (replyPacket)
                {
                    if (replyPacket.ErrorCode == 0)
                    {
                        AuthenticateRes authenticateRes = AuthenticateRes.Parser.ParseFrom(replyPacket.Data);
                    }
                }   
            });
            

            Console.WriteLine("Hello, World!");
        }
    }
}