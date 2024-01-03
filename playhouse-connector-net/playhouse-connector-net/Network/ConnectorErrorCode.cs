using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouseConnector.Network
{
    public enum ConnectorErrorCode
    {
        DISCONNECTED = 60201,
        REQUEST_TIMEOUT = 60202,
        UNAUTHENTICATED = 60203
    }
}
