
using System.Collections.Generic;

public struct XTERM_BLOCK
{
    public List<XTERM_CODE> Codes;
    public string TEXT;

    public XTERM_BLOCK(string str, List<XTERM_CODE> commands)
    {
        TEXT = str;
        Codes = commands;
    }
}