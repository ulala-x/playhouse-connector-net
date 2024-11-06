namespace PlayHouseConnector
{
    public class ConnectorConfig
    {
        /// <summary>
        ///     서버와의 연결을 확인하기 위한 connection idle timeout 설정(단위 : mili sec, 0일 경우 사용 안함, 기본값 30000).
        /// </summary>
        public int ConnectionIdleTimeoutMs { get; set; } = 30000;

        /// <summary>
        ///     서버와의 연결을 확인하기 위한 heartbeat 주기 설정(단위 : mili sec, 0일 경우 사용 안함, 기본값 1000).
        /// </summary>
        public int HeartBeatIntervalMs { get; set; } = 10000;

        /// <summary>
        ///     TimeOut 기본 대기시간 설정(단위 : ms, 기본값 6000).
        /// </summary>
        public int RequestTimeoutMs { get; set; } = 30000;

        public bool UseWebsocket { get; set; } = false;

        public bool EnableLoggingResponseTime { get; set; } = false;

        //public bool UseExtendStage = false;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 0;

        public bool TurnOnTrace { get; set; } = false;
    }
}