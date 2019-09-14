using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace xLog.VirtualTerminal
{
    public enum KeyState { Up, Down }
    public delegate void ConsoleKeyPressHandler(ConsoleKeyInfo Key, KeyState State);

    public static class ConsoleInput
    {
        private struct InputHandlers
        {
            public ConsoleKeyPressHandler KeyHandler;
        };

        #region Properties
        /// <summary>
        /// List of current observers
        /// </summary>
        static Dictionary<Guid, InputHandlers> Observers = new Dictionary<Guid, InputHandlers>();
        /// <summary>
        /// Task that runs the polling engine
        /// </summary>
        static Task PollingTask = null;
        /// <summary>
        /// Used to cancel and stop the polling engine 
        /// </summary>
        static CancellationTokenSource CancelSource;
        #endregion

        #region Events
        static event ConsoleKeyPressHandler onKeyPress;
        #endregion

        #region Constructors
        static ConsoleInput()
        {
        }
        #endregion

        #region Polling
        private static void Run_Input_Polling()
        {
            try
            {
                Console.CancelKeyPress += Console_CancelKeyPress;
                Console.TreatControlCAsInput = true;

                while (!CancelSource.IsCancellationRequested)
                {
                    if (!Console.KeyAvailable)
                    {
                        if (!SpinWait.SpinUntil(() => Console.KeyAvailable || CancelSource.IsCancellationRequested, 10))
                        {/* Condition not satisfied, continue waiting */
                            continue;
                        }

                        if (CancelSource.IsCancellationRequested)
                            break;
                    }

                    if (!Console.KeyAvailable)
                    {
                        continue;
                    }

                    ConsoleKeyInfo key = Console.ReadKey(true);

                    if (key == null)
                    {
                        throw new ArgumentNullException("Console.ReadKey() returned null");
                    }

                    onKeyPress?.Invoke(key, KeyState.Down);
                }

            }
            finally
            {
                Console.TreatControlCAsInput = false;
                Console.CancelKeyPress -= Console_CancelKeyPress;
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) => CancelSource.Cancel();
        #endregion


        #region Registration
        /// <summary>
        /// Starts the engine so it is capturing input from the console
        /// </summary>
        /// <returns></returns>
        public static Guid Capture(ConsoleKeyPressHandler KeyHandler)
        {
            Guid id = Guid.NewGuid();

            var Handlers = new InputHandlers() { KeyHandler = KeyHandler };
            Observers.Add(id, Handlers);
            Register_Handlers(Handlers);

            Start();
            return id;
        }

        public static void Release(Guid id)
        {
            if (Observers.TryGetValue(id, out InputHandlers Handlers))
            {
                if (Observers.Remove(id))
                {
                    Unregister_Handlers(Handlers);
                }
            }

            if (Observers.Count <= 0)
            {
                Stop();
            }
        }

        private static void Register_Handlers(InputHandlers Handlers)
        {
            onKeyPress += Handlers.KeyHandler;
        }

        private static void Unregister_Handlers(InputHandlers Handlers)
        {
            onKeyPress -= Handlers.KeyHandler;
        }
        #endregion

        #region Capturing Engine
        static void Start()
        {

            if (PollingTask != null && CancelSource != null)
            {/* Already running */
                return;
            }
            else if (PollingTask != null || CancelSource != null)
            {
                Stop();
            }

            CancelSource = new CancellationTokenSource();
            PollingTask = System.Threading.Tasks.Task.Factory.StartNew(Run_Input_Polling, CancelSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        static void Stop()
        {
            if (!ReferenceEquals(null, PollingTask))
            {
                CancelSource?.Cancel(throwOnFirstException: true);

                PollingTask.Wait();
                PollingTask.Dispose();
                PollingTask = null;
            }

            if (!ReferenceEquals(null, CancelSource))
            {
                CancelSource.Dispose();
                CancelSource = null;
            }
        }
        #endregion
    }
}
