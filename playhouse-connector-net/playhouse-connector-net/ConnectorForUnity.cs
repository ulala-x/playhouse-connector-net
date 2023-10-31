using PlayHouseConnector;
using System.Collections;

namespace playhouse_connector_net
{
    public class ConnectorForUnity : Connector
    {
        public ConnectorForUnity(ConnectorConfig config) : base(config)
        {
        }

        public new IEnumerator Start()
        {
            return AsyncManager.MainThreadActionCoroutine();
        }
    }
}
