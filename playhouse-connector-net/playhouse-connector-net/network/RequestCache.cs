using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using PlayHouse.Utils;

namespace PlayHouseConnector.Network
{
    public class ReplyObject
    {
        private readonly Action<ushort, IPacket>? _replyCallback;
        private readonly DateTime _time = DateTime.Now;

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
            var difference = DateTime.Now - _time;
            return difference.TotalMilliseconds > timeoutMs;
        }
    }

    public class RequestCache
    {
        private readonly ConcurrentDictionary<int, ReplyObject> _cache = new();

        private readonly LOG<RequestCache> _log = new();
        private readonly AtomicShort _sequence = new();
        private readonly int _timeoutMs;

        public RequestCache(int timeout)
        {
            _timeoutMs = timeout;
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
    }
}