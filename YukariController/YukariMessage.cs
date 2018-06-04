using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukariController
{
    public class YukariMessage
    {
        public YukariCommand Command { get; }
        public string Msg { get; }

        public YukariMessage(string msg) : this(YukariCommand.Play, msg) { }

        public YukariMessage(YukariCommand command, string msg)
        {
            this.Command = command;
            this.Msg = msg;
        }
    }
}
