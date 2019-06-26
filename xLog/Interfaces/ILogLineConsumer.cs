using System;

namespace xLog
{
    /// <summary>
    /// Defines an object that consumes log lines.
    /// <para>Uses might include an object which writes log lines to file, or an object which streams log lines over a network connection.</para>
    /// </summary>
    public interface ILogLineConsumer : IDisposable
    {
        void Consume(LogLine Line);
        void Flush();
    }
}
