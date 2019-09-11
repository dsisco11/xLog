using System.Threading.Tasks;

namespace xLog.Widgets.Prompts
{
    public class Widget_Prompt : Widget_PromptBase<string>
    {
        #region Constructors
        public Widget_Prompt(string Prompt_Message, string Initial_Value = null, PromptInputValidatorDelegate input_validator = null, PromptResultValidatorDelegate result_validator = null, bool Conceal_Input = false) : base(Prompt_Message, Initial_Value, input_validator, result_validator, Conceal_Input)
        {
        }

        protected override string Translate_UserInput(string userInput)
        {
            return userInput;
        }
        #endregion

        public static async Task<string> Prompt(string Prompt_Message, string Initial_Value = null, bool Conceal_Input = false)
        {
            using (var p = new Widget_Prompt(Prompt_Message, Initial_Value, Conceal_Input: Conceal_Input))
            {
                return await p.ConfigureAwait(false);
            }
        }
    }
}
