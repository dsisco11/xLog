using System;
using System.Runtime.Serialization;

namespace xLog.VirtualTerminal
{
    public class SgrFormatError : SgrError
    {
        public SgrFormatError()
        {
        }

        public SgrFormatError(string message) : base(message)
        {
        }

        public SgrFormatError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SgrFormatError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
