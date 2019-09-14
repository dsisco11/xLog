using System;
using xLog;
using xLog.Widgets.Prompts;
using xLog.VirtualTerminal;
using System.Threading.Tasks;

namespace Test_VirtualTerminal
{
    class Program
    {
        static async Task Main(string[] args)
        {
            xLogEngine.Start();

            TerminalText text = new TerminalText("Lorum Ipsum Lorum Ipsum Lorum Ipsum Lorum Ipsum Lorum Ipsum Lorum Ipsum Lorum Ipsum Lorum Ipsum");
            System.Threading.Thread.SpinWait(1);
            Terminal.ActiveScreen.Add(text);
            text.Set("Smol string");

            await Widget_PressAny.Prompt();
        }
    }
}
