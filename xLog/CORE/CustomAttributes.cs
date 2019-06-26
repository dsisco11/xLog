using System;

namespace xLog
{
    /// <summary>
    /// A custom attribute we use to mark certain logging functions so we can erase them from stack traces(so they are more readable)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class LoggingMethod : Attribute
    {
    };
}