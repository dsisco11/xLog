namespace xLog.VirtualTerminal
{
    public class CSI_BLOCK
    {
        #region Properties
        /// <summary>
        /// The command byte (aka. Final Byte, aka. Terminating Byte) occurs at the end of a CSI block and specifies what kind of CSI command to execute.
        /// </summary>
        public readonly byte? CmdByte = null;
        /// <summary>
        /// Parameters list passed to the CSI Command
        /// </summary>
        public readonly int[] Parameters = null;
        /// <summary>
        /// Text following the command block
        /// </summary>
        public readonly StringPtr Text = string.Empty;
        public readonly int BlockStart = 0;
        public readonly int TextStart = 0;
        public readonly int BlockEnd = 0;
        public readonly bool HasControlChar = false;
        #endregion

        #region Accessors
        public int CmdLength => (TextStart - BlockStart);
        #endregion

        #region Constructors
        public CSI_BLOCK(byte? cmdByte, int[] parameters, StringPtr text, int blockStart, int blockEnd, int textStart, bool hasControlChar)
        {
            CmdByte = cmdByte;
            Parameters = parameters;
            Text = text;
            BlockStart = blockStart;
            BlockEnd = blockEnd;
            TextStart = textStart;
            HasControlChar = hasControlChar;
        }
        #endregion

        #region Overrides
        public override string ToString()
        {
            if (!CmdByte.HasValue) return Text.ToString();
            return $"\x1b[{string.Join(ANSI.sSEP, Parameters)}{(char)CmdByte}{Text.ToString()}";
        }
        #endregion
    }
}
