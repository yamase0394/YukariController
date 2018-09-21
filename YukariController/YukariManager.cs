using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YukariController
{
    public delegate void PauseStateChangedHandler(bool isPaused);

    public class YukariManager
    {
        public enum Command { Play, Save, Stop, Pause, Unpause }
        public PauseStateChangedHandler OnPauseStateChanged;

        private const int DefaultProcessingId = 0;
        private const string SavePath = "";

        private SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        private int processingId = DefaultProcessingId;
        private Command processingCommand;
        private volatile bool isPaused = false;
        private bool isCanceled = false;
        private bool needsResume = false;

        private Yukari yukari;
        private MessageDispatcherSync msgDispatcher;

        public YukariManager()
        {
            yukari = new Yukari();

            msgDispatcher = new MessageDispatcherSync();
            msgDispatcher.OnDispatchEvent += OnDispatchEvent;
            msgDispatcher.OnInterruptEvent += OnInterruptEvent;
            msgDispatcher.StartLoop();
        }

        public MessageDispatcherSync GetDispatcher()
        {
            return msgDispatcher;
        }

        private async Task<YukariCallback> OnDispatchEvent(int id, YukariMessage msg)
        {
            Logger.Log($"{id}:{msg.Msg}");
            await SemaphoreSlim.WaitAsync();
            try
            {
                processingId = id;
                processingCommand = msg.Command;
            }
            finally
            {
                SemaphoreSlim.Release();
            }

            YukariCallback callback;
            var res = "ok";
            switch (msg.Command)
            {
                case Command.Play:
                    while (isPaused && !isCanceled)
                    {
                        await Task.Delay(100);
                    }

                    if (!isCanceled) await yukari.Play(msg.Msg);

                    await SemaphoreSlim.WaitAsync();
                    try
                    {
                        if (isCanceled)
                        {
                            res = "canceled";
                        }

                        ResetCurrentEventStatus();
                    }
                    finally
                    {
                        SemaphoreSlim.Release();
                    }

                    return new YukariCallback(msg.Command, res);
                case Command.Save:
                    var dateStr = DateTime.Now.ToString("yyyyMMdd HHmmss");
                    var fileName = dateStr + ".wav";
                    await yukari.Save(msg.Msg, fileName);
                    callback = new YukariCallback(msg.Command, res, SavePath + fileName);
                    break;
                default:
                    throw new ArgumentException(msg.Command.ToString());
            }

            ResetCurrentEventStatus();
            return callback;
        }

        private void ResetCurrentEventStatus()
        {
            isCanceled = false;
            processingId = DefaultProcessingId;
        }

        private async Task<YukariCallback> OnInterruptEvent(YukariMessage msg)
        {
            switch (msg.Command)
            {
                case Command.Stop:
                    await SemaphoreSlim.WaitAsync();
                    try
                    {
                        if (int.TryParse(msg.Msg, out int id) && processingId != id)
                        {
                            return new YukariCallback(msg.Command, $"Designated Id:{id} is Not Found");
                        }

                        if (processingId == 0 || processingCommand != Command.Play)
                            return new YukariCallback(msg.Command, "Not Playing");

                        if (needsResume) needsResume = false;

                        isCanceled = true;
                        yukari.Stop();
                        return new YukariCallback(msg.Command, $"Stop Id={processingId}");
                    }
                    finally
                    {
                        SemaphoreSlim.Release();
                    }
                case Command.Pause:
                    await SemaphoreSlim.WaitAsync();
                    try
                    {
                        if (isPaused)
                        {
                            return new YukariCallback(msg.Command, "failed");
                        }

                        isPaused = true;
                        OnPauseStateChanged(isPaused);

                        if (processingId != 0 && processingCommand == Command.Play)
                        {
                            yukari.Pause();
                            needsResume = true;
                        }

                        return new YukariCallback(msg.Command, "ok");
                    }
                    finally
                    {
                        SemaphoreSlim.Release();
                    }
                case Command.Unpause:
                    if (!isPaused)
                    {
                        return new YukariCallback(msg.Command, "failed");
                    }

                    isPaused = false;
                    OnPauseStateChanged(isPaused);

                    await SemaphoreSlim.WaitAsync();
                    try
                    {
                        if (needsResume)
                        {
                            yukari.Pause();
                            needsResume = false;
                        }
                    }
                    finally
                    {
                        SemaphoreSlim.Release();
                    }
                    return new YukariCallback(msg.Command, "ok");
                default:
                    throw new ArgumentException(msg.Command.ToString());
            }
        }
    }
}
