using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace YukariController
{
    public class MqttSubscriber
    {
        private MqttClient client;
        private Voiceroid2 yukari;
        private volatile Queue msgQueue;
        private volatile string preMsg = "";
        private System.Timers.Timer resetPreMsgTimer;
        private const string topicAndroidNotification = "android/notification";
        private const string topicYukariWav = "yukari/wav";
        private const string brokerIp = "192.168.1.97";

        public MqttSubscriber()
        {
            client = new MqttClient(brokerIp);
            var clientId = Guid.NewGuid().ToString();
            Console.WriteLine(clientId);
            client.Connect(clientId, null, null);
            yukari = new Voiceroid2();
            Subscribe(topicAndroidNotification);
            msgQueue = new Queue();
            resetPreMsgTimer = new System.Timers.Timer();
            resetPreMsgTimer.Elapsed += new System.Timers.ElapsedEventHandler(ResetPreMsg);
            resetPreMsgTimer.Interval = 30000;
            Loop();
        }

        private void OnReceive(object sender, MqttMsgPublishEventArgs e)
        {
            Task.Run(() =>
            {
                Console.WriteLine("receoved message from " + e.Topic);
                string msg = Encoding.UTF8.GetString(e.Message);
                Console.WriteLine("enqueue message;" + msg);
                msgQueue.Enqueue(msg);
            });
        }

        public void Subscribe(string topic)
        {
            // callbackを登録
            client.MqttMsgPublishReceived += this.OnReceive;
            client.Subscribe(new string[] { topic }, new byte[] {
            MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        private void Loop()
        {
            Task.Run(() =>
            {
                Console.WriteLine("start receiving message loop");
                while (true)
                {
                    if (msgQueue.Count == 0)
                    {
                        Thread.Sleep(100);
                    }
                    else
                    {
                        var json = (string)msgQueue.Dequeue();
                        Console.WriteLine("dequeue message");

                        if (!json.Equals(preMsg))
                        {
                            var notificationData = JsonConvert.DeserializeObject<NotificationData>(json);
                            Console.WriteLine("title:" + notificationData.Title);
                            Console.WriteLine("text:", notificationData.Text);
                            var msg = notificationData.Title + "\r\n" + notificationData.Text;

                            var dateStr = DateTime.Now.ToString("yyyyMMdd HHmmss");
                            var fileName = dateStr + ".wav";
                            yukari.Save(msg, fileName);
                            File.Copy(@"D:\VOICEROID2\" + fileName, @"\\chtholly.local\home\yukari\" + fileName);
                            fileName = dateStr + ".txt";
                           // File.Copy(@"D:\VOICEROID2\" + fileName, @"\\chtholly.local\home\yukari\" + fileName);
                           using(var writer = new StreamWriter(@"\\chtholly.local\home\yukari\" + fileName, false))
                            {
                                writer.Write(json);
                            }
                            client.Publish(topicYukariWav, Encoding.UTF8.GetBytes(dateStr));
                        }
                        else
                        {
                            Console.WriteLine("same with previout message:" + json);
                        }
                        preMsg = json;
                        resetPreMsgTimer.Stop();
                        resetPreMsgTimer.Start();
                    }

                }
                Console.WriteLine("terminate receiving message loop");
            });
        }

        private void ResetPreMsg(object sender, EventArgs args)
        {
            preMsg = "";
            Console.WriteLine("reset preMessage");
            resetPreMsgTimer.Stop();
        }
    }
}
