using System;
using System.Linq;
using System.Collections.Generic;

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
    public static class XTERM
    {

        #region OS Testing
        public static bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }

        public static bool IsWindows
        {
            get
            {
                PlatformID p = Environment.OSVersion.Platform;
                return (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows || p == PlatformID.WinCE);
            }
        }
        #endregion
        /// <summary>
        /// Returns <c>True</c> if the current environment is one which does not support ANSI Escape Codes.
        /// </summary>
        public static bool RequiresEmulation => !IsLinux;
        /// <summary>
        /// The char that begins an ANSI command.
        /// </summary>
        private const char CSI = '\x1b';
        private const char BEL = '\x7';
        /// <summary>
        /// The ANSI command to reset the Foreground & Background colors back to default
        /// </summary>
        private static string COLOR_RESET => string.Concat(CSI, "[", (int)ANSI_CODE.RESET_COLOR_FG, ";", (int)ANSI_CODE.RESET_COLOR_BG, "m");
        private static string COLOR_RESET_FG => string.Concat(CSI, "[", (int)ANSI_CODE.RESET_COLOR_FG, "m");


        #region COMMAND BUILDING

        internal static string oCmd(int code) { return string.Concat(CSI, '[', (int)code, 'm'); }
        internal static string oCmd(int A, int B) { return string.Concat(CSI, '[', A, ';', B, 'm'); }
        internal static string oCmd(int A, int B, int C) { return string.Concat(CSI, '[', A, ';', B, ';', C, 'm'); }

        internal static string xCmd(int code) { return string.Concat(CSI, '[', (int)code, 'm'); }
        internal static string xCmd(int codeA, int codeB) { return string.Concat(CSI, '[', (int)codeA, ';', (int)codeB, 'm'); }
        internal static string xCmd(int code, byte R, byte G, byte B) { return string.Concat(CSI, '[', (int)code, ';', R, ';', G, ';', B, 'm'); }
        internal static string xCmd(int[] codes) { return string.Concat(CSI, '[', string.Join(";", codes), 'm'); }
        //internal static string xCmd(params object[] codes) { return string.Concat(CSI, '[', string.Join(";", codes), 'm'); }


        internal static string Set_BG_Color(XTERM_COLOR color, string msg) { return xCmd((int)ANSI_CODE.SET_COLOR_BG + (int)color) + msg + COLOR_RESET; }
        internal static string Set_BG_Color(ANSI_COLOR color, string msg) { return xCmd((int)ANSI_CODE.SET_COLOR_BG + (int)color) + msg + COLOR_RESET; }
        internal static string Set_BG_Color_Bright(ANSI_COLOR color, string msg) { return xCmd((int)ANSI_CODE.SET_COLOR_BG_BRIGHT + (int)color) + msg + COLOR_RESET; }

        internal static string Set_FG_Color(XTERM_COLOR color, string msg) { return xCmd((int)ANSI_CODE.SET_COLOR_FG + (int)color) + msg + COLOR_RESET_FG; }
        internal static string Set_FG_Color(ANSI_COLOR color, string msg) { return xCmd((int)ANSI_CODE.SET_COLOR_FG + (int)color) + msg + COLOR_RESET_FG; }
        internal static string Set_FG_Color_Bright(ANSI_COLOR color, string msg) { return xCmd((int)ANSI_CODE.SET_COLOR_FG_BRIGHT + (int)color) + msg + COLOR_RESET_FG; }

        #endregion

        #region STYLING
        public static string Bold(object obj) { return string.Concat(xCmd((int)ANSI_CODE.BOLD), obj.ToString(), xCmd((int)ANSI_CODE.NORMAL)); }
        public static string Italic(object obj) { return string.Concat(xCmd((int)ANSI_CODE.ITALIC), obj.ToString(), xCmd((int)ANSI_CODE.NORMAL)); }
        public static string Reset_Style(object obj) { return string.Concat(xCmd((int)ANSI_CODE.RESET_STYLE), obj.ToString(), xCmd((int)ANSI_CODE.NORMAL)); }
        #endregion

        /// <summary>
        /// Briefly changes the default Foreground color, formats a message, and then resets the foregound color.
        /// <para>This allows having multiple colors in a single message without interrupting the initial color, Eg: for log lines.</para>
        /// </summary>
        public static string asColor(XTERM_COLOR fg, string msg)
        {
            // change the default fg color
            string fgc = xCmd((int)ANSI_CODE.SET_COLOR_FG + (int)fg);
            string str = string.Concat(fgc, msg);
            str = Replace((int)XTERM_CODE.SET_FG_DEFAULT, (int)ANSI_CODE.SET_COLOR_FG + (int)fg, str);
            return string.Concat(str, COLOR_RESET_FG);

        }

        public static string asColor(XTERM_COLOR fg, XTERM_COLOR bg, string msg)
        {
            string str = string.Concat(xCmd((int)ANSI_CODE.SET_COLOR_FG + (int)fg, (int)ANSI_CODE.SET_COLOR_BG + (int)bg), msg);
            str = Replace((int)XTERM_CODE.SET_FG_DEFAULT, (int)ANSI_CODE.SET_COLOR_FG + (int)fg, str);
            str = Replace((int)XTERM_CODE.SET_BG_DEFAULT, (int)ANSI_CODE.SET_COLOR_BG + (int)bg, str);
            return string.Concat(str, COLOR_RESET);
        }

        internal static string Replace(int target, int substitute, string str)
        {
            // Get the list of CSI's 
            List<string> CSIS = Tokenize_Control_Sequence_Initiators(str);

            // Now build an XTERM_COMMAND_BLOCK for each CSI and add it to our list
            string Str = "";
            foreach (string block in CSIS)
            {
                XTERM_BLOCK xb = Compile_Xterm_Command_Block(block);
                int[] codes = xb.Codes.Select(c => ((int)c == target ? substitute : (int)c)).ToArray();
                Str += string.Concat(xCmd(codes), xb.TEXT);
            }

            return Str;
        }

        internal static string Replace(int target, int[] substitute, string str)
        {
            // Get the list of CSI's 
            List<string> CSIS = Tokenize_Control_Sequence_Initiators(str);

            // Now build an XTERM_COMMAND_BLOCK for each CSI and add it to our list
            string Str = "";
            foreach (string block in CSIS)
            {
                XTERM_BLOCK xb = Compile_Xterm_Command_Block(block);
                List<int> codes = xb.Codes.Select(c => (int)c).ToList();
                int idx = codes.IndexOf(target);
                if (idx > -1)
                {
                    codes[idx] = target;
                    foreach (int c in substitute) { codes.InsertRange(idx, substitute); }
                }

                Str += string.Concat(xCmd(codes.ToArray()), xb.TEXT);
            }

            return Str;
        }
        // XXX: RESET COLORS AT END OF EACH LINE!
        internal static string Custom(int R, int G, int B, string msg)
        {
            int[] sub = new int[] { (int)ANSI_CODE.SET_COLOR_CUSTOM_FG, 2, R, G, B };
            string str = string.Concat(xCmd(sub), msg);
            return string.Concat(str, COLOR_RESET);

        }

        #region FOREGROUND COLORS

        public static string orange(object obj) { return Custom(255, 120, 0, obj.ToString()); }

        public static string black(object obj) { return Set_FG_Color(ANSI_COLOR.BLACK, obj.ToString()); }

        public static string red(object obj) { return Set_FG_Color(ANSI_COLOR.RED, obj.ToString()); }

        public static string green(object obj) { return Set_FG_Color(ANSI_COLOR.GREEN, obj.ToString()); }

        public static string yellow(object obj) { return Set_FG_Color(ANSI_COLOR.YELLOW, obj.ToString()); }

        public static string blue(object obj) { return Set_FG_Color(ANSI_COLOR.BLUE, obj.ToString()); }

        public static string magenta(object obj) { return Set_FG_Color(ANSI_COLOR.MAGENTA, obj.ToString()); }

        public static string cyan(object obj) { return Set_FG_Color(ANSI_COLOR.CYAN, obj.ToString()); }

        public static string white(object obj) { return Set_FG_Color(ANSI_COLOR.WHITE, obj.ToString()); }
        #endregion

        #region BRIGHT FOREGROUND COLORS

        public static string orangeBright(object obj) { return Custom(255, 170, 0, obj.ToString()); }

        public static string blackBright(object obj) { return Set_FG_Color(XTERM_COLOR.BLACK_BRIGHT, obj.ToString()); }

        public static string redBright(object obj) { return Set_FG_Color(XTERM_COLOR.RED_BRIGHT, obj.ToString()); }

        public static string greenBright(object obj) { return Set_FG_Color(XTERM_COLOR.GREEN_BRIGHT, obj.ToString()); }

        public static string yellowBright(object obj) { return Set_FG_Color(XTERM_COLOR.YELLOW_BRIGHT, obj.ToString()); }

        public static string blueBright(object obj) { return Set_FG_Color(XTERM_COLOR.BLUE_BRIGHT, obj.ToString()); }

        public static string magentaBright(object obj) { return Set_FG_Color(XTERM_COLOR.MAGENTA_BRIGHT, obj.ToString()); }

        public static string cyanBright(object obj) { return Set_FG_Color(XTERM_COLOR.CYAN_BRIGHT, obj.ToString()); }

        public static string whiteBright(object obj) { return Set_FG_Color(XTERM_COLOR.WHITE_BRIGHT, obj.ToString()); }
        #endregion

        #region UTILITY
        public static bool Is_CSI_Termination_Char(char c)
        {
            return (c >= 64 && c <= 126);
        }

        public static string From_Console_Color(ConsoleColor clr, string msg)
        {
            switch (clr)
            {
                case ConsoleColor.DarkYellow:
                    return yellow(msg);
                case ConsoleColor.Yellow:
                    return yellowBright(msg);
                case ConsoleColor.DarkRed:
                    return red(msg);
                case ConsoleColor.Red:
                    return redBright(msg);
                case ConsoleColor.DarkMagenta:
                    return magenta(msg);
                case ConsoleColor.Magenta:
                    return magentaBright(msg);
                case ConsoleColor.DarkGreen:
                    return green(msg);
                case ConsoleColor.Green:
                    return greenBright(msg);
                case ConsoleColor.DarkCyan:
                    return cyan(msg);
                case ConsoleColor.Cyan:
                    return cyanBright(msg);
                case ConsoleColor.DarkBlue:
                    return blue(msg);
                case ConsoleColor.Blue:
                    return blueBright(msg);
                case ConsoleColor.White:
                    return whiteBright(msg);
                case ConsoleColor.Gray:
                    return white(msg);
                case ConsoleColor.DarkGray:
                    return blackBright(msg);
                case ConsoleColor.Black:
                    return black(msg);
            }

            return msg;
        }

        private static XTERM_BLOCK Compile_Xterm_Command_Block(string str)
        {
            // Each escape sequence begins with ESC (0x1B) followed by a single char in the range <64-95>(most often '[') followed by a series of command chars each sperated by a semicolon ';', the entire sequence is ended with a character in the range <64-126>(most often 'm')
            // Regex CSI = new Regex(@"^(\x1b\[(?<CMD>(\d+;)*\d*)[@-~]{1})?(?<TEXT>.*)?$");
            //Match match = CSI.Match(str);
            List<XTERM_CODE> codes = new List<XTERM_CODE>();
            // Ok first let's grab all the CSI command codes(if any)
            DumbStringTokenizer tok = new DumbStringTokenizer(str);
            if (tok.TryConsume(XTERM.CSI))//it's only a CSI if it starts with the Control Sequence escape character.
            {
                if (tok.TryConsume('['))// If '[' is right after the control char then it indicates that this is a multi-command-char sequence, AKA THE ONLY ONE ANYBODY EVER USES! (Even though there ARE other kinds) So it's the only one we care to handle
                {
                    // what follows the first two chars SHOULD be a text list of numbers seperated by ';' chars. we need to gather said number list
                    while (tok.HasNext())
                    {
                        if (char.IsDigit(tok.Peek()))// Okay this char is part of a number
                        {
                            // Consume all consecutive digits
                            while (char.IsDigit(tok.PeekNext())) tok.Next();// move to the next char
                            //since the next char is not a digit we want to consume the list of digits we just verified and push them to the control code list now.
                            string num = tok.Consume();
                            int code = Convert.ToInt32(num);
                            codes.Add((XTERM_CODE)code);
                        }

                        // Okay we have encountered the first non-digit char.
                        // IF it is ';' then we can consume and continue, otherwise we abort the loop.
                        if (!tok.TryConsume(';')) break;
                    }
                    //now verify that the next char is the Control Sequence termination char
                    char c = tok.Peek();
                    if (Is_CSI_Termination_Char(c)) tok.Consume();
                }
            }
            // Now that we will have parsed out the CSI block, consume what remains of the string as our text!
            string TEXT = tok.ConsumeAll();
            return new XTERM_BLOCK(TEXT, codes);
        }

        /// <summary>
        /// Strips all of the XTERM command sequences from a string and returns the cleaned string.
        /// </summary>
        public static string Strip(string format, params object[] args)
        {
            string str = format;
            if (args.Length > 0) str = string.Format(format, args);
            // Get the list of CSI's 
            List<string> CSIS = Tokenize_Control_Sequence_Initiators(str);

            // Now build an XTERM_COMMAND_BLOCK for each CSI and add it to our list
            string cleanStr = "";
            foreach (string block in CSIS)
            {
                XTERM_BLOCK xb = Compile_Xterm_Command_Block(block);
                cleanStr += xb.TEXT;
            }

            return cleanStr;
        }
        #endregion

        #region TERMINAL OUTPUT EMULATION
        public static void Write(string format, params object[] args)
        {
            string str = format;
            if (args.Length > 0) str = string.Format(format, args);
            if (IsLinux)
            {
                Console.Write(str);
            }
            else
            {
                // Get the list of CSI's 
                List<string> CSIS = Tokenize_Control_Sequence_Initiators(str);
                // Now build an XTERM_COMMAND_BLOCK for each CSI and add it to our list
                foreach (string block in CSIS)
                {
                    Execute_Xterm_Command_Block(block);
                }
            }
        }

        public static void WriteLine(string format, params object[] args)
        {
            Write( format, args );
            Write( COLOR_RESET );
            Console.WriteLine();
        }

        private static List<string> Tokenize_Control_Sequence_Initiators(string str)
        {
            List<string> list = new List<string>();
            if (string.IsNullOrEmpty(str)) return list;

            int len = str.Length;
            int p = 0;// current processing idx
            int c = 0;// last consumed idx
            const char ESC = '\x1b';

            while (++p < len)
            {
                if (str[p] == ESC)
                {
                    int l = (p - c);
                    if (l <= 0) continue;
                    string tok = str.Substring(c, l);// extract our token
                    list.Add(tok);
                    c = p;
                }
            }

            string tk = str.Substring(c, (p - c));// extract our token
            list.Add(tk);

            return list;
        }

        private static void Execute_Xterm_Command_Block(string str)
        {
            XTERM_BLOCK xb = Compile_Xterm_Command_Block(str);
            foreach (XTERM_CODE cmd in xb.Codes)
            {
                Emulate_Xterm_Command(cmd);
            }

            Console.Write(xb.TEXT);
        }

        private static void Emulate_Xterm_Command(XTERM_CODE cmd)
        {
            switch (cmd)
            {
                case XTERM_CODE.SET_BG_DEFAULT:
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
                case XTERM_CODE.SET_FG_DEFAULT:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;


                case XTERM_CODE.SET_BG_BLACK:
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
                case XTERM_CODE.SET_BG_BLACK_BRIGHT:
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                    break;

                case XTERM_CODE.SET_BG_RED:
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    break;
                case XTERM_CODE.SET_BG_RED_BRIGHT:
                    Console.BackgroundColor = ConsoleColor.Red;
                    break;

                case XTERM_CODE.SET_BG_GREEN:
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    break;
                case XTERM_CODE.SET_BG_GREEN_BRIGHT:
                    Console.BackgroundColor = ConsoleColor.Green;
                    break;

                case XTERM_CODE.SET_BG_YELLOW:
                    Console.BackgroundColor = ConsoleColor.DarkYellow;
                    break;
                case XTERM_CODE.SET_BG_YELLOW_BRIGHT:
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    break;

                case XTERM_CODE.SET_BG_BLUE:
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    break;
                case XTERM_CODE.SET_BG_BLUE_BRIGHT:
                    Console.BackgroundColor = ConsoleColor.Blue;
                    break;

                case XTERM_CODE.SET_BG_MAGENTA:
                    Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    break;
                case XTERM_CODE.SET_BG_MAGENTA_BRIGHT:
                    Console.BackgroundColor = ConsoleColor.Magenta;
                    break;

                case XTERM_CODE.SET_BG_CYAN:
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    break;
                case XTERM_CODE.SET_BG_CYAN_BRIGHT:
                    Console.BackgroundColor = ConsoleColor.Cyan;
                    break;

                case XTERM_CODE.SET_BG_WHITE:
                    Console.BackgroundColor = ConsoleColor.Gray;
                    break;
                case XTERM_CODE.SET_BG_WHITE_BRIGHT:
                    Console.BackgroundColor = ConsoleColor.White;
                    break;


                // FOREGROUND COLORS
                case XTERM_CODE.SET_FG_BLACK:
                    Console.ForegroundColor = ConsoleColor.Black;
                    break;
                case XTERM_CODE.SET_FG_BLACK_BRIGHT:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;

                case XTERM_CODE.SET_FG_RED:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case XTERM_CODE.SET_FG_RED_BRIGHT:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                case XTERM_CODE.SET_FG_GREEN:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
                case XTERM_CODE.SET_FG_GREEN_BRIGHT:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;

                case XTERM_CODE.SET_FG_YELLOW:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case XTERM_CODE.SET_FG_YELLOW_BRIGHT:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case XTERM_CODE.SET_FG_BLUE:
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    break;
                case XTERM_CODE.SET_FG_BLUE_BRIGHT:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;

                case XTERM_CODE.SET_FG_MAGENTA:
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    break;
                case XTERM_CODE.SET_FG_MAGENTA_BRIGHT:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;

                case XTERM_CODE.SET_FG_CYAN:
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    break;
                case XTERM_CODE.SET_FG_CYAN_BRIGHT:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;

                case XTERM_CODE.SET_FG_WHITE:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case XTERM_CODE.SET_FG_WHITE_BRIGHT:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
        }
        #endregion
    }
}