using System;
using System.Collections.Generic;
using System.Text;

namespace PlayHouseConnector.Network
{
    internal static class PacketConst
    {
        public static readonly int MsgIdLimit = 256;
        public static readonly int MaxBodySize = 1024 * 1024 * 2;
        public static readonly int MinHeaderSize = 23;

        public static readonly string HeartBeat = "@Heart@Beat@";
        public static readonly string Debug = "@Debug@";
        public static readonly string Timeout = "@Timeout@";
    }
}
