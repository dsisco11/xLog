/*
 * Copyright 2017 David Sisco 
 */

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace xLog
{
    /// <summary>
    /// Performs the actual output part of the logging process
    /// </summary>
    public static class xLogEngine
    {
        #region Config
        /// <summary>
        /// Holds all logger settings
        /// </summary>
        public static xLogSettings Settings = new xLogSettings();
        #endregion

        #region Properties
        static StreamWriter FileStream;
        static int[] LineIndent;
        static string[] LogLevel_Name;
        static BlockingCollection<LogLine> Queue = new BlockingCollection<LogLine>();
        #endregion

        #region Constructors
        static xLogEngine()
        {
            uint MAX = (uint)LogLevel.All+1;
            LineIndent = new int[MAX];
            LogLevel_Name = new string[MAX];

            for (uint i=0; i<MAX; i++)
            {
                LineIndent[i] = 0;
                LogLevel_Name[i] = Enum.GetName(typeof(LogLevel), (LogLevel)i).ToUpper();
            }


            // Start the logging thread!
            Task.Factory.StartNew(() =>
            {
                if (Thread.CurrentThread.Name == null) Thread.CurrentThread.Name = "Logger";
                foreach (LogLine line in Queue.GetConsumingEnumerable())
                {
                    string fileFormattedString = (xLogEngine.Settings.stripXTERM ? XTERM.Strip(line.Text) : line.Text);

                    if (line.Level >= xLogEngine.Settings.MinFileLogLevel) FileStream.WriteLine(fileFormattedString);
                    if (line.Level >= xLogEngine.Settings.MinOutputLevel) XTERM.WriteLine(line.Text);
                }
            });

            /*AppDomain.CurrentDomain.ProcessExit += (EventHandler)delegate (object sender, EventArgs e)
            {
            };*/
        }
        
        public static string Get_Todays_LogFile() {
            return Path.Combine(xLogEngine.Settings.Log_Directory, string.Concat(DateTime.Now.ToString("yyyy_MM_dd"), xLogEngine.Settings.Log_File_Ext));
        }

        /// <summary>
        /// Begins logging to a file, overwriting the contents of the file if it already exists.
        /// </summary>
        public static void Begin(string logFile, LogLevel consoleLogLevel = LogLevel.Info, LogLevel fileLogLevel = LogLevel.Debug)
        {
            string logPath = Path.GetFullPath(logFile);
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            FileStream fs;
            try
            {
                fs = File.OpenWrite(logPath);
                fs.Seek(0, SeekOrigin.Begin);// Move to start of file.
                fs.SetLength(0);// Erase all contents.
                FileStream = new StreamWriter(fs);
                FileStream.AutoFlush = true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(Format_Exception(ex));
            }

            Settings.MinOutputLevel = consoleLogLevel;
            Settings.MinFileLogLevel = fileLogLevel;
        }

        /// <summary>
        /// Continues logging to a file if it already exists or creates it.
        /// </summary>
        public static void BeginAppend(string logFile, LogLevel consoleLogLevel = LogLevel.Info, LogLevel fileLogLevel = LogLevel.Debug)
        {
            string logPath = Path.GetFullPath(logFile);
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            try
            {
                FileStream = File.AppendText(logPath);
                FileStream.AutoFlush = true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(Format_Exception(ex));
            }

            xLogEngine.Settings.MinOutputLevel = consoleLogLevel;
            xLogEngine.Settings.MinFileLogLevel = fileLogLevel;
        }
        
        /// <summary>
        /// Releases the logfile
        /// </summary>
        public static void End()
        {
            Queue.CompleteAdding();// Stop accepting log messages
            FileStream.Dispose();//Just helps to ensure we release the file.
        }
        #endregion

        #region Output logic
        /// <summary>
        /// Outputs a line to both the log and output streams, if applicable
        /// </summary>
        internal static void OutputLine(LogLevel level, string Source, params object[] args)
        {
            string lineStr = "";
            for (int i = 0; i < args.Length; i++)
            {
                lineStr += string.Concat(args[i], " ");
            }

            OutputLine(level, Source, lineStr);
        }

        /// <summary>
        /// Outputs a line to both the log and output streams, if applicable
        /// </summary>
        internal static void OutputLine(LogLevel level, string Source, StackTrace Stack, params object[] args)
        {
            string lineStr = "";
            for (int i = 0; i < args.Length; i++)
            {
                lineStr += string.Concat(args[i], " ");
            }

            lineStr += "\n==[ Stack Trace ]==\n";
            if (Stack != null) lineStr += Stack.ToString();
            else lineStr += "<NULL>";

            OutputLine(level, Source, lineStr);
        }

        /// <summary>
        /// Outputs a line to both the log and output streams, if applicable
        /// </summary>
        internal static void OutputLine(LogLevel level, string Source, string format, params object[] args)
        {
            // Silence test
            if (level < xLogEngine.Settings.MinOutputLevel && level < xLogEngine.Settings.MinFileLogLevel) return;
            
            // Append the log level string
            string logLevelStr = "";
            if(xLogEngine.Settings.Show_Log_Levels) logLevelStr = string.Concat(Color_LogLevel_Title(level, Get_LogLevel_Title(level)), ": ");

            // Get indentation amount
            int indentCount = (LineIndent[(uint)level] + LineIndent[(uint)LogLevel.All]) * xLogEngine.Settings.IndentSize;
            string indentStr = new string(' ', indentCount);

            // Assemble the line string
            string fmtStr = (args != null && args.Any() ? string.Format(format, args) : format);
            string lineStr = Color_LogLine(level, string.Concat(indentStr, fmtStr));

            // Append the timestamp
            string timeStr = "";
            if (xLogEngine.Settings.showTimestamps) timeStr = string.Concat("[", DateTime.Now.ToString(xLogEngine.Settings.Timestamp_Format), "] ");

            string SourceStr = "";
            if (xLogEngine.Settings.showModuleNames)
            {
                SourceStr = "(System) ";
                if (Source != null) SourceStr = (Source + " ");
            }

            string formattedString = string.Concat(timeStr, SourceStr, logLevelStr, lineStr);
            /*
            string fileFormattedString = (Logger.stripXTERM ? XTERM.Strip(formattedString) : formattedString);

            if (level >= FileLogLevel) _FileStream.WriteLine(fileFormattedString);
            if (level >= OutputLevel) XTERM.WriteLine(formattedString);
            */
            Queue.Add(new LogLine() { Text = formattedString, Level = level });
        }

        internal static string Color_LogLine(LogLevel level, string msg)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return XTERM.blackBright(msg);
                case LogLevel.Info:
                case LogLevel.Debug:
                    return XTERM.white(msg);
                case LogLevel.Success:
                    return XTERM.green(msg);
                case LogLevel.Failure:
                    return XTERM.red(msg);
                case LogLevel.Warn:
                    return XTERM.yellow(msg);
                case LogLevel.Error:
                    return XTERM.red(msg);
                case LogLevel.Assert:
                    return XTERM.magenta(msg);
                case LogLevel.Interface:
                    return XTERM.cyan(msg);
                default:
                    return msg;
            }
        }

        internal static string Color_LogLevel_Title(LogLevel level, string name)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return XTERM.white(name);
                case LogLevel.Info:
                case LogLevel.Debug:
                    return XTERM.whiteBright(name);
                case LogLevel.Success:
                    return XTERM.greenBright(name);
                case LogLevel.Failure:
                    return XTERM.redBright(name);
                case LogLevel.Warn:
                    return XTERM.yellowBright(name);
                case LogLevel.Error:
                    return XTERM.redBright(name);
                case LogLevel.Assert:
                    return XTERM.magentaBright(name);
                case LogLevel.Interface:
                    return XTERM.cyanBright(name);
                default:
                    return name;
            }
        }

        /// <summary>
        /// Returns the string used to indicate a LogLevel
        /// </summary>
        internal static string Get_LogLevel_Title(LogLevel level)
        {
            return LogLevel_Name[(uint)level];
            /*
            switch (level)
            {
                case LogLevel.TRACE:
                    return "INFO";
                case LogLevel.Info:
                    return "INFO";
                case LogLevel.Debug:
                    return "DEBUG";
                case LogLevel.Success:
                    return "SUCCESS";
                case LogLevel.Warn:
                    return "WARN";
                case LogLevel.Error:
                    return "ERROR";
                case LogLevel.Assert:
                    return "ASSERT";
                case LogLevel.Interface:
                    return "INTERFACE";
                default:
                    return "undef";
            }
            */
        }
        
        public static string Format_Exception(Exception ex)
        {
            string MSG = ex.Message;
            string TRACE = ex.StackTrace;
            if (ex.InnerException != null)
            {
                MSG = string.Concat(MSG, " ", ex.InnerException.Message);
                TRACE = string.Concat(ex.InnerException.StackTrace, "\n\nOutter Stack Trace: ", ex.StackTrace);
            }

            return string.Concat(MSG, "\nStack Trace: ", TRACE);
        }
        #endregion

        #region Logging functions

        /// <summary>
        /// Checks for a condition; if the condition is <c>false</c>, outputs a specified message and displays a message box that shows the call stack.
        /// This method is equivalent to System.Diagnostics.Debug.Assert, however, it was modified to also write to the Logger output.
        /// Borrowed from <c>SteamKit2</c>
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate. If the condition is <c>true</c>, the specified message is not sent and the message box is not displayed.</param>
        /// <param name="category">The category of the message.</param>
        /// <param name="message">The message to display if the assertion fails.</param>
        public static void Assert(bool condition, string category, string message)
        {
            // make use of .NET's assert facility first
            System.Diagnostics.Debug.Assert(condition, string.Concat(category, ": ", message));

            // then spew to our debuglog, so we can get info in release builds
            if (!condition)
                OutputLine(LogLevel.Assert, category, "Assertion Failed! " + message);
        }

        /// <summary>
        /// Adds a level of indentation to the specified LogLevel line type.
        /// </summary>
        /// <param name="level">log line type to indent</param>
        public static void Indent(LogLevel level)
        {
            LineIndent[(uint)level] += 1;
        }

        /// <summary>
        /// Removes a level of indentation from the specified LogLevel line type.
        /// </summary>
        /// <param name="level">log line type to unindent</param>
        public static void Unindent(LogLevel level)
        {
            LineIndent[(uint)level] = Math.Max(0, LineIndent[(uint)level] - 1);
        }

        /// <summary>
        /// This outputs a log entry at the <c>Info</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Info(string Source, string format, params object[] args)
        {
            OutputLine(LogLevel.Info, Source, format, args);
        }
        /// <summary>
        /// This outputs a log entry at the <c>Info</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Info(string Source, params object[] args)
        {
            OutputLine(LogLevel.Info, Source, args);
        }


        /// <summary>
        /// This outputs a log entry at the <c>Debug</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Debug(string Source, string format, params object[] args)
        {
            OutputLine(LogLevel.Debug, Source, format, args);
        }
        /// <summary>
        /// This outputs a log entry at the <c>Debug</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Debug(string Source, params object[] args)
        {
            OutputLine(LogLevel.Debug, Source, args);
        }


        internal static StackTrace Get_Trace(int frameOffset=0)
        {
            return new StackTrace(frameOffset+2);// we add 2 to the offset to account for this function aswell as the calling one
        }
        /// <summary>
        /// This outputs a log entry at the <c>Trace</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Trace(string Source, string format, params object[] args)
        {
            format += string.Concat("\n==[ Stack Trace ]==\n", Get_Trace());
            OutputLine(LogLevel.Trace, Source, format, args);
        }
        /// <summary>
        /// This outputs a log entry at the <c>Trace</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Trace(string Source, params object[] args)
        {
            OutputLine(LogLevel.Trace, Source, Get_Trace(), args);
        }


        /// <summary>
        /// This outputs a log entry at the <c>Trace</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Trace(string Source, int frameOffset, string format, params object[] args)
        {
            format += string.Concat("\n==[ Stack Trace ]==\n", Get_Trace(frameOffset));
            OutputLine(LogLevel.Trace, Source, format, args);
        }
        /// <summary>
        /// This outputs a log entry at the <c>Trace</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Trace(string Source, int frameOffset, params object[] args)
        {
            OutputLine(LogLevel.Trace, Source, Get_Trace(frameOffset), args);
        }


        /// <summary>
        /// This outputs a log entry at the <c>Success</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Success(string Source, string format, params object[] args)
        {
            OutputLine(LogLevel.Success, Source, format, args);
        }
        /// <summary>
        /// This outputs a log entry at the <c>Success</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Success(string Source, params object[] args)
        {
            OutputLine(LogLevel.Success, Source, args);
        }


        /// <summary>
        /// This outputs a log entry at the <c>Failure</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Failure(string Source, string format, params object[] args)
        {
            OutputLine(LogLevel.Failure, Source, format, args);
        }
        /// <summary>
        /// This outputs a log entry at the <c>Failure</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Failure(string Source, params object[] args)
        {
            OutputLine(LogLevel.Failure, Source, args);
        }


        /// <summary>
        /// This outputs a log entry at the <c>Warning</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Warn(string Source, string format, params object[] args)
        {
            OutputLine(LogLevel.Warn, Source, format, args);
        }
        /// <summary>
        /// This outputs a log entry at the <c>Warning</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Warn(string Source, params object[] args)
        {
            OutputLine(LogLevel.Warn, Source, args);
        }


        /// <summary>
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Error(string Source, string format, params object[] args)
        {
            OutputLine(LogLevel.Error, Source, format, args);
        }
        /// <summary>
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Error(string Source, params object[] args)
        {
            OutputLine(LogLevel.Error, Source, args);
        }
        /// <summary>
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Error(string Source, Exception ex)
        {
            OutputLine(LogLevel.Error, Source, Format_Exception(ex));
        }
        /// <summary>
        /// This outputs a log entry at the <c>Error</c> level, Specifically for Null item errors.
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        /// <param name="nullObjectName">The name of the object in question.</param>
        public static void ErrorNull(string Source, string nullObjectName)
        {
            OutputLine(LogLevel.Error, Source, string.Concat("\"", nullObjectName, "\" IS NULL!"));
        }


        /// <summary>
        /// This outputs a log entry at the <c>Interface</c> level.
        /// Normally, this means user input is required to continue...
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Interface(string Source, string format, params object[] args)
        {
            OutputLine(LogLevel.Interface, Source, format, args);
        }
        /// <summary>
        /// This outputs a log entry at the <c>Interface</c> level.
        /// Normally, this means user input is required to continue...
        /// </summary>
        /// <param name="Source">A Label to be pre-appended to the log line.</param>
        public static void Interface(string Source, params object[] args)
        {
            OutputLine(LogLevel.Interface, Source, args);
        }

        #endregion
    }

}
