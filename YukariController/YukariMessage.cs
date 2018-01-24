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

        public YukariMessage(string msg)
        {
            this.Command = YukariCommand.Play;
            this.Msg = msg;
        }
    }
}
