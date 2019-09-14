using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using xLog.Types;

namespace xLog.VirtualTerminal
{
    /// <summary>
    /// Represents a sequence of text in the terminal output
    /// </summary>
    public partial class TerminalText
    {
        [Flags]
        public enum EDirtFlags {Text = 0x1, Position = 0x2};

        #region Properties
        private int Disposed = 0;
        public CursorPos Position { get; private set; }
        public StringPtr Buffer { get; private set; } = null;
        public EDirtFlags Dirt { get; private set; } = 0x0;
        /// <summary>
        /// Text that is currently displayed by the terminal to represent this object
        /// </summary>
        public StringPtr DisplayText { get; private set; } = null;
        /// <summary>
        /// Used to signal to the owning terminal that this text has changed and should be updated
        /// </summary>
        private Action<TerminalText> ChangeCallback;
        private WeakReference<VirtualScreen> Owner;
        #endregion

        #region Constructors
        public TerminalText(StringPtr text)
        {
            Buffer = text;
            Dirt = (EDirtFlags.Position & EDirtFlags.Text);
            Owner = new WeakReference<VirtualScreen>(null);
        }

        ~TerminalText()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref Disposed, 1) == 1)
            {
                return;
            }

            if (Owner != null && Owner.TryGetTarget(out VirtualScreen outScreen))
            {
                outScreen.Remove(this);
            }

            GC.SuppressFinalize(this);
        }
        #endregion

        #region Setters
        public void Set_Owner(VirtualScreen Screen)
        {
            Owner.SetTarget(Screen);
            Dirt |= (EDirtFlags.Text & EDirtFlags.Position);
            if (Screen != null)
                ChangeCallback = Screen.onText_Change;
            else
                ChangeCallback = null;
        }
        
        /// <summary>
        /// Sets the text displayed
        /// </summary>
        /// <param name="Text"></param>
        public void Set(StringPtr Text)
        {
            lock (Buffer)
            {
                Buffer = Text;
                Dirt |= EDirtFlags.Text;
            }
            ChangeCallback?.Invoke(this);
        }

        public void Set_Position(CursorPos pos)
        {
            Position = pos;
            Dirt |= EDirtFlags.Position;
            ChangeCallback?.Invoke(this);
        }
        #endregion

        /// <summary>
        /// Updates this text within it's terminal and returns a delta change size
        /// </summary>
        public int Update()
        {
            if (Disposed == 1)
            {
                return 0;
            }

            bool bUpdatedPos = false;
            if (0 != (Dirt & EDirtFlags.Position))
            {
                /*if (Owner.TryGetTarget(out VirtualScreen Screen))
                {
                    Position = new CursorPos(Screen.CursorLeft, Screen.CursorTop);
                    bUpdatedPos = true;
                    Dirt &= ~EDirtFlags.Position;
                }*/

                Position = new CursorPos(Terminal.CursorLeft, Terminal.CursorTop);
                bUpdatedPos = true;
                Dirt &= ~EDirtFlags.Position;
            }

            if (bUpdatedPos)
            {
                Terminal.Push_Cursor();
                Terminal.Set_Cursor(Position.X, Position.Y);
            }

            if (0 != (Dirt & EDirtFlags.Text) || bUpdatedPos)
            {
                /* Figure out which parts we need to reprint */
                var Commands = DiffEngine.Compile_Transformations(DisplayText, Buffer);
                foreach (TextChunk Command in Commands)
                {
                    /* Translate the commands position (which is relative the this object position) into actual screen buffer coordinates */
                    var X = Position.X + Command.Offset;
                    Terminal.Set_Cursor(X % Terminal.Width, Position.Y + (X / Terminal.Width));
                    Terminal.Output(Command.Text);
                }

                //ANSI.Write(Buffer.AsMemory());
                DisplayText = new string(Buffer.AsMemory().ToArray());
                Dirt &= ~EDirtFlags.Text;
            }

            if (bUpdatedPos) Terminal.Pop_Cursor();

            return 0;
        }

        public void Clear()
        {
            if (DisplayText?.AsMemory() == null || DisplayText?.AsMemory().Length <= 0) return;

            VirtualTerminal.Terminal.Blit(DisplayText.AsMemory().Length, Position.X, Position.Y);
            DisplayText = null;
            Dirt |= EDirtFlags.Text;
        }
    }
}
