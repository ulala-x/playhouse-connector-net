using PlayHouse.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PlayHouseConnector.Network
{
    public class ReplyObject
    {
        private Action<ushort ,IPacket>? _replyCallback;
        private int _timeoutMs;
        public int MsgSeq { get; set; }
        public DateTime _time = DateTime.Now;
        
        public ReplyObject(int msgSeq,int timeoutMs, Action<ushort,IPacket>? callback = null)
        {
            MsgSeq = msgSeq;
            _timeoutMs = timeoutMs;
            _replyCallback = callback;
            
        }
        public void OnReceive(ushort errorCode,IPacket packet)
        {
            _replyCallback?.Invoke(errorCode,packet);
        }

        public bool IsExpired()
        {
            var difference = DateTime.Now - _time;
            return difference.TotalMilliseconds > _timeoutMs;
        }
    }
    public class RequestCache
    {
        
        private LOG<RequestCache> _log = new();
        private readonly AtomicShort _sequence = new();
        private int _timeoutMs = 0;
        private ConcurrentDictionary<int, ReplyObject> _cache = new();
        public RequestCache(int timeout)
        {
            _timeoutMs = timeout;
        }

        public void CheckExpire()
        {
            if(_timeoutMs > 0)
            {
                List<int> keysToDelete = new();
                
                foreach(var item in _cache)
                {
                    if(item.Value.IsExpired())
                    {
                        item.Value.OnReceive((ushort)ConnectorErrorCode.REQUEST_TIMEOUT, new Packet(-3));
                        keysToDelete.Add(item.Key);
                    }
                }
                foreach(int key in keysToDelete)
                {
                    Remove(key);
                }
            }
        }

        public int GetSequence()
        {
            return _sequence.IncrementAndGet();
        }

        public void Put(int seq,ReplyObject replyObject)
        {
            _cache[seq] = replyObject;
        }

        public ReplyObject? Get(int seq)
        {
            if(_cache.TryGetValue(seq, out var replyObject))
            {
                return replyObject;
            }
            else
            {
                return null;
            }
        }
        private void Remove(int seq)
        {
            _cache.TryRemove(seq, out var _);
            
        }

        public void OnReply(ClientPacket clientPacket)
        {
            int msgSeq = clientPacket.MsgSeq;
            int stageKey = clientPacket.Header.StageIndex;
            ReplyObject? replyObject = Get(msgSeq) ;

            if (replyObject != null)
            {
                var packet = clientPacket.ToPacket();
                var errorCode = clientPacket.Header.ErrorCode;
                replyObject.OnReceive(errorCode,packet);
                Remove(msgSeq);
            }
            else
            {
                _log.Error(
                    ()=>$"OnReply Already Removed - [errorCode:{clientPacket.Header.ErrorCode},msgSeq:{msgSeq},msgId{clientPacket.MsgId},stageKey:{stageKey}]");    
            }
        }
    }

}
