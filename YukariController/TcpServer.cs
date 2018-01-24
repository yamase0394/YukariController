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
    class TcpServer : ServerBase
    {
        private TcpListener tcpListener;

        public TcpServer(MessageDispatcherSync msgDispatcher) : base(msgDispatcher)
        {
            Task.Run(() =>
            {
                var ipEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.3"), 8888);
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
                        EnqueueMessage(new YukariMessage(streamReader.ReadLine()));
                    }
                }
            });
        }

        protected override void OnCompleteMessageDispatch(YukariCallback callback)
        {
            
        }

        public static string GetIPAddress()
        {
            var ipaddress = "";
            IPHostEntry ipentry = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in ipentry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipaddress = ip.ToString();
                    break;
                }
            }
            return ipaddress;
        }
    }
}
