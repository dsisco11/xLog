using System;

namespace xLog.Widgets
{
    /// <summary>
    /// Manages access to static console lines provided by the logging system.
    /// A 'static' line is one that does not appear in log files but appears on the console output stream and whose position does not change within the console yet allows text to scroll around it.
    /// </summary>
    public class StaticConsoleLine : IDisposable
    {
        #region Properties
        /// <summary>
        /// Index within the logging system's map of static lines.
        /// </summary>
        public Guid ID { get; private set; }
        /// <summary>
        /// Tracks the length of the text this line currently has displayed on-screen.
        /// </summary>
        public int Current_Display_Length { get; internal set; } = 0;
        /// <summary>
        /// The text this line wants to display on-screen (can differ from what is currently displayed)
        /// </summary>
        public string Buffer { get; private set; } = string.Empty;
        /// <summary>
        /// Whichever <see cref="StaticConsoleLine"/> has control of the console cursor will always be rendered last
        /// </summary>
        public readonly bool HasCursorControl = false;
        public int CursorPos { get; private set; } = 0;
        private Action<StaticConsoleLine> ChangeCallback;
        #endregion

        #region Constructors
        public StaticConsoleLine()
        {
            ID = Guid.NewGuid();
            ChangeCallback = xLogEngine.Register_Static_Line(this);
        }
        public StaticConsoleLine(string Text) : this()
        {
            Set(Text);
        }
        public StaticConsoleLine(string Text, bool RequireCursorControl) : this()
        {
            if (RequireCursorControl)
            {
                HasCursorControl = xLogEngine.Request_Cursor_Control(this);
            }
            Set(Text);
        }

        ~StaticConsoleLine()
        {
            Dispose();
        }

        public void Dispose()
        {
            xLogEngine.Unregister_Static_Line(this);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Overrides
        public override bool Equals(object obj)
        {
            if (obj is StaticConsoleLine)
            {
                return ((StaticConsoleLine)obj).ID == ID;
            }

            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
        #endregion

        /// <summary>
        /// Sets the text displayed by this <see cref="StaticConsoleLine"/>
        /// </summary>
        /// <param name="Text"></param>
        public void Set(string Text)
        {
            lock (Buffer)
            {
                Buffer = Text;
                ChangeCallback?.Invoke(this);
            }
        }
        /// <summary>
        /// Appends text to this <see cref="StaticConsoleLine"/>
        /// </summary>
        /// <param name="Text"></param>
        public void Append(string Text)
        {
            lock (Buffer)
            {
                Buffer = string.Concat(Buffer, Text);
                ChangeCallback?.Invoke(this);
            }
        }

        public void Set_Cursor_Pos(int pos)
        {
            if (HasCursorControl && CursorPos != pos)
            {
                CursorPos = pos;
                ChangeCallback?.Invoke(this);
            }
        }
    }
}