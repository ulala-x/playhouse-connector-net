using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;

namespace playhouse_connector_net.network
{
    public class AsyncManager
    {

        private readonly ConcurrentQueue<Action> _mainThreadActions  = new();
        private bool _isClose;

        public void AddJob(Action action)
        {
            _mainThreadActions.Enqueue(action);
        }


        public IEnumerator MainThreadActionCoroutine()
        {
            while (!_isClose)
            {
                Action action;
                if (_mainThreadActions.TryDequeue(out action))
                {   
                    action.Invoke();
                }
                yield return null;
            }
        }

        public void MainThreadAction()
        {


            Thread thread = new Thread(new ThreadStart(() =>
            {
                while (!_isClose)
                {
                    Action action;
                    if (_mainThreadActions.TryDequeue(out action))
                    {
                        action.Invoke();
                    }
                    Thread.Sleep(10);
                }
            }));
            thread.Name = "AsyncManage Thread";
            thread.Start();

            //RunAync(() =>
            //{
            //    while (!_isClose)
            //    {
            //        Action action;
            //        if (_mainThreadActions.TryDequeue(out action))
            //        {
            //            action.Invoke();
            //        }
            //        Thread.Sleep(50);
            //    }
            //});
        }

        public void Close()
        {
            _isClose = true;
        }

    }
}
