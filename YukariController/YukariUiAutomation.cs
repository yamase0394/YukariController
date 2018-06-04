
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
    /// <summary>
    /// CodeerでなくUiAutomationで結月ゆかりを操作する。
    /// ウィンドウのフォーカスを奪ってしまうため開発中止。
    /// </summary>
    public class YukariUiAutomation
    {
        private const int SW_HIDE = 0;
        private const int SW_MINIMIZE = 6;
        private const int SW_SHOWMINNOACTIVE = 7;

        [DllImport("User32")]
        private static extern int ShowWindow(int hwnd, int nCmdShow);

        private IntPtr mainWindowHandle;

        private AutomationElement mainForm;
        private ValuePattern speechTextBoxValue;
        private InvokePattern playBtnInvokePattern;
        private InvokePattern stopBtnInvokePattern;
        private InvokePattern headBtnInvokePattern;
        private InvokePattern saveBtnInvokePattern;
        private AutomationElement saveBtn;
        private AutomationElement headBtn;

        public YukariUiAutomation()
        {
            Process process = null;

            var processList = Process.GetProcessesByName("VoiceroidEditor");
            //VOICEROID2が起動していない
            if (processList.Length == 0)
            {
                process = Process.Start(@"C:\Program Files (x86)\AHS\VOICEROID2\VoiceroidEditor.exe");
                process.WaitForInputIdle(10000);
                while (process.MainWindowTitle != "MainWindow" || process.MainWindowHandle == IntPtr.Zero)
                {
                    process.Refresh();
                    Thread.Sleep(100);
                }
                Logger.Log(process.MainWindowTitle);
                //process.MainWindowHandleがnullにならないよう少し待つ
                Thread.Sleep(100);
            }
            else
            {
                process = processList[0];
            }
            mainWindowHandle = process.MainWindowHandle;

            mainForm = AutomationElement.FromHandle(process.MainWindowHandle);

            var textEditView = mainForm.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, "c"));
            var textBox = textEditView.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, "TextBox"));
            speechTextBoxValue = textBox.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
            var buttons = textEditView.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ClassNameProperty, "Button")).Cast<AutomationElement>();
            buttons.ToList<AutomationElement>().ForEach(btn =>
            {
                var btnText = btn.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ClassNameProperty, "TextBlock"));
                switch (btnText.Current.Name)
                {
                    case "再生":
                        playBtnInvokePattern = btn.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                        break;
                    case "停止":
                        stopBtnInvokePattern = btn.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                        break;
                    case "先頭":
                        headBtn = btn;
                        headBtnInvokePattern = headBtn.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                        break;
                    case "音声保存":
                        saveBtn = btn;
                        saveBtnInvokePattern = saveBtn.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                        break;
                }
            });
        }

        public async Task Play(String msg)
        {
            await Task.Run(async () =>
             {
                 try
                 {
                    ShowWindow(mainWindowHandle.ToInt32(), SW_HIDE);

                     stopBtnInvokePattern.Invoke();

                     while (!saveBtn.Current.IsEnabled) await Task.Delay(10);

                     try
                     {
                         Logger.Log("not read only");
                         speechTextBoxValue.SetValue(msg);
                     }
                     catch (Exception)
                     {
                         Logger.Log("read only");

                         //ここにきたことはない
                         //textBoxが読み取り専用になっているのを解除する
                         while (!headBtn.Current.IsEnabled) await Task.Delay(1);
                         headBtnInvokePattern.Invoke();
                         await Task.Delay(20);

                         playBtnInvokePattern.Invoke();
                         while (!saveBtn.Current.IsEnabled) await Task.Delay(1);

                         stopBtnInvokePattern.Invoke();
                         await Task.Delay(20);
                         while (!saveBtn.Current.IsEnabled) await Task.Delay(1);

                         speechTextBoxValue.SetValue(msg);
                     }

                     headBtnInvokePattern.Invoke();
                     playBtnInvokePattern.Invoke();

                     //再生が始まり音声保存ボタンが非アクティブ化するのを待つ
                     await Task.Delay(1000);
                     //音声保存の状態で再生中か確認
                     //なぜか一回目はFalseにならない
                     while (!saveBtn.Current.IsEnabled) await Task.Delay(100);

                     Logger.Log("finish playing message");
                 }
                 finally
                 {
                    ShowWindow(mainWindowHandle.ToInt32(), SW_MINIMIZE);
                 }
             });
        }

        public async Task Save(string msg, string fileName)
        {
            await Task.Run(async () =>
            {
                try
                {
                    Logger.Log("Yukari.Save()");

                    // VOICEROID2のウィンドウを現在のデスクトップに移動、隠す
                    ShowWindow(mainWindowHandle.ToInt32(), SW_HIDE);

                    //再生完了待機
                    Logger.Log("wait finish playing");
                    while (!saveBtn.Current.IsEnabled)
                    {
                        await Task.Delay(10);
                    }
                    Logger.Log("fin waiting");

                    try
                    {
                        Logger.Log("speechTextBox is not read only");
                        speechTextBoxValue.SetValue(msg);
                    }
                    catch (Exception)
                    {
                        //ここに行ったことない...
                        Logger.Log("speechTextBox is read only");

                        //textBoxが読み取り専用になっているのを解除する
                        while (!headBtn.Current.IsEnabled) await Task.Delay(1);
                        headBtnInvokePattern.Invoke();
                        await Task.Delay(20);

                        playBtnInvokePattern.Invoke();
                        await Task.Delay(20);
                        while (!saveBtn.Current.IsEnabled) await Task.Delay(1);

                        stopBtnInvokePattern.Invoke();
                        while (!saveBtn.Current.IsEnabled) await Task.Delay(1);

                        speechTextBoxValue.SetValue(msg);
                    }

                    headBtnInvokePattern.Invoke();
                    saveBtnInvokePattern.Invoke();

                    //SW_HIDEだとダイアログがRootElementの子になるのでSW_MINIMIZEにする
                    ShowWindow(mainWindowHandle.ToInt32(), SW_MINIMIZE);
                    Logger.Log("find saveFileDialog");
                    var saveFileDialogCondition = new PropertyCondition(AutomationElement.NameProperty, "名前を付けて保存");
                    var saveFileDialog = mainForm.FindFirst(TreeScope.Children, saveFileDialogCondition);
                    while (saveFileDialog == null)
                    {
                        saveFileDialog = mainForm.FindFirst(TreeScope.Children, saveFileDialogCondition);
                        await Task.Delay(10);
                    }
                    Logger.Log("discover saveFileDialog");

                    var fileNameTextBoxCondition = new AndCondition(
                        new PropertyCondition(AutomationElement.NameProperty, "ファイル名:"),
                        new PropertyCondition(AutomationElement.ClassNameProperty, "Edit"));
                    var fileNameTextBox = saveFileDialog.FindFirst(TreeScope.Descendants, fileNameTextBoxCondition);
                    var fileNameTextBoxValue = fileNameTextBox.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
                    fileNameTextBoxValue.SetValue(fileName);

                    var saveFileBtn = saveFileDialog.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "保存(S)"));

                    //クリック連打対策
                    ShowWindow(saveFileDialog.Current.NativeWindowHandle, SW_HIDE);

                    var saveFileBtnInvokePattern = saveFileBtn.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                    saveFileBtnInvokePattern.Invoke();

                    //情報ダイアログの親ウィンドウ
                    Logger.Log("find save sound window");
                    var saveSoundWindowCondition = new PropertyCondition(AutomationElement.NameProperty, "音声保存");
                    var saveSoundWindow = mainForm.FindFirst(TreeScope.Children, saveSoundWindowCondition);
                    while (saveSoundWindow == null)
                    {
                        saveSoundWindow = mainForm.FindFirst(TreeScope.Children, saveSoundWindowCondition);
                        await Task.Delay(10);
                    }
                    Logger.Log("discovered save sound window");

                    Logger.Log("find info dialog");
                    var infoDialogCondition = new PropertyCondition(AutomationElement.NameProperty, "情報");
                    var infoDialog = saveSoundWindow.FindFirst(TreeScope.Children, infoDialogCondition);
                    while (infoDialog == null)
                    {
                        infoDialog = saveSoundWindow.FindFirst(TreeScope.Children, infoDialogCondition);
                        await Task.Delay(10);

                        //クリック連打すると情報ダイアログがルートの子になる
                        if (infoDialog == null)
                        {
                            infoDialog = AutomationElement.RootElement.FindFirst(TreeScope.Children, infoDialogCondition);
                        }
                    }
                    Logger.Log("discovered info dialog");
                    var okBtn = infoDialog.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "OK"));
                    var okBtnInvokePattern = okBtn.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                    while (true)
                    {
                        okBtnInvokePattern.Invoke();
                        await Task.Delay(10);
                    }

                    Logger.Log("complete saving");
                }
                catch (Exception e)
                {
                    Logger.Log(e.ToString());
                }
                finally
                {
                    //再生、保存ボタンがときどきアクティブに戻らない
                    //クリック連打時？
                    ShowWindow(mainWindowHandle.ToInt32(), 1);
                    await Task.Delay(10);
                    stopBtnInvokePattern.Invoke();
                    ShowWindow(mainWindowHandle.ToInt32(), SW_MINIMIZE);
                }
            });
        }
    }
}
