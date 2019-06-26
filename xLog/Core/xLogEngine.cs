using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace xLog
{
    /// <summary>
    /// Manages the actual output part of the logging process
    /// </summary>
    public static class xLogEngine
    {
        public static DateTime UNIX_TIMESTAMP_ZERO_POINT = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

        #region Config
        public static LogEngineSettings Settings = new LogEngineSettings();
        #endregion

        #region Properties
        private static int Disposed = 0;
        private static Task writerTask;
        private static LineFormatter DefaultFormatter = new LineFormatter();

        /// <summary>
        /// Tracks current line indentation for all log levels
        /// </summary>
        private static int[] LineIndent;
        /// <summary>
        /// The output names for <see cref="ELogLevel"/>s
        /// </summary>
        private static string[] LogLevel_Name;

        /// <summary>
        /// The log line queue which our <see cref="writerTask"/> processes.
        /// </summary>
        private static ConcurrentQueue<LogLine> Queue = null;
        /// <summary>
        /// Allows signaling the log writer thread that there are lines in queue.
        /// </summary>
        private static AutoResetEvent Queue_Update_Signal = null;
        /// <summary>
        /// Allows cancelling the log writer thread.
        /// </summary>
        private static CancellationTokenSource writerCancel = null;
        private static ManualResetEvent writerFree;

        /// <summary>
        /// A list of all non-moving lines within the console
        /// </summary>
        private static List<StaticConsoleLine> StaticLines = new List<StaticConsoleLine>();
        /// <summary>
        /// Tracks the length status of all static lines currently shown on-screen
        /// </summary>
        private static Stack<StaticConsoleLine> Static_Display_Stack = new Stack<StaticConsoleLine>();
        /// <summary>
        /// Allows signaling the log writer thread to update the frozen line's text.
        /// </summary>
        private static AutoResetEvent StaticLine_Update_Signal = new AutoResetEvent( false );
        /// <summary>
        /// Handles showing user input
        /// </summary>
        private static WeakReference<StaticConsoleLine> CursorControlLine = new WeakReference<StaticConsoleLine>(null);

        /// <summary>
        /// Holds a list of all active log line consumers
        /// </summary>
        private static List<ILogLineConsumer> Consumers = new List<ILogLineConsumer>();
        /// <summary>
        /// Private Consumer used for writing output to a log file.
        /// </summary>
        private static ILogLineConsumer FileConsumer = null;
        #endregion

        #region Accessors
        private static DateTime Now { get { return Settings.Use_UTC_Time ? DateTime.UtcNow : DateTime.Now; } }
        private static ILogLineFormatter CurrentFormatter => (Settings.Formatter ?? DefaultFormatter);
        #endregion
    
        #region Constructors
        static xLogEngine()
        {
            byte LOGLEVEL_MAX = (byte)ELogLevel.MAX;
            LineIndent = new int[LOGLEVEL_MAX];
            LogLevel_Name = new string[LOGLEVEL_MAX];
            Settings.LogLevel_Name_Shown = new bool[LOGLEVEL_MAX];

            // Set all Line indentation states to 0 and Populate our LogLevel name map
            for (byte i = 0; i < LOGLEVEL_MAX; i++)
            {
                LineIndent[i] = 0;
                LogLevel_Name[i] = Enum.GetName(typeof(ELogLevel), (ELogLevel)i).ToUpper();
                Settings.LogLevel_Name_Shown[i] = true;
            }

            Queue = new ConcurrentQueue<LogLine>();
            Queue_Update_Signal = new AutoResetEvent( false );
            writerFree = new ManualResetEvent( false );

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");// Set the current culture to US English. (I forget why, maybe it had something to do with the XTERM emulator and parsing the format? Probably.)
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;

            Start_Processing();
        }

        private static void Shutdown()
        {
            Dispose();
        }

        private static void OnDomainUnload(object sender, EventArgs e)
        {
            Shutdown();
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            Shutdown();
        }

        /// <summary>
        /// Called by the log writer to release it's resources
        /// </summary>
        private static void Processing_Thread_Release()
        {
            if (writerCancel != null)
            {
                writerCancel.Dispose();
                writerCancel = null;
            }

            writerFree.Set();
        }

        private static void Start_Processing()
        {
            Stop_Processing();
            // Create a new cancellation token
            writerCancel = new CancellationTokenSource();
            writerFree.Reset();
            // Start our log writer.
            writerTask = Task.Factory.StartNew(Threaded_Log_Writer, writerCancel.Token, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Stops the active log writer and waits for it to exit
        /// </summary>
        public static void Stop_Processing()
        {
            if (writerCancel != null)
            {
                writerCancel.Cancel();
                writerFree.WaitOne();

                if (writerTask != null)
                {
                    writerTask.Wait();
                    writerTask.Dispose();
                    writerTask = null;
                }
            }
        }
    
        #endregion

        #region Finalizers
        /// <summary>
        /// Releases all logging system resources permanently
        /// </summary>
        private static void Dispose()
        {
            if (Interlocked.Exchange(ref Disposed, 1) == 0)
            {
                Stop_Processing();
                Queue = null;

                if (writerFree != null)
                {
                    writerFree.Dispose();
                    writerFree = null;
                }

                if (Queue_Update_Signal != null)
                {
                    Queue_Update_Signal.Dispose();
                    Queue_Update_Signal = null;
                }

                if (StaticLine_Update_Signal != null)
                {
                    StaticLine_Update_Signal.Dispose();
                    StaticLine_Update_Signal = null;
                }

                Static_Display_Stack = null;
                if (StaticLines != null)
                {
                    foreach (var line in StaticLines)
                    {
                        line.Dispose();
                    }

                    StaticLines.Clear();
                    StaticLines = null;
                }

                if (Consumers != null)
                {
                    foreach (ILogLineConsumer consumer in Consumers)
                    {
                        consumer.Dispose();
                    }
                    Consumers.Clear();
                }
            }
        }
        #endregion

        #region Log Writer Thread
        private static void Threaded_Log_Writer(object state)
        {
            try
            {
                CancellationToken cancelToken = (CancellationToken)state;
                if (Thread.CurrentThread.Name == null) Thread.CurrentThread.Name = "xLog Writer";
                WaitHandle[] Signals = new WaitHandle[] { cancelToken.WaitHandle, StaticLine_Update_Signal, Queue_Update_Signal };

                while ( !cancelToken.IsCancellationRequested )
                {
                    WaitHandle.WaitAny( Signals );
                    cancelToken.ThrowIfCancellationRequested();

                    Clear_Static_Lines();
                    RunQueue();
                    Print_Static_Lines();

                    foreach(ILogLineConsumer consumer in Consumers)
                    {
                        consumer.Flush();
                    }
                }
            }
            catch (OperationCanceledException)// Ignore these
            {
            }
            catch (Exception ex)
            {
                System.Console.WriteLine( "=====[ LOGGING SYSTEM ENCOUNTERED A FATAL ERROR ]=====" );
                System.Console.WriteLine( CurrentFormatter.Format_Exception(ex) );
                throw;
            }
            finally
            {
                foreach(ILogLineConsumer consumer in Consumers)
                {
                    consumer.Flush();
                }

                Processing_Thread_Release();
            }
        }

        private static void RunQueue()
        {
            while (Queue.TryDequeue(out LogLine line) != false)
            {
                try
                {
                    if ( !xLogEngine.Settings.AllowXTERM )
                    {
                        line.Text = XTERM.Strip( line.Text );
                    }

                    // Consumer output
                    if (line.Level >= xLogEngine.Settings.LoggingLevel && line.Level != ELogLevel.Console)// NEVER write dummy lines to anything but an active console interface, not to file or to a log-network stream
                    {
                        string FormattedString = null;
                        if (xLogEngine.Settings.AllowXTERM && xLogEngine.Settings.stripXTERM)
                        {
                            FormattedString = XTERM.Strip(line.Text);
                        }
                        else
                        {
                            FormattedString = line.Text;
                        }

                        foreach (ILogLineConsumer consumer in Consumers)
                        {
                            consumer.Consume( line );
                        }
                    }
                
                    // Console output
                    System.Diagnostics.Debug.WriteLine( line.Text );
                    if (line.Level >= xLogEngine.Settings.OutputLevel)
                    {
                        if (xLogEngine.Settings.AllowXTERM )
                        {
                            XTERM.WriteLine( line.Text );
                        }
                        else
                        {
                            System.Console.Write( line.Text );
                        }
                    }
                }
                catch (Exception ex) when (ex.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture( ex.InnerException ).Throw();
                }
            }
        }
        #endregion

        #region Consumers
        public static void AddConsumer(ILogLineConsumer consumer)
        {
            if (object.ReferenceEquals(consumer, null))
                throw new ArgumentNullException(nameof(consumer));

            lock( Consumers )
            {
                Consumers.Add( consumer );
            }
        }

        public static void RemoveConsumer(ILogLineConsumer consumer)
        {
            if (object.ReferenceEquals(consumer, null))
                throw new ArgumentNullException(nameof(consumer));

            lock ( Consumers )
            {
                Consumers.Remove( consumer );
            }
        }
        #endregion

        #region Logging Startup
        public static string Get_Todays_LogFile() { return string.Concat(Now.ToString(xLogEngine.Settings.LogFile_Date_Format), xLogEngine.Settings.Log_File_Ext); }

        /// <summary>
        /// Finalizes a given log filename by ensuring the specified Log_Directory and Log_File_Ext are attached to the path.
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        private static string Finalize_Log_FileName(string FileName)
        {
            if (!FileName.EndsWith(xLogEngine.Settings.Log_File_Ext))
            {
                FileName = string.Concat(FileName, xLogEngine.Settings.Log_File_Ext);
            }

            if (!FileName.StartsWith(xLogEngine.Settings.Log_Directory))
            {
                FileName = Path.Combine(xLogEngine.Settings.Log_Directory, FileName);
            }

            return FileName;
        }

        /// <summary>
        /// Begins logging to a file named by the current date. When the date changes the logger will automatically switch to a new file.
        /// </summary>
        public static void Begin()
        {
            Resume(Get_Todays_LogFile());

            DateTime today = Now.Date;
            DateTime tomorrow = Now.AddDays(1);

            // NOTE (12/07/17): IF THE DATES BREAK THEN UNCOMMENT THIS LINE
            //tomorrow = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0).AddDays(1);

            TimeSpan dt = tomorrow.Subtract(today);
            Timing.setTimeout(dt, () =>
            {
                Banner(ELogLevel.Info, nameof(xLogEngine), "SWITCHING LOG FILES -> {0}", Get_Todays_LogFile());
                Begin();
            });
        }

        /// <summary>
        /// Begins logging to a file, overwriting the contents of the file if it already exists.
        /// </summary>
        public static void Begin(string logFile)
        {
    #if DEBUG_VERBOSE
            xLogEngine.Settings.OutputLevel = ELogLevel.All;
            xLogEngine.Settings.LoggingLevel = ELogLevel.All;
    #endif
            string filePath = Finalize_Log_FileName( logFile );
            try
            {
                Stop_Processing();
                if (FileConsumer != null)
                {
                    RemoveConsumer( FileConsumer );
                    FileConsumer.Dispose();
                }

                FileConsumer = new FileLogConsumer(filePath, FileMode.Create);
                AddConsumer(FileConsumer);

                Start_Processing();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(CurrentFormatter.Format_Exception( ex ) );
                throw;
            }
    
        }

        /// <summary>
        /// Continues logging to a file if it already exists or creates it.
        /// </summary>
        public static void Resume(string logFile)
        {
    #if DEBUG_VERBOSE
            xLogEngine.Settings.OutputLevel = ELogLevel.All;
            xLogEngine.Settings.LoggingLevel = ELogLevel.All;
    #endif

            string filePath = Finalize_Log_FileName( logFile );
            try
            {
                Stop_Processing();
                if (FileConsumer != null)
                {
                    RemoveConsumer(FileConsumer);
                    FileConsumer.Dispose();
                }

                FileConsumer = new FileLogConsumer(filePath, FileMode.Append);
                AddConsumer(FileConsumer);

                Start_Processing();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(CurrentFormatter.Format_Exception(ex));
                throw;
            }

            Banner(ELogLevel.Info, nameof(xLogEngine), "Resuming Logs");
        }

        /// <summary>
        /// Starts the logger without initializing a file, meaning output goes to the console *ONLY*.
        /// </summary>
        public static void Start()
        {
    #if DEBUG_VERBOSE
            xLogEngine.Settings.OutputLevel = ELogLevel.All;
    #endif

            try
            {
                Start_Processing();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(CurrentFormatter.Format_Exception(ex));
                throw;
            }
        }
        #endregion

        #region Unhandled Exception Handler
        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Banner(ELogLevel.Error, "System", "UNHANDLED EXCEPTION\r\nSHUTTING DOWN");
            Error(null, CurrentFormatter.Format_Exception(e.ExceptionObject as Exception));
            Debug(null, Environment.StackTrace);
    #if DEBUG
    #else
            Environment.Exit(1);// only if NOT debugging...
    #endif
        }
        #endregion

        #region Output Logic
        /// <summary>
        /// Outputs a line all applicable <see cref="ILogLineConsumer"/>s
        /// </summary>
        internal static void OutputLine(ELogLevel Level, ICollection<RawLogLine> lines)
        {
            foreach(RawLogLine line in lines)
            {
                OutputLine(Level, line);
            }
        }

        /// <summary>
        /// Outputs a line all applicable <see cref="ILogLineConsumer"/>s
        /// </summary>
        internal static void OutputLine(ELogLevel Level, RawLogLine line)
        {
            OutputLine(Level, line.Source, line.Format, line.Args);
        }

        /// <summary>
        /// Outputs a line all applicable <see cref="ILogLineConsumer"/>s
        /// </summary>
        internal static void OutputLine(ELogLevel Level, string Source, params object[] args)
        {
            string lineStr = "";
            for (int i = 0; i < args.Length; i++)
            {
                lineStr += string.Concat(args[i], " ");
            }

            OutputLine(Level, Source, lineStr);
        }

        /// <summary>
        /// Outputs a line all applicable <see cref="ILogLineConsumer"/>s
        /// </summary>
        internal static void OutputLine(ELogLevel Level, string Source, StackTrace Stack, params object[] args)
        {
            string lineStr = "";
            for (int i = 0; i < args.Length; i++)
            {
                lineStr += string.Concat(args[i], " ");
            }

            lineStr += "\n==[ Stack Trace ]==\n";
            if (Stack != null) lineStr += Stack.ToString();
            else lineStr += "<NULL>";

            OutputLine(Level, Source, lineStr);
        }

        /// <summary>
        /// Outputs a line all applicable <see cref="ILogLineConsumer"/>s
        /// </summary>
        internal static void OutputLine(ELogLevel Level, string Source, string format, params object[] args)
        {
            string timeStr = string.Empty;
            string sourceStr = string.Empty;
            string logLevelStr = string.Empty;
            string indentStr = string.Empty;
            string lineStr = string.Empty;

            if (Level < ELogLevel.MAX)
            {
                // Silence test
                if (Level < xLogEngine.Settings.OutputLevel && Level < xLogEngine.Settings.LoggingLevel) return;

                // Cast Level to int and clamp to safe range
                int iLevel = Math.Max(0, Math.Min((int)Level, (int)ELogLevel.MAX));
                // Get indentation amount
                int indentCount = (LineIndent[iLevel] + LineIndent[(int)ELogLevel.All]) * xLogEngine.Settings.IndentSize;
                indentStr = new string(' ', indentCount);

                // Append the timestamp
                if (xLogEngine.Settings.showTimestamps) timeStr = string.Concat("[", Now.ToString(xLogEngine.Settings.Timestamp_Format), "] ");

                if (xLogEngine.Settings.showSources)
                {
                    if (!string.IsNullOrEmpty(Source)) sourceStr = string.Concat(Source, " ");
                    else if (object.ReferenceEquals(Source, null)) sourceStr = string.Concat(System.AppDomain.CurrentDomain.FriendlyName, " ");
                }

                // Append the log level string
                if (xLogEngine.Settings.Show_LogLevel_Names && xLogEngine.Settings.LogLevel_Name_Shown[iLevel]) logLevelStr = string.Concat(CurrentFormatter.Color_LogLevel_Title(Level, LogLevel_Name[iLevel]), ": ");
            }

            // Assemble the line string
            string fmtStr = (args != null && args.Any() ? string.Format(format, args) : format);
            lineStr = CurrentFormatter.Color_LogLine(Level, fmtStr);
            // Assemble the final string
            string FinalText = string.Concat(timeStr, sourceStr, logLevelStr, indentStr, lineStr);

            uint ts = (uint)(DateTime.UtcNow - UNIX_TIMESTAMP_ZERO_POINT).TotalSeconds;
            Queue.Enqueue(new LogLine() { Text = FinalText, Level = Level, Timestamp = ts });
            Queue_Update_Signal.Set();
        }
    
        // XXX: Need to make a better system for handling input Prompting.
        //  Like showing user input as a frozen line at the bottom of the screen and injecting it into the logstream once the input is submitted...
        /// <summary>
        /// Appends text to the end of the previous line
        /// </summary>
        internal static void Append(ELogLevel level, string format, params object[] args)
        {
            string formattedString = string.Format(format, args);
            //if (Stream != null && level >= FileLogLevel) Stream.Write(formattedString);
            if (level >= xLogEngine.Settings.OutputLevel)
            {
                XTERM.Write(formattedString);
            }
        }

        internal static StackTrace Get_Trace(int offset = 0)
        {// We increase offset by two to account for this function and the logging function that is calling it.
            return new StackTrace(offset + 2, true);
        }

        /// <summary>
        /// Returns an exceptions stacktrace adjusted and offset so it originates before any of the logging method calls which lead here.
        /// <para>
        /// In laymans terms this function returns a modified <see cref="StackTrace"/> object that omits any functions marked with the <see cref="LoggingMethod"/> attribute,
        /// this helps make stack traces much more useful and less cluttered.
        /// </para>
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        [LoggingMethod]
        internal static Exception Make_Exception_External(Exception ex)
        {
            int skip = 0;
            var stack = new StackTrace();

            StackFrame[] Frames = stack.GetFrames();
            for (int i = 0; i < Frames.Length; i++)
            {
                StackFrame Frame = Frames[i];
                System.Reflection.MethodBase method = Frame.GetMethod();
                object[] attr = method.GetCustomAttributes(typeof(LoggingMethod), true);
                if (!ReferenceEquals(attr, null) && attr.Length > 0)
                {
                    skip++;
                }
                else
                {// stop at the first method that ISNT one we wanna skip, we only skip at the beginning of the stack
                    break;
                }
            }
        
            return ex.SetStackTrace( new StackTrace(skip, true) );
        }

        [LoggingMethod]
        public static string Format_Exception(Exception ex)
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
    
        #endregion

        #region Static Lines
        public static Action<StaticConsoleLine> Register_Static_Line(StaticConsoleLine line)
        {
            lock (StaticLines)
            {
                StaticLines.Add(line);
            }
            return (StaticConsoleLine o) =>
            {
                StaticLine_Update_Signal.Set();
            };
        }

        public static void Unregister_Static_Line(StaticConsoleLine line)
        {
            lock (StaticLines)
            {
                StaticLines.Remove( line );
                if ( CursorControlLine.TryGetTarget(out StaticConsoleLine ccl) )
                {
                    if ( ReferenceEquals(line, CursorControlLine) )
                    {
                        CursorControlLine.SetTarget( null );
                    }
                }
            }

            StaticLine_Update_Signal.Set();
        }

        public static void Move_Static_Line(StaticConsoleLine line, int offset)
        {
            if (offset == 0) return;

            lock (StaticLines)
            {
                int idx = StaticLines.IndexOf(line);
                StaticConsoleLine Target = null;
                // Find the position indicated by our offset
                int tp = idx + offset;
                int sp = Math.Max(Math.Min(tp, StaticLines.Count - 2), 0);
                // Who is in our way?
                Target = StaticLines[sp];

                if (Target != null)
                {
                    // Find the current index of our Target
                    int tidx = StaticLines.IndexOf(Target);
                    StaticLines.Remove(line);
                    StaticLines.Insert(tidx, line);
                    StaticLine_Update_Signal.Set();
                }
            }
        }

        /// <summary>
        /// Allows a line to request control of the console cursor.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static bool Request_Cursor_Control(StaticConsoleLine line)
        {
            lock (StaticLines)
            {
                if ( !CursorControlLine.TryGetTarget(out _) )
                {
                    CursorControlLine.SetTarget(line);
                    StaticLine_Update_Signal.Set();
                    return true;
                }
            }

            return false;
        }

        private static void Clear_Console_Line(int LineLength)
        {
            // Jump to beginning of line and Overwrite all the previous characters with spaces
            System.Console.Write("\r");
            System.Console.Write(new string(' ', Math.Min(System.Console.BufferWidth, LineLength) - 1));
            System.Console.Write("\r");
        }

        /// <summary>
        /// Clears all static line text from the console.
        /// </summary>
        private static void Clear_Static_Lines()
        {
            lock (StaticLines)
            {
                while (Static_Display_Stack.Count > 0)
                {
                    StaticConsoleLine line = Static_Display_Stack.Pop();
                    if (line == null) continue;

                    Clear_Console_Line(line.Current_Display_Length);
                    line.Current_Display_Length = 0;
                }
            }
        }


        /// <summary>
        /// Used by the logger thread to update the display for a line
        /// </summary>
        /// <param name="line"></param>
        private static void Bump_Line(StaticConsoleLine line)
        {
            if (!string.IsNullOrEmpty(line.Buffer))
            {
                XTERM.Write(line.Buffer);
                line.Current_Display_Length = line.Buffer.Length;
                Static_Display_Stack.Push(line);
            }
        }
        /// <summary>
        /// Prints all static lines into the console.
        /// </summary>
        private static void Print_Static_Lines()
        {
            lock (StaticLines)
            {
                foreach(StaticConsoleLine line in StaticLines)
                {
                    if ( !ReferenceEquals(line, CursorControlLine) )
                    {
                        Bump_Line( line );
                    }
                }

                // The cursor control line must ALWAYS come last!
                if ( CursorControlLine.TryGetTarget(out StaticConsoleLine ccl) )
                {
                    int y = System.Console.CursorTop;
                    Bump_Line( ccl );
                    System.Console.CursorLeft = ccl.CursorPos;
                    System.Console.CursorTop = y;
                }

                StaticLine_Update_Signal.Reset();
            }
        }
        #endregion

        #region Accessors
        public static string Get_LogLevel_Name(ELogLevel Level)
        {
            if (Level >= ELogLevel.MAX || Level <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Level));
            }

            return LogLevel_Name[(int)Level];
        }
        public static void Set_LogLevel_Name(ELogLevel Level, string Name)
        {
            if (Level >= ELogLevel.MAX || Level <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Level));
            }

            LogLevel_Name[(int)Level] = Name;
        }
        #endregion

        #region Output functions

        /// <summary>
        /// Adds a level of indentation to the specified LogLevel line type.
        /// </summary>
        /// <param name="Level">log line type to indent</param>
        [LoggingMethod]
        public static void Indent(ELogLevel Level)
        {
            int iLevel = Math.Max(0, Math.Min((int)ELogLevel.MAX, (int)Level));
            LineIndent[iLevel] += 1;
        }

        /// <summary>
        /// Removes a level of indentation from the specified LogLevel line type.
        /// </summary>
        /// <param name="Level">log line type to unindent</param>
        [LoggingMethod]
        public static void Unindent(ELogLevel Level)
        {
            int iLevel = Math.Max(0, Math.Min((int)ELogLevel.MAX, (int)Level));
            LineIndent[iLevel] = Math.Max(0, LineIndent[iLevel] - 1);
        }
    
        /// <summary>
        /// Checks for a condition; if the condition is <c>false</c>, outputs a specified message and displays a message box that shows the call stack.
        /// This method is equivalent to System.Diagnostics.Debug.Assert, however, it was modified to also write to the Logger output.
        /// Borrowed from <c>SteamKit2</c>
        /// </summary>
        /// <param name="Condition">The conditional expression to evaluate. If the condition is <c>true</c>, the specified message is not sent and the message box is not displayed.</param>
        /// <param name="Origin">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        /// <param name="Message">The message to display if the assertion fails.</param>
        [LoggingMethod]
	    public static void Assert(bool Condition, string Origin, string Message)
        {
            CurrentFormatter.Assert(ref Origin, ref Message);
            // make use of .NET's assert facility first
            System.Diagnostics.Debug.Assert(Condition, string.Concat(Origin, ": ", Message));

            // then spew to our debuglog, so we can get info in release builds
            if (!Condition)
            {
                OutputLine(ELogLevel.Assert, Origin, Message);
            }
        }
    
        /// <summary>
        /// Use to display generic log messages
        /// This outputs a log entry at the <c>Info</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Info(string Source, string Format, params object[] args)
        {
            CurrentFormatter.Info(ref Source, ref Format, args);
            OutputLine(ELogLevel.Info, Source, Format, args);
        }
        /// <summary>
        /// Use to display generic log messages
        /// This outputs a log entry at the <c>Info</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Info(string Source, params object[] args)
        {
            CurrentFormatter.Info(ref Source, args);
            OutputLine(ELogLevel.Info, Source, args);
        }


        /// <summary>
        /// Use to display unlogged console messages.
        /// Using <see cref="Console.Write"/> or <see cref="Console.WriteLine"/> will corrupt the displayed text in the console window.
        /// </summary>
        [LoggingMethod]
        public static void Console(string Format, params object[] args)
        {
            CurrentFormatter.Dummy(ref Format, args);
            OutputLine(ELogLevel.Console, null, Format, args);
        }
        /// <summary>
        /// Use to display unlogged console messages.
        /// Using <see cref="Console.Write"/> or <see cref="Console.WriteLine"/> will corrupt the displayed text in the console window.
        /// </summary>
        [LoggingMethod]
        public static void Console(params object[] args)
        {
            CurrentFormatter.Dummy(args);
            OutputLine(ELogLevel.Console, null, args);
        }

        /// <summary>
        /// Indicates information which is only useful for debugging
        /// This outputs a log entry at the <c>Debug</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Debug(string Source, string Format, params object[] args)
        {
            CurrentFormatter.Debug(ref Source, ref Format, args);
            OutputLine(ELogLevel.Debug, Source, Format, args);
        }
        /// <summary>
        /// Indicates information which is only useful for debugging
        /// This outputs a log entry at the <c>Debug</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Debug(string Source, params object[] args)
        {
            CurrentFormatter.Debug(ref Source, args);
            OutputLine(ELogLevel.Debug, Source, args);
        }


        /// <summary>
        /// This outputs a log entry at the <c>Trace</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Trace(string Source, string format, params object[] args)
        {
            CurrentFormatter.Trace(ref Source, ref format, args);
            OutputLine(ELogLevel.Trace, Source, format, args);
        }
        /// <summary>
        /// This outputs a log entry at the <c>Trace</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Trace(string Source, params object[] args)
        {
            CurrentFormatter.Trace(ref Source, args);
            OutputLine(ELogLevel.Trace, Source, Get_Trace(), args);
        }


        /// <summary>
        /// This outputs a log entry at the <c>Trace</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Trace(string Source, int frameOffset, string format, params object[] args)
        {
            CurrentFormatter.Trace(ref Source, ref frameOffset, ref format, args);
            OutputLine(ELogLevel.Trace, Source, format, args);
        }
        /// <summary>
        /// This outputs a log entry at the <c>Trace</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Trace(string Source, int frameOffset, params object[] args)
        {
            xLogEngine.OutputLine(ELogLevel.Trace, Source, Get_Trace(frameOffset), args);
        }


        /// <summary>
        /// Indicates an operation's success
        /// This outputs a log entry at the <c>Success</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Success(string Source, string format, params object[] args)
        {
            CurrentFormatter.Success(ref Source, ref format, args);
            OutputLine(ELogLevel.Success, Source, format, args);
        }
        /// <summary>
        /// Indicates an operation's success
        /// This outputs a log entry at the <c>Success</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Success(string Source, params object[] args)
        {
            CurrentFormatter.Success(ref Source, args);
            OutputLine(ELogLevel.Success, Source, args);
        }


        /// <summary>
        /// Indicates an Acceptable/Expected operation failure
        /// This outputs a log entry at the <c>Failure</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Failure(string Source, string format, params object[] args)
        {
            CurrentFormatter.Failure(ref Source, ref format, args);
            OutputLine(ELogLevel.Failure, Source, format, args);
        }
        /// <summary>
        /// Indicates an Acceptable/Expected operation failure
        /// This outputs a log entry at the <c>Failure</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Failure(string Source, params object[] args)
        {
            CurrentFormatter.Failure(ref Source, args);
            OutputLine(ELogLevel.Failure, Source, args);
        }

    
        /// <summary>
        /// Indicates an event which will NOT cause an operation to fail but which the user should be aware of
        /// This outputs a log entry at the <c>Warning</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Warn(string Source, string format, params object[] args)
        {
            CurrentFormatter.Warn(ref Source, ref format, args);
            OutputLine(ELogLevel.Warn, Source, format, args);
        }
        /// <summary>
        /// Indicates an event which will NOT cause an operation to fail but which the user should be aware of
        /// This outputs a log entry at the <c>Warning</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Warn(string Source, params object[] args)
        {
            CurrentFormatter.Warn(ref Source, args);
            OutputLine(ELogLevel.Warn, Source, args);
        }


        /// <summary>
        /// Indicates an event which will cause an operation to fail
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Error(string Source, string format, params object[] args)
        {
            CurrentFormatter.Error(ref Source, ref format, args);
            OutputLine(ELogLevel.Error, Source, format, args);
        }
        /// <summary>
        /// Indicates an event which will cause an operation to fail
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Error(string Source, params object[] args)
        {
            CurrentFormatter.Error(ref Source, args);
            OutputLine(ELogLevel.Error, Source, args);
        }
        /// <summary>
        /// Indicates an event which will cause an operation to fail
        /// This outputs a log entry at the <c>Error</c> level.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Error(string Source, Exception ex)
        {
            CurrentFormatter.Error(ref Source, ex, out string Message);
            OutputLine(ELogLevel.Error, Source, Message);
        }
        /// <summary>
        /// Indicates an event which will cause an operation to fail
        /// This outputs a log entry at the <c>Error</c> level, Specifically for Null item errors.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        /// <param name="paramName">The name of the object in question.</param>
        [LoggingMethod]
        public static string ErrorNull(string Source, string paramName)
        {
            CurrentFormatter.ErrorNull(ref Source, paramName, out string Message);
            OutputLine(ELogLevel.Error, Source, Message);
            return Message;
        }

        /// <summary>
        /// Indicates an event which will cause an operation to fail.
        /// Additionally throws an <see cref="ArgumentNullException"/> at the location of the calling code.
        /// This outputs a log entry at the <c>Error</c> level, Specifically for Null argument errors.
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        /// <param name="paramName">The name of the object in question.</param>
        [LoggingMethod]
        public static void ErrorNullThrow(string Source, string paramName)
        {
            CurrentFormatter.ErrorNull(ref Source, paramName, out string Message);
            OutputLine(ELogLevel.Error, Source, Message);

            var originEx = new ArgumentNullException(paramName);
            var ex = Make_Exception_External(originEx);
            // throw the exception from the place where the logging methods got called originally, though the debugger WILL break here...
            ExceptionDispatchInfo.Capture( ex ).Throw();
        }


        /// <summary>
        /// This outputs a log entry at the <c>Interface</c> level.
        /// Normally, this means user input is required to continue...
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Interface(string Source, string format, params object[] args)
        {
            CurrentFormatter.Interface(ref Source, ref format, args);
            OutputLine(ELogLevel.Interface, Source, format, args);
        }
        /// <summary>
        /// This outputs a log entry at the <c>Interface</c> level.
        /// Normally, this means user input is required to continue...
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Interface(string Source, params object[] args)
        {
            CurrentFormatter.Interface(ref Source, args);
            OutputLine(ELogLevel.Interface, Source, args);
        }
    
        /// <summary>
        /// Outputs a message with a solid line of '=' chars above and below it of equal width to the longest line in the message
        /// </summary>
        /// <param name="Source">Name of the calling <see cref="LogSource"/> object or <c>Null</c></param>
        [LoggingMethod]
	    public static void Banner(ELogLevel level, string Source, string Format, params object[] Args)
        {
            var lines = CurrentFormatter.Banner(Source, Format, Args);
            OutputLine(level, lines);
        }

        #endregion
    
    }
}