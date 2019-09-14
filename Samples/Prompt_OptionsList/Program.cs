using System;
using System.Threading.Tasks;
using xLog;
using xLog.Widgets.Prompts;

namespace Prompt_OptionsList
{
    class Program
    {
        static async Task Main(string[] args)
        {
            xLogEngine.Start();
            Log.Info("Hello World!");

            var prompt = new Widget_Prompt_OptionList("Select something:",
                new Widget_Prompt_OptionList.ListItem[3] {
                    new Widget_Prompt_OptionList.ListItem("Item1", null, null),
                    new Widget_Prompt_OptionList.ListItem("Something", ANSI_COLOR.WHITE, ANSI_COLOR.BLUE_BRIGHT),
                    new Widget_Prompt_OptionList.ListItem("Else", ANSI_COLOR.WHITE, ANSI_COLOR.GREEN_BRIGHT),
            });

            var result = prompt.Run_Prompt();
            Log.Info($"You selected: {result}");


            await Widget_PressAny.Prompt();
            Log.Info("Goodbye World!");
        }

    }
}
