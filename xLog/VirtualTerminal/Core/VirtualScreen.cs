using System;
using System.Collections.Generic;
using xLog.Types;

namespace xLog.VirtualTerminal
{
    public delegate void ScreenUpdateHandler(int Index, TerminalText Text, EScreenUpdateType UpdateType);
    public enum EScreenUpdateType { TextAdded, TextRemoved, TextCleared, TextChanged }
    /// <summary>
    /// Represents an individual terminal "screen" that holds output from a buffer
    /// </summary>
    public class VirtualScreen
    {
        [Flags]
        public enum EScreenDirtFlags { BufferSize = 0x1 };
        const int CHUNK_SIZE = 32;

        #region Backing Values
        private int? _buffer_width = null;
        private int? _buffer_height = null;
        #endregion

        #region Properties
        public EScreenDirtFlags Dirt = 0x0;
        public readonly Guid ID = Guid.NewGuid();
        /// <summary>
        /// Fired whenever the screens buffer is altered
        /// </summary>
        public event ScreenUpdateHandler onUpdate;
        public readonly List<TerminalText> Buffer;
        public int CursorTop { get; private set; } = 0;
        public int CursorLeft { get; private set; } = 0;
        internal int WritePosLeft = 0;
        internal int WritePosTop = 0;
        #endregion

        #region Accessors
        public int BufferWidth
        {
            get => _buffer_width.GetValueOrDefault(Console.BufferWidth);
            set
            {
                if (value <= 0) _buffer_width = null;
                else _buffer_width = value;
            }
        }
        public int BufferHeight
        {
            get => _buffer_height.GetValueOrDefault(Console.BufferHeight);
            set
            {
                if (value <= 0) _buffer_height = null;
                else _buffer_height = value;
            }
        }
        #endregion

        #region States
        bool bIgnoreTextUpdates = false;
        #endregion

        #region Constructors
        public VirtualScreen()
        {
            Buffer = new List<TerminalText>(CHUNK_SIZE);
            Terminal.Register_Screen(this);
        }
        #endregion

        #region Events
        internal void onText_Change(TerminalText Text)
        {
            if (bIgnoreTextUpdates) return;

            int index = Buffer.IndexOf(Text);
            onUpdate?.Invoke(index, Text, EScreenUpdateType.TextChanged);
        }
        #endregion

        internal void Update()
        {
            if (0 != (Dirt & EScreenDirtFlags.BufferSize))
            {
                Reflow();
            }

            Dirt = 0x0;
        }


        #region Buffer Management
        /// <summary>
        /// "Write" text to this screen
        /// </summary>
        /// <param name="Text"></param>
        /// <returns></returns>
        public int Add(TerminalText Text)
        {
            if (Text == null) return 0;
            int idx = Buffer.Count;
            if (idx == (Buffer.Capacity - 1))
            {/* Add more space to our buffer and keep it chunk-aligned */
                Buffer.Capacity = ((int)(Buffer.Capacity / CHUNK_SIZE) * CHUNK_SIZE) + CHUNK_SIZE;
            }
            Buffer.Add(Text);

            Text.Set_Owner(this);
            Flow_Text(Text);
            onUpdate?.Invoke(idx, Text, EScreenUpdateType.TextAdded);
            return ANSI.Strip(Text.DisplayText)?.Length ?? 0;
        }

        public void Remove(TerminalText Text)
        {
            if (Text == null)
                return;

            var index = Buffer.IndexOf(Text);
            Text.Set_Owner(null);
            Buffer.RemoveAt(index);
            Reflow(index);

            onUpdate?.Invoke(index, Text, EScreenUpdateType.TextRemoved);
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index > Buffer.Count-1)
                return;

            var Text = Buffer[index];
            Text.Set_Owner(null);
            Buffer.RemoveAt(index);
            Reflow(index);

            onUpdate?.Invoke(index, Text, EScreenUpdateType.TextRemoved);
        }

        public void Clear()
        {
            if (Buffer.Count <= 0)
                return;
            foreach(TerminalText Text in Buffer)
            {
                Text.Set_Owner(null);
            }

            Buffer.Clear();
            Buffer.Capacity = CHUNK_SIZE;
            CursorLeft = CursorTop = 0;
            WritePosLeft = WritePosTop = 0;

            onUpdate?.Invoke(0, null, EScreenUpdateType.TextCleared);
        }
        #endregion

        #region Text Flow
        /// <summary>
        /// Reflows and repositions all text
        /// </summary>
        private void Reflow(int Start = 0)
        {
            bIgnoreTextUpdates = true;
            try
            {
                WritePosLeft = WritePosTop = 0;
                if (Start > 0)
                {
                    Start = Math.Min(Start, Buffer.Count - 1);
                    var Text = Buffer[Start];
                    WritePosLeft = Text.Position.X;
                    WritePosTop = Text.Position.Y;
                }

                for (int i = Start; i < Buffer.Count; i++)
                {
                    TerminalText Text = Buffer[i];
                    Flow_Text(Text);
                }
            }
            finally
            {
                bIgnoreTextUpdates = false;
            }
            /* Dont need to update text here, it's most likely that the VirtualTerminal is about to reprint this entire screen */
            /*for (int i=0; i<Buffer.Count; i++)
            {
                TerminalText Text = Buffer[i];
                onUpdate?.Invoke(i, Text, EScreenUpdateType.TextChanged);
            }*/
        }

        internal void Flow_Text(TerminalText Text)
        {
            var Blocks = Terminal.Compile_Command_Blocks(Text.Buffer);

            foreach (CSI_BLOCK Block in Blocks)
            {
                /* If the block has a control char then we have to search for newlines and whatnot but if it doesnt we can just do some simple math */
                if (!Block.HasControlChar)
                {
                    Flow_WritePos(Block.Text.Length);
                }
                else
                {
                    var Stream = new DataStream<char>(Block.Text, '\0');

                    while (!Stream.atEnd)
                    {
                        if (Stream.Consume_While(x => x != '\n' && x != '\r', out ReadOnlyMemory<char> outChars))
                        {
                            Flow_WritePos(outChars.Length);
                        }

                        switch (Stream.Next)
                        {
                            case '\n':
                                {
                                    WritePosTop++;
                                    WritePosLeft = 0;
                                }
                                break;
                            case '\r':
                                {
                                    WritePosLeft = 0;
                                }
                                break;
                        }
                    }
                }
            }

            Text.Set_Position(new CursorPos(WritePosLeft, WritePosTop));
        }

        /// <summary>
        /// Simulates the cursor movement by a given number of characters
        /// </summary>
        /// <param name="count"></param>
        private void Flow_WritePos(int Count)
        {
            int dx = WritePosLeft + Count;
            int dy = WritePosTop;

            if (dx > BufferWidth)
            {
                int Spillover = (dx / BufferWidth);
                if (Spillover > 0)
                {
                    dy += Spillover;
                    dx = (dx % BufferWidth);
                }
            }

            WritePosLeft = dx;
            WritePosTop = dy;
        }
        #endregion
    }
}
