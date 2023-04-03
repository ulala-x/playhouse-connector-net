using playhouse_connector_net.network;
using PlayHouseConnector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace playhouse_connector_net
{
    public class ConnectorForUnity : Connector
    {
        public ConnectorForUnity(ConnectorConfig config) : base(config)
        {
        }

        public new IEnumerator Start()
        {
            return AsyncManager.Instance.MainThreadActionCoroutine();
        }
    }
}
