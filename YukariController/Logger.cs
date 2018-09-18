using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace YukariController
{
    class Logger
    {
        private static WriteLogDelegate Dispatcher;

        public static void Init(WriteLogDelegate dispatcher)
        {
            Dispatcher = dispatcher;
        }

        public static void Log(
            string msg,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = -1)
        {
            string fileName = System.IO.Path.GetFileName(filePath);
            Dispatcher.Invoke(
                $"{DateTime.Now.ToString("HH:mm:ss.fff")} D/{fileName}:{lineNumber}/{memberName} {msg}{Environment.NewLine}");
        }
    }
}
