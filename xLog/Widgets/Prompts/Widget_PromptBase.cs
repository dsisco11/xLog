using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using xLog.Widgets;

namespace xLog.Widgets
{
    /// <summary>
    /// Delegate describing functions which can validate user input for the logging systems prompt functions.
    /// </summary>
    /// <param name="PreInput">The complete user input string before the current key</param>
    /// <param name="Input">The complete user input string</param>
    /// <param name="Key">The key which was just pressed</param>
    /// <returns>Accept Input. if <c>False</c>, the input key will be disregarded.</returns>
    public delegate bool PromptInputValidatorDelegate(string PreInput, string Input, ConsoleKeyInfo Key);

    /// <summary>
    /// Function that validates a prompt result.
    /// </summary>
    /// <param name="UserInput">The complete user input string</param>
    /// <returns>Input Valid. If <c>False</c>, prompt will be repeated.</returns>
    public delegate bool PromptResultValidatorDelegate(string UserInput);

    public abstract class Widget_PromptBase<Ty> : ConsoleWidget
    {

        #region Properties
        /// <summary>
        /// The prompt message
        /// </summary>
        protected string Message = string.Empty;
        /// <summary>
        /// If true then characters in the user input string will be replaced with the masking character
        /// </summary>
        bool ConcealInput = false;
        PromptInputValidatorDelegate InputValidator = null;
        PromptResultValidatorDelegate ResultValidator = null;
        /// <summary>
        /// Console cursor position
        /// </summary>
        int CursorPos = 0;

        Task<Ty> myTask = null;
        CancellationTokenSource taskCancel;

        #endregion

        #region Constructors
        protected Widget_PromptBase(string Prompt_Message, string Initial_Value = null, PromptInputValidatorDelegate input_validator = null, PromptResultValidatorDelegate result_validator = null, bool Conceal_Input = false) : base(ConsoleWidgetType.Input)
        {
            Set_Message(Prompt_Message);
            Set_Input(Initial_Value ?? string.Empty);
            ConcealInput = Conceal_Input;

            InputValidator = input_validator;
            ResultValidator = result_validator;

            taskCancel = new CancellationTokenSource();
            myTask = Task.Run(Run_Prompt_And_Translate, taskCancel.Token);
        }

        public override void Dispose()
        {
            if (myTask != null)
            {
                taskCancel.Cancel();
                myTask.Wait();

                myTask.Dispose();
                myTask = null;
            }

            if (taskCancel != null)
            {
                taskCancel.Dispose();
                taskCancel = null;
            }

            base.Dispose();
        }
        #endregion

        #region Prompt Loop
        private async Task<Ty> Run_Prompt_And_Translate()
        {
            List<string> opts = Get_Valid_Options()?.ToList();
            if (!ReferenceEquals(opts, null) && opts.Count > 0)
            {
                string strOpts = string.Join(", ", opts);
                xLogEngine.Console(string.Concat(ANSIColor.whiteBright("Options: "), ANSIColor.white(strOpts)));
                Update();
            }

            string userInput = Run_Prompt();
            if (taskCancel.IsCancellationRequested)
                return default;

            if (!Validate_Result(userInput))
            {
                xLogEngine.Console(ANSIColor.redBright($"Invalid Response: \"{userInput}\""));
                Set_Input(string.Empty);
                return await Run_Prompt_And_Translate().ConfigureAwait(false);
            }

            xLogEngine.Interface(string.Empty, string.Concat(Message, ANSIColor.white(userInput)));
            return Translate_Prompt_Result(userInput);
        }

        private string Run_Prompt()
        {
            do
            {
                var key = Console.ReadKey(true);

                if (key == null) throw new ArgumentNullException("Console ReadKey returned null");
                if (key.KeyChar == '\n' || key.KeyChar == '\r' || key.Key == ConsoleKey.Enter)
                    break;

                Handle_Input_Key(key);
                Update();
            }
            while (!taskCancel.IsCancellationRequested);

            return Buffer.ToString();
        }

        async Task<Ty> Start()
        {
            if (Equals(Disposed, 1))
                return default;

            Ty result = await myTask.ConfigureAwait(false);
            // Print the users input value
            xLogEngine.Interface(null, string.Concat(Message, result));
            return result;
        }

        void End()
        {
            taskCancel.Cancel();
            myTask.Wait();
        }
        #endregion

        #region Task Implementation
        public Ty Result => myTask.Result;
        public Task<Ty> ToTask() => myTask;
        public TaskAwaiter<Ty> GetAwaiter() => myTask.GetAwaiter();
        public ConfiguredTaskAwaitable<Ty> ConfigureAwait(bool continueOnCapturedContext) => myTask.ConfigureAwait(continueOnCapturedContext);

        public void Wait() => myTask.Wait();
        #endregion

        #region Valid Options
        protected virtual IEnumerable<string> Get_Valid_Options()
        {
            return null;
        }
        #endregion

        #region Validators
        protected virtual bool Validate_Result(string Result)
        {
            if (ResultValidator != null)
            {
                return ResultValidator.Invoke(Result);
            }
            else if (Get_Valid_Options() != null)
            {
                return Get_Valid_Options().ToList().Contains(Result, StringComparer.CurrentCultureIgnoreCase);
            }

            return true;// if we dont have validation logic defined then by default ALL input is valid!
        }

        protected virtual bool Validate_Input(string PreInput, string userInput, ConsoleKeyInfo key)
        {
            if (InputValidator != null)
            {
                return InputValidator.Invoke(PreInput, userInput, key);
            }
            return true;
        }
        #endregion

        protected abstract Ty Translate_Prompt_Result(string Result);


        void Set_Message(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
            {
                Message = string.Empty;
                return;
            }

            // Remove any periods from the end and any whitespace around the message
            msg = ANSIColor.whiteBright(msg.TrimEnd(new char[] { ' ', '.' }));

            // If the message ends with punctuation then we don't append ": "
            if (!char.IsPunctuation(msg.Last()))
            {
                msg = string.Concat(msg, ": ");
            }

            Message = msg;
        }

        /// <summary>
        /// Sets the complete user input string value
        /// </summary>
        /// <param name="Input"></param>
        void Set_Input(string Input)
        {
            if (Equals(Disposed, 1))
                return;

            Buffer.Clear();
            Buffer.Append(Input);
            Set_Cursor(int.MaxValue);
            Update();
        }

        void Set_Cursor(int pos)
        {
            if (Equals(Disposed, 1))
                return;

            pos = Math.Max(0, Math.Min(Buffer.Length, pos));
            if (CursorPos != pos)
            {
                CursorPos = pos;
                Line.Set_Cursor_Pos(Message.Length + CursorPos);
            }
        }

        void Handle_Input_Key(ConsoleKeyInfo key)
        {
            if (Equals(Disposed, 1))
                return;

            switch (key.Key)
            {
                case ConsoleKey.Delete:
                    if (CursorPos == Buffer.Length)
                        return;
                    Buffer.Remove(CursorPos, 1);
                    break;
                case ConsoleKey.Backspace:
                    if (CursorPos == 0) return;
                    Set_Cursor(CursorPos - 1);
                    Buffer.Remove(CursorPos, 1);
                    break;
                case ConsoleKey.LeftArrow:
                    Set_Cursor(CursorPos - 1);
                    break;
                case ConsoleKey.RightArrow:
                    Set_Cursor(CursorPos + 1);
                    break;
                case ConsoleKey.Home:
                    Set_Cursor(0);
                    break;
                case ConsoleKey.End:
                    Set_Cursor(int.MaxValue);
                    break;
                default:
                    string Post = string.Concat(Buffer.ToString(), key.KeyChar);
                    if (!Validate_Input(Buffer.ToString(), Post, key))
                    {
                        return;
                    }

                    if (key.KeyChar != '\u0000' && !char.IsControl(key.KeyChar))
                    {
                        Buffer.Insert(CursorPos++, key.KeyChar);// Add users input to the input buffer
                    }
                    break;
            }
        }

        void Update()
        {
            if (Equals(Disposed, 1))
                return;

            lock (Line)
            {
                // Update input
                string displayedInput = ConcealInput ? new string('*', Buffer.Length) : Buffer.ToString();// Apply input masking if needed
                Line.Set(string.Concat(Message, displayedInput));// Update the userinput display line
            }
        }


    }
}