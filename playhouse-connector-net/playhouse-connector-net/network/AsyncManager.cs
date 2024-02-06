using System;
using System.Collections;
using System.Collections.Concurrent;

// ReSharper disable once CheckNamespace
namespace PlayHouseConnector.Network
{
    public class AsyncManager
    {
        private readonly ConcurrentQueue<Action> _mainThreadActions  = new();
        public void AddJob(Action action)
        {
            _mainThreadActions.Enqueue(action);
        }
        public IEnumerator MainCoroutineAction()
        {
            while (_mainThreadActions.TryDequeue(out var action))
            {   
                action.Invoke();
            }
            yield return null;
        }

        public void MainThreadAction()
        {
            while (_mainThreadActions.TryDequeue(out var action))
            {
                action.Invoke();
            }
            //Thread.Sleep(10);
        }

        internal void Clear()
        {
            _mainThreadActions.Clear();
        }
    }
}
