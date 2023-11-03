using System.Dynamic;

namespace PlayHouseConnector
{
     public class ConnectorConfig
    {
            private const int DEFAULT_TIMEOUT = 5000;

            /// <summary>
            /// TimeOut 기본 대기시간 설정(단위 : ms, 기본값 5).
            /// </summary>
            public int RequestTimeout = DEFAULT_TIMEOUT;
            
            /// <summary>
            /// 서버와의 연결을 확인하기 위한 ping 주기 설정(단위 : sec, 0일 경우 사용 안함, 기본값 3).
            /// </summary>
            public int PingInterval = 3;

            public bool UseWebsocket { get; set; } = false;
            public bool EnableLoggingResponseTime { get; set; } = false;

    }
   
}
