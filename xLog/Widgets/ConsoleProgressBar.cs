using System;
using System.Collections.Generic;
using System.Threading;

namespace xLog
{
    public class ConsoleProgressBar : ConsoleWidget
    {
        /// <summary>
        /// Maximum number of history samples to use when calculating ETA
        /// </summary>
        private const int MAX_HIST = 5;
        internal Queue<Tuple<TimeSpan, float>> History = new Queue<Tuple<TimeSpan, float>>();
        internal DateTime? last_time = null;
        internal float LastPct = 0f;
        const int PROG_BAR_WIDTH = 50;
        //private bool Disposed = false;
        //private StringBuilder Buffer = new StringBuilder();

        public ConsoleProgressBar() : base(ConsoleWidgetType.Progress)
        {
            Set_Progress(0);
        }
    

        protected TimeSpan Get_ETA()
        {
            TimeSpan sum = new TimeSpan();
            float dt = 0f;

            foreach(var tuple in History)
            {
                sum = sum.Add(tuple.Item1);
                dt += tuple.Item2;
            }

            // we have the amount of change over time
            // make it an average
            double ms = sum.TotalMilliseconds / MAX_HIST;
            float avg = dt / MAX_HIST;
            // find how much longer it will take at this rate.
            float remain = (1f - LastPct);
            float cycles = (remain / avg);

            double ms_remain = (ms * cycles);
            return new TimeSpan(0, 0, 0, 0, (int)ms_remain);
        }
    
        /// <summary>
        /// Sets the progress being displayed to a new value
        /// </summary>
        /// <param name="Percent">Progress percentage in the [0.0 - 1.0] range</param>
        public void Set_Progress(double Percent)
        {
            lock (Line)
            {
                if (Interlocked.Equals(Disposed, 1))
                {
                    return;
                }

                if (!last_time.HasValue)
                {
                    last_time = DateTime.UtcNow;
                    LastPct = (float)Percent;
                }
                else
                {
                    History.Enqueue(new Tuple<TimeSpan, float>(DateTime.UtcNow.Subtract(last_time.Value), (float)Percent - LastPct));
                    while (History.Count > MAX_HIST) { History.Dequeue(); }
                }

                Buffer.Clear();
                Buffer.Append(string.Format(XTERM.yellow("{0,6:#00.00}%") + XTERM.magentaBright(" ["), Percent * 100f));// 9 chars
                                                                                                                     // draw the active '=' portion of the bar
                const int ACTIVE_SPACE = (PROG_BAR_WIDTH);
                double progSafe = Math.Min(1.0, Math.Max(0.0, Percent));
                int active = (int)(progSafe * ACTIVE_SPACE);
                bool has_cap = (active < ACTIVE_SPACE);
                string active_str = new string('=', active);// always draw an arrow head to cap the active portion of the bar UNLESS it would extend past the bars end
                if (has_cap) active_str += ">";

                Buffer.Append(XTERM.cyanBright(active_str));
                Buffer.Append(new string(' ', ACTIVE_SPACE - active_str.Length));// pad out the bar's unused space 
                Buffer.Append(XTERM.magentaBright("]"));
            
                Line.Set(Buffer.ToString());
            }
        }
    
        public static void Test()
        {
            ConsoleProgressBar prog = new ConsoleProgressBar();
            float progress = 0f;
            while (progress < 1)
            {
                progress += 0.1f;
                prog.Set_Progress(progress);
                System.Threading.Thread.Sleep(1000);
            }

            ConsolePressAny.Prompt().Wait();
            Environment.Exit(0);
        }
    }
}