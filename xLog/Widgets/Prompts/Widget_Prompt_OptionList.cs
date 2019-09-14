using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xLog.VirtualTerminal;

namespace xLog.Widgets.Prompts
{
    public class Widget_Prompt_OptionList : Widget_PromptBase<int>
    {
        #region Sub Types
        public enum EListDirection { Vertical, Horizontal };
        public struct ListItem
        {
            public string Text; public TerminalColor ForegroundColor; public TerminalColor BackgroundColor;
            public ListItem(string text, TerminalColor foregroundColor, TerminalColor backgroundColor)
            {
                Text = text;
                ForegroundColor = foregroundColor;
                BackgroundColor = backgroundColor;
            }
        }
        #endregion

        #region Backing Values
        private int _selection;
        #endregion

        #region Properties
        bool bInitialized = false;
        public readonly EListDirection Direction;
        private readonly ListItem[] Options = null;

        /// <summary>
        /// The currently selected item in the list
        /// </summary>
        public int Selection {
            get => _selection;
            set {
                _selection = Math.Max(Math.Min(value, Options.Length-1), 0);
                Update();
            }
        }

        #endregion

        #region Constructors
        /// <summary>
        /// Prompts the user to select an item from a list of options displayed in a graphical list.
        /// 
        /// </summary>
        /// <param name="Prompt_Message"></param>
        /// <param name="Options"></param>
        /// <param name="DefaultSelection"></param>
        public Widget_Prompt_OptionList(string Prompt_Message, IEnumerable<ListItem> Options, EListDirection? Direction = null, int DefaultSelection = 0) : base(Prompt_Message)
        {
            this.Options = Options.ToArray();
            this.Direction = Direction.GetValueOrDefault(EListDirection.Vertical);
            bUserInputVisible = false;
            for (int i = 0; i < this.Options.Length; i++)
            {
                StaticLines.Add(new StaticConsoleLine());
            }
            bInitialized = true;
            Update();
        }
        #endregion

        protected override int Translate_UserInput(string UserInput)
        {
            return Selection;
            /*if (string.IsNullOrEmpty(UserInput)) return 0;
            return Convert.ToInt32(UserInput);*/
        }

        protected override void Update()
        {
            if (!bInitialized) return;

            lock (StaticLines)
            {
                StaticLines[0].Set(string.Concat(Message, "\r\n"));// Update the userinput display line
                for (int i = 0; i < Options.Length; i++)
                {
                    var opt = Options[i];

                    var cmds = new List<int>( opt.ForegroundColor.Get_SGR_Code() );
                    var bgclr = opt.BackgroundColor.Get_SGR_Code(true);
                    cmds.AddRange(bgclr);

                    string Text = opt.Text;
                    if (i == Selection) Text = ANSI.Invert(Text);
                    Text = ANSI.Format_Command(Text, cmds.ToArray());

                    switch (Direction)
                    {
                        case EListDirection.Vertical:
                            Text = string.Concat('[', i, ']', ' ', Text, "\r\n");
                            break;
                        case EListDirection.Horizontal:
                            Text = string.Concat(' ', Text, ' ');
                            break;
                    }

                    StaticLines[i + 1]?.Set(Text);
                }
            }
        }


        protected override void Handle_Input_Key(ConsoleKeyInfo key, KeyState state)
        {
            if (Disposed == 1)
                return;

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    Selection -= 1;
                    break;
                case ConsoleKey.DownArrow:
                    Selection += 1;
                    break;

                case ConsoleKey.LeftArrow:
                    Selection -= 1;
                    break;
                case ConsoleKey.RightArrow:
                    Selection += 1;
                    break;

                case ConsoleKey.Home:
                    Selection = 0;
                    break;
                case ConsoleKey.End:
                    Selection = Options.Length - 1; ;
                    break;
                default:
                    Console.Beep();
                    break;
            }
        }

        #region Awaitable Implementation
        public static async Task<int> Prompt(string Prompt_Message, IEnumerable<ListItem> Options, EListDirection? Direction = null, int DefaultSelection = 0)
        {
            using (var p = new Widget_Prompt_OptionList(Prompt_Message, Options, Direction, DefaultSelection))
            {
                return await p.ConfigureAwait(true);
            }
        }

        public static async Task<int> Prompt(string Prompt_Message, IEnumerable<string> Options, EListDirection? Direction = null, int DefaultSelection = 0)
        {
            using (var p = new Widget_Prompt_OptionList(Prompt_Message, Options.Select(x => new ListItem() { Text = x, ForegroundColor = null, BackgroundColor = null }), Direction, DefaultSelection))
            {
                return await p.ConfigureAwait(true);
            }
        }
        #endregion
    }
}
