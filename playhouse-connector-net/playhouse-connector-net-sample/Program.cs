using Playhouse.Sample;
using playhouse_connector_net;
using PlayHouseConnector;

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
            String serviceId = "Api";

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

            connector.OnReceive += (String serviceId, Packet packet) =>
            {
                Console.WriteLine($"OnReceive {serviceId},{packet.MsgName}");
            };



            connector.Request(serviceId, new Packet(authenticateReq), (ReplyPacket replyPacket) =>
            {
                if(replyPacket.ErrorCode == 0)
                {

                }
            });
            

            Console.WriteLine("Hello, World!");
        }
    }
}