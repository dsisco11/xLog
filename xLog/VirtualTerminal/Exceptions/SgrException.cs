using System;
using System.Runtime.Serialization;

namespace xLog.VirtualTerminal
{
    public class SgrError : Exception
    {
        static string ERROR_PREFIX = "SelectGraphicsRendition Error";
        public SgrError() : base(ERROR_PREFIX)
        {
        }

        public SgrError(string message) : base(string.Concat(ERROR_PREFIX, ": ", message))
        {
        }

        public SgrError(string message, Exception innerException) : base(string.Concat(ERROR_PREFIX, ": ", message), innerException)
        {
        }

        protected SgrError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
