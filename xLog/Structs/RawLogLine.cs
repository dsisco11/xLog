public struct RawLogLine
{
    /// <summary>
    /// The source of this line
    /// </summary>
    public string Source;

    /// <summary>
    /// The format of this line
    /// </summary>
    public string Format;

    /// <summary>
    /// The arguments for the format string of this line
    /// </summary>
    public object[] Args;

    public RawLogLine(string Source, string Format)
    {
        this.Source = Source;
        this.Format = Format;
        this.Args = null;
    }

    public RawLogLine(string Source, string Format, object[] Args)
    {
        this.Source = Source;
        this.Format = Format;
        this.Args = Args;
    }
}
