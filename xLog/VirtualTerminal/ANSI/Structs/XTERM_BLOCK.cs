using System.Collections.Generic;

namespace xLog
{
    internal struct XTERM_BLOCK
    {
        public List<VT_CODE> Codes;
        public string TEXT;

        public XTERM_BLOCK(string str, List<VT_CODE> commands)
        {
            TEXT = str;
            Codes = commands;
        }
    }
}