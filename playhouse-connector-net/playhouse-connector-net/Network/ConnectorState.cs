using System;
using System.Collections.Generic;
using System.Text;

namespace PlayHouseConnector.Network
{
    public enum ConnectorState
    {
        Before_Connected = 0,
        Before_Authenticated = 1,
        Ready = 2,
        Disconnected = 3
    }

    public enum DisconnectType 
    { 
        None = 0,
        System_Disconnect = 1,
        Self_Disconnect = 2,
        AuthenticateFail_Disconnect = 3
    }

}
