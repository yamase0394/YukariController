using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace YukariController
{
    public delegate void WriteLogDelegate(string text);

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public WriteLogDelegate writeLogDelegate;
        private LocalServer localServer;

        public MainWindow()
        {
            InitializeComponent();

            writeLogDelegate = WriteLog;
            Logger.Init(writeLogDelegate);

            Loaded += async (sender, e) =>
            {
                Logger.Log("準備中");
                await Task.Run(() =>
                {
                    var yukariManager = new YukariManager();
                    yukariManager.OnPauseStateChanged += OnPauseStateChanged;
                    var tcpServer = new TcpServer(yukariManager.GetDispatcher());
                    localServer = new LocalServer(yukariManager.GetDispatcher());
                });
                Logger.Log("準備完了");
            };
        }

        private void WriteLog(string text)
        {
            this.Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText(text);

                var maxLine = 50;
                if (LogTextBox.LineCount > maxLine)
                {
                    var sb = new StringBuilder();
                    for (int i = 1; i < maxLine; i++)
                    {
                        sb.Append(LogTextBox.GetLineText(i));
                    }

                    LogTextBox.Text = sb.ToString();
                }

                LogTextBox.Focus();
                LogTextBox.CaretIndex = LogTextBox.Text.Length;
                LogTextBox.ScrollToEnd();
            });
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            if (isPaused)
            {
                this.Dispatcher.Invoke(() => { ButtonPause.Content = "再開"; });
            }
            else
            {
                this.Dispatcher.Invoke(() => { ButtonPause.Content = "一時停止"; });
            }
        }

        private async void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            string result;
            var button = (Button)sender;
            switch ((string)button.Content)
            {
                case "一時停止":
                    result = await localServer.EnqueueMessage(YukariCommand.Pause, "");
                    break;
                case "再開":
                    result = await localServer.EnqueueMessage(YukariCommand.Unpause, "");
                    break;
                default:
                    throw new Exception((string)button.Content);
            }
            Logger.Log(result);
        }

        private async void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            var result = await localServer.EnqueueMessage(YukariCommand.Stop, "");
            Logger.Log(result);
        }
    }
}
