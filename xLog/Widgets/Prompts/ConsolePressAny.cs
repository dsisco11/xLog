using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace xLog
{
    /// <summary>
    /// Outputs a "Press ANY key to continue" message and allows waiting for the user to press a key
    /// </summary>
    public class ConsolePressAny : ConsoleWidget, IDisposable
    {
        #region Properties
        Task myTask = null;
        CancellationTokenSource Cancel = null;
        #endregion

        #region Constructors
        public ConsolePressAny(string Message = null) : base(ConsoleWidgetType.Input)
        {
            Cancel = new CancellationTokenSource();
            myTask = Task.Run(() => Read_Input(), Cancel.Token);

            if (string.IsNullOrWhiteSpace(Message))
                Message = "Press ANY key to continue";

            Line.Set( Message );
        }

        public override void Dispose()
        {
            base.Dispose();

            if (myTask != null)
            {
                Cancel?.Cancel();
                myTask.Wait();
                myTask.Dispose();
                myTask = null;
            }

            if (Cancel != null)
            {
                Cancel.Dispose();
                Cancel = null;
            }
        }
        #endregion

        void Read_Input()
        {
            Console.ReadKey(true);
        }
    
        #region Task Implementation
        public Task ToTask() => myTask;
        public TaskAwaiter GetAwaiter() => myTask.GetAwaiter();
        public ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) => myTask.ConfigureAwait(continueOnCapturedContext);

        public void Wait() => myTask.Wait();
        #endregion

        public static async Task Prompt(string Message = null)
        {
            using (var p = new ConsolePressAny(Message))
            {
                await p.ConfigureAwait(false);
            }
        }
    }
}