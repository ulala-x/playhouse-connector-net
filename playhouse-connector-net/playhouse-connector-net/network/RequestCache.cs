using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using PlayHouse.Utils;

namespace PlayHouseConnector.Network
{
    public class ReplyObject
    {
        private readonly Action<ushort, IPacket>? _replyCallback;
        private readonly DateTime _requestTime = DateTime.UtcNow;
        private DateTime _responseTime = DateTime.MinValue;
        public ReplyObject(int msgSeq, Action<ushort, IPacket>? callback = null)
        {
            MsgSeq = msgSeq;
            _replyCallback = callback;
        }

        public int MsgSeq { get; set; }

        public void OnReceive(ushort errorCode, IPacket packet)
        {
            _replyCallback?.Invoke(errorCode, packet);
        }

        public bool IsExpired(int timeoutMs)
        {
            if (_responseTime != DateTime.MinValue)
            {
                return false;
            }

            var difference = DateTime.UtcNow - _requestTime;
            return difference.TotalMilliseconds > timeoutMs;
        }

        public void TouchReceive()
        {
            _responseTime = DateTime.UtcNow;
        }

        public double GetElapsedTime()
        {
            if (_responseTime == DateTime.MinValue) return 0;
            var difference = _responseTime - _requestTime;
            return difference.TotalMilliseconds;
        }
    }

    public class RequestCache
    {
        private readonly ConcurrentDictionary<int, ReplyObject> _cache = new();

        private readonly LOG<RequestCache> _log = new();
        private readonly AtomicShort _sequence = new();
        private readonly int _timeoutMs;
        private readonly bool _enableLoggingResponseTime;

        public RequestCache(int timeout, bool enableLoggingResponseTime)
        {
            _timeoutMs = timeout;
            _enableLoggingResponseTime = enableLoggingResponseTime;
        }

        public void CheckExpire()
        {
            if (_timeoutMs > 0)
            {
                List<int> keysToDelete = new();

                foreach (var item in _cache)
                {
                    if (item.Value.IsExpired(_timeoutMs))
                    {
                        item.Value.OnReceive((ushort)ConnectorErrorCode.REQUEST_TIMEOUT, new Packet(PacketConst.Timeout));
                        keysToDelete.Add(item.Key);
                    }
                }

                foreach (var key in keysToDelete)
                {
                    Remove(key);
                }
            }
        }

        public int GetSequence()
        {
            return _sequence.IncrementAndGet();
        }

        public void Put(int seq, ReplyObject replyObject)
        {
            _cache[seq] = replyObject;
        }

        public ReplyObject? Get(int seq)
        {
            return _cache.GetValueOrDefault(seq);
        }

        private void Remove(int seq)
        {
            _cache.TryRemove(seq, out var _);
        }

        public void OnReply(ClientPacket clientPacket)
        {
            var msgSeq = clientPacket.MsgSeq;
            var stageId = clientPacket.Header.StageId;
            var replyObject = Get(msgSeq);

            if (replyObject != null)
            {
                var packet = clientPacket.ToPacket();
                var errorCode = clientPacket.Header.ErrorCode;
                replyObject.OnReceive(errorCode, packet);
                Remove(msgSeq);

                if (_enableLoggingResponseTime)
                {
                    _log.Info(() =>
                        $"response time - [msgId:{clientPacket.MsgId},msgSeq:{msgSeq},elapsedTime:{replyObject.GetElapsedTime()}]");
                }
                    
            }
            else
            {
                _log.Error(
                    () =>
                        $"OnReply Already Removed - [errorCode:{clientPacket.Header.ErrorCode},msgSeq:{msgSeq},msgId{clientPacket.MsgId},stageId:{stageId}]");
            }
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public void TouchReceive(int msgSeq)
        {
            var replyObject = Get(msgSeq);
            replyObject?.TouchReceive();
        }
    }
}