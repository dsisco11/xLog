using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        #region Properties
        bool bInitialized = false;
        public readonly EListDirection Direction;
        private readonly ListItem[] Options = null;
        /// <summary>
        /// The currently selection item from the list
        /// </summary>
        public int Selection => Translate_UserInput(UserInput.ToString());
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
            for (int i = 0; i < this.Options.Length; i++)
            {
                StaticLines.Add(new StaticConsoleLine());
            }
            bInitialized = true;
            Update();
        }
        #endregion

        protected override int Translate_UserInput(string Result)
        {
            return Convert.ToInt32(Result);
        }

        protected override void Update()
        {
            if (!bInitialized) return;

            lock (StaticLines)
            {
                StaticLines[0].Set(string.Concat(Message, UserInput.ToString()));// Update the userinput display line
                for (int i = 0; i < Options.Length; i++)
                {
                    var opt = Options[i];

                    var Text = (i == Selection) ? ANSI.Invert(opt.Text) : opt.Text;
                    var cmds = opt.ForegroundColor.Get_ANSI_Code().Union(opt.BackgroundColor.Get_ANSI_Code());
                    Text = string.Concat(ANSI.xCmd(cmds.ToArray()), Text, ANSI.COLOR_RESET);

                    switch (Direction)
                    {
                        case EListDirection.Vertical:
                            Text = string.Concat('[', i, ']', ' ', Text);
                            break;
                        case EListDirection.Horizontal:
                            Text = string.Concat(Text, ' ');
                            break;
                    }

                    StaticLines[i + 1]?.Set(Text);
                }
            }
        }

        #region Prompts
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
