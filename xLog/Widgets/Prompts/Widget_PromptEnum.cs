using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using xLog.Widgets;

namespace xLog.Widgets.Prompts
{
    public class Widget_PromptEnum<T> : Widget_PromptBase<T> where T : struct, IConvertible
    {
        #region Properties
        IEnumerable<T> Exclude = null;
        List<string> ExcludeNames = null;
        #endregion

        public Widget_PromptEnum(string Prompt_Message, T Initial_Value, IEnumerable<T> Excluded_Values = null) : base(Prompt_Message, Enum.GetName(typeof(T), Initial_Value))
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            Exclude = Excluded_Values;

            ExcludeNames = new List<string>();
            foreach (var o in Exclude) ExcludeNames.Add(Enum.GetName(typeof(T), o));
        }

        protected override IEnumerable<string> Get_Valid_Options()
        {
            var res = Enum.GetNames(typeof(T)).ToList();
            res.RemoveAll((str) => ExcludeNames.Contains(str));
            return res;
        }

        protected override bool Validate_Result(string userInput)
        {
            bool valid = Get_Valid_Options().ToList().Contains(userInput, StringComparer.CurrentCultureIgnoreCase);
            if (valid && !ReferenceEquals(Exclude, null) && Exclude.Count() > 0)
            {
                if (Exclude.Contains((T)Enum.Parse(typeof(T), userInput)))
                {
                    return false;
                }
            }

            return valid;
        }

        protected override T Translate_UserInput(string userInput)
        {
            object choice = null;
            if (Get_Valid_Options().Contains(userInput, StringComparer.CurrentCultureIgnoreCase))
            {
                try
                {
                    choice = Enum.Parse(typeof(T), userInput, true);
                }
                catch (ArgumentException)
                {
                    Log.Error($"Unable to find \"{userInput}\" value in enum, but it's in the Names list!");
                }
            }

            if (choice == null)
            {
                throw new ArgumentException($"Unable to translate input(\"{userInput}\") to enum value");
            }

            return (T)choice;
        }


        public static async Task<T> Prompt(string Prompt_Message, T Initial_Value, IEnumerable<T> Excluded_Values = null)
        {
            using (var p = new Widget_PromptEnum<T>(Prompt_Message, Initial_Value, Excluded_Values))
            {
                return await p.ConfigureAwait(false);
            }
        }
    }
}