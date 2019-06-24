/*
 * Copyright 2017 David Sisco 
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace xLog
{
    public enum LogLevel : uint
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Success = 3,
        Failure = 4,
        Warn = 5,
        Error = 6,
        Assert = 7,
        /// <summary>This log level dictates that user input is required before the code progresses</summary>
        Interface = 8,
        /// <summary>Maximum possible <see cref="LogLevel"/> value</summary>
        All,
    }
}
