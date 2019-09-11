using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace xLog.VirtualTerminal
{
    /* XXX: Finish implementing entirety of ANSI codes */
    /* Docs: https://en.wikipedia.org/wiki/ANSI_escape_code#CSI_sequences */
    /* Docs: http://www.lihaoyi.com/post/BuildyourownCommandLinewithANSIescapecodes.html */
    internal static class VT
    {
        #region States
        static bool bModeInvertVideo = false;
        #endregion

        #region Structures
        internal struct VT_BLOCK { public int[] Codes; public ReadOnlyMemory<char> Text; }
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

        #region Command Parsing
        private static List<int> Consume_CSI_Parameters(DataStream<char> Stream)
        {
            var RetList = new List<int>(3);
            /* Check if the stream is currently at a Control Sequence Initiator */
            if (Stream.Next == ANSI.CSI && Stream.NextNext == '[')
            {
                Stream.Consume(2);/* Consume the CSI block start */
                /* Extract the control sequence block */
                var BlockStream = Stream.Substream(x => !Is_Final_Byte(Stream.Next));
                if (!Is_Final_Byte(Stream.Next))
                {
                    throw new FormatException($"Malformed ANSI escape sequence. The sequence lacks a terminator character @ \"{BlockStream.AsMemory().ToString()}{Stream.Slice(0, 15).ToString()}\"");
                }
                Stream.Consume(); /* Consume CSI terminator */

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
                            if (bLastWasDigit)
                            {
                                RetList.Add(paramCode);
                            }
                            bLastWasDigit = false;
                        }
                        /*if (!Enum.IsDefined(typeof(ANSI_CODE), code))
                        {
                            throw new ArgumentException($"Unrecognized ANSI escape code: \"\"");
                        }*/
                    }
                }
            }

            return RetList;
        }

        public static IEnumerable<VT_BLOCK> Compile_Command_Blocks(ReadOnlyMemory<char> Data)
        {
            // Each escape sequence begins with ESC (0x1B) followed by a single char in the range <64-95>(most often '[') followed by a series of command chars each sperated by a semicolon ';', the entire sequence is ended with a character in the range <64-126>(most often 'm')
            var RetList = new LinkedList<VT_BLOCK>();
            DataStream<char> Stream = new DataStream<char>(Data, char.MinValue);

            while (!Stream.atEnd)
            {
                int[] codes = null;
                if (Stream.Next == ANSI.CSI)
                {
                    codes = Consume_CSI_Parameters(Stream).ToArray();
                }
                else
                {
                    codes = new int[0];
                }
                /* Consume all text up until the next Control Sequence Initiator */
                Stream.Consume_While(x => x != ANSI.CSI, out ReadOnlyMemory<char> outText);

                var block = new VT_BLOCK() { Codes = codes, Text = outText };
                RetList.AddLast(block);
            }

            return RetList;
        }
        #endregion

        #region Emulation
        public static void Emulate(ReadOnlyMemory<char> Str)
        {
            var CompiledBlocks = Compile_Command_Blocks(Str);
            foreach (VT_BLOCK Block in CompiledBlocks)
            {
                foreach (VT_CODE cmd in Block.Codes)
                {
                    Execute_VT_Command(cmd);
                }

                Console.Write(Block.Text);
            }
        }

        private static void Execute_VT_Command(VT_CODE Code)
        {
            /* Currently we are only emulating color codes. */
            /* XXX: Implement emulation for other codes such as the cursor controls */
            switch (Code)
            {
                case VT_CODE.SET_BG_DEFAULT:
                    Set_BG( ConsoleColor.Black );
                    break;
                case VT_CODE.SET_FG_DEFAULT:
                    Set_FG( ConsoleColor.Gray );
                    break;


                case VT_CODE.SET_BG_BLACK:
                    Set_BG( ConsoleColor.Black );
                    break;
                case VT_CODE.SET_BG_BLACK_BRIGHT:
                    Set_BG( ConsoleColor.DarkGray );
                    break;

                case VT_CODE.SET_BG_RED:
                    Set_BG( ConsoleColor.DarkRed );
                    break;
                case VT_CODE.SET_BG_RED_BRIGHT:
                    Set_BG( ConsoleColor.Red );
                    break;

                case VT_CODE.SET_BG_GREEN:
                    Set_BG( ConsoleColor.DarkGreen );
                    break;
                case VT_CODE.SET_BG_GREEN_BRIGHT:
                    Set_BG( ConsoleColor.Green );
                    break;

                case VT_CODE.SET_BG_YELLOW:
                    Set_BG( ConsoleColor.DarkYellow );
                    break;
                case VT_CODE.SET_BG_YELLOW_BRIGHT:
                    Set_BG( ConsoleColor.Yellow );
                    break;

                case VT_CODE.SET_BG_BLUE:
                    Set_BG( ConsoleColor.DarkBlue );
                    break;
                case VT_CODE.SET_BG_BLUE_BRIGHT:
                    Set_BG( ConsoleColor.Blue );
                    break;

                case VT_CODE.SET_BG_MAGENTA:
                    Set_BG( ConsoleColor.DarkMagenta );
                    break;
                case VT_CODE.SET_BG_MAGENTA_BRIGHT:
                    Set_BG( ConsoleColor.Magenta );
                    break;

                case VT_CODE.SET_BG_CYAN:
                    Set_BG( ConsoleColor.DarkCyan );
                    break;
                case VT_CODE.SET_BG_CYAN_BRIGHT:
                    Set_BG( ConsoleColor.Cyan );
                    break;

                case VT_CODE.SET_BG_WHITE:
                    Set_BG( ConsoleColor.Gray );
                    break;
                case VT_CODE.SET_BG_WHITE_BRIGHT:
                    Set_BG( ConsoleColor.White );
                    break;


                // FOREGROUND COLORS
                case VT_CODE.SET_FG_BLACK:
                    Set_FG( ConsoleColor.Black );
                    break;
                case VT_CODE.SET_FG_BLACK_BRIGHT:
                    Set_FG( ConsoleColor.DarkGray );
                    break;

                case VT_CODE.SET_FG_RED:
                    Set_FG( ConsoleColor.DarkRed );
                    break;
                case VT_CODE.SET_FG_RED_BRIGHT:
                    Set_FG( ConsoleColor.Red );
                    break;

                case VT_CODE.SET_FG_GREEN:
                    Set_FG( ConsoleColor.DarkGreen );
                    break;
                case VT_CODE.SET_FG_GREEN_BRIGHT:
                    Set_FG( ConsoleColor.Green );
                    break;

                case VT_CODE.SET_FG_YELLOW:
                    Set_FG( ConsoleColor.DarkYellow );
                    break;
                case VT_CODE.SET_FG_YELLOW_BRIGHT:
                    Set_FG( ConsoleColor.Yellow );
                    break;

                case VT_CODE.SET_FG_BLUE:
                    Set_FG( ConsoleColor.DarkBlue );
                    break;
                case VT_CODE.SET_FG_BLUE_BRIGHT:
                    Set_FG( ConsoleColor.Blue );
                    break;

                case VT_CODE.SET_FG_MAGENTA:
                    Set_FG( ConsoleColor.DarkMagenta );
                    break;
                case VT_CODE.SET_FG_MAGENTA_BRIGHT:
                    Set_FG( ConsoleColor.Magenta );
                    break;

                case VT_CODE.SET_FG_CYAN:
                    Set_FG( ConsoleColor.DarkCyan );
                    break;
                case VT_CODE.SET_FG_CYAN_BRIGHT:
                    Set_FG( ConsoleColor.Cyan );
                    break;

                case VT_CODE.SET_FG_WHITE:
                    Set_FG( ConsoleColor.Gray );
                    break;
                case VT_CODE.SET_FG_WHITE_BRIGHT:
                    Set_FG( ConsoleColor.White );
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Set_FG(ANSI_COLOR Color) => Console.ForegroundColor = ANSI.Color_ANSI_To_Console(Color);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Set_BG(ANSI_COLOR Color) => Console.BackgroundColor = ANSI.Color_ANSI_To_Console(Color);
        #endregion
    }
}
