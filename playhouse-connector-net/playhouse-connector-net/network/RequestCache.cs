using playhouse_connector_net;
using playhouse_connector_net.network;
using Serilog;
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
            this._replyCallback = callback;
            this._taskCompletionSource = taskCompletionSource;
        }

        public void OnReceive(ReplyPacket replayPacket)
        {
            using (replayPacket)
            {
                AsyncManager.Instance.AddJob(() =>
                {
                    _replyCallback?.Invoke(replayPacket);
                    _taskCompletionSource?.SetResult(replayPacket);
                });
                
            }
        }

        public void Throw(int errorCode)
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
        private ILogger _log = Log.ForContext<RequestCache>();
        private int _atomicInt;
        private CacheItemPolicy _policy;

        public RequestCache(int timeout) 
        {
            _policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(timeout) };

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
            return Interlocked.Increment(ref _atomicInt);
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
            int msgSeq = clientPacket.GetMsgSeq();
            ReplyObject replyObject = (ReplyObject)MemoryCache.Default.Get(msgSeq.ToString());

            if (replyObject != null) { 
                replyObject.OnReceive(clientPacket.ToReplyPacket());
            }
            else
            {
                _log.Error($"{msgSeq},${clientPacket.MsgName()} request is not exist");
            }
        }
    }

}
