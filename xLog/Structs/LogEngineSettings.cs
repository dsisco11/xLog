
public class LogEngineSettings
{
    /// <summary>
    /// Should log source names be included in the output?
    /// </summary>
    public bool showSources = true;

    /// <summary>
    /// Should log lines be timestamped?
    /// </summary>
    public bool showTimestamps = true;

    /// <summary>
    /// Specifies if XTERM colors are allowed in log output, if false then XTERM commands will be stripped from ALL log lines before they are output.
    /// </summary>
    public bool AllowXTERM = true;

    /// <summary>
    /// Should colors be stripped from log lines before they are output to consumers?
    /// </summary>
    public bool stripXTERM = true;

    /// <summary>
    /// If <c>TRUE</c> then every log line output will show it's LogLevel at the start of the line.
    /// Meaning that debug lines start with "DEBUG: " and error lines start with "ERROR: " etc.
    /// </summary>
    public bool Show_LogLevel_Names = true;

    /// <summary>
    /// If <c>True</c> then the logging system will base it's log line timecode output as well as it's file names and auto log file switching times on UTC standard time instead of local time.
    /// </summary>
    public bool Use_UTC_Time = false;

    /// <summary>
    /// The time format string to use for log lines.
    /// </summary>
    public string Timestamp_Format = "HH:mm:ss tt";

    /// <summary>
    /// The date format string to use for log file names.
    /// </summary>
    public string LogFile_Date_Format = "yyyy_MM_dd";

    /// <summary>
    /// The directory that <c>Get_Todays_LogFile()</c> will prepend to its returned path.
    /// </summary>
    public string Log_Directory = "./logs/";

    /// <summary>
    /// The file extension that <c>Get_Todays_LogFile()</c> will append to its returned path.
    /// </summary>
    public string Log_File_Ext = ".log";

    /// <summary>
    /// Size of line indentations in spaces
    /// </summary>
    public int IndentSize = 4;

    /// <summary>
    /// Used to assign a custom <see cref="LineFormatter"/> to manipulate log line output
    /// </summary>
    public LineFormatter Formatter = null;

    /// <summary>
    /// Determines if lines of a particular <see cref="ELogLevel"/> will have the level name prepended to them.
    /// (Default: All True)
    /// </summary>
    /// <example>
    /// Usage:
    /// <code>
    /// LogLevel_Name_Show[ELogLevel.Debug] = false;
    /// </code>
    /// </example>
    public bool[] LogLevel_Name_Shown;

    /// <summary>
    /// The MINIMUM <see cref="ELogLevel"/> a log line must be to appear in the CONSOLE
    /// </summary>
    public ELogLevel OutputLevel;
    /// <summary>
    /// The MINIMUM <see cref="ELogLevel"/> a log line must be to appear in the LOG FILE
    /// </summary>
    public ELogLevel LoggingLevel;
}