using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukariController
{
    public class YukariMessage
    {
        public YukariManager.Command Command { get; }
        public string Msg { get; }

        public YukariMessage(string msg) : this(YukariManager.Command.Play, msg) { }

        public YukariMessage(YukariManager.Command command, string msg)
        {
            this.Command = command;
            this.Msg = msg;
        }
    }
}
