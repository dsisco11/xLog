/*
 * Copyright 2017 David Sisco 
 */

using System;

namespace xLog
{
    /// <summary>
    /// Allows access to a generic (nameless) <see cref="LogSource"/> instance.
    /// using this there is no need to pass a ModuleName to logging fucntions.
    /// </summary>
    public static class Log
    {
        private static LogSource log = new LogSource(null);
        #region Logging Functions

        /// <summary>
        /// This outputs a log entry at the <c>Info</c> level.
        /// </summary>
        public static void Info(string format, params object[] args) { log.Info(format, args); }

        /// <summary>
        /// This outputs a log entry at the <c>Info</c> level.
        /// </summary>
        public static void Info(params object[] args) { log.Info(args); }

        /// <summary>
        /// This outputs a log entry at the <c>Debug</c> level.
        /// </summary>
        public static void Debug(string format, params object[] args) { log.Debug(format, args); }

        /// <summary>
        /// This outputs a log entry at the <c>Debug</c> level.
        /// </summary>
        public static void Debug(params object[] args) { log.Debug(args); }

        /// <summary>
        /// This outputs a log entry at the <c>Debug</c> level.
        /// </summary>
        public static void Trace(string format, params object[] args) { log.Trace(format, args); }

        /// <summary>
        /// This outputs a log entry at the <c>Debug</c> level.
        /// </summary>
        public static void Trace(params object[] args) { log.Trace(args); }

        /// <summary>
        /// This outputs a log entry at the <c>Success</c> level.
        /// </summary>
        public static void Success(string format, params object[] args) { log.Success(format, args); }

        /// <summary>
        /// This outputs a log entry at the <c>Success</c> level.
        /// </summary>
        public static void Success(params object[] args) { log.Success(args); }

        /// <summary>
        /// This outputs a log entry at the <c>Failure</c> level.
        /// </summary>
        public static void Failure(string format, params object[] args) { log.Failure(format, args); }

        /// <summary>
        /// This outputs a log entry at the <c>Failure</c> level.
        /// </summary>
        public static void Failure(params object[] args) { log.Failure(args); }

        /// <summary>
        /// This outputs a log entry at the <c>Warning</c> level.
        /// </summary>
        public static void Warn(string format, params object[] args) { log.Warn(format, args); }

        /// <summary>
        /// This outputs a log entry at the <c>Warning</c> level.
        /// </summary>
        public static void Warn(params object[] args) { log.Warn(args); }

        /// <summary>
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        public static void Error(string format, params object[] args) { log.Error(format, args); }

        /// <summary>
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        public static void Error(params object[] args) { log.Error(args); }

        /// <summary>
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        public static void Error(Exception ex) { log.Error(ex); }

        /// <summary>
        /// This outputs a log entry at the <c>Interface</c> level.
        /// Normally, this means user input is required to continue...
        /// </summary>
        public static void Interface(string format, params object[] args) { log.Interface(format, args); }

        /// <summary>
        /// This outputs a log entry at the <c>Interface</c> level.
        /// Normally, this means user input is required to continue...
        /// </summary>
        public static void Interface(params object[] args) { log.Interface(args); }
        #endregion
    }

}
