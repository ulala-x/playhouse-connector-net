using System.Collections.Generic;

namespace PlayHouseConnector
{
    public class ConnectorConfig
    {
            private const int DEFAULT_TIMEOUT = 3000;

            /// <summary>
            /// TimeOut 기본 대기시간 설정(단위 : ms, 기본값 3000).
            /// </summary>
            public int RequestTimeoutMs = DEFAULT_TIMEOUT;
            
            /// <summary>
            /// 서버와의 연결을 확인하기 위한 heartbeat 주기 설정(단위 : mili sec, 0일 경우 사용 안함, 기본값 1000).
            /// </summary>
            public int HeartBeatIntervalMs = 1000;
            /// <summary>
            /// 서버와의 연결을 확인하기 위한 connection idle timeout 설정(단위 : mili sec, 0일 경우 사용 안함, 기본값 4000).
            /// </summary>
            public int ConnectionIdleTimeoutMs = 4000;

            public bool UseWebsocket { get; set; } = false;
            public bool EnableLoggingResponseTime { get; set; } = false;
            public string Host { get; set; } = string.Empty;
            public int Port { get; set; }  = 0;
            public bool DebugMode { get; internal set; } = false;
            public int RetryCount = 1;
            public HashSet<ushort> StageIds { get; }  = new() ;

    }
   
}
