using System.Collections;

namespace PlayHouseConnector
{
    public class ConnectorForUnity : Connector
    {
        public ConnectorForUnity(ConnectorConfig config) : base(config)
        {
        }

        public IEnumerator MainCoroutineAction()
        {
            return AsyncManager.MainCoroutineAction();
        }
    }
}
