using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukariController
{
    public class LocalServer : ServerBase
    {
        private ConcurrentDictionary<int, Task<string>> sessionMap;
        private ConcurrentDictionary<int, string> resultMap;

        public LocalServer(MessageDispatcherSync msgDispatcher) : base(msgDispatcher)
        {
            sessionMap = new ConcurrentDictionary<int, Task<string>>();
            resultMap = new ConcurrentDictionary<int, string>();
        }

        public async Task<string> EnqueueMessage(YukariManager.Command command, string text)
        {
            Console.WriteLine("Enqueue");
            var req = new YukariRequest();
            req.Command = command;
            req.Text = text;

            Func<YukariCallback, Task<string>> onInterruptedMessage = async (callback) => callback.Msg;

            Func<int, Task<string>> onEnquedMessage = async (id) =>
            {
                var task = new Task<string>(() =>
                {
                    return resultMap[id];
                });
                sessionMap.TryAdd(id, task);
                return await task;
            };

           return await HandleRequest(req, onEnquedMessage, onInterruptedMessage);
        }

        protected override void OnCompleteMessageDispatch(int id, YukariCallback callback)
        {
            Console.WriteLine("complete");
            resultMap.TryAdd(id, callback.Msg);
            sessionMap[id].Start();
        }
    }
}
