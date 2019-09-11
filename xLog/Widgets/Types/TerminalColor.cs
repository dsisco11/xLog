using System;
using System.Collections.Generic;
using System.Drawing;

namespace xLog.Widgets
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

        public static ICollection<int> Get_ANSI_Code(this TerminalColor Self, bool isBG = false)
        {
            if (Self == null)
            {
                var code = (int)(isBG ? ANSI_CODE.RESET_COLOR_BG : ANSI_CODE.RESET_COLOR_FG);
                return new int[1] { code };
            }

            if (Self.Data is Color clr)
            {/* Custom Color */
                var cmd = isBG ? ANSI_CODE.SET_COLOR_CUSTOM_BG : ANSI_CODE.SET_COLOR_CUSTOM_FG;
                return new int[4] { (int)cmd, clr.R, clr.G, clr.B };// string.Concat(cmd, ANSI.SEP, clr.R, ANSI.SEP, clr.G, ANSI.SEP, clr.B, ANSI.SEP);
            }
            else
            {
                int cmd = (int)(isBG ? ANSI_CODE.SET_COLOR_BG: ANSI_CODE.SET_COLOR_FG);
                int code = cmd + (int)Self.Data;
                return new int[1] { code };// string.Concat((int)cmd+(int)Data, ANSI.SEP);
            }
        }

    }
}
