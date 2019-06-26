using System;

namespace xLog
{
    /// <summary>
    /// A global <see cref="LogSource"/> instance
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// If set to a non-null value, specifies the progress state of an ongoing task.
        /// <para>The progress percentage will be shown on all log messages sent through the generic <see cref="Log"/> class.</para>
        /// </summary>
        public static int? JOB_TOTAL = null, JOB_CURRENT = null;
        private static LogSource log = new LogSource(() =>
        {
            if (!JOB_TOTAL.HasValue || !JOB_CURRENT.HasValue) return null;
            float prog = ((float)JOB_CURRENT.Value / (float)JOB_TOTAL.Value);
            return XTERM.greenBright(prog.ToString("P2"));
        });

        public static void Set_Name_Function(Func<string> func)
        {
            lock (log)
            {
                log = null;
                log = new LogSource(func);
            }
        }

        #region Logging Functions

        /// <summary>
        /// Adds a level of indentation to the specified LogLevel line type.
        /// </summary>
        /// <param name="level">log line type to indent</param>
        public static void Indent(ELogLevel level)
        {
            xLogEngine.Indent(level);
        }
        /// <summary>
        /// Removes a level of indentation from the specified LogLevel line type.
        /// </summary>
        /// <param name="level">log line type to unindent</param>
        public static void Unindent(ELogLevel level)
        {
            xLogEngine.Unindent(level);
        }

        // This outputs a log entry of the level info.
        [LoggingMethod]
	    public static void Info(string Format, params object[] args)
        {
            lock (log)
            {
                log.Info(Format, args);
            }
        }

        // This outputs a log entry of the level info.
        [LoggingMethod]
	    public static void Info(params object[] args)
        {
            lock (log)
            {
                log.Info(args);
            }
        }


        /// <summary>
        /// Use to display unlogged console messages.
        /// Using <see cref="Console.Write"/> or <see cref="Console.WriteLine"/> will corrupt the displayed text in the console window.
        /// </summary>
        [LoggingMethod]
	    public static void Dummy(string Format, params object[] args)
        {
            lock (log)
            {
                log.Console(Format, args);
            }
        }

        /// <summary>
        /// Use to display unlogged console messages.
        /// Using <see cref="Console.Write"/> or <see cref="Console.WriteLine"/> will corrupt the displayed text in the console window.
        /// </summary>
        [LoggingMethod]
	    public static void Dummy(params object[] args)
        {
            lock (log)
            {
                log.Console(args);
            }
        }


        // This outputs a log entry of the level debug.
        [LoggingMethod]
	    public static void Debug(string format, params object[] args)
        {
            lock (log)
            {
                log.Debug(format, args);
            }
        }

        // This outputs a log entry of the level debug.
        [LoggingMethod]
	    public static void Debug(params object[] args)
        {
            lock (log)
            {
                log.Debug(args);
            }
        }

        // This outputs a log entry of the level success.
        [LoggingMethod]
	    public static void Success(string format, params object[] args)
        {
            lock (log)
            {
                log.Success(format, args);
            }
        }

        // This outputs a log entry of the level success.
        [LoggingMethod]
	    public static void Success(params object[] args)
        {
            lock (log)
            {
                log.Success(args);
            }
        }

        // This outputs a log entry of the level success.
        [LoggingMethod]
	    public static void Failure(string format, params object[] args)
        {
            lock (log)
            {
                log.Failure(format, args);
            }
        }

        // This outputs a log entry of the level success.
        [LoggingMethod]
	    public static void Failure(params object[] args)
        {
            lock (log)
            {
                log.Failure(args);
            }
        }

        // This outputs a log entry of the level warn.
        [LoggingMethod]
	    public static void Warn(string format, params object[] args)
        {
            lock (log)
            {
                log.Warn(format, args);
            }
        }

        // This outputs a log entry of the level warn.
        [LoggingMethod]
	    public static void Warn(params object[] args)
        {
            lock (log)
            {
                log.Warn(args);
            }
        }

        // This outputs a log entry of the level error.
        [LoggingMethod]
	    public static void Error(string format, params object[] args)
        {
            lock (log)
            {
                log.Error(format, args);
            }
        }

        // This outputs a log entry of the level error.
        [LoggingMethod]
	    public static void Error(params object[] args)
        {
            lock (log)
            {
                log.Error(args);
            }
        }

        // This outputs a log entry of the level error.
        [LoggingMethod]
	    public static void Error(Exception ex)
        {
            lock (log)
            {
                log.Error(ex);
            }
        }


        /// <summary>
        /// Outputs a log entry of the level error, specifically for NULL Argument events
        /// </summary>
        [LoggingMethod]
        public static string ErrorNull(string ParamName)
        {
            lock (log)
            {
                return log.ErrorNull(ParamName);
            }
        }

        /// <summary>
        /// Indicates an event which will cause an operation to fail.
        /// Additionally throws an <see cref="ArgumentNullException"/> at the location of the calling code.
        /// This outputs a log entry at the <c>Error</c> level, Specifically for Null argument errors.
        /// </summary>
        [LoggingMethod]
        public static void ErrorNullThrow(string ParamName)
        {
            lock (log)
            {
                log.ErrorNullThrow(ParamName);
            }
        }

        // This outputs a log entry of the level interface;
        // normally, this means that some sort of user interaction
        // is required.
        [LoggingMethod]
	    public static void Interface(string format, params object[] args)
        {
            lock (log)
            {
                log.Interface(format, args);
            }
        }

        // This outputs a log entry of the level interface;
        // normally, this means that some sort of user interaction
        // is required.
        [LoggingMethod]
	    public static void Interface(params object[] args)
        {
            lock (log)
            {
                log.Interface(args);
            }
        }
    
        /// <summary>
        /// Outputs a message with a solid line of '=' chars above and below it of equal width to the longest line in the message
        /// </summary>
        [LoggingMethod]
	    public static void Banner(ELogLevel level, string format, params object[] args)
        {
            lock(log)
            {
                log.Banner(level, format, args);
            }
        }
        #endregion

    #if INCLUDE_LOGGER_PROMPTS
    #region Prompts
        /// <summary>
        /// Displays a prompt message in the console and then passes any user input to the specified callback handler, repeating the input process whenever the handler returns <c>False</c>
        /// </summary>
        /// <param name="Message">Message to display to the user</param>
        /// <param name="concealInput">If <c>TRUE</c> characters the user inputs will show as '*' in the console and logs</param>
        /// <returns>User input string</returns>
        public static async Task<string> Prompt(string Message, bool concealInput = false)
        {
            return await log.Prompt(Message, concealInput).ConfigureAwait(false);
        }

        /// <summary>
        /// Displays a prompt message in the console and then passes any user input to the specified callback handler, repeating the input process whenever the handler returns <c>False</c>
        /// </summary>
        /// <param name="Message">Message to display to the user</param>
        /// <param name="ValidatorCallback">Handler for user input called for each character/key the user inputs, the functions argument contains the full string of user input up to that point, return <c>False</c> to reject a given character.</param>
        /// <param name="concealInput">If <c>TRUE</c> characters the user inputs will show as '*' in the console and logs</param>
        /// <param name="InitialValue">Value to use for the users initial input</param>
        /// <returns>User input string</returns>
        public static async Task<string> Prompt(string Message, PromptInputValidatorDelegate ValidatorCallback, bool concealInput = false, string InitialValue = null)
        {
            return await log.Prompt(Message, ValidatorCallback, concealInput, InitialValue).ConfigureAwait(false);
        }

        /// <summary>
        /// Displays a prompt message in the console and then passes any user input to the specified callback handler, repeating the input process whenever the handler returns <c>False</c>
        /// </summary>
        /// <param name="Message">Message to display to the user</param>
        /// <param name="concealInput">If <c>TRUE</c> characters the user inputs will show as '*' in the console and logs</param>
        /// <param name="InitialValue">Value to use for the users initial input</param>
        /// <returns>User input string</returns>
        public static async Task<bool> PromptYesNo(string Message, bool concealInput = false, string InitialValue = null)
        {
            return await log.PromptYesNo(Message, concealInput, InitialValue).ConfigureAwait(false);
        }
    #endregion
    #endif
    }
}