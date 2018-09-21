using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukariController
{
    public class YukariCallback
    {
        public YukariManager.Command Command { get; }
        public string Msg { get; }
        public string Filepath { get; }

        public YukariCallback(YukariManager.Command command, string msg)
        {
            Command = command;
            Msg = msg;
        }

        public YukariCallback(YukariManager.Command command, string msg, string filepath) :this(command, msg)
        {
            Filepath = filepath;
        }
    }
}
