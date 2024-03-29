﻿using System;
using System.Threading;
using System.Threading.Tasks;
using xLog.Widgets.Prompts;

namespace xLog.Widgets
{
    public class Widget_Countdown : ConsoleWidget, IDisposable
    {
        #region Properties
        private DateTime TargetTime;
        private TimeSpan UpdateRate = TimeSpan.FromSeconds(1);
        private Task Updater = null;
        private CancellationTokenSource UpdaterCancel = null;
        private string Title = null;
        #endregion

        public Widget_Countdown(DateTime EndTime, string title = null) : base(ConsoleWidgetType.Timer)
        {
            TargetTime = EndTime;
            Title = title;
            Update();
            Start_Updater();
        }

        public Widget_Countdown(TimeSpan Duration, string title = null) : base(ConsoleWidgetType.Timer)
        {
            TargetTime = DateTime.UtcNow.Add(Duration);
            Title = title;
            Update();
            Start_Updater();
        }

        public override void Dispose()
        {
            base.Dispose();
            Stop_Updater();
        }

        private void Start_Updater()
        {
            Stop_Updater();// Safety
            UpdaterCancel = new CancellationTokenSource();
            Updater = Task.Factory.StartNew((start) =>
            {
                CancellationToken Cancel = (CancellationToken)start;

                // Get a zero time which is the next whole second 2 seconds in the future
                DateTime ZeroTime = DateTime.Now.Add(TimeSpan.FromSeconds(1).Subtract(TimeSpan.FromMilliseconds(DateTime.Now.Millisecond)));
                // Time until next whole second
                TimeSpan Delta = ZeroTime.Subtract(DateTime.Now);
                // Wait for it
                Task.Delay(Delta).Wait();

                // Start updates
                while (!Cancel.IsCancellationRequested)
                {
                    if (DateTime.UtcNow > TargetTime) break;

                    Task.Delay(UpdateRate).Wait();
                    Update();
                }

            }, UpdaterCancel.Token);
        }

        public Task Get_Task()
        {
            return Updater;
        }

        private void Stop_Updater()
        {
            if (Updater != null)
            {
                UpdaterCancel.Cancel();
                Updater.Wait();// Wait for the updater thread to die

                UpdaterCancel.Dispose();
                Updater.Dispose();

                UpdaterCancel = null;
                Updater = null;
            }
        }

        private void Update()
        {
            Buffer.Clear();

            TimeSpan ETA = TargetTime.Subtract(DateTime.UtcNow);
            if (!string.IsNullOrEmpty(Title)) Buffer.Append(string.Concat(Title, ": "));
            Buffer.Append(ETA.ToHumanString());
            Line.Set(Buffer.ToString());
        }

        public static void Test()
        {
            Widget_Countdown widget = new Widget_Countdown(TimeSpan.FromSeconds(20), ANSI.CyanBright("Countdown"));

            Task.WaitAll(widget.Get_Task());

            using (Widget_Prompt prompt = new Widget_Prompt("Press ANY key to exit."))
            {
                prompt.Wait();
            }
            Environment.Exit(0);
        }
    }
}