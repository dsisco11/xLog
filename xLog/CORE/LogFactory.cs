using System;

namespace xLog
{
    /// <summary>
    /// Facilitates the creation of new loggers
    /// </summary>
    public static class LogFactory
    {
        /// <summary>
        /// Used to initialize a new logger instance.
        /// <para>Ex: GetLogger( System.Reflection.MethodBase.GetCurrentMethod().DeclaringType )</para>
        /// </summary>
        /// <returns><see cref="ILogger"/> instance</returns>
        public static ILogger GetLogger(Type Source, ILogger Parent = null)
        {
            return new LogSource(Source, Parent);
        }

        /// <summary>
        /// Used to initialize a new logger instance.
        /// <para>Ex: GetLogger( "FooBar" )</para>
        /// </summary>
        /// <returns><see cref="ILogger"/> instance</returns>
        public static ILogger GetLogger(string Name, ILogger Parent = null)
        {
            return new LogSource(Name, Parent);
        }

        /// <summary>
        /// Used to initialize a new logger instance.
        /// <para>Ex: GetLogger(() => "FooBar" )</para>
        /// </summary>
        /// <returns><see cref="ILogger"/> instance</returns>
        public static ILogger GetLogger(Func<string> Get_Name, ILogger Parent = null)
        {
            return new LogSource(Get_Name, Parent);
        }
    }
}