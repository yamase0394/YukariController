using NetDiff;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace YukariController
{
    class DiffTcpServer : ServerBase
    {
        private TcpListener tcpListener;

        private int textId;
        private string partial;

        /// <summary>
        /// Androidで音声認識のpartialの差分をとる
        /// </summary>
        public DiffTcpServer(MessageDispatcherSync msgDispatcher) : base(msgDispatcher)
        {
            textId = 0;
            partial = "";

            Task.Run(() =>
            {
                var ipEndPoint = new IPEndPoint(IPAddress.Parse("sora.nov"), 8888);
                Console.WriteLine(ipEndPoint.ToString());
                tcpListener = new TcpListener(ipEndPoint);
                tcpListener.Start();

                while (true)
                {
                    Console.WriteLine("待機中");
                    using (var client = tcpListener.AcceptTcpClient())
                    using (var netStream = client.GetStream())
                    using (var streamReader = new StreamReader(netStream, Encoding.UTF8))
                    {
                        var idStr = streamReader.ReadLine();
                        Console.WriteLine(idStr);
                        var id = int.Parse(idStr);
                        if (textId != id)
                        {
                            partial = "";
                            textId = id;
                        }

                        var receivedText = streamReader.ReadLine();
                        if (receivedText.Length == 0) continue;
                        Console.WriteLine("receive : " + receivedText);
                        var lastAdd = -1;
                        if (partial.Length > 0)
                        {
                            Console.WriteLine("diff");
                            var results = DiffUtil.Diff(partial, receivedText);
                            var resultList = results.ToList();
                            lastAdd = resultList.FindLastIndex(r => r.ToFormatString().StartsWith("=") || r.ToFormatString().StartsWith("-"));
                            var index = 0;
                            var deleteCount = resultList.Where(r => r.ToFormatString().StartsWith("-") && index++ < lastAdd).Count();
                            if (lastAdd == -1 && resultList.Any(r => r.ToFormatString().StartsWith("+")))
                            {
                                //同じ文字がなく追加と削除しかない
                                deleteCount = 0;
                                lastAdd = resultList.FindIndex(r =>
                                {
                                    if (r.ToFormatString().StartsWith("-"))
                                    {
                                        deleteCount++;
                                    }
                                    return r.ToFormatString().StartsWith("+");
                                });
                            }
                            Console.WriteLine("deleteCount:" + deleteCount);
                            lastAdd -= deleteCount;

                            index = 0;
                            resultList.ForEach(r => Console.Write(index++ + ":" + r.ToFormatString() + " "));
                            Console.WriteLine();
                        }

                        var askText = receivedText;
                        Console.WriteLine("lastAdd : " + lastAdd);
                        Console.WriteLine("receivedText.Length : " + receivedText.Length);
                        if (lastAdd != -1 && lastAdd != receivedText.Length - 1)
                        {
                            askText = receivedText.Substring(lastAdd + 1);
                        }
                        else if (partial.Length > 0)
                        {
                            continue;
                        }

                        Console.WriteLine("enqueue : " + askText);
                        EnqueueMessage(new YukariMessage(askText));
                        partial = receivedText;
                    }
                }
            });
        }

        protected override void OnCompleteMessageDispatch(int id, YukariCallback callback)
        {

        }
    }
}
