using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace YukariController
{
    class MqttServer : ServerBase
    {
        private const string TopicAndroidNotification = "android/notification";
        private const string TopicYukariWav = "yukari/wav";
        private const string BrokerIp = "192.168.1.97";

        private MqttClient client;

        MqttServer(MessageDispatcherSync msgDispatcher) : base(msgDispatcher)
        {
            client = new MqttClient(BrokerIp);
            var clientId = Guid.NewGuid().ToString();
            Console.WriteLine(clientId);
            client.Connect(clientId, null, null);
            Subscribe(TopicAndroidNotification);
        }

        private void Subscribe(string topic)
        {
            // callbackを登録
            client.MqttMsgPublishReceived += this.OnReceive;
            client.Subscribe(new string[] { topic }, new byte[] {
            MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        private void OnReceive(object sender, MqttMsgPublishEventArgs e)
        {
            Console.WriteLine("receoved message from " + e.Topic);
            var notificationData = JsonConvert.DeserializeObject<NotificationData>(Encoding.UTF8.GetString(e.Message));
            var msg = notificationData.Title + "\r\n" + notificationData.Text;
            Console.WriteLine("enqueue message;" + msg);
            EnqueueMessage(new YukariMessage(msg));
        }

        protected override void OnCompleteMessageDispatch(YukariCallback callback)
        {
            var fileName = Path.GetFileName(callback.Filepath);
            File.Copy(callback.Filepath, @"\\chtholly.local\home\yukari\" + fileName);

            var split = callback.Msg.Replace("\r\n", "\n").Split('\n');
            var notificationData = new NotificationData()
            {
                Title = split[0],
                Text = split[1]
            };
            var json = JsonConvert.SerializeObject(notificationData);
            fileName =  Path.GetFileNameWithoutExtension(fileName) + ".json";
            using (var writer = new StreamWriter(@"\\chtholly.local\home\yukari\" + fileName, false))
            {
                writer.Write(json);
            }
            client.Publish(TopicYukariWav, Encoding.UTF8.GetBytes(callback.Filepath));
        }

        [JsonObject("notification")]
        class NotificationData
        {
            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }
        }
    }
}
