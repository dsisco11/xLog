
namespace xLog
{
    public class xLogSettings
    {
        /// <summary>
        /// Should module names be included in the output?
        /// </summary>
        public bool showModuleNames;

        /// <summary>
        /// Should log lines be timestamped?
        /// </summary>
        public bool showTimestamps;

        /// <summary>
        /// Should colors be stripped from log lines before they are output to file?
        /// </summary>
        public bool stripXTERM;

        /// <summary>
        /// If <c>TRUE</c> then every log line output will show it's LogLevel at the start of the line.
        /// Meaning that debug lines start with "DEBUG: " and error lines start with "ERROR: " etc.
        /// </summary>
        public bool Show_Log_Levels;

        /// <summary>
        /// The time format string to use for log lines.
        /// </summary>
        public string Timestamp_Format;

        /// <summary>
        /// Minimum log level for things output to console
        /// </summary>
        public LogLevel MinOutputLevel;

        /// <summary>
        /// Minimum log level for things output to console
        /// </summary>
        public LogLevel MinFileLogLevel;

        /// <summary>
        /// The directory that <c>Get_Todays_LogFile()</c> will prepend to its returned path.
        /// </summary>
        public string Log_Directory;

        /// <summary>
        /// The file extension that <c>Get_Todays_LogFile()</c> will append to its returned path.
        /// </summary>
        public string Log_File_Ext;

        /// <summary>
        /// Size of line indentations in spaces
        /// </summary>
        public int IndentSize;


        public xLogSettings()
        {
            showModuleNames = true;

            showTimestamps = true;

            stripXTERM = true;

            Show_Log_Levels = true;

            Timestamp_Format = "HH:mm:ss tt";

            MinOutputLevel = LogLevel.Info;

            MinFileLogLevel = LogLevel.Debug;

            Log_Directory = "./logs/";

            Log_File_Ext = ".log";

            IndentSize = 5;
        }
    }
}
