using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace xLog.Widgets.Prompts
{
    /// <summary>
    /// Prompts the user for a boolean (yes/no) response
    /// </summary>
    public class Widget_PromptBool : Widget_PromptBase<bool>
    {
        public Widget_PromptBool(string Prompt_Message, string Initial_Value = null) : base(Prompt_Message, Initial_Value)
        {
        }

        static string[] VALID = new string[] { "Y", "N" };
        protected override IEnumerable<string> Get_Valid_Options()
        {
            return VALID;
        }

        protected override bool Translate_UserInput(string userInput)
        {
            // We translate any input where the first character is upper/lowercase 'y' to yes, all others are false.
            return !string.IsNullOrWhiteSpace(userInput) && userInput.ToUpper()[0] == 'Y';
        }

        protected override bool Validate_Result(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                return false;

            string c = new string(userInput.ToLower()[0], 1);
            return Get_Valid_Options().Contains(c.ToUpper());
        }


        public static async Task<bool> Prompt(string Prompt_Message, string Initial_Value = null)
        {
            using (var p = new Widget_PromptBool(Prompt_Message, Initial_Value))
            {
                return await p.ConfigureAwait(false);
            }
        }
    }
}