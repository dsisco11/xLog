using System;
using System.Collections.Generic;

namespace xLog
{
    /// <summary>
    /// Base class for all <see cref="LineFormatter"/> which format the output text for all logging functions.
    /// <para>By inheriting this class custom formatters can be made to change the output text for log functions.</para>
    /// <para>To use a custom formatter an instance of it must be assigned to the <see cref="xLogEngine.CurrentFormatter"/> variable.</para>
    /// </summary>
    public class LineFormatter : ILogLineFormatter
    {
        public LineFormatter()
        {
        }


        public virtual string Color_LogLine(ELogLevel level, string msg)
        {
            switch (level)
            {
                case ELogLevel.Trace:
                    return XTERM.blackBright(msg);
                case ELogLevel.Info:
                case ELogLevel.Debug:
                    return XTERM.white(msg);
                case ELogLevel.Success:
                    return XTERM.green(msg);
                case ELogLevel.Failure:
                    return XTERM.red(msg);
                case ELogLevel.Warn:
                    return XTERM.yellow(msg);
                case ELogLevel.Error:
                    return XTERM.red(msg);
                case ELogLevel.Assert:
                    return XTERM.magenta(msg);
                case ELogLevel.Interface:
                    return XTERM.white(msg);
                default:
                    return msg;
            }
        }

        public virtual string Color_LogLevel_Title(ELogLevel level, string name)
        {
            switch (level)
            {
                case ELogLevel.Trace:
                    return XTERM.white(name);
                case ELogLevel.Info:
                case ELogLevel.Debug:
                    return XTERM.whiteBright(name);
                case ELogLevel.Success:
                    return XTERM.greenBright(name);
                case ELogLevel.Failure:
                    return XTERM.redBright(name);
                case ELogLevel.Warn:
                    return XTERM.yellowBright(name);
                case ELogLevel.Error:
                    return XTERM.redBright(name);
                case ELogLevel.Assert:
                    return XTERM.magentaBright(name);
                case ELogLevel.Interface:
                    return XTERM.whiteBright(name);
                default:
                    return name;
            }
        }

        public string Format_Exception(Exception ex)
        {
            string MSG = ex.Message;
            string TRACE = ex.StackTrace;
            if (ex.InnerException != null)
            {
                MSG = ex.InnerException.Message;
                TRACE = ex.InnerException.StackTrace;
            }

            return string.Format("{0}\n==StackTrace==\n{1}", MSG, TRACE);
        }

        #region Logging Functions
        /// <summary>
        /// Checks for a condition; if the condition is <c>false</c>, outputs a specified message and displays a message box that shows the call stack.
        /// This method is equivalent to System.Diagnostics.Debug.Assert, however, it was modified to also write to the Logger output.
        /// Borrowed from <c>SteamKit2</c>
        /// </summary>
        /// <param name="Condition">The conditional expression to evaluate. If the condition is <c>true</c>, the specified message is not sent and the message box is not displayed.</param>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        /// <param name="Message">The message to display if the assertion fails.</param>
        public virtual void Assert(ref string Source, ref string Message)
        {
            Message = string.Concat("Assertion Failed! (", Message, ")");
        }

        /// <summary>
        /// This outputs a log entry at the <c>Info</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Info(ref string Source, ref string Format, params object[] args) { }
        /// <summary>
        /// This outputs a log entry at the <c>Info</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Info(ref string Source, params object[] args) { }



        /// <summary>
        /// <see cref="xLogEngine.Dummy(string, string, object[])"/>
        /// </summary>
        public virtual void Dummy(ref string Format, params object[] args) { }
        /// <summary>
        /// <see cref="xLogEngine.Console(string, object[])"/>
        /// </summary>
        public virtual void Dummy(params object[] args) { }


        /// <summary>
        /// This outputs a log entry at the <c>Debug</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Debug(ref string Source, ref string Format, params object[] args) { }
        /// <summary>
        /// This outputs a log entry at the <c>Debug</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Debug(ref string Source, params object[] args) { }


        /// <summary>
        /// This outputs a log entry at the <c>Trace</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Trace(ref string Source, ref string Format, params object[] args)
        {
            Format += string.Concat("\n==[ Stack Trace ]==\n", xLogEngine.Get_Trace());
        }
        /// <summary>
        /// This outputs a log entry at the <c>Trace</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Trace(ref string Source, params object[] args) { }


        /// <summary>
        /// This outputs a log entry at the <c>Trace</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Trace(ref string Source, ref int frameOffset, ref string Format, params object[] args)
        {
            Format += string.Concat("\n==[ Stack Trace ]==\n", xLogEngine.Get_Trace(frameOffset));
        }
        /// <summary>
        /// This outputs a log entry at the <c>Trace</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Trace(ref string Source, ref int frameOffset, params object[] args) { }


        /// <summary>
        /// This outputs a log entry at the <c>Success</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Success(ref string Source, ref string Format, params object[] args) { }
        /// <summary>
        /// This outputs a log entry at the <c>Success</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Success(ref string Source, params object[] args) { }


        /// <summary>
        /// This outputs a log entry at the <c>Failure</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Failure(ref string Source, ref string Format, params object[] args) { }
        /// <summary>
        /// This outputs a log entry at the <c>Failure</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Failure(ref string Source, params object[] args) { }


        /// <summary>
        /// This outputs a log entry at the <c>Warning</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Warn(ref string Source, ref string Format, params object[] args) { }
        /// <summary>
        /// This outputs a log entry at the <c>Warning</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Warn(ref string Source, params object[] args) { }


        /// <summary>
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Error(ref string Source, ref string Format, params object[] args) { }
        /// <summary>
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Error(ref string Source, params object[] args) { }
        /// <summary>
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Error(ref string Source, Exception ex, out string Message)
        {
            Message = Format_Exception(ex);
        }
        /// <summary>
        /// This outputs a log entry at the <c>Error</c> level, Specifically for Null item errors.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        /// <param name="nullObjectName">The name of the object in question.</param>
        public virtual void ErrorNull(ref string Source, string nullObjectName, out string Message)
        {
            Message = string.Concat("\"", nullObjectName, "\" IS NULL!");
        }


        /// <summary>
        /// This outputs a log entry at the <c>Interface</c> level.
        /// Normally, this means user input is required to continue...
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Interface(ref string Source, ref string Format, params object[] args) { }
        /// <summary>
        /// This outputs a log entry at the <c>Interface</c> level.
        /// Normally, this means user input is required to continue...
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual void Interface(ref string Source, params object[] args) { }


        /// <summary>
        /// Displays a prompt message in the console and then passes any user input to the specified callback handler, repeating the input process whenever the handler returns <c>False</c>
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        /// <param name="Message">Message to display to the user</param>
        public virtual void Prompt(ref string Source, ref string Message) { }


        /// <summary>
        /// Outputs a message with a solid line of '=' chars above and below it of equal width to the longest line in the message
        /// <para>(This outputs a log entry of the level success.)</para>
        /// </summary>
        /// <param name="Output">Final string to be output by the logger</param>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        public virtual ICollection<RawLogLine> Banner(string Source, string Format, params object[] Args)
        {
            int overhang = 2;// How many extra = chars will hang over the messages total width on the left/right sides
            int len = 0;
            // pre format our message here
            string msg = string.Format(Format, Args);
            string[] lines = msg.Replace("\r", string.Empty).Split('\n');
            foreach (string line in lines) { len = Math.Max(len, line.Length); }
            len = Math.Min(50, len);// make sure the line break isnt TOO long
            string bar = new string('=', len + (overhang * 2));

            string spacer = new string(' ', overhang);
            var output = new List<RawLogLine>();

            // Add the first line to output which is the top bar
            output.Add(new RawLogLine(Source, bar));
            foreach (string line in lines)
            {
                output.Add(new RawLogLine(Source, string.Concat(spacer, line, spacer)));
            }
            // Add the last line which is the bottom bar
            output.Add(new RawLogLine(Source, bar));

            /*xLogEngine.OutputLine(level, Source, bar);
            foreach (string line in lines)
            {
                xLogEngine.OutputLine(level, Source, string.Concat(spacer, line, spacer));
            }
            xLogEngine.OutputLine(level, Source, bar);*/

            return output;
        }
        #endregion

    }
}