public enum ConsoleWidgetType
{
    Unknown = 0,
    /// <summary>
    /// Anything that portrays a task being in progress.
    /// </summary>
    Spinner,
    /// <summary>
    /// Anything that displays a progress value of some sort.
    /// </summary>
    Progress,
    /// <summary>
    /// Anything that displays a timer.
    /// </summary>
    Timer,
    /// <summary>
    /// Anything accepting user input.
    /// </summary>
    Input,
}
