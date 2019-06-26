using System;
using System.Text;
using System.Threading;
/// <summary>
/// Basis for a generic console text based tool used for displaying some form of information.
/// </summary>
public abstract class ConsoleWidget : IDisposable
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public readonly ConsoleWidgetType Type;
    protected int Disposed = 0;
    /// <summary>
    /// The <see cref="StaticConsoleLine"/> for this widget.
    /// </summary>
    protected StaticConsoleLine Line = new StaticConsoleLine();
    protected StringBuilder Buffer = new StringBuilder();

    public ConsoleWidget(ConsoleWidgetType ty) { Type = ty; }
    ~ConsoleWidget() { Dispose(); }

    public virtual void Dispose()
    {
        if ( Interlocked.Exchange(ref Disposed, 1) == 1 )
        {
            return;
        }

        lock (Buffer)
        {
            Buffer.Clear();
            Buffer = null;
        }

        lock (Line)
        {
            Line.Dispose();
            Line = null;
        }

        GC.SuppressFinalize( this );
    }
}
