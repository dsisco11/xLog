using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace xLog.Widgets
{
    /// <summary>
    /// Basis for a generic console text based tool used for displaying some form of information.
    /// </summary>
    public abstract class ConsoleWidget : IDisposable
    {
        #region Properties
        public int X { get; private set; }
        public int Y { get; private set; }
        public readonly ConsoleWidgetType Type;
        /// <summary>
        /// Tracks the disposal state for the widget
        /// </summary>
        protected int Disposed = 0;
        protected List<StaticConsoleLine> StaticLines = null;
        /// <summary>
        /// The <see cref="StaticConsoleLine"/> for this widget.
        /// </summary>
        protected StaticConsoleLine Line => StaticLines[0];
        protected StringBuilder Buffer = new StringBuilder();
        #endregion

        #region Constructors
        public ConsoleWidget(ConsoleWidgetType ty)
        {
            Type = ty;
            StaticLines = new List<StaticConsoleLine>(1);
            StaticLines.Add(new StaticConsoleLine());
        }

        ~ConsoleWidget()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            if (Interlocked.Exchange(ref Disposed, 1) == 1)
            {
                return;
            }

            lock (Buffer)
            {
                Buffer.Clear();
                Buffer = null;
            }

            lock (StaticLines)
            {
                for(int i=0; i<StaticLines.Count; i++)
                {
                    StaticLines[i].Dispose();
                    StaticLines[i] = null;
                }

                StaticLines.Clear();
                StaticLines = null;
            }

            GC.SuppressFinalize(this);
        }
        #endregion
    }

}