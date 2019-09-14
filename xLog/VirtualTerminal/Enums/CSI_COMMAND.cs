
namespace xLog.VirtualTerminal
{
    public enum CSI_COMMAND : byte
    {
        CursorUp = 0x41,
        CursorDown,
        CursorForward,
        CursorBack,
        CursorNextLine,
        CursorPreviousLine,
        CursorHorizontalAbsolute,
        CursorPosition,
        EraseInDisplay = 0x4A,
        EraseInLine = 0x4B,
        ScrollUp = 0x53,
        ScrollDown = 0x54,
        HVP = 0x66,
        SGR = 0x6D,
        AUX = 0x69,
        Device = 0x6E,
        SaveCursorPos = 0x73,
        RestoreCursorPos = 0x75,
        /* Begin 'Private' codes (custom, application defined) */
        XLOG = 0x7A,
    }
}
