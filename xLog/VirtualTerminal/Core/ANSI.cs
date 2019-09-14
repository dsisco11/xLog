using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using xLog.VirtualTerminal;
using System.Text;

/*
 * === SOURCES ===
    https://invisible-island.net/xterm/ctlseqs/ctlseqs.html
    https://en.wikipedia.org/wiki/ANSI_escape_code#Windows_and_DOS
*/

namespace xLog
{
    /// <summary>
    /// Provides functions for adding ANSI color codes and other CSI codes to text.
    /// </summary>
    public static class ANSI
    {
        /// <summary>
        /// The char that begins an ANSI command.
        /// </summary>
        public const char CSI = '\x1b';
        public const char BEL = '\x7';
        /// <summary>
        /// Control sequence parameter seperator
        /// </summary>
        public const char SEP = ';';
        public static string sSEP = Convert.ToString(SEP);


        #region Common Command Strings

        public static string CSI_PREFIX = "\x1b[";

        public static string sRESET_COLOR => xCmd(SGR_CODE.RESET_COLOR_FG, SGR_CODE.RESET_COLOR_BG);
        public static string sRESET_FG => xCmd(SGR_CODE.RESET_COLOR_FG);
        public static string sRESET_BG => xCmd(SGR_CODE.RESET_COLOR_BG);

        public static string sPUSH_FG => string.Concat(CSI_PREFIX, (int)XLOG_CODE.PUSH_FG, (char)CSI_COMMAND.XLOG);
        public static string sPUSH_BG => string.Concat(CSI_PREFIX, (int)XLOG_CODE.PUSH_BG, (char)CSI_COMMAND.XLOG);

        public static string sPOP_FG => string.Concat(CSI_PREFIX, (int)XLOG_CODE.POP_FG, (char)CSI_COMMAND.XLOG);
        public static string sPOP_BG => string.Concat(CSI_PREFIX, (int)XLOG_CODE.POP_BG, (char)CSI_COMMAND.XLOG);
        #endregion

        #region CSI Formatting
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string xCmd(params SGR_CODE[] Code) => string.Concat(CSI_PREFIX, string.Join(sSEP, Code.Select(x=>(int)x)), (char)CSI_COMMAND.SGR);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string xCmd(int Code) => string.Concat(CSI_PREFIX, Code, (char)CSI_COMMAND.SGR);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string xCmd(params int[] args) => string.Concat(CSI_PREFIX, string.Join(sSEP, args), (char)CSI_COMMAND.SGR);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string xCmd(int Code, params int[] args) => string.Concat(CSI_PREFIX, Code, SEP, string.Join(sSEP, args), (char)CSI_COMMAND.SGR);
        #endregion

        #region Formatting Helpers
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Format_Command(string Msg, params int[] Codes) => string.Concat(sPUSH_FG, sPUSH_BG, xCmd(Codes), Msg, sPOP_FG, sPOP_BG);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Set_FG_Color(ANSI_COLOR color, string Msg) => string.Concat(sPUSH_FG, xCmd((int)SGR_CODE.SET_COLOR_CUSTOM_FG, 5, (int)color), Msg, sPOP_FG);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Set_BG_Color(ANSI_COLOR color, string Msg) => string.Concat(sPUSH_BG, xCmd((int)SGR_CODE.SET_COLOR_CUSTOM_BG, 5, (int)color), Msg, sPOP_BG);
        #endregion


        #region Style Commands
        public static string Bold(object obj) => string.Concat(xCmd(SGR_CODE.BOLD), Convert.ToString(obj), xCmd(SGR_CODE.BOLD_OFF));
        public static string Italic(object obj) => string.Concat(xCmd(SGR_CODE.ITALIC), Convert.ToString(obj), xCmd(SGR_CODE.ITALIC_OFF));
        public static string Underline(object obj) => string.Concat(xCmd(SGR_CODE.UNDERLINE), Convert.ToString(obj), xCmd(SGR_CODE.UNDERLINE_OFF));
        public static string BlinkSlow(object obj) => string.Concat(xCmd(SGR_CODE.BLINK_SLOW), Convert.ToString(obj), xCmd(SGR_CODE.BLINK_OFF));
        public static string BlinkFast(object obj) => string.Concat(xCmd(SGR_CODE.BLINK_FAST), Convert.ToString(obj), xCmd(SGR_CODE.BLINK_OFF));
        public static string Invert(object obj) => string.Concat(xCmd(SGR_CODE.INVERT), Convert.ToString(obj), xCmd(SGR_CODE.INVERT_OFF));
        public static string Reset_Style(object obj) => string.Concat(Convert.ToString(obj), xCmd(SGR_CODE.RESET_STYLE));
        #endregion
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Set_FG_Custom(int R, int G, int B, StringPtr Str)
        {
            int[] sub = new int[] { 2, R, G, B };
            return string.Concat(xCmd((int)SGR_CODE.SET_COLOR_CUSTOM_FG, sub), Str.ToString(), sRESET_COLOR);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Set_BG_Custom(int R, int G, int B, StringPtr Str)
        {
            int[] sub = new int[] { 2, R, G, B };
            return string.Concat(xCmd((int)SGR_CODE.SET_COLOR_CUSTOM_BG, sub), Str.ToString(), sRESET_BG);
        }

        #region Command Manipulation
        /// <summary>
        /// Substitutes a <paramref name="Target"/> ANSI code for a <paramref name="Substitute"/>
        /// </summary>
        /// <param name="Target"></param>
        /// <param name="Substitute"></param>
        /// <param name="Str"></param>
        /// <returns>Altered string with all instances of the <paramref name="Target"/> code replaced with the <paramref name="Substitute"/> one</returns>
        internal static string Substitute(int Target, int Substitute, ReadOnlyMemory<char> Str)
        {
            // Get the list of CSI's 
            var Commands = Terminal.Compile_Command_Blocks(Str);

            StringBuilder sb = new StringBuilder();
            foreach (var Block in Commands)
            {
                int[] codes = Block.Parameters.Select(c => ((int)c == Target ? Substitute : (int)c)).ToArray();
                sb.Append(string.Concat(xCmd(codes), Block.Text.ToString()));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Substitutes a <paramref name="Target"/> ANSI code for a <paramref name="Substitute"/>
        /// </summary>
        /// <returns>Altered string with all instances of the <paramref name="Target"/> code replaced with the <paramref name="Substitute"/> one</returns>
        internal static string Substitute(int Target, int[] Substitute, ReadOnlyMemory<char> Str)
        {
            // Get the list of CSI's 
            var Commands = Terminal.Compile_Command_Blocks(Str);

            StringBuilder sb = new StringBuilder();
            foreach (var Block in Commands)
            {
                List<int> codes = Block.Parameters.Select(c => (int)c).ToList();
                int idx = codes.IndexOf(Target);
                if (idx > -1)
                {
                    codes[idx] = Target;
                    foreach (int c in Substitute) { codes.InsertRange(idx, Substitute); }
                }

                sb.Append(string.Concat(xCmd(codes.ToArray()), Block.Text));
            }

            return sb.ToString();
        }
        #endregion

        #region Utility
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConsoleColor Color_ANSI_To_Console(ANSI_COLOR Color)
        {
            switch (Color)
            {
                case ANSI_COLOR.YELLOW:
                    return ConsoleColor.DarkYellow;
                case ANSI_COLOR.YELLOW_BRIGHT:
                    return ConsoleColor.Yellow;
                case ANSI_COLOR.RED:
                    return ConsoleColor.DarkRed;
                case ANSI_COLOR.RED_BRIGHT:
                    return ConsoleColor.Red;
                case ANSI_COLOR.MAGENTA:
                    return ConsoleColor.DarkMagenta;
                case ANSI_COLOR.MAGENTA_BRIGHT:
                    return ConsoleColor.Magenta;
                case ANSI_COLOR.GREEN:
                    return ConsoleColor.DarkGreen;
                case ANSI_COLOR.GREEN_BRIGHT:
                    return ConsoleColor.Green;
                case ANSI_COLOR.CYAN:
                    return ConsoleColor.DarkCyan;
                case ANSI_COLOR.CYAN_BRIGHT:
                    return ConsoleColor.Cyan;
                case ANSI_COLOR.BLUE:
                    return ConsoleColor.DarkBlue;
                case ANSI_COLOR.BLUE_BRIGHT:
                    return ConsoleColor.Blue;
                case ANSI_COLOR.WHITE_BRIGHT:
                    return ConsoleColor.White;
                case ANSI_COLOR.WHITE:
                    return ConsoleColor.Gray;
                case ANSI_COLOR.BLACK_BRIGHT:
                    return ConsoleColor.DarkGray;
                case ANSI_COLOR.BLACK:
                    return ConsoleColor.Black;
                default:
                    {
                        System.Diagnostics.Debugger.Break();
                        throw new ArgumentOutOfRangeException($"{nameof(Color)}({Color.ToString()})");
                    }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ANSI_COLOR Color_Console_To_ANSI(ConsoleColor Color)
        {
            switch (Color)
            {
                case ConsoleColor.DarkYellow:
                    return ANSI_COLOR.YELLOW;
                case ConsoleColor.Yellow:
                    return ANSI_COLOR.YELLOW_BRIGHT;
                case ConsoleColor.DarkRed:
                    return ANSI_COLOR.RED;
                case ConsoleColor.Red:
                    return ANSI_COLOR.RED_BRIGHT;
                case ConsoleColor.DarkMagenta:
                    return ANSI_COLOR.MAGENTA_BRIGHT;
                case ConsoleColor.Magenta:
                    return ANSI_COLOR.MAGENTA_BRIGHT;
                case ConsoleColor.DarkGreen:
                    return ANSI_COLOR.GREEN_BRIGHT;
                case ConsoleColor.Green:
                    return ANSI_COLOR.GREEN_BRIGHT;
                case ConsoleColor.DarkCyan:
                    return ANSI_COLOR.CYAN;
                case ConsoleColor.Cyan:
                    return ANSI_COLOR.CYAN_BRIGHT;
                case ConsoleColor.DarkBlue:
                    return ANSI_COLOR.BLUE;
                case ConsoleColor.Blue:
                    return ANSI_COLOR.BLUE_BRIGHT;
                case ConsoleColor.White:
                    return ANSI_COLOR.WHITE_BRIGHT;
                case ConsoleColor.Gray:
                    return ANSI_COLOR.WHITE;
                case ConsoleColor.DarkGray:
                    return ANSI_COLOR.BLACK_BRIGHT;
                case ConsoleColor.Black:
                    return ANSI_COLOR.BLACK;
                default:
                    {
                        System.Diagnostics.Debugger.Break();
                        throw new ArgumentOutOfRangeException($"{nameof(Color)}({Color.ToString()})");
                    }
            }
        }
        #endregion

        /// <summary>
        /// Strips all of the XTERM command sequences from a string and returns the cleaned string.
        /// </summary>
        public static string Strip(StringPtr Str)
        {
            if (Str == null) return null;
            var Commands = Terminal.Compile_Command_Blocks(Str);

            StringBuilder sb = new StringBuilder();
            foreach (var Block in Commands)
            {
                sb.Append(Block.Text);
            }

            return sb.ToString();
        }

        #region FOREGROUND COLORS
        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string CustomFG(byte R, byte G, byte B, object obj) => Set_FG_Custom(R, G, B, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string CustomFG(byte R, byte G, byte B, StringPtr Str) => Set_FG_Custom(R, G, B, Str);
        #region DARK
        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string Orange(object obj) => Set_FG_Custom(255, 120, 0, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string Black(object obj) => Set_FG_Color(ANSI_COLOR.BLACK, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string Red(object obj) => Set_FG_Color(ANSI_COLOR.RED, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string Green(object obj) => Set_FG_Color(ANSI_COLOR.GREEN, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string Yellow(object obj) => Set_FG_Color(ANSI_COLOR.YELLOW, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string Blue(object obj) => Set_FG_Color(ANSI_COLOR.BLUE, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string Magenta(object obj) => Set_FG_Color(ANSI_COLOR.MAGENTA, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string Cyan(object obj) => Set_FG_Color(ANSI_COLOR.CYAN, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string White(object obj) => Set_FG_Color(ANSI_COLOR.WHITE, Convert.ToString(obj));
        #endregion

        #region BRIGHT
        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string OrangeBright(object obj) => Set_FG_Custom(255, 170, 0, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string BlackBright(object obj) => Set_FG_Color(ANSI_COLOR.BLACK_BRIGHT, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string RedBright(object obj) => Set_FG_Color(ANSI_COLOR.RED_BRIGHT, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string GreenBright(object obj) => Set_FG_Color(ANSI_COLOR.GREEN_BRIGHT, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string YellowBright(object obj) => Set_FG_Color(ANSI_COLOR.YELLOW_BRIGHT, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string BlueBright(object obj) => Set_FG_Color(ANSI_COLOR.BLUE_BRIGHT, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string MagentaBright(object obj) => Set_FG_Color(ANSI_COLOR.MAGENTA_BRIGHT, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string CyanBright(object obj) => Set_FG_Color(ANSI_COLOR.CYAN_BRIGHT, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string WhiteBright(object obj) => Set_FG_Color(ANSI_COLOR.WHITE_BRIGHT, Convert.ToString(obj));
        #endregion
        #endregion

        #region BACKGROUND COLORS
        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string CustomBG(byte R, byte G, byte B, object obj) => Set_BG_Custom(R, G, B, Convert.ToString(obj));
        #region DARK
        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string OrangeBG(object obj) => Set_BG_Custom(255, 120, 0, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string BlackBG(object obj) => Set_BG_Color(ANSI_COLOR.BLACK, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string RedBG(object obj) => Set_BG_Color(ANSI_COLOR.RED, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string GreenBG(object obj) => Set_BG_Color(ANSI_COLOR.GREEN, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string YellowBG(object obj) => Set_BG_Color(ANSI_COLOR.YELLOW, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string BlueBG(object obj) => Set_BG_Color(ANSI_COLOR.BLUE, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string MagentaBG(object obj) => Set_BG_Color(ANSI_COLOR.MAGENTA, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string CyanBG(object obj) => Set_BG_Color(ANSI_COLOR.CYAN, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string WhiteBG(object obj) => Set_BG_Color(ANSI_COLOR.WHITE, Convert.ToString(obj));
        #endregion

        #region BRIGHT
        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string OrangeBrightBG(object obj) => Set_BG_Custom(255, 170, 0, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string BlackBrightBG(object obj) => Set_BG_Color(ANSI_COLOR.BLACK_BRIGHT, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string RedBrightBG(object obj) => Set_BG_Color(ANSI_COLOR.RED_BRIGHT, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string GreenBrightBG(object obj) => Set_BG_Color(ANSI_COLOR.GREEN_BRIGHT, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string YellowBrightBG(object obj) => Set_BG_Color(ANSI_COLOR.YELLOW_BRIGHT, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string BlueBrightBG(object obj) => Set_BG_Color(ANSI_COLOR.BLUE_BRIGHT, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string MagentaBrightBG(object obj) => Set_BG_Color(ANSI_COLOR.MAGENTA_BRIGHT, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string CyanBrightBG(object obj) => Set_BG_Color(ANSI_COLOR.CYAN_BRIGHT, Convert.ToString(obj));

        /// <summary>
        /// Sets the foreground color for the given text
        /// </summary>
        /// <returns>ANSI Color encoded string</returns>
        public static string WhiteBrightBG(object obj) => Set_BG_Color(ANSI_COLOR.WHITE_BRIGHT, Convert.ToString(obj));
        #endregion
        #endregion
    }
}