using System;

namespace xLog
{
    /// <summary>
    /// *THIS CLASS SHOULD NOT BE USED EXTERNALLY, USE <see cref="LogFactory"/> TO CREATE NEW LOG SOURCES
    /// Allows named log output for classes and members.
    /// <para>
    /// [LEVEL][ModuleName] log message here
    /// </para>
    /// </summary>
    internal class LogSource : ILogger
    {
        #region Variables
        /// <summary>
        /// The parent of this source, the parent source's name will appear before this one's in log messages.
        /// </summary>
        private ILogger Parent = null;
        private Type Source = null;
        /// <summary>
        /// Name of the module which is outputting the messages.
        /// </summary>
        public string Name
        {
            get
            {
                string nm = string.Empty;
                if (this.source_name_funct != null)
                {
                    string str = this.source_name_funct();
                    if (!string.IsNullOrEmpty(str)) nm = string.Concat("[", str, "]");
                }
                return (Parent != null ? Parent.Name + " " : "") + nm;
            }
        }
        protected Func<string> source_name_funct = null;
        private string Tag = null;

        #endregion

        #region Constructors
        /// <summary>
        /// </summary>
        /// <param name="Tag"></param>
        /// <param name="Parent">The parent log-source of this log-source</param>
        public LogSource(Type Source, ILogger Parent = null)
        {
            this.Source = Source;
            this.Parent = Parent;
            source_name_funct = () => this.Source.Name;
        }

        /// <summary>
        /// </summary>
        /// <param name="Tag"></param>
        /// <param name="Parent">The parent log-source of this log-source</param>
        public LogSource(string Tag, ILogger Parent = null)
        {
            this.Parent = Parent;
            this.Tag = Tag;
            source_name_funct = () => this.Tag;
        }

        /// <summary>
        /// </summary>
        /// <param name="sourceNameFunc"></param>
        public LogSource(Func<string> sourceNameFunc, ILogger Parent = null)
        {
            this.source_name_funct = sourceNameFunc;
            this.Parent = Parent;
        }
        #endregion

        #region Logging functions

        /// <summary>
        /// Adds a level of indentation to the specified LogLevel line type.
        /// </summary>
        /// <param name="level">log line type to indent</param>
        public void Indent(ELogLevel level = ELogLevel.All)
        {
            xLogEngine.Indent(level);
        }
        /// <summary>
        /// Removes a level of indentation from the specified LogLevel line type.
        /// </summary>
        /// <param name="level">log line type to unindent</param>
        public void Unindent(ELogLevel level = ELogLevel.All)
        {
            xLogEngine.Unindent(level);
        }

        [LoggingMethod]
        public void Assert(bool condition, string message)
        {
            xLogEngine.Assert(condition, Name, message);
        }

        /// <summary>
        /// Use to display generic log messages
        /// This outputs a log entry at the <c>Info</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
        public void Info(string Format, params object[] args)
        {
            xLogEngine.Info(Name, Format, args);
            //Logger.OutputLine(LogLevel.Info, Name, format, args);
        }
        /// <summary>
        /// Use to display generic log messages
        /// This outputs a log entry at the <c>Info</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
        public void Info(params object[] args)
        {
            xLogEngine.Info(Name, args);
            //Logger.OutputLine(LogLevel.Info, Name, args);
        }

        /// <summary>
        /// Use to display unlogged console messages.
        /// Using <see cref="Console.Write"/> or <see cref="Console.WriteLine"/> will corrupt the displayed text in the console window.
        /// </summary>
        [LoggingMethod]
        public void Console(string Format, params object[] args)
        {
            xLogEngine.Console(Format, args);
        }
        /// <summary>
        /// Use to display unlogged console messages.
        /// Using <see cref="Console.Write"/> or <see cref="Console.WriteLine"/> will corrupt the displayed text in the console window.
        /// </summary>
        [LoggingMethod]
        public void Console(params object[] args)
        {
            xLogEngine.Console(args);
        }


        /// <summary>
        /// Indicates information which is only useful for debugging
        /// This outputs a log entry at the <c>Debug</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
        public void Debug(string format, params object[] args)
        {
            xLogEngine.Debug(Name, format, args);
        }
        /// <summary>
        /// Indicates information which is only useful for debugging
        /// This outputs a log entry at the <c>Debug</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
        public void Debug(params object[] args)
        {
            xLogEngine.Debug(Name, args);
        }


        /// <summary>
        /// Indicates an operation's success
        /// This outputs a log entry at the <c>Success</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
        public void Success(string format, params object[] args)
        {
            xLogEngine.Success(Name, format, args);
        }
        /// <summary>
        /// Indicates an operation's success
        /// This outputs a log entry at the <c>Success</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
        public void Success(params object[] args)
        {
            xLogEngine.Success(Name, args);
        }


        /// <summary>
        /// Indicates an Acceptable/Expected operation failure
        /// This outputs a log entry at the <c>Failure</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
        public void Failure(string format, params object[] args)
        {
            xLogEngine.Failure(Name, format, args);
        }
        /// <summary>
        /// Indicates an Acceptable/Expected operation failure
        /// This outputs a log entry at the <c>Failure</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
        public void Failure(params object[] args)
        {
            xLogEngine.Failure(Name, args);
        }


        /// <summary>
        /// Indicates an event which will NOT cause an operation to fail but which the user should be aware of
        /// This outputs a log entry at the <c>Warning</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
        public void Warn(string format, params object[] args)
        {
            xLogEngine.Warn(Name, format, args);
        }
        /// <summary>
        /// Indicates an event which will NOT cause an operation to fail but which the user should be aware of
        /// This outputs a log entry at the <c>Warning</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
        public void Warn(params object[] args)
        {
            xLogEngine.Warn(Name, args);
        }


        /// <summary>
        /// Indicates an event which will cause an operation to fail
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        [LoggingMethod]
        public void Error(string format, params object[] args)
        {
            xLogEngine.Error(Name, format, args);
        }
        /// <summary>
        /// Indicates an event which will cause an operation to fail
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        [LoggingMethod]
        public void Error(params object[] args)
        {
            xLogEngine.Error(Name, args);
        }
        /// <summary>
        /// Indicates an event which will cause an operation to fail
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        [LoggingMethod]
        public void Error(Exception ex)
        {
            xLogEngine.Error(Name, ex);
        }
        /// <summary>
        /// Indicates an event which will cause an operation to fail
        /// Outputs a log entry of the level error, specifically for NULL Argument events
        /// </summary>
        [LoggingMethod]
        public string ErrorNull(string ParamName)
        {
            return xLogEngine.ErrorNull(Name, ParamName);
        }
        /// <summary>
        /// Indicates an event which will cause an operation to fail.
        /// Additionally throws an <see cref="ArgumentNullException"/> at the location of the calling code.
        /// Outputs a log entry of the level error, specifically for NULL Argument events
        /// </summary>
        [LoggingMethod]
        public void ErrorNullThrow(string ParamName)
        {
            xLogEngine.ErrorNullThrow(Name, ParamName);
        }


        // This outputs a log entry of the level interface;
        // normally, this means that some sort of user interaction
        // is required.
        [LoggingMethod]
        public void Interface(string format, params object[] args)
        {
            xLogEngine.Interface(Name, format, args);
        }
        // This outputs a log entry of the level interface;
        // normally, this means that some sort of user interaction
        // is required.
        [LoggingMethod]
        public void Interface(params object[] args)
        {
            xLogEngine.Interface(Name, args);
        }

        /// <summary>
        /// Outputs a message with a solid line of '=' chars above and below it of equal width to the longest line in the message
        /// </summary>
        [LoggingMethod]
        public void Banner(ELogLevel level, string format, params object[] args)
        {
            xLogEngine.Banner(level, Name, format, args);
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
        public async Task<string> Prompt(string message, bool concealInput = false)
        {
            return await Logger.Prompt(Name, message, concealInput).ConfigureAwait(false);
        }

        /// <summary>
        /// Displays a prompt message in the console and then passes any user input to the specified callback handler, repeating the input process whenever the handler returns <c>False</c>
        /// </summary>
        /// <param name="Message">Message to display to the user</param>
        /// <param name="ValidatorCallback">Handler for user input called for each character/key the user inputs, the functions argument contains the full string of user input up to that point, return <c>False</c> to reject a given character.</param>
        /// <param name="concealInput">If <c>TRUE</c> characters the user inputs will show as '*' in the console and logs</param>
        /// <param name="InitialValue">Value to use for the users initial input</param>
        /// <returns>User input string</returns>
        public async Task<string> Prompt(string message, PromptInputValidatorDelegate ValidatorCallback, bool concealInput = false, string InitialValue = null)
        {
            return await Logger.Prompt(Name, message, ValidatorCallback, concealInput, InitialValue).ConfigureAwait(false);
        }

        /// <summary>
        /// Displays a prompt message in the console and then passes any user input to the specified callback handler, repeating the input process whenever the handler returns <c>False</c>
        /// </summary>
        /// <param name="Message">Message to display to the user</param>
        /// <param name="concealInput">If <c>TRUE</c> characters the user inputs will show as '*' in the console and logs</param>
        /// <param name="InitialValue">Value to use for the users initial input</param>
        /// <returns>Choice</returns>
        public async Task<bool> PromptYesNo(string Message, bool concealInput = false, string InitialValue = null)
        {
            return await Logger.PromptYesNo(Name, Message, concealInput, InitialValue).ConfigureAwait(false);
        }
            #endregion
    #endif

    }

}