using System.Collections.Generic;
using System.Threading.Tasks;

namespace xLog.Widgets
{
    public class ConsolePromptOneOf : ConsolePromptBase<string>
    {
        #region Properties
        private IEnumerable<string> Options;
        #endregion

        #region Constructors
        public ConsolePromptOneOf(string Prompt_Message, IEnumerable<string> Options, string Initial_Value = null) : base(Prompt_Message, Initial_Value)
        {
            this.Options = Options;
        }
        #endregion

        protected override string Translate_Prompt_Result(string Result)
        {
            return Result;
        }


        public static async Task<string> Prompt(string Prompt_Message, IEnumerable<string> Options, string Initial_Value = null)
        {
            using (var p = new ConsolePromptOneOf(Prompt_Message, Options, Initial_Value))
            {
                return await p.ConfigureAwait(false);
            }
        }
    }
}