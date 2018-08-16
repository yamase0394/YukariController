using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace YukariController
{
    class Logger
    {
        public static void Log(string msg)
        {
            StackFrame sf1 = new StackFrame(1, true);
            MethodBase mb1 = sf1.GetMethod();
            Console.WriteLine(
                DateTime.Now.ToString("HH:mm:ss.fff")
                 + " D/" + sf1.GetMethod().ToString() + ": "
                 + Path.GetFileName(sf1.GetFileName()) + ":"
                 + sf1.GetFileLineNumber() + "行目 "
                 + msg);

            // ※ デバッグ情報が無く、行番号を取得できないなら
            // string address = sf2.GetMethod().ToString() 
            //      + "@ IL_" + sf2.GetILOffset().ToString("X4");
            // Debug.WriteLine(address);
        }
    }
}
