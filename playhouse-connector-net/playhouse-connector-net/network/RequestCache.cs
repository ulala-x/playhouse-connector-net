using PlayHouse.Utils;
using playhouse_connector_net.network;
using System;
using System.Runtime.Caching;
using System.Threading;
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

        public void Throw(short errorCode)
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

        public RequestCache(int timeout) 
        {
            _policy = new CacheItemPolicy() ;
            if(timeout > 0) {
                _policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(timeout);
            }
            

            // Set a callback to be called when the cache item is removed
            _policy.RemovedCallback = new CacheEntryRemovedCallback((args) => {
                if (args.RemovedReason == CacheEntryRemovedReason.Expired)
                {
                    var replyObject = (ReplyObject)args.CacheItem.Value;
                    replyObject.OnReceive(ClientPacket.OfErrorPacket((int)BaseErrorCode.RequestTimeout));
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
            MemoryCache.Default.Add(cacheItem, _policy);
        }

        public ReplyObject? Get(int seq)
        {
            return (ReplyObject)MemoryCache.Default.Get(seq.ToString());
        }

        public void OnReply(ClientPacket clientPacket)
        {
            int msgSeq = clientPacket.MsgSeq;
            string key = msgSeq.ToString();
            ReplyObject replyObject = (ReplyObject)MemoryCache.Default.Get(key);

            if (replyObject != null) { 
                replyObject.OnReceive(clientPacket.ToReplyPacket());
                MemoryCache.Default.Remove(key);
            }
            else
            {
                LOG.Error($"{msgSeq},${clientPacket.MsgId} request is not exist",GetType());
            }
        }
    }

}
