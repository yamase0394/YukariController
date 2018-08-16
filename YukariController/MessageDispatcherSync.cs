using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace YukariController
{
    public delegate Task<YukariCallback> MessageDispatchEventHandler(int id, YukariMessage command);
    public delegate Task<YukariCallback> MessageInterruptEventHandler(YukariMessage command);

    public class MessageDispatcherSync
    {
        //dequeueされたメッセージを処理するハンドラ
        public event MessageDispatchEventHandler OnDispatchEvent;
        public event MessageInterruptEventHandler OnInterruptEvent;

        private Object syncLock = new Object();
        private Task loopTask;
        private CancellationTokenSource tokenSource;
        private ConcurrentQueue<Message> msgQueue;
        private IdManager idManager;

        public MessageDispatcherSync()
        {
            msgQueue = new ConcurrentQueue<Message>();
            idManager = new IdManager();
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
            Logger.Log("enqueue message");
            var id = idManager.GetId();
            msgQueue.Enqueue(new Message(msg, handler, id));
            return id;
        }

        public async Task<YukariCallback> InterruptMessage(YukariMessage msg)
        {
            Logger.Log("interrupt message");
            if (int.TryParse(msg.Msg, out int id))
            {
                if (id <= 0 || 100 < id)
                {
                    return new YukariCallback(msg.Command, $"Out of range Id={id}");
                }

                lock (syncLock)
                {
                    if (msgQueue.Any(x => x.Id == id))
                    {
                        msgQueue = new ConcurrentQueue<Message>(msgQueue.Where(x => x.Id != id).ToArray());
                        return new YukariCallback(msg.Command, $"Cancel Id={id}");
                    }
                }
            }
            return await OnInterruptEvent(msg);
        }

        private async void EventLoop(CancellationToken ct)
        {
            while (true)
            {
                await Task.Delay(10);
                ct.ThrowIfCancellationRequested();

                Message msg;
                lock (syncLock)
                {
                    if (msgQueue.Count == 0)
                        continue;

                    Logger.Log("dequeue message");
                    if (!msgQueue.TryDequeue(out msg))
                        continue;
                }

                var callback = await OnDispatchEvent(msg.Id, msg.YukariMsg);
                msg.Callback.Invoke(msg.Id, callback);
                idManager.Release(msg.Id);
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

        private class IdManager
        {
            private Random r;
            private HashSet<int> idSet;
            private readonly object syncRoot = new object();

            public IdManager()
            {
                r = new Random();
                idSet = new HashSet<int>();
            }

            public int GetId()
            {
                var id = r.Next(1, 100);
                lock (syncRoot)
                {
                    while (idSet.Contains(id))
                    {
                        id = r.Next(1, 100);
                    }
                    idSet.Add(id);
                }
                return id;
            }

            public void Release(int id)
            {
                lock (syncRoot)
                    idSet.Remove(id);
            }
        }
    }
}
