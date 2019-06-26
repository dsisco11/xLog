using System;
using System.Collections.Generic;

/// <summary>
/// Defines all of the functionality a log line formatter should implement
/// </summary>
public interface ILogLineFormatter
{

    string Color_LogLine(ELogLevel level, string msg);
    string Color_LogLevel_Title(ELogLevel level, string name);

    string Format_Exception(Exception ex);

    #region Logging Functions
    /// <summary>
    /// Checks for a condition; if the condition is <c>false</c>, outputs a specified message and displays a message box that shows the call stack.
    /// This method is equivalent to System.Diagnostics.Debug.Assert, however, it was modified to also write to the Logger output.
    /// Borrowed from <c>SteamKit2</c>
    /// </summary>
    /// <param name="Condition">The conditional expression to evaluate. If the condition is <c>true</c>, the specified message is not sent and the message box is not displayed.</param>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    /// <param name="Message">The message to display if the assertion fails.</param>
    void Assert(ref string Source, ref string Message);

    /// <summary>
    /// This outputs a log entry at the <c>Info</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Info(ref string Source, ref string Format, params object[] args);
    /// <summary>
    /// This outputs a log entry at the <c>Info</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Info(ref string Source, params object[] args);



    /// <summary>
    /// <see cref="xLogEngine.Dummy(string, string, object[])"/>
    /// </summary>
    void Dummy(ref string Format, params object[] args);
    /// <summary>
    /// <see cref="xLogEngine.Console(string, object[])"/>
    /// </summary>
    void Dummy(params object[] args);


    /// <summary>
    /// This outputs a log entry at the <c>Debug</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Debug(ref string Source, ref string Format, params object[] args);
    /// <summary>
    /// This outputs a log entry at the <c>Debug</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Debug(ref string Source, params object[] args);


    /// <summary>
    /// This outputs a log entry at the <c>Trace</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Trace(ref string Source, ref string Format, params object[] args);
    /// <summary>
    /// This outputs a log entry at the <c>Trace</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Trace(ref string Source, params object[] args);


    /// <summary>
    /// This outputs a log entry at the <c>Trace</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Trace(ref string Source, ref int frameOffset, ref string Format, params object[] args);
    /// <summary>
    /// This outputs a log entry at the <c>Trace</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Trace(ref string Source, ref int frameOffset, params object[] args);


    /// <summary>
    /// This outputs a log entry at the <c>Success</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Success(ref string Source, ref string Format, params object[] args);
    /// <summary>
    /// This outputs a log entry at the <c>Success</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Success(ref string Source, params object[] args);


    /// <summary>
    /// This outputs a log entry at the <c>Failure</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Failure(ref string Source, ref string Format, params object[] args);
    /// <summary>
    /// This outputs a log entry at the <c>Failure</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Failure(ref string Source, params object[] args);


    /// <summary>
    /// This outputs a log entry at the <c>Warning</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Warn(ref string Source, ref string Format, params object[] args);
    /// <summary>
    /// This outputs a log entry at the <c>Warning</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Warn(ref string Source, params object[] args);


    /// <summary>
    /// This outputs a log entry at the <c>Error</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Error(ref string Source, ref string Format, params object[] args);
    /// <summary>
    /// This outputs a log entry at the <c>Error</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Error(ref string Source, params object[] args);
    /// <summary>
    /// This outputs a log entry at the <c>Error</c> level.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Error(ref string Source, Exception ex, out string Message);
    /// <summary>
    /// This outputs a log entry at the <c>Error</c> level, Specifically for Null item errors.
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    /// <param name="nullObjectName">The name of the object in question.</param>
    void ErrorNull(ref string Source, string nullObjectName, out string Message);


    /// <summary>
    /// This outputs a log entry at the <c>Interface</c> level.
    /// Normally, this means user input is required to continue...
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Interface(ref string Source, ref string Format, params object[] args);
    /// <summary>
    /// This outputs a log entry at the <c>Interface</c> level.
    /// Normally, this means user input is required to continue...
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    void Interface(ref string Source, params object[] args);


    /// <summary>
    /// Displays a prompt message in the console and then passes any user input to the specified callback handler, repeating the input process whenever the handler returns <c>False</c>
    /// </summary>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    /// <param name="Message">Message to display to the user</param>
    void Prompt(ref string Source, ref string Message);


    /// <summary>
    /// Outputs a message with a solid line of '=' chars above and below it of equal width to the longest line in the message
    /// <para>(This outputs a log entry of the level success.)</para>
    /// </summary>
    /// <param name="Output">Final string to be output by the logger</param>
    /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
    ICollection<RawLogLine> Banner(string Source, string Format, params object[] Args);
    #endregion

}