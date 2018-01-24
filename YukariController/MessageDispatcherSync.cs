using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YukariController
{
    delegate YukariCallback MessageDispatchEventHandler(YukariMessage command);

    class MessageDispatcherSync
    {
       public  event MessageDispatchEventHandler OnDispatchEvent;

        private Task loopTask;
        private CancellationTokenSource tokenSource;
        private Queue<YukariMessage> msgQueue;
        private Queue<Action<YukariCallback>> callbackQueue;

        public MessageDispatcherSync()
        {
            msgQueue = new Queue<YukariMessage>();
            callbackQueue = new Queue<Action<YukariCallback>>();
        }

        public void StartLoop()
        {
            tokenSource = new CancellationTokenSource();
            loopTask = Task.Run(() => EventLoop(tokenSource.Token), tokenSource.Token);
        }

        public void StopLoop()
        {
            if (tokenSource == null) return;

            try
            {
                tokenSource.Cancel();
                loopTask.Wait();
            }
            catch (AggregateException)
            {
                Console.WriteLine("stop loop");
            }
        }

        public void EnqueueMessage(YukariMessage msg, Action<YukariCallback> handler)
        {
            msgQueue.Enqueue(msg);
            callbackQueue.Enqueue(handler);
        }

        private void EventLoop(CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                if (msgQueue.Count == 0)
                {
                    Thread.Sleep(20);
                    continue;
                }

                var callback = OnDispatchEvent(msgQueue.Dequeue());
                callbackQueue.Dequeue().Invoke(callback);
            }
        }
    }
}
