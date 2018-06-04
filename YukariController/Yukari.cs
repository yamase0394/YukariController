
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
using System.Windows.Automation;
using System.Linq;
using System.Text;

namespace YukariController
{
    public class Yukari
    {
        private const int SW_HIDE = 0;
        private const int SW_MINIMIZE = 6;
        private const int SW_SHOWMINNOACTIVE = 7;

        [DllImport("User32")]
        private static extern int ShowWindow(int hwnd, int nCmdShow);

        private IntPtr mainWindowHandle;

        private WindowsAppFriend _app;
        private WindowControl uiTreeTop;
        private WPFTextBox talkTextBox;
        private WPFButtonBase playButton;
        private WPFButtonBase saveButton;

        public Yukari()
        {
            Process process = null;

            var processList = Process.GetProcessesByName("VoiceroidEditor");
            //VOICEROID2が起動していない
            if (processList.Length == 0)
            {
                Process.Start(@"C:\Program Files (x86)\AHS\VOICEROID2\VoiceroidEditor.exe");
                while (processList.Length == 0 || processList[0].MainWindowTitle != "VOICEROID2" || processList[0].MainWindowHandle == IntPtr.Zero)
                {
                    processList = Process.GetProcessesByName("VoiceroidEditor");
                    Thread.Sleep(500);
                }
                process = processList[0];
            }
            else
            {
                process = processList[0];
            }

            mainWindowHandle = process.MainWindowHandle;

            _app = new WindowsAppFriend(process);
            uiTreeTop = WindowControl.FromZTop(_app);

            var editUis = uiTreeTop.GetFromTypeFullName("AI.Talk.Editor.TextEditView")[0].LogicalTree();
            talkTextBox = new WPFTextBox(editUis[4]);
            playButton = new WPFButtonBase(editUis[6]);
            saveButton = new WPFButtonBase(editUis[24]);
        }

        public async Task Play(String msg)
        {
            await Task.Run(async () =>
             {
                 talkTextBox.EmulateChangeText(msg);

                 if (!saveButton.IsEnabled)
                 {
                     while (!saveButton.IsEnabled)
                     {
                         await Task.Delay(100);
                     }
                 }

                 playButton.EmulateClick();
                 await Task.Delay(1000);

                 if (!saveButton.IsEnabled)
                 {
                     while (!saveButton.IsEnabled)
                     {
                         await Task.Delay(100);
                     }
                 }
             });
        }

        public async Task Save(string msg, string fileName)
        {
            await Task.Run(async () =>
            {
                try
                {
                    //フォーカスしないようにする
                    //ShowWindow(mainWindowHandle.ToInt32(), SW_HIDE);
                    await Task.Delay(100);

                    //再生完了待機
                    while (!saveButton.IsEnabled)
                    {
                        Console.WriteLine("saveBtn is not enabled");
                        await Task.Delay(100);
                    }

                    talkTextBox.EmulateChangeText(msg);

                    var async = new Async();
                    saveButton.EmulateClick(async);
                    //名前を付けて保存ダイアログ
                    var saveFileWindow = uiTreeTop.WaitForNextModal();
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
                    var progressWindow = uiTreeTop.WaitForNextModal();
                    if (progressWindow == null)
                    {
                        progressWindow = uiTreeTop.WaitForNextModal();
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
                            var windows = WindowControl.GetTopLevelWindows(uiTreeTop.App);
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
                        var completeWindow = WindowControl.FromZTop(uiTreeTop.App);
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
                    //ShowWindow(mainWindowHandle.ToInt32(), SW_MINIMIZE);
                    Console.WriteLine("complete saving");
                }
            });
        }
    }
}
