using PlayHouse.Utils;
using System;
using System.Collections.Specialized;
using System.Runtime.Caching;

namespace PlayHouseConnector.Network
{
    public class ReplyObject
    {
        private Action<ushort ,IPacket>? _replyCallback;
        public int MsgSeq { get; set; }
        public ReplyObject(int msgSeq,Action<ushort,IPacket>? callback = null)
        {
            MsgSeq = msgSeq;
            _replyCallback = callback;
        }
        public void OnReceive(ushort errorCode,IPacket packet)
        {
            _replyCallback?.Invoke(errorCode,packet);
            // _asyncManager.AddJob(() =>
            // {
            //     _replyCallback?.Invoke(errorCode,packet);
            // });
        }
    }
    public class RequestCache
    {
        
        private LOG<RequestCache> _log = new();
        private readonly AtomicShort _sequence = new();
        private readonly CacheItemPolicy _policy;
        private const ushort RequestTimeoutErrorCode = 60003;
        private readonly MemoryCache _cache ;

        public RequestCache(int timeout)
        {
            NameValueCollection cacheSettings = new ()
            {
                {"CacheMemoryLimitMegabytes", "10"},
                {"PhysicalMemoryLimitPercentage", "10"},
            };

            if (timeout > 0)
            {
                cacheSettings.Add("PollingInterval", "00:00:01");
            }

            _cache =  new("PlayHouseConnector", cacheSettings);
            _policy = new CacheItemPolicy
            {
                SlidingExpiration = TimeSpan.FromSeconds(timeout),
                // Set a callback to be called when the cache item is removed
                RemovedCallback = (args) => {
                    if (args.RemovedReason == CacheEntryRemovedReason.Expired)
                    {
                        var replyObject = (ReplyObject)args.CacheItem.Value;
                        replyObject.OnReceive(RequestTimeoutErrorCode,new Packet());
                    }
                }
            };
        }

        public int GetSequence()
        {
            return _sequence.IncrementAndGet();
        }

        public void Put(int seq,ReplyObject replyObject)
        {
            var cacheItem = new CacheItem(seq.ToString(), replyObject);
            _cache.Add(cacheItem, _policy);
        }

        public ReplyObject? Get(int seq)
        {
            return (ReplyObject)_cache.Get(seq.ToString());
        }

        public void OnReply(ClientPacket clientPacket)
        {
            int msgSeq = clientPacket.MsgSeq;
            int stageKey = clientPacket.Header.StageIndex;
            string key = msgSeq.ToString();
            ReplyObject? replyObject = (ReplyObject?)_cache.Get(key) ;

            if (replyObject != null)
            {
                var packet = clientPacket.ToPacket();
                var errorCode = clientPacket.Header.ErrorCode;
                replyObject.OnReceive(errorCode,packet);
                _cache.Remove(key);
            }
            else
            {
                _log.Error(
                    ()=>$"OnReply Already Removed - [errorCode:{clientPacket.Header.ErrorCode},msgSeq:{msgSeq},msgId{clientPacket.MsgId},stageKey:{stageKey}]");    
            }
            
        }
    }

}
