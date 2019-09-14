using System;
using System.Collections.Generic;
using System.Drawing;

namespace xLog.VirtualTerminal
{
    public class TerminalColor
    {
        public readonly object Data;

        public TerminalColor(ANSI_COLOR Color) { Data = Color; }
        public TerminalColor(Color color) { Data = color; }
        public TerminalColor(object obj) { Data = obj; }

        public static implicit operator TerminalColor(ANSI_COLOR color) => new TerminalColor(color);
        public static implicit operator TerminalColor(Color color) => new TerminalColor(color);
    }

    public static class TerminalColorExt
    {

        public static int[] Get_SGR_Code(this TerminalColor Self, bool isBG = false)
        {
            if (Self == null)
            {
                var code = (int)(isBG ? SGR_CODE.RESET_COLOR_BG : SGR_CODE.RESET_COLOR_FG);
                return new int[] { code };
            }

            if (Self.Data is Color clr)
            {/* Custom Color */
                var Cmd = isBG ? SGR_CODE.SET_COLOR_CUSTOM_BG : SGR_CODE.SET_COLOR_CUSTOM_FG;
                return new int[] { (int)Cmd, 2, clr.R, clr.G, clr.B };
            }
            else
            {
                int Cmd = (int)(isBG ? SGR_CODE.SET_COLOR_CUSTOM_BG : SGR_CODE.SET_COLOR_CUSTOM_FG);
                int Clr = (int)Self.Data;
                return new int[] { Cmd, 5, Clr };
            }
        }

    }
}
