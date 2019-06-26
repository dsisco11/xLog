
using System.Threading.Tasks;

public class ConsolePromptInteger : ConsolePromptBase<int>
{
    public ConsolePromptInteger(string Prompt_Message, int? Initial_Value = null) : base(Prompt_Message, Initial_Value.HasValue ? Initial_Value.Value.ToString() : null)
    {
    }

    protected override bool Validate_Result(string Result) => int.TryParse(Result, out var _);

    protected override int Translate_Prompt_Result(string Result) => int.Parse(Result);


    public static async Task<int> Prompt(string Prompt_Message, int? Initial_Value = null)
    {
        using (var p = new ConsolePromptInteger(Prompt_Message, Initial_Value))
        {
            return await p.ConfigureAwait(false);
        }
    }
}
