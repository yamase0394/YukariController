using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukariController
{
    class YukariManager
    {
        private const string SavePath = "";

        private Yukari yukari;
        private MessageDispatcherSync msgDispatcher;

        public YukariManager()
        {
            yukari = new Yukari();

            msgDispatcher = new MessageDispatcherSync();
            msgDispatcher.OnDispatchEvent += OnDispatchEvent;
            msgDispatcher.StartLoop();

            var tcpServer = new TcpServer(msgDispatcher);
        }

        private YukariCallback OnDispatchEvent(YukariMessage msg)
        {
            switch (msg.Command)
            {
                case YukariCommand.Play:
                    yukari.Play(msg.Msg);
                    return new YukariCallback(msg.Command, msg.Msg);
                case YukariCommand.Save:
                    var dateStr = DateTime.Now.ToString("yyyyMMdd HHmmss");
                    var fileName = dateStr + ".wav";
                    yukari.Save(msg.Msg, fileName);
                    return new YukariCallback(msg.Command, msg.Msg, SavePath + fileName);
                default:
                    throw new ArgumentException(msg.Command.ToString());
            }
        }
    }
}
