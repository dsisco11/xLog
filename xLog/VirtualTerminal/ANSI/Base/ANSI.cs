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
    /// Provides a helper class for adding XTERM/ANSI color codes to log messages.
    /// </summary>
    public static class ANSI
    {
        /// <summary>
        /// Returns <c>True</c> if the current environment is one which does not support ANSI Escape Codes.
        /// </summary>
        public static bool RequiresEmulation => !Platform.Supports_VirtualTerminal();
        /// <summary>
        /// The char that begins an ANSI command.
        /// </summary>
        public const char CSI = '\x1b';
        public const char BEL = '\x7';
        /// <summary>
        /// Control sequence parameter seperator
        /// </summary>
        public const char SEP = ';';
        public const char END = 'm';

        /// <summary>
        /// The ANSI command to reset the Foreground & Background colors back to default
        /// </summary>
        public static string COLOR_RESET => string.Concat(CSI, "[", (int)ANSI_CODE.RESET_COLOR_FG, SEP, (int)ANSI_CODE.RESET_COLOR_BG, END);
        public static string COLOR_RESET_FG => string.Concat(CSI, "[", (int)ANSI_CODE.RESET_COLOR_FG, END);
        public static string COLOR_RESET_BG => string.Concat(CSI, "[", (int)ANSI_CODE.RESET_COLOR_BG, END);


        #region COMMAND BUILDING

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string xCmd(int code) => string.Concat(CSI, '[', (int)code, END);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string xCmd(int codeA, int codeB) => string.Concat(CSI, '[', (int)codeA, SEP, (int)codeB, END);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string xCmd(int code, byte R, byte G, byte B) => string.Concat(CSI, '[', (int)code, SEP, R, SEP, G, SEP, B, END); 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string xCmd(int[] codes) => string.Concat(CSI, '[', string.Join(Convert.ToString(SEP), codes), END);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Set_BG_Color(ANSI_COLOR color, string msg) => string.Concat(xCmd((int)ANSI_CODE.SET_COLOR_BG + (int)color), msg, COLOR_RESET);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Set_BG_Color_Bright(ANSI_COLOR color, string msg) => string.Concat(xCmd((int)ANSI_CODE.SET_COLOR_BG_BRIGHT + (int)color), msg, COLOR_RESET);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Set_FG_Color(ANSI_COLOR color, string msg) => string.Concat(xCmd((int)ANSI_CODE.SET_COLOR_FG + (int)color), msg, COLOR_RESET_FG);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Set_FG_Color_Bright(ANSI_COLOR color, string msg) => string.Concat(xCmd((int)ANSI_CODE.SET_COLOR_FG_BRIGHT + (int)color), msg, COLOR_RESET_FG);

        #endregion

        #region STYLING
        public static string Bold(object obj) => string.Concat(xCmd((int)ANSI_CODE.BOLD), Convert.ToString(obj), xCmd((int)ANSI_CODE.BOLD_OFF));
        public static string Italic(object obj) => string.Concat(xCmd((int)ANSI_CODE.ITALIC), Convert.ToString(obj), xCmd((int)ANSI_CODE.ITALIC_OFF));
        public static string Underline(object obj) => string.Concat(xCmd((int)ANSI_CODE.UNDERLINE), Convert.ToString(obj), xCmd((int)ANSI_CODE.UNDERLINE_OFF));
        public static string BlinkSlow(object obj) => string.Concat(xCmd((int)ANSI_CODE.BLINK_SLOW), Convert.ToString(obj), xCmd((int)ANSI_CODE.BLINK_OFF));
        public static string BlinkFast(object obj) => string.Concat(xCmd((int)ANSI_CODE.BLINK_FAST), Convert.ToString(obj), xCmd((int)ANSI_CODE.BLINK_OFF));
        public static string Invert(object obj) => string.Concat(xCmd((int)ANSI_CODE.INVERT), Convert.ToString(obj), xCmd((int)ANSI_CODE.INVERT_OFF));
        public static string Reset_Style(object obj) => string.Concat(xCmd((int)ANSI_CODE.RESET_STYLE), Convert.ToString(obj));
        #endregion

        /// <summary>
        /// Briefly changes the default Foreground color, formats a message, and then resets the foregound color.
        /// <para>This allows having multiple colors in a single message without interrupting the initial color, Eg: for log lines.</para>
        /// </summary>
        public static string asColor(ANSI_COLOR fg, string msg)
        {
            // change the default fg color
            string fgc = xCmd((int)ANSI_CODE.SET_COLOR_FG + (int)fg);
            string str = string.Concat(fgc, msg);
            str = Substitute((int)ANSI_CODE.SET_DEFAULT_FG, (int)ANSI_CODE.SET_COLOR_FG + (int)fg, str.AsMemory());
            return string.Concat(str, COLOR_RESET_FG);

        }

        /// <summary>
        /// Briefly changes the default Foreground & Background colors, formats a message, and then resets the colors.
        /// <para>This allows having multiple colors in a single message without interrupting the initial color, Eg: for log lines.</para>
        /// </summary>
        public static string asColor(ANSI_COLOR fg, ANSI_COLOR bg, string msg)
        {
            string str = string.Concat(xCmd((int)ANSI_CODE.SET_COLOR_FG + (int)fg, (int)ANSI_CODE.SET_COLOR_BG + (int)bg), msg);
            str = Substitute((int)ANSI_CODE.SET_DEFAULT_FG, (int)ANSI_CODE.SET_COLOR_FG + (int)fg, str.AsMemory());
            str = Substitute((int)ANSI_CODE.SET_DEFAULT_BG, (int)ANSI_CODE.SET_COLOR_BG + (int)bg, str.AsMemory());
            return string.Concat(str, COLOR_RESET);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Set_FG_Custom(int R, int G, int B, string msg)
        {
            int[] sub = new int[] { (int)ANSI_CODE.SET_COLOR_CUSTOM_FG, 2, R, G, B };
            return string.Concat(xCmd(sub), msg, COLOR_RESET);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Set_BG_Custom(int R, int G, int B, string msg)
        {
            int[] sub = new int[] { (int)ANSI_CODE.SET_COLOR_CUSTOM_BG, 2, R, G, B };
            return string.Concat(xCmd(sub), msg, COLOR_RESET_BG);
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
            var Commands = VT.Compile_Command_Blocks(Str);

            StringBuilder sb = new StringBuilder();
            foreach (var Block in Commands)
            {
                int[] codes = Block.Codes.Select(c => ((int)c == Target ? Substitute : (int)c)).ToArray();
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
            var Commands = VT.Compile_Command_Blocks(Str);

            StringBuilder sb = new StringBuilder();
            foreach (var Block in Commands)
            {
                List<int> codes = Block.Codes.Select(c => (int)c).ToList();
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

        #region UTILITY


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
                    throw new ArgumentOutOfRangeException($"Unknown color mapping: {Color.ToString()}");
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
                    throw new ArgumentOutOfRangeException($"Unknown color mapping: {Color.ToString()}");
            }
        }
        #endregion

        #region OUTPUT
        internal static void Write(ReadOnlyMemory<char> Str)
        {
            if (!RequiresEmulation)
            {
                Console.Write(Str);
            }
            else
            {
                VT.Emulate(Str);
            }
        }

        internal static void WriteLine(ReadOnlyMemory<char> Str)
        {
            Write( Str );
            Write( COLOR_RESET.AsMemory() );
            Console.WriteLine();
        }

        #endregion

        /// <summary>
        /// Strips all of the XTERM command sequences from a string and returns the cleaned string.
        /// </summary>
        public static string Strip(ReadOnlyMemory<char> Str)
        {
            var Commands = VT.Compile_Command_Blocks(Str);

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