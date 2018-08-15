﻿using System;
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
        private IdManager idManager;

        public MessageDispatcherSync()
        {
            msgQueue = new Queue<Message>();
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
            var id = idManager.GetId();
            msgQueue.Enqueue(new Message(msg, handler, id));
            return id;
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
                var id = r.Next(100);
                lock (syncRoot)
                {
                    while (idSet.Contains(id))
                    {
                        id = r.Next(100);
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
