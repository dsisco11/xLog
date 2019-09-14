using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using xLog.Types;

namespace xLog.VirtualTerminal
{
    /* XXX: Finish implementing entirety of ANSI codes */
    /* Docs: https://en.wikipedia.org/wiki/ANSI_escape_code#CSI_sequences */
    /* Docs: http://www.lihaoyi.com/post/BuildyourownCommandLinewithANSIescapecodes.html */
    public static partial class Terminal
    {
        #region Properties
        private static Stack<CursorPos> Stack_CursorPos = new Stack<CursorPos>();
        private static Stack<bool> Stack_CursorVis= new Stack<bool>();
        private static List<VirtualScreen> Screens = new List<VirtualScreen>();
        private static int ActiveScreenIndex = 0;
        #endregion

        #region States
        static bool bModeInvertVideo = false;
        #endregion

        #region Accessors
        /// <summary>
        /// Returns <c>True</c> if the current environment is one which does not support ANSI Escape Codes.
        /// </summary>
        public static bool RequiresEmulation => !Platform.Supports_VirtualTerminal();
        /// <summary>
        /// Buffer width
        /// </summary>
        public static int Width => Console.BufferWidth;
        /// <summary>
        /// Buffer height
        /// </summary>
        public static int Height => Console.BufferHeight;
        /// <summary>
        /// The currently active screen
        /// </summary>
        public static VirtualScreen ActiveScreen => Screens[ActiveScreenIndex];

        public static int CursorTop => ActiveScreen.CursorTop;
        public static int CursorLeft => ActiveScreen.CursorLeft;
        #endregion

        #region Constructor
        static Terminal()
        {
            var Screen = new VirtualScreen();
            Set_Active_Screen(Screen);
        }
        #endregion


        #region Screens
        internal static void Register_Screen(VirtualScreen Screen)
        {
            if (Screens.Contains(Screen))
                return;

            Screens.Add(Screen);
        }

        /// <summary>
        /// Changes which screen is currently displayed.
        /// </summary>
        /// <param name="Screen"></param>
        public static void Set_Active_Screen(VirtualScreen Screen)
        {
            /* Unregister the currently active screen */
            if (ActiveScreen != null)
            {
                ActiveScreen.onUpdate -= ActiveScreen_onUpdate;
            }

            int i = Screens.IndexOf(Screen);
            ActiveScreenIndex = i;
            ActiveScreen.onUpdate += ActiveScreen_onUpdate;
            ActiveScreen.Update();
            Print_Screen(Screen);
        }

        private static void ActiveScreen_onUpdate(int Index, TerminalText Text, EScreenUpdateType UpdateType)
        {
            Text.Update();
        }

        /// <summary>
        /// Clears the console and then prints the entire contents of the given screen to it
        /// </summary>
        /// <param name="Screen"></param>
        private static void Print_Screen(VirtualScreen Screen)
        {
            /* Clear the console and print this screen */
            Console.Clear();
            Push_Cursor();
            Push_CursorVisible();
            Console.CursorVisible = false;
            foreach (TerminalText Text in Screen.Buffer)
            {
                Set_Cursor(Text.Position.X, Text.Position.Y);
                Terminal.Write(Text.Buffer);
            }
            Pop_Cursor();
            Pop_CursorVisible();
            // Set_Cursor(0, Console.BufferHeight-1);
        }
        #endregion


        #region Checks
        static bool Is_CSI_Termination_Char(char c)
        {
            return (c >= 64 && c <= 126);
        }
        static bool Is_CSI_Separation_Char(char c)
        {
            return (c == ';' || c == ':');
        }

        /// <summary>
        /// Parameter bytes specify commands within a control sequence
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        static bool Is_Parameter_Byte(char c)
        {
            return (c >= 0x30 && c <= 0x3F);
        }
        /// <summary>
        /// Intermediate bytes are used to seperate parameters within a control sequence
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        static bool Is_Intermediate_Byte(char c)
        {
            return (c >= 0x20 && c <= 0x2F);
        }
        /// <summary>
        /// Final bytes signal the end of a control sequence
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        static bool Is_Final_Byte(char c)
        {
            return (c >= 0x40 && c <= 0x7E);
        }
        #endregion

        #region Command Sequence Parsing
        private static bool Consume_CSI_Parameters(DataStream<char> Stream, out List<int> outCodes, out byte? outFinalByte)
        {
            var RetList = new List<int>(3);
            byte? FinalByte = null;
            /* Check if the stream is currently at a Control Sequence Initiator */
            if (Stream.Next == ANSI.CSI && Stream.NextNext == '[')
            {
                Stream.Consume(2);/* Consume the CSI block start */
                /* Extract the control sequence block */
                var BlockStream = Stream.Substream(x => !Is_Final_Byte(Stream.Next) && Stream.Next != ANSI.CSI);
                if (!Is_Final_Byte(Stream.Next))
                {
                    throw new FormatException($"Malformed ANSI escape sequence. The sequence lacks a terminator character @ \"{BlockStream.AsMemory().ToString()}{Stream.Slice(0, 15).ToString()}\"");
                }
                FinalByte = (byte)Stream.Consume(); /* Consume CSI terminator */

                /* Exctract all CSI block codes from the BlockStream */
                if (!BlockStream.atEnd)
                {
                    /* Extract the parameter bytes */
                    var ParamStream = BlockStream.Substream(x => Is_Parameter_Byte(x));
                    /* Consume all intermediate bytes */
                    /*if (!BlockStream.Consume_While(x => Is_Intermediate_Byte(x)))
                    {
                        //throw new FormatException($"Malformed ANSI escape sequence. Expected an intermidate character such as ';' @ \"{BlockStream.AsMemory().ToString()}{Stream.Slice(0, 15).ToString()}\"");
                    }
                    */

                    bool bLastWasDigit = false;
                    while (!ParamStream.atEnd)
                    {
                        int paramCode = 0;
                        if (char.IsDigit(ParamStream.Next))
                        {
                            if (!ParamStream.Consume_While(x => char.IsDigit(x), out ReadOnlyMemory<char> outDigits))
                            {
                                throw new Exception($"Unable to read from stream @ \"{ParamStream.AsMemory().ToString()}\"");
                            }
                            else
                            {
                                paramCode = Int32.Parse(outDigits.ToString());
                            }
                            RetList.Add(paramCode);
                            bLastWasDigit = true;
                        }
                        else
                        {
                            ParamStream.Consume();
                            if (!bLastWasDigit)
                            {
                                RetList.Add(paramCode);
                            }
                            bLastWasDigit = false;
                        }
                    }
                }
            }

            outCodes = RetList;
            outFinalByte = FinalByte;

            return true;
        }

        public static LinkedList<CSI_BLOCK> Compile_Command_Blocks(ReadOnlyMemory<char> Data)
        {
            // Each escape sequence begins with ESC (0x1B) followed by a single char in the range <64-95>(most often '[') followed by a series of command chars each sperated by a semicolon ';', the entire sequence is ended with a character in the range <64-126>(most often 'm')
            var RetList = new LinkedList<CSI_BLOCK>();
            DataStream<char> Stream = new DataStream<char>(Data, char.MinValue);

            while (!Stream.atEnd)
            {
                int[] Codes = null;
                byte? FinalByte = null;
                int BlockStart = Stream.Position;
                if (Stream.Next == ANSI.CSI)
                {
                    if( Consume_CSI_Parameters(Stream, out List<int> outCodes, out byte? outFinalByte))
                    {
                        Codes = outCodes.ToArray();
                        FinalByte = outFinalByte;
                    }
                }
                else
                {
                    Codes = new int[0];
                }
                /* Consume all text up until the next Control Sequence Initiator */
                int TextStart = Stream.Position;
                bool HasControlChar = false;

                /* While we are consuming the text we can also check for control chars */
                Stream.Consume_While(x => x != ANSI.CSI && !char.IsControl(x));
                if (Stream.Next != ANSI.CSI && char.IsControl( Stream.Next ))
                {
                    HasControlChar = true;
                    Stream.Consume_While(x => x != ANSI.CSI);/* Go to the next CSI */
                }
                int BlockEnd = Stream.Position;
                int TextLength = BlockEnd - TextStart;
                var Text = Stream.AsMemory().Slice(TextStart, TextLength);

                var block = new CSI_BLOCK(FinalByte, Codes, Text, BlockStart, BlockEnd, TextStart, HasControlChar);
                RetList.AddLast(block);
            }

            return RetList;
        }
        #endregion

        #region Output
        public static int Write(StringPtr Str)
        {
            ActiveScreen.Add(new TerminalText(Str));
            return ANSI.Strip(Str)?.Length ?? 0;
        }

        public static int WriteLine(StringPtr Str)
        {
            var len = Write(Str);
            Write(Environment.NewLine.AsMemory());
            return len;
        }
        #endregion


        #region Console Emulation

        internal static int Output(StringPtr Str)
        {
            if (RequiresEmulation)
            {
                return Emulate(Str);
            }
            else
            {
                Console.Write(Str);
                return ANSI.Strip(Str)?.Length ?? 0;
            }
        }

        private static int Emulate(StringPtr Str)
        {
            int Count = 0;
            var CompiledBlocks = Compile_Command_Blocks(Str);
            foreach (CSI_BLOCK Block in CompiledBlocks)
            {
                if (Block.CmdByte.HasValue)
                {
                    CSI_COMMAND Command = (CSI_COMMAND)Block.CmdByte;
                    var ParameterStream = new DataStream<int>(Block.Parameters, Int32.MinValue);

                    while (!ParameterStream.atEnd)
                    {
                        try
                        {
                            if (!Execute_CSI_Command(Command, ParameterStream))
                            {
                                throw new Exception($"Failure while emulating VirtualTerminal command @ \"{Block}\"");
                            }
                        }
                        catch (SgrError ex)
                        {
                            Debugger.Break();
                            throw ex;
                        }

                    }

                    Debug.Assert(ParameterStream.atEnd);
                }

                Console.Write(Block.Text);
                Count += Block.Text.AsMemory().Length;
            }
            

            return Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Execute_CSI_Command(CSI_COMMAND Command, DataStream<int> ParameterStream)
        {
            switch (Command)
            {
                case CSI_COMMAND.SGR:
                    {
                        return Execute_SGR_Command(ParameterStream);
                    }
                case CSI_COMMAND.XLOG:/* Custom Commands */
                    {
                        return Execute_XLOG_Command(ParameterStream);
                    }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Execute_SGR_Command(DataStream<int> ParameterStream)
        {
            /* Currently we are only emulating color codes. */
            /* XXX: Implement emulation for other codes such as the cursor controls */
            SGR_CODE Code = SGR_CODE.INVALID;
            if (!ParameterStream.atEnd) Code = (SGR_CODE)ParameterStream.Consume();

            switch (Code)
            {
                case SGR_CODE.RESET_STYLE:
                    {/* All Attribute OFF */
                        bModeInvertVideo = false;
                        Color_Stack_FG.Clear();
                        Color_Stack_BG.Clear();
                        Console.ResetColor();
                    }
                    break;

                case SGR_CODE.RESET_COLOR_FG:
                    {
                        var Defaults = Get_Console_Default_Colors();
                        var Clr = bModeInvertVideo ? Defaults.Item2 : Defaults.Item1;
                        Set_FG(Clr);
                    }
                    break;
                case SGR_CODE.RESET_COLOR_BG:
                    {
                        var Defaults = Get_Console_Default_Colors();
                        var Clr = bModeInvertVideo ? Defaults.Item1 : Defaults.Item2;
                        Set_BG(Clr);
                    }
                    break;

                case SGR_CODE.INVERT:
                    Set_Inverted(true);
                    break;
                case SGR_CODE.INVERT_OFF:
                    Set_Inverted(false);
                    break;

                /* 8-bit Colors */
                case SGR_CODE.SET_COLOR_CUSTOM_FG:
                    {
                        if (ParameterStream.Remaining < 2)
                            throw new SgrFormatError($"Malformed command({Code}) requires atleast 2 parameters!");

                        ANSI_COLOR Clr = ANSI_COLOR.WHITE;
                        /* Read custom mode */
                        int mode = ParameterStream.Consume();
                        switch (mode)
                        {
                            case 2:/* 8-bit */
                                {
                                    if (ParameterStream.Remaining < 3) throw new SgrFormatError($"Malformed command. 8-bit colors require 3 parameters!");
                                }
                                break;
                            case 5: /* 4-bit */
                                Clr = (ANSI_COLOR)ParameterStream.Consume();
                                break;
                            default:
                                throw new SgrFormatError($"Malformed command. '{mode}' is not a valid mode for {Code}");
                        }

                        if (!bModeInvertVideo) Set_FG(Clr);
                        else Set_BG(Clr);
                    }
                    break;
                case SGR_CODE.SET_COLOR_CUSTOM_BG:
                    {
                        if (ParameterStream.Remaining < 2)
                            throw new SgrFormatError($"Malformed command({Code}) requires atleast 2 parameters!");
                        ANSI_COLOR Clr = ANSI_COLOR.BLACK;
                        /* Read custom mode */
                        int mode = ParameterStream.Consume();
                        switch (mode)
                        {
                            case 2:/* 8-bit */
                                {
                                    if (ParameterStream.Remaining < 3) throw new SgrFormatError($"Malformed command. 8-bit colors require 3 parameters!");
                                }
                                break;
                            case 5: /* 4-bit */
                                Clr = (ANSI_COLOR)ParameterStream.Consume();
                                break;
                            default:
                                throw new SgrFormatError($"Malformed command. '{mode}' is not a valid mode for {Code}");
                        }

                        if (!bModeInvertVideo) Set_BG(Clr);
                        else Set_FG(Clr);
                    }
                    break;


                /* 3/4-bit Colors */
                case SGR_CODE n when (n >= SGR_CODE.SET_COLOR_FG && n <= SGR_CODE.SET_COLOR_FG + 7):
                    {
                        var Clr = (ANSI_COLOR)(n - SGR_CODE.SET_COLOR_FG);
                        if (!bModeInvertVideo) Set_FG(Clr);
                        else Set_BG(Clr);
                    }
                    break;
                case SGR_CODE n when (n >= SGR_CODE.SET_COLOR_FG_BRIGHT && n <= SGR_CODE.SET_COLOR_FG_BRIGHT + 7):
                    {
                        var Clr = (ANSI_COLOR)(n - SGR_CODE.SET_COLOR_FG_BRIGHT);
                        if (!bModeInvertVideo) Set_FG(Clr);
                        else Set_BG(Clr);
                    }
                    break;

                case SGR_CODE n when (n >= SGR_CODE.SET_COLOR_BG && n <= SGR_CODE.SET_COLOR_BG + 7):
                    {
                        var Clr = (ANSI_COLOR)(n - SGR_CODE.SET_COLOR_BG);
                        if (!bModeInvertVideo) Set_BG(Clr);
                        else Set_FG(Clr);
                    }
                    break;
                case SGR_CODE n when (n >= SGR_CODE.SET_COLOR_BG_BRIGHT && n <= SGR_CODE.SET_COLOR_BG_BRIGHT + 7):
                    {
                        var Clr = (ANSI_COLOR)(n - SGR_CODE.SET_COLOR_BG_BRIGHT);
                        if (!bModeInvertVideo) Set_BG(Clr);
                        else Set_FG(Clr);
                    }
                    break;
                default:
                    {
                        throw new ArgumentException($"Unrecognized (SelectGraphicRendition) code: {(SGR_CODE)Code}");
                    }

            }

            return true;
        }

        private static Tuple<ANSI_COLOR, ANSI_COLOR> Get_Console_Default_Colors()
        {
            ConsoleColor stackFG = Console.ForegroundColor;
            ConsoleColor stackBG = Console.BackgroundColor;

            Console.ResetColor();

            ANSI_COLOR fgVal = ANSI.Color_Console_To_ANSI(Console.ForegroundColor);
            ANSI_COLOR bgVal = ANSI.Color_Console_To_ANSI(Console.BackgroundColor);

            Console.ForegroundColor = stackFG;
            Console.BackgroundColor = stackBG;

            return new Tuple<ANSI_COLOR, ANSI_COLOR>(fgVal, bgVal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Set_FG(ANSI_COLOR Color) => Console.ForegroundColor = ANSI.Color_ANSI_To_Console(Color);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Set_BG(ANSI_COLOR Color) => Console.BackgroundColor = ANSI.Color_ANSI_To_Console(Color);
        #endregion

        #region XLOG Commands

        private static Stack<ANSI_COLOR> Color_Stack_FG = new Stack<ANSI_COLOR>();
        private static Stack<ANSI_COLOR> Color_Stack_BG = new Stack<ANSI_COLOR>();

        private static bool Execute_XLOG_Command(DataStream<int> ParameterStream)
        {
            /* Currently we are only emulating color codes. */
            /* XXX: Implement emulation for other codes such as the cursor controls */
            XLOG_CODE Code = (XLOG_CODE)Int32.MinValue;
            if (!ParameterStream.atEnd) Code = (XLOG_CODE)ParameterStream.Consume();

            switch (Code)
            {
                case XLOG_CODE.PUSH_FG:
                    Color_Stack_FG.Push(ANSI.Color_Console_To_ANSI(Console.ForegroundColor));
                    break;
                case XLOG_CODE.POP_FG:
                    {
                        if (Color_Stack_FG.Count == 0) break;
                        ANSI_COLOR clr = Color_Stack_FG.Pop();
                        Console.ForegroundColor = ANSI.Color_ANSI_To_Console(clr);
                    }
                    break;

                case XLOG_CODE.PUSH_BG:
                    Color_Stack_BG.Push(ANSI.Color_Console_To_ANSI(Console.BackgroundColor));
                    break;
                case XLOG_CODE.POP_BG:
                    {
                        if (Color_Stack_BG.Count == 0) break;
                        ANSI_COLOR clr = Color_Stack_BG.Pop();
                        Console.BackgroundColor = ANSI.Color_ANSI_To_Console(clr);
                    }
                    break;
                default:
                    {
                        throw new ArgumentException($"Unrecognized xLOG code: {(XLOG_CODE)Code}");
                    }
            }

            return true;
        }
        #endregion

        #region Utility Functions

        public static void Push_Cursor() => Stack_CursorPos.Push(new CursorPos(Console.CursorLeft, Console.CursorTop));
        public static void Pop_Cursor()
        {
            var pos = Stack_CursorPos.Pop();
            Console.CursorLeft = pos.X;
            Console.CursorTop = pos.Y;
        }
        public static void Set_Cursor(int? X = null, int? Y = null)
        {
            Console.CursorLeft = X.GetValueOrDefault(Console.CursorLeft);
            Console.CursorTop = Y.GetValueOrDefault(Console.CursorTop);
        }

        public static void Push_CursorVisible() => Stack_CursorVis.Push(Console.CursorVisible);
        public static void Pop_CursorVisible() => Console.CursorVisible = Stack_CursorVis.Pop();

        public static void Set_Inverted(bool State)
        {
            if (State == bModeInvertVideo) return;

            bModeInvertVideo = State;
            var fgc = Console.ForegroundColor;
            var bgc = Console.BackgroundColor;
            Console.ForegroundColor = bgc;
            Console.BackgroundColor = fgc;
        }

        /// <summary>
        /// Erases a portion of the terminal buffer
        /// </summary>
        public static void Blit(int Length, int? X=null, int? Y = null)
        {
            if (Length <= 0) return;

            Push_Cursor();
            Console.CursorLeft = X.GetValueOrDefault(Console.CursorLeft);
            Console.CursorTop = Y.GetValueOrDefault(Console.CursorTop);

            // Jump to beginning of line and Overwrite all the previous characters with spaces
            //char ERASER = '\b';
            char ERASER = ' ';
            System.Console.Write(new string(ERASER, Math.Min(System.Console.BufferWidth, Length) - 1));
            Pop_Cursor();
        }
        #endregion

    }
}
