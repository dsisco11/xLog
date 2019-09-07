using System;

namespace xLog
{
    /// <summary>
    /// Contains helpful functions for performing complex UI related operations on console output
    /// </summary>
    public static class ConsoleExt
    {
        #region Console Control Handler
        /*
        private static void onReceivedSignal(Mono.Unix.Native.Signum code)
        {
            Console.WriteLine("Exiting due to: {0}", code);
            // Put your own handler here
            onConsoleExit?.Invoke(code);
            //shutdown right away so there are no lingering threads
            Environment.Exit(-1);
        }

        public static event Action<Signum> onConsoleExit;
        // Catch SIGINT and SIGUSR1
        static UnixSignal[] signals = new UnixSignal[] {
            new UnixSignal (Mono.Unix.Native.Signum.SIGINT),
            new UnixSignal (Mono.Unix.Native.Signum.SIGUSR1),
        };
        */

        static ConsoleExt()
        {
            /*
            Thread th = new Thread(delegate() {
                while (true)
                {
                    // Wait for a signal to be delivered
                    int index = UnixSignal.WaitAny(signals, -1);

                    Signum signal = signals[index].Signum;

                    // Notify the main thread that a signal was received,
                    // you can use things like:
                    //    Application.Invoke () for Gtk#
                    //    Control.Invoke on Windows.Forms
                    //    Write to a pipe created with UnixPipes for server apps.
                    //    Use an AutoResetEvent

                    // For example, this works with Gtk#
                    ((Action<Signum>)onReceivedSignal).Invoke(signal);
                }
            });
            */
        }
        #endregion

        /// <summary>
        /// Erases the current console line and replaces it with different text
        /// </summary>
        /// <param name="line"></param>
        public static void Rewrite_Line(string line)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write("".PadRight(Console.BufferWidth));
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(line);
        }

        public class ConsoleLinePos {
            public int X,Y;
            public object[] Data;
        }


        public static void Clear_Line(ref ConsoleLinePos pos)
        {
            if (pos == null) return;
            Console.SetCursorPosition(pos.X, pos.Y);
            Console.Write("".PadRight(Console.BufferWidth - pos.X));//clear the line
            Console.SetCursorPosition(pos.X, pos.Y);
            pos = null;
        }

        #region Progress Bar
        /// <summary>
        /// Creates a progress bar at the current console cursor position and returns that position so the progress bar may be continually updated.
        /// </summary>
        /// <param name="percent"></param>
        /// <returns></returns>
        public static ConsoleLinePos Progress(float percent)
        {
            ConsoleLinePos pos = new ConsoleLinePos() { Y = Console.CursorTop, X = Console.CursorLeft };
            Progress(percent, ref pos);
            Console.WriteLine("");// Finish the line here so other output's don't end up writing over it
            return pos;
        }

        /// <summary>
        /// Draws a grey and green progress bar in the console
        /// </summary>
        /// <param name="percent">The normalized percentage for the progress bar in the range [0f-1f]</param>
        public static void Progress(float percent, ref ConsoleLinePos pos)
        {
            int Y = Console.CursorTop;
            int X = Console.CursorLeft;

            Console.SetCursorPosition(pos.X, pos.Y);
            Console.Write("".PadRight(Console.BufferWidth - pos.X));//clear the line

            ConsoleColor bg = Console.BackgroundColor;
            Console.SetCursorPosition(pos.X, pos.Y);
            Console.Write("\r ");
            int width = (Console.WindowWidth - pos.X - 3);
            int prog = (int)(percent * width);

            Console.BackgroundColor = ConsoleColor.Green;
            Console.Write(new string(' ', prog));
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.Write(new string(' ', width-prog));

            Console.BackgroundColor = bg;
            Console.SetCursorPosition(X, Y);
        }
        #endregion


        #region Percentage

        public static ConsoleLinePos Draw_Percentage(float percent)
        {
            ConsoleLinePos pos = new ConsoleLinePos() { Y = Console.CursorTop, X = Console.CursorLeft };
            Progress(percent, ref pos);
            Console.WriteLine("");// Finish the line here so other output's don't end up writing over it
            return pos;
        }

        public static void Draw_Percentage(float percent, ref ConsoleLinePos pos)
        {
            int Y = Console.CursorTop;
            int X = Console.CursorLeft;

            Console.SetCursorPosition(pos.X, pos.Y);
            Console.Write(new string(' ', 7));//clear the line
        
            Console.SetCursorPosition(pos.X, pos.Y);
            Console.Write("{0,6:#00.00}%", 100f * percent);
        
            Console.SetCursorPosition(X, Y);
        }
        #endregion

        public static ConsoleLinePos Draw_Ellipses(ref int stage)
        {
            ConsoleLinePos pos = new ConsoleLinePos() { Y = Console.CursorTop, X = Console.CursorLeft };
            Draw_Ellipses(ref stage, 3, ref pos);
            Console.WriteLine("");// Finish the line here so other output's don't end up writing over it
            return pos;
        }

        /// <summary>
        /// Draws a grey and green progress bar in the console
        /// </summary>
        /// <param name="percent">The normalized percentage for the progress bar in the range [0f-1f]</param>
        public static void Draw_Ellipses(ref int stage, int max, ref ConsoleLinePos pos)
        {
            stage = ((stage + 1) % (max+1));
            int Y = Console.CursorTop;
            int X = Console.CursorLeft;

            Console.SetCursorPosition(pos.X, pos.Y);
            Console.Write("".PadRight(Console.BufferWidth - pos.X));//clear the line
        
            Console.SetCursorPosition(pos.X, pos.Y);
            Console.Write("\r ");        
            Console.Write(new string('.', stage));
            Console.SetCursorPosition(X, Y);
            //Console.SetCursorPosition(Console.BufferWidth-1, Math.Max(0, Y - 1));
        }
    }
}