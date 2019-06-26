using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace xLog
{
    public static class Timing
    {
        #region Timers

        /// <summary>
        /// Tracks callback actions so multiple timeout calls to the same function can cancel and pending one to prevent multiple triggers
        /// </summary>
        private static Dictionary<Action, Timer> timers = new Dictionary<Action, Timer>();


        /// <summary>
        /// For async methods just use "await Task.Delay" as it uses Timer internally
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static Timer setTimeout(TimeSpan dt, Action callback)
        {
            return setTimeout((int)dt.TotalMilliseconds, callback);
        }

        /// <summary>
        /// For async methods just use "await Task.Delay" as it uses Timer internally
        /// </summary>
        /// <param name="ms"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static Timer setTimeout(int ms, Action callback)
        {
            if (ms <= 0) throw new ArgumentException("MS must be greater than 0!");
            if (callback == null) throw new ArgumentNullException("Callback cannot be null!");

            System.Threading.Timer timer = null;
            timer = new System.Threading.Timer((obj) =>
            {
                try
                {
                    timers.Remove(callback);
                    if (timer != null) timer.Dispose();
                }
                finally
                {
                    callback();
                }
            }, null, ms, System.Threading.Timeout.Infinite);

            //Cancel and remove any ongoing timer for the same function.
            Timer old;
            if (timers.TryGetValue(callback, out old))
            {
                old.Dispose();
                timers.Remove(callback);
            }

            timers.Add(callback, timer);
            return timer;
        }

        /// <summary>
        /// Cancels a previously queued delay timer if it has not fired already.
        /// </summary>
        public static void cancelTimeout(Timer timer)
        {
            var kvp = timers.FirstOrDefault(o => o.Value == timer);
            if (kvp.Key != null)
            {
                Log.Debug("Cancelling timer");
                timers.Remove(kvp.Key);
                timer.Dispose();
            }
        }

        /// <summary>
        /// Cancels a previously queued delay timer if it has not fired already.
        /// </summary>
        public static void cancelTimeout(Action act)
        {
            Timer timer;
            if (timers.TryGetValue(act, out timer))
            {
                Log.Debug("Cancelling timer");
                timer.Dispose();
                timers.Remove(act);
            }
        }
        #endregion
    }
}