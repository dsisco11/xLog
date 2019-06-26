using System;

public static class LogFactory
{
    /// <summary>
    /// Used to initialize a new logger instance.
    /// <para>Ex: GetLogger( System.Reflection.MethodBase.GetCurrentMethod().DeclaringType )</para>
    /// </summary>
    /// <returns><see cref="ILog"/> instance</returns>
    public static ILog GetLogger(Type Source, ILog Parent = null)
    {
        return new LogSource( Source, Parent );
    }
    
    /// <summary>
    /// Used to initialize a new logger instance.
    /// <para>Ex: GetLogger( "FooBar" )</para>
    /// </summary>
    /// <returns><see cref="ILog"/> instance</returns>
    public static ILog GetLogger(string Name, ILog Parent = null)
    {
        return new LogSource(Name, Parent);
    }

    /// <summary>
    /// Used to initialize a new logger instance.
    /// <para>Ex: GetLogger(() => "FooBar" )</para>
    /// </summary>
    /// <returns><see cref="ILog"/> instance</returns>
    public static ILog GetLogger(Func<string> Get_Name, ILog Parent = null)
    {
        return new LogSource( Get_Name, Parent);
    }
}
