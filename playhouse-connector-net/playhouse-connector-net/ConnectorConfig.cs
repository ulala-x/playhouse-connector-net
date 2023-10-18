using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouseConnector
{
     public class ConnectorConfig
    {

            private const int DEFAULT_TIMEOUT = 5000;

            /// <summary>
            /// TimeOut 기본 대기시간 설정(단위 : ms, 기본값 5).
            /// </summary>
            public int ReqestTimeout = DEFAULT_TIMEOUT;

            
            /// <summary>
            /// 서버와의 연결을 확인하기 위한 ping 주기 설정(단위 : sec, 0일 경우 사용 안함, 기본값 3).
            /// </summary>
            public int PingInterval = 3;

            /// <summary>
            /// 접속이 끈겼을 경우 재접속 시도 횟수(기본값 : 0).
            /// </summary>
            public int ReconnectCount = 0;

            /// <summary>
            /// 재접속 실패시 다음 재접속 시도까지 대기시간(단위 : sec, 기본값 : 3).
            /// </summary>
            public int ReconnectDelay = 3;


            public bool UseWebsocket = false;

        }
   
}
