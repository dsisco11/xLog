
public enum ELogLevel : ushort
{
    All = 0,
    Trace,
    Debug,
    Info,
    Success,
    Failure,
    Warn,
    Error,
    Assert,
    /// <summary>This log level dictates that user input is required before the code progresses</summary>
    Interface,
    /// <summary>Maximum possible <see cref="ELogLevel"/> value</summary>
    MAX,
    /// <summary>
    /// Console lines aren't displayed in log file output and have no log level tag,
    /// They look just like normal output from the <see cref="Console.Write"/> or <see cref="Console.WriteLine"/> functions.
    /// </summary>
    Console,
}