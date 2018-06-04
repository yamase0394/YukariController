using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YukariController
{
    delegate Task<YukariCallback> MessageDispatchEventHandler(YukariMessage command);

    class MessageDispatcherSync
    {
        //dequeueされたメッセージを処理するハンドラ
        public event MessageDispatchEventHandler OnDispatchEvent;

        private Task loopTask;
        private CancellationTokenSource tokenSource;

        private Queue<Message> msgQueue;
        private int id = 0;

        public MessageDispatcherSync()
        {
            msgQueue = new Queue<Message>();
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

        public int EnqueueMessage(YukariMessage msg, Action<int, YukariCallback> handler)
        {
            msgQueue.Enqueue(new Message(msg, handler, id));
            return id++;
        }

        private async void EventLoop(CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                if (msgQueue.Count == 0)
                {
                    await Task.Delay(10);
                    continue;
                }

                Logger.Log("dequeue message");
                var msg = msgQueue.Dequeue();
                var callback = await OnDispatchEvent(msg.YukariMsg);
                Logger.Log("dequeue callback");
                msg.Callback.Invoke(msg.Id, callback);
            }
        }

        class Message
        {
            public YukariMessage YukariMsg { get; }
            public Action<int, YukariCallback> Callback { get; }
            public int Id { get; }

           public Message(YukariMessage yukariMsg, Action<int, YukariCallback> callback, int id)
            {
                this.YukariMsg = yukariMsg;
                this.Callback = callback;
                this.Id = id;
            }
        }
    }
}
