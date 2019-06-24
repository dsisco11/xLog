/*
 * Copyright 2017 David Sisco 
 */

using System;

namespace xLog
{
    /// <summary>
    /// Wraps the base <see cref="xLogEngine"/> class and allows outputting log lines whose prepended ModuleNames are determines via a function or simply prespecified.
    /// The usefulness of this class becomes apparent in cases where a program might want to be more specific about WHERE a log line came from; eg which class.
    /// Also supports linking to other <see cref="LogSource"/> instances so their logging tag will be prepended before it's own lines in the case that classes need to be more specific about where the lines are coming from.
    /// </summary>
    public class LogSource
    {
        #region Variables
        /// <summary>
        /// If set, all logged messages will be routed through a seperate Logger instance.
        /// </summary>
        private LogSource _Proxy = null;
        /// <summary>
        /// Name of the module which is outputting the messages
        /// </summary>
        protected string ModuleName { get { return (_Proxy != null ? _Proxy.ModuleName + " " : "") + (_module_name_str != null ? _module_name_str : (null==_module_name_funct ? "" : String.Concat("[", _module_name_funct(), "]"))); } }
        /// <summary>
        /// If non null the string result of this function will be used as the <c>ModuleName</c>
        /// </summary>
        protected Func<string> _module_name_funct = null;
        /// <summary>
        /// If non-null this string will be used for the module name.
        /// </summary>
        private string _module_name_str = null;

        #endregion

        #region Constructors

        public LogSource(string tag, LogSource prxy = null)
        {
            _Proxy = prxy;
            _module_name_str = tag;
        }

        /// <summary>
        /// Creates a new <see cref="LogSource"/> instance whose moduleName is determined by a function which is called whenever the moduleName is needed.
        /// This is useful for cases where the moduleName might be colored and change over time.
        /// </summary>
        /// <param name="nameFunc"></param>
        public LogSource(Func<string> nameFunc) { _module_name_funct = nameFunc; }
        #endregion

        #region Logging functions

        /// <summary>
        /// Adds a level of indentation to the specified LogLevel line type.
        /// </summary>
        /// <param name="level">log line type to indent</param>
        public void Indent(LogLevel level) { xLogEngine.Indent(level); }

        /// <summary>
        /// Removes a level of indentation from the specified LogLevel line type.
        /// </summary>
        /// <param name="level">log line type to unindent</param>
        public void Unindent(LogLevel level) { xLogEngine.Unindent(level); }

        /// <summary>
        /// This outputs a log entry at the <c>Info</c> level.
        /// </summary>
        public void Info(string format, params object[] args) { xLogEngine.OutputLine(LogLevel.Info, ModuleName, format, args); }        
        /// <summary>
        /// This outputs a log entry at the <c>Info</c> level.
        /// </summary>
        public void Info(params object[] args) { xLogEngine.OutputLine(LogLevel.Info, ModuleName, args); }


        /// <summary>
        /// This outputs a log entry at the <c>Debug</c> level.
        /// </summary>
        public void Debug(string format, params object[] args) { xLogEngine.OutputLine(LogLevel.Debug, ModuleName, format, args); }
        /// <summary>
        /// This outputs a log entry at the <c>Debug</c> level.
        /// </summary>
        public void Debug(params object[] args) { xLogEngine.OutputLine(LogLevel.Debug, ModuleName, args); }


        /// <summary>
        /// This outputs a log entry at the <c>Trace</c> level.
        /// </summary>
        public void Trace(string format, params object[] args) { xLogEngine.OutputLine(LogLevel.Trace, ModuleName, format, args); }
        /// <summary>
        /// This outputs a log entry at the <c>Trace</c> level.
        /// </summary>
        public void Trace(params object[] args) { xLogEngine.OutputLine(LogLevel.Trace, ModuleName, args); }


        /// <summary>
        /// This outputs a log entry at the <c>Success</c> level.
        /// </summary>
        public void Success(string format, params object[] args) { xLogEngine.OutputLine(LogLevel.Success, ModuleName, format, args); }        
        /// <summary>
        /// This outputs a log entry at the <c>Success</c> level.
        /// </summary>
        public void Success(params object[] args) { xLogEngine.OutputLine(LogLevel.Success, ModuleName, args); }


        /// <summary>
        /// This outputs a log entry at the <c>Failure</c> level.
        /// </summary>
        public void Failure(string format, params object[] args) { xLogEngine.OutputLine(LogLevel.Failure, ModuleName, format, args); }
        /// <summary>
        /// This outputs a log entry at the <c>Failure</c> level.
        /// </summary>
        public void Failure(params object[] args) { xLogEngine.OutputLine(LogLevel.Failure, ModuleName, args); }


        /// <summary>
        /// This outputs a log entry at the <c>Warn</c> level.
        /// </summary>
        public void Warn(string format, params object[] args) { xLogEngine.OutputLine(LogLevel.Warn, ModuleName, format, args); }
        /// <summary>
        /// This outputs a log entry at the <c>Warn</c> level.
        /// </summary>
        public void Warn(params object[] args) { xLogEngine.OutputLine(LogLevel.Warn, ModuleName, args); }


        /// <summary>
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        public void Error(string format, params object[] args) { xLogEngine.OutputLine(LogLevel.Error, ModuleName, format, args); }
        /// <summary>
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        public void Error(params object[] args) { xLogEngine.OutputLine(LogLevel.Error, ModuleName, args); }
        /// <summary>
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        public void Error(Exception ex) { xLogEngine.Error(ModuleName, ex); }


        /// <summary>
        /// This outputs a log entry at the <c>Interface</c> level.
        /// Normally, this means user input is required to continue...
        /// </summary>
        public void Interface(string format, params object[] args) { xLogEngine.OutputLine(LogLevel.Interface, ModuleName, format, args); }
        /// <summary>
        /// This outputs a log entry at the <c>Interface</c> level.
        /// Normally, this means user input is required to continue...
        /// </summary>
        public void Interface(params object[] args) { xLogEngine.OutputLine(LogLevel.Interface, ModuleName, args); }

        #endregion
    }
}