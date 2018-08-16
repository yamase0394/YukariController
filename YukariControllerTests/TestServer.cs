using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace YukariController
{
    class TestServer : ServerBase
    {
        private ConcurrentDictionary<int, Task<string>> sessionMap;
        private ConcurrentDictionary<int, string> resultMap;

        public TestServer(MessageDispatcherSync msgDispatcher) : base(msgDispatcher)
        {
            sessionMap = new ConcurrentDictionary<int, Task<string>>();
            resultMap = new ConcurrentDictionary<int, string>();
        }

        public async Task<string> EnqueueMessage(YukariCommand command, string text)
        {
            Console.WriteLine("Enqueue");
            if (command == YukariCommand.Stop)
            {
                var callback = await InterruptMessage(new YukariMessage(command, text));
                return callback.Msg;
            }
            else
            {
                var id = EnqueueMessage(new YukariMessage(command, text));
                Console.WriteLine(id);
                var task = new Task<string>(() =>
                {
                    return resultMap[id];
                });
                sessionMap.TryAdd(id, task);
                return await task;
            }
        }

        protected override void OnCompleteMessageDispatch(int id, YukariCallback callback)
        {
            Console.WriteLine("complete");
            resultMap.TryAdd(id, callback.Msg);
            sessionMap[id].Start();
        }
    }
}