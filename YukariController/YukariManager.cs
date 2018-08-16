using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace YukariController
{
    public class YukariManager
    {
        private const string SavePath = "";

        private volatile int processingId = 0;
        private volatile YukariCommand processingCommand;
        private volatile bool isCanceled = false;

        private Yukari yukari;
        private MessageDispatcherSync msgDispatcher;

        public YukariManager()
        {
            yukari = new Yukari();

            msgDispatcher = new MessageDispatcherSync();
            msgDispatcher.OnDispatchEvent += OnDispatchEvent;
            msgDispatcher.OnInterruptEvent += OnInterruptEvent;
            msgDispatcher.StartLoop();
        }

        public MessageDispatcherSync GetDispatcher() {
            return msgDispatcher;
        }

        private async Task<YukariCallback> OnDispatchEvent(int id, YukariMessage msg)
        {
            processingId = id;
            processingCommand = msg.Command;
            YukariCallback callback;

            var res = "ok";
            switch (msg.Command)
            {
                case YukariCommand.Play:
                    if (!isCanceled)
                        await yukari.Play(msg.Msg);
      
                    if (isCanceled) res = "canceled";
                    callback = new YukariCallback(msg.Command, res);
                    break;
                case YukariCommand.Save:
                    var dateStr = DateTime.Now.ToString("yyyyMMdd HHmmss");
                    var fileName = dateStr + ".wav";
                    await yukari.Save(msg.Msg, fileName);
                    callback = new YukariCallback(msg.Command, res, SavePath + fileName);
                    break;
                default:
                    throw new ArgumentException(msg.Command.ToString());
            }

            isCanceled = false;
            processingId = 0;
            return callback;
        }

        private async Task<YukariCallback> OnInterruptEvent(YukariMessage msg)
        {
            switch (msg.Command)
            {
                case YukariCommand.Stop:
                    if (int.TryParse(msg.Msg, out int id))
                    {
                        if (processingId != id)
                            return new YukariCallback(msg.Command, $"Designated Id:{id} is Not Found");

                        if (processingCommand != YukariCommand.Play)
                            return new YukariCallback(msg.Command, $"Id:{id} is {processingCommand} Command");
                    }

                    if (processingId == 0 || processingCommand != YukariCommand.Play)
                        return new YukariCallback(msg.Command, "Not Playing");

                    await yukari.Stop();
                    isCanceled = true;
                    return new YukariCallback(msg.Command, $"Stop Id={processingId}");
                default:
                    throw new ArgumentException(msg.Command.ToString());
            }
        }
    }
}
