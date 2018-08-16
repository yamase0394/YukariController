using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukariController
{
    public class YukariCallback
    {
        public YukariCommand Command { get; }
        public string Msg { get; }
        public string Filepath { get; }

        public YukariCallback(YukariCommand command, string msg)
        {
            Command = command;
            Msg = msg;
        }

        public YukariCallback(YukariCommand command, string msg, string filepath) :this(command, msg)
        {
            Filepath = filepath;
        }
    }
}
