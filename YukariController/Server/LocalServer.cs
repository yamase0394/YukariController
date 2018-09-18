using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukariController
{
    class LocalServer : ServerBase
    {
        private ConcurrentDictionary<int, Task<string>> sessionMap;
        private ConcurrentDictionary<int, string> resultMap;

        public LocalServer(MessageDispatcherSync msgDispatcher) : base(msgDispatcher)
        {
            sessionMap = new ConcurrentDictionary<int, Task<string>>();
            resultMap = new ConcurrentDictionary<int, string>();
        }

        public async Task<string> EnqueueMessage(YukariCommand command, string text)
        {
            Console.WriteLine("Enqueue");
            switch (command)
            {
                case YukariCommand.Stop:
                case YukariCommand.Pause:
                case YukariCommand.Unpause:
                    var callback = await InterruptMessage(new YukariMessage(command, text));
                    return callback.Msg;
                default:
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
