
namespace xLog.VirtualTerminal
{
    public enum XLOG_CODE : int
    {
        /// <summary>
        /// Pushes the current console Foreground color onto the stack
        /// </summary>
        PUSH_FG = 10,
        /// <summary>
        /// Pushes the current console Background color onto the stack
        /// </summary>
        PUSH_BG = 11,

        /// <summary>
        /// Pops a color from the stack and assigns the console Foreground color to it
        /// </summary>
        POP_FG = 20,
        /// <summary>
        /// Pops a color from the stack and assigns the console Background color to it
        /// </summary>
        POP_BG = 21,
    }
}
