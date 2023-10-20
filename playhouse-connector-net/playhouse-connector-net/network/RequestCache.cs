using PlayHouse.Utils;
using playhouse_connector_net.network;
using System;
using System.Collections.Specialized;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace PlayHouseConnector.network
{
    public class ReplyObject
    {
        private Action<ReplyPacket>? _replyCallback = null;
        private TaskCompletionSource<ReplyPacket>? _taskCompletionSource= null;
        public ReplyObject(Action<ReplyPacket>? callback = null, TaskCompletionSource<ReplyPacket>? taskCompletionSource = null)  
        { 
            _replyCallback = callback;
            _taskCompletionSource = taskCompletionSource;
        }

        public void OnReceive(ReplyPacket replayPacket)
        {
            AsyncManager.Instance.AddJob(() =>
            {
                _replyCallback?.Invoke(replayPacket);
                _taskCompletionSource?.SetResult(replayPacket);
            });
        }

        public void Throw(ushort errorCode)
        {
            AsyncManager.Instance.AddJob(() =>
            {
                _replyCallback?.Invoke(ClientPacket.OfErrorPacket(errorCode));
                _taskCompletionSource?.SetResult(ClientPacket.OfErrorPacket(errorCode));
            });
            //_replyCallback?.Throws(exception);
            //_taskCompletionSource?.SetException(exception);
        }
    }
    public class RequestCache
    {
        private AtomicShort _sequece = new AtomicShort();
        private CacheItemPolicy _policy;
        private const ushort requestTimeoutErrorCode = 60003;
        private  MemoryCache _cache ;

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
            _policy = new CacheItemPolicy() ;
            _policy.SlidingExpiration = TimeSpan.FromMilliseconds(timeout);


            // Set a callback to be called when the cache item is removed
            _policy.RemovedCallback = new CacheEntryRemovedCallback((args) => {
                if (args.RemovedReason == CacheEntryRemovedReason.Expired)
                {
                    var replyObject = (ReplyObject)args.CacheItem.Value;
                    replyObject.OnReceive(ClientPacket.OfErrorPacket(requestTimeoutErrorCode));
                }
            });

            // Add item to the cache with the specified policy
            //MemoryCache.Default.Add(cacheItem, policy);
        }

        public int GetSequence()
        {
            return _sequece.IncrementAndGet();
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
            string key = msgSeq.ToString();
            ReplyObject? replyObject = _cache.Get(key) as ReplyObject ;

            if (replyObject != null) { 
                replyObject.OnReceive(clientPacket.ToReplyPacket());
                _cache.Remove(key);
            }
            else
            {
                LOG.Error($"{msgSeq},${clientPacket.MsgId} request is not exist",GetType());
            }
        }
    }

}
