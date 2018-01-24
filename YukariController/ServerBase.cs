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

        protected void  EnqueueMessage(YukariMessage msg)
        {
            msgDispatcher.EnqueueMessage(msg, OnCompleteMessageDispatch);
        }

        protected abstract void OnCompleteMessageDispatch(YukariCallback callback);
    }
}
