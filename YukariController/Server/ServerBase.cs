using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukariController
{
    abstract class ServerBase
    {
        private MessageDispatcherSync msgDispatcher;

        protected ServerBase(MessageDispatcherSync msgDispatcher)
        {
            this.msgDispatcher = msgDispatcher;
        }

        protected int  EnqueueMessage(YukariMessage msg)
        {
            Console.WriteLine("ServerBase:EnqueMessage");
            return msgDispatcher.EnqueueMessage(msg, OnCompleteMessageDispatch);
        }

        /// <summary>
        /// ゆかりさんの処理が終了したときに呼ばれる
        /// </summary>
        protected abstract void OnCompleteMessageDispatch(int id, YukariCallback callback);
    }
}
