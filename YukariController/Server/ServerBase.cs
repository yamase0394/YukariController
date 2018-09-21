using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukariController
{
    public abstract class ServerBase
    {
        private MessageDispatcherSync msgDispatcher;

        protected ServerBase(MessageDispatcherSync msgDispatcher)
        {
            this.msgDispatcher = msgDispatcher;
        }

        protected async void HandleRequest(YukariRequest req, Action<int> onEnquedMessage, Action<YukariCallback> onInterruptedMessage)
        {
            switch (req.Command)
            {
                case YukariManager.Command.Stop:
                case YukariManager.Command.Pause:
                case YukariManager.Command.Unpause:
                    var callback = await InterruptMessage(new YukariMessage(req.Command, req.Text));
                    onInterruptedMessage(callback);
                    break;
                default:
                    var id = EnqueueMessage(new YukariMessage(req.Command, req.Text));
                    onEnquedMessage(id);
                    break;
            }
        }

        protected async Task<T> HandleRequest<T>(YukariRequest req, Func<int, Task<T>> onEnquedMessage, Func<YukariCallback, Task<T>> onInterruptedMessage)
        {
            switch (req.Command)
            {
                case YukariManager.Command.Stop:
                case YukariManager.Command.Pause:
                case YukariManager.Command.Unpause:
                    var callback = await InterruptMessage(new YukariMessage(req.Command, req.Text));
                    return await onInterruptedMessage(callback);
                default:
                    var id = EnqueueMessage(new YukariMessage(req.Command, req.Text));
                    return await onEnquedMessage(id);
            }
        }


        private int EnqueueMessage(YukariMessage msg)
        {
            Logger.Log("");
            return msgDispatcher.EnqueueMessage(msg, OnCompleteMessageDispatch);
        }

        private async Task<YukariCallback> InterruptMessage(YukariMessage msg)
        {
            Logger.Log("");
            return await msgDispatcher.InterruptMessage(msg);
        }

        /// <summary>
        /// ゆかりさんの処理が終了したときに呼ばれる
        /// </summary>
        protected abstract void OnCompleteMessageDispatch(int id, YukariCallback callback);
    }
}
