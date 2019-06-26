using System;

public interface ILog
{
    string Name { get; }

    /// <summary>
    /// Adds a level of indentation to the specified LogLevel line type.
    /// </summary>
    /// <param name="level">log line type to indent</param>
    [LoggingMethod]
    void Indent(ELogLevel level = ELogLevel.All);

    /// <summary>
    /// Removes a level of indentation from the specified LogLevel line type.
    /// </summary>
    /// <param name="level">log line type to unindent</param>
    [LoggingMethod]
    void Unindent(ELogLevel level = ELogLevel.All);

    [LoggingMethod]
    void Assert(bool condition, string message);

    /// <summary>
    /// Use to display generic log messages
    /// This outputs a log entry at the <c>Info</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    [LoggingMethod]
    void Info(string Format, params object[] args);
    /// <summary>
    /// Use to display generic log messages
    /// This outputs a log entry at the <c>Info</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    [LoggingMethod]
    void Info(params object[] args);

    /// <summary>
    /// Use to display unlogged console messages.
    /// Using <see cref="Console.Write"/> or <see cref="Console.WriteLine"/> will corrupt the displayed text in the console window.
    /// </summary>
    [LoggingMethod]
    void Console(string Format, params object[] args);
    /// <summary>
    /// Use to display unlogged console messages.
    /// Using <see cref="Console.Write"/> or <see cref="Console.WriteLine"/> will corrupt the displayed text in the console window.
    /// </summary>
    [LoggingMethod]
    void Console(params object[] args);


    /// <summary>
    /// Indicates information which is only useful for debugging
    /// This outputs a log entry at the <c>Debug</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    [LoggingMethod]
    void Debug(string format, params object[] args);
    /// <summary>
    /// Indicates information which is only useful for debugging
    /// This outputs a log entry at the <c>Debug</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    [LoggingMethod]
    void Debug(params object[] args);


    /// <summary>
    /// Indicates an operation's success
    /// This outputs a log entry at the <c>Success</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    [LoggingMethod]
    void Success(string format, params object[] args);
    /// <summary>
    /// Indicates an operation's success
    /// This outputs a log entry at the <c>Success</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    [LoggingMethod]
    void Success(params object[] args);


    /// <summary>
    /// Indicates an Acceptable/Expected operation failure
    /// This outputs a log entry at the <c>Failure</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    [LoggingMethod]
    void Failure(string format, params object[] args);
    /// <summary>
    /// Indicates an Acceptable/Expected operation failure
    /// This outputs a log entry at the <c>Failure</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    [LoggingMethod]
    void Failure(params object[] args);


    /// <summary>
    /// Indicates an event which will NOT cause an operation to fail but which the user should be aware of
    /// This outputs a log entry at the <c>Warning</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    [LoggingMethod]
    void Warn(string format, params object[] args);
    /// <summary>
    /// Indicates an event which will NOT cause an operation to fail but which the user should be aware of
    /// This outputs a log entry at the <c>Warning</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    [LoggingMethod]
    void Warn(params object[] args);


    /// <summary>
    /// Indicates an event which will cause an operation to fail
    /// This outputs a log entry at the <c>Error</c> level.
    /// </summary>
    [LoggingMethod]
    void Error(string format, params object[] args);
    /// <summary>
    /// Indicates an event which will cause an operation to fail
    /// This outputs a log entry at the <c>Error</c> level.
    /// </summary>
    [LoggingMethod]
    void Error(params object[] args);
    /// <summary>
    /// Indicates an event which will cause an operation to fail
    /// This outputs a log entry at the <c>Error</c> level.
    /// </summary>
    [LoggingMethod]
    void Error(Exception ex);
    /// <summary>
    /// Indicates an event which will cause an operation to fail
    /// Outputs a log entry of the level error, specifically for NULL Argument events
    /// </summary>
    [LoggingMethod]
    string ErrorNull(string ParamName);
    /// <summary>
    /// Indicates an event which will cause an operation to fail.
    /// Additionally throws an <see cref="ArgumentNullException"/> at the location of the calling code.
    /// Outputs a log entry of the level error, specifically for NULL Argument events
    /// </summary>
    [LoggingMethod]
    void ErrorNullThrow(string ParamName);

    /// <summary> 
    /// This outputs a log entry of the level interface;
    /// normally, this means that some sort of user interaction
    /// is required.
    /// </summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    [LoggingMethod]
    void Interface(string format, params object[] args);

    /// <summary> 
    /// This outputs a log entry of the level interface;
    /// normally, this means that some sort of user interaction
    /// is required.
    /// </summary>
    [LoggingMethod]
    void Interface(params object[] args);

    /// <summary>
    /// Outputs a message with a solid line of '=' chars above and below it of equal width to the longest line in the message
    /// </summary>
    [LoggingMethod]
    void Banner(ELogLevel level, string format, params object[] args);
}
