using Newtonsoft.Json;
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
        private Dictionary<int, TcpClient> sessionMap;

        public TcpServer(MessageDispatcherSync msgDispatcher) : base(msgDispatcher)
        {
            Task.Run(() =>
            {
                var ipEndPoint = new IPEndPoint(IPAddress.Parse(GetIPAddress()), 8888);
                tcpListener = new TcpListener(ipEndPoint);
                tcpListener.Start();

                sessionMap = new Dictionary<int, TcpClient>();

                while (true)
                {
                    var client = tcpListener.AcceptTcpClient();

                    var streamReader = new StreamReader(client.GetStream(), Encoding.UTF8);
                    var req = JsonConvert.DeserializeObject<YukariRequest>(streamReader.ReadLine());

                    var command = (YukariCommand)Enum.Parse(typeof(YukariCommand), req.Command, true);
                    var id = EnqueueMessage(new YukariMessage(command, req.Text));
                    sessionMap.Add(id, client);
                }
            });
        }

        protected override void OnCompleteMessageDispatch(int id, YukariCallback callback)
        {
            using (var client = sessionMap[id])
            using (var netStream = client.GetStream())
            {
                var msg = Encoding.UTF8.GetBytes("ok");
                netStream.Write(msg, 0, msg.Length);
                netStream.Flush();
            }

            sessionMap.Remove(id);
        }

        private string GetIPAddress()
        {
            string ipaddress = "";
            IPHostEntry ipentry = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in ipentry.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipaddress = ip.ToString();
                    break;
                }
            }
            return ipaddress;
        }

        [JsonObject("yukariRequest")]
        class YukariRequest
        {
            [JsonProperty("command")]
            public string Command { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }
        }
    }
}