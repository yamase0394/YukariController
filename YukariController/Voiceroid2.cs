
using Codeer.Friendly.Windows;
using System;
using System.Diagnostics;
using RM.Friendly.WPFStandardControls;
using Codeer.Friendly;
using System.Windows.Controls;
using Codeer.Friendly.Windows.Grasp;
using Codeer.Friendly.Windows.NativeStandardControls;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace YukariController
{
    public class Voiceroid2
    {
        private const int SW_HIDE = 0;
        //private const int SW_NORMAL = 1;
        //private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_MINIMIZE = 6;
        //private const int SW_SHOWNA = 8;

        [DllImport("User32")]
        private static extern int ShowWindow(int hwnd, int nCmdShow);

        private WindowControl mainWindow = null;
        private WPFTextBox tb = null;
        private WPFButtonBase playBtn = null;
        private WPFButtonBase saveBtn = null;

        public Voiceroid2()
        {
            Process process = null;

            var processList = Process.GetProcessesByName("VoiceroidEditor");
            //VOICEROID2が起動していない
            if (processList.Length == 0)
            {
                process = Process.Start(@"C:\Program Files (x86)\AHS\VOICEROID2\VoiceroidEditor.exe");
                process.WaitForInputIdle(30000);

                while (process.MainWindowHandle == IntPtr.Zero)
                {
                    Thread.Sleep(100);
                    process.Refresh();
                }

                //メインウィンドウ起動まで待つ
                while (true)
                {
                    try
                    {
                        mainWindow = WindowControl.FromZTop(new WindowsAppFriend(process));

                        if (mainWindow.TypeFullName.Equals("AI.Talk.Editor.MainWindow"))
                        {
                            break;
                        }
                    }
                    catch (FriendlyOperationException)
                    {
                        processList = Process.GetProcessesByName("VoiceroidEditor");
                        process = processList[0];
                    }
                    Thread.Sleep(100);
                }

            }
            //既に起動している
            else
            {
                process = processList[0];
                mainWindow = WindowControl.FromZTop(new WindowsAppFriend(process));
            }

            tb = new WPFTextBox(mainWindow.LogicalTree().ByType<TextBox>()[0]);

            var btnList = mainWindow.LogicalTree().ByType<Button>();
            var count = btnList.Count;
            for (int i = 0; i < count; i++)
            {
                var btn = new WPFButtonBase(btnList[i]);
                var btnTxtList = btn.LogicalTree(TreeRunDirection.Descendants).ByType<TextBlock>();
                if (btnTxtList.Count == 1)
                {
                    var btnTxt = new WPFTextBlock(btnTxtList.Single());
                    if (playBtn == null && btnTxt.Text.Equals("再生"))
                    {
                        playBtn = new WPFButtonBase(btnList[i]);
                    }
                    else if (saveBtn == null && btnTxt.Text.Equals("音声保存"))
                    {
                        saveBtn = new WPFButtonBase(btnList[i]);
                    }
                }
            }
        }

        //playとsaveを連続して呼ぶとうまくsaveできない
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Play(String msg)
        {
            //再生完了待機
            while (!saveBtn.IsEnabled)
            {
                Thread.Sleep(10);
            }

            tb.EmulateChangeText(msg);

            var async = new Async();
            playBtn.EmulateClick(async);
            //これがないと再生完了待機も飛ばして終了してしまう
            mainWindow.WaitForNextModal(async);

            if (!async.IsCompleted)
            {
                Console.WriteLine("sync is not completed");
                try
                {
                    async.WaitForCompletion();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            //再生完了待機
            int waitSecond = 1;
            while (!saveBtn.IsEnabled)
            {
                Thread.Sleep(1000);
                Console.WriteLine("wating " + waitSecond + " second");
                waitSecond++;
            }

            Console.WriteLine("finish playing message");
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Save(string msg, string fileName)
        {
            Console.WriteLine("start saving");

            try
            {
                //フォーカスしないようにする
                ShowWindow(mainWindow.Handle.ToInt32(), SW_HIDE);
                Thread.Sleep(100);

                //再生完了待機
                while (!saveBtn.IsEnabled)
                {
                    Console.WriteLine("saveBtn is not enabled");
                    Thread.Sleep(100);
                }

                tb.EmulateChangeText(msg);

                var async = new Async();
                saveBtn.EmulateClick(async);
                //名前を付けて保存ダイアログ
                var saveFileWindow = mainWindow.WaitForNextModal();
                var saveFileDialog = new NativeMessageBox(saveFileWindow);

                //ファイル名を入力
                //右上の検索欄にも入力されてしまうが無視
                var edits = saveFileDialog.Window.GetFromWindowClass("Edit");
                foreach (var t in edits)
                {
                    var edit = new NativeEdit(t);
                    edit.EmulateChangeText(fileName);
                }

                saveFileDialog.EmulateButtonClick("保存(&S)");
                //saveFileWindow.WaitForDestroy();

                //出力状況を表示するダイアログの表示を待つ
                Console.WriteLine("waiting for showing progress window");
                var progressWindow = mainWindow.WaitForNextModal();
                if (progressWindow == null)
                {
                    progressWindow = mainWindow.WaitForNextModal();
                }
                Console.WriteLine("showed " + progressWindow.GetWindowText());

                var tokenSource = new CancellationTokenSource();
                var task = new TaskFactory().StartNew(() =>
                {
                    //完了通知ダイアログの表示を待つ
                    Console.WriteLine("wationg for showing saving complete window");
                    var completeWindow = progressWindow.WaitForNextModal();
                    if (completeWindow != null)
                    {
                        Console.WriteLine("showed " + completeWindow.GetWindowText());
                        Console.WriteLine(DateTime.Now);
                        try
                        {
                            var completeDialog = new NativeMessageBox(completeWindow);
                            completeDialog.EmulateButtonClick("OK");
                            Console.WriteLine("wating for destroying");
                            completeWindow.WaitForDestroy();
                            Console.WriteLine("finish");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }, tokenSource.Token);
                try
                {
                    Console.WriteLine(DateTime.Now);
                    if (!task.Wait(5000))
                    {
                        tokenSource.Cancel();
                        Console.WriteLine("timeout");
                        Console.WriteLine(DateTime.Now);
                        var windows = WindowControl.GetTopLevelWindows(mainWindow.App);
                        foreach (var window in windows)
                        {
                            Console.WriteLine(window.GetWindowText());
                            var btnList = window.LogicalTree().ByType<Button>();
                            var count = btnList.Count;
                            for (int i = 0; i < count; i++)
                            {
                                var btn = new WPFButtonBase(btnList[i]);
                                var btnTxtList = btn.LogicalTree(TreeRunDirection.Descendants).ByType<TextBlock>();
                                if (btnTxtList.Count == 1)
                                {
                                    var btnTxt = new WPFTextBlock(btnTxtList.Single());
                                    Console.WriteLine(btnTxt.Text);
                                    if (btnTxt.Text.Equals("キャンセル"))
                                    {
                                        btn.EmulateClick();
                                    }
                                }
                            }
                        }

                        var completeWindow = progressWindow.WaitForNextModal();
                        Console.WriteLine("showed2 " + completeWindow.GetWindowText());
                        Console.WriteLine("2" + DateTime.Now);
                        var completeDialog = new NativeMessageBox(completeWindow);
                        completeDialog.EmulateButtonClick("OK");
                        Console.WriteLine("wating for destroying2");
                        completeWindow.WaitForDestroy();
                        Console.WriteLine("finish2");
                    }
                }
                catch (AggregateException)
                {
                    //タスクがキャンセルされた
                    Console.WriteLine("task was canceled");
                    var completeWindow = WindowControl.FromZTop(mainWindow.App);
                    Console.WriteLine("showed3 " + completeWindow.GetWindowText());
                    var completeDialog = new NativeMessageBox(completeWindow);
                    completeDialog.EmulateButtonClick("OK");
                    Console.WriteLine("wating for destroying3");
                    completeWindow.WaitForDestroy();
                    Console.WriteLine("finish3");
                }

                if (!async.IsCompleted)
                {
                    try
                    {
                        Console.WriteLine("wating for async finish");
                        async.WaitForCompletion();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            finally
            {
                ShowWindow(mainWindow.Handle.ToInt32(), SW_MINIMIZE);
                Console.WriteLine("complete saving");
            }
        }
    }
}
