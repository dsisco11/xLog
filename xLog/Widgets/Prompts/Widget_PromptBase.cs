using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xLog.Widgets;
using xLog.VirtualTerminal;

namespace xLog.Widgets.Prompts
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
        /// Determines whether or not the users input will be shown in the console
        /// </summary>
        protected bool bUserInputVisible = true;
        /// <summary>
        /// The prompt message
        /// </summary>
        protected string Message = string.Empty;
        /// <summary>
        /// If true then characters in the user input string will be replaced with the masking character
        /// </summary>
        protected bool MaskUserInput = false;
        /// <summary>
        /// Console cursor position
        /// </summary>
        protected int CursorPos = 0;
        /// <summary>
        /// The captured <see cref="ConsoleInput"/> context
        /// </summary>
        protected Guid InputContext = Guid.Empty;

        PromptInputValidatorDelegate InputValidator = null;
        PromptResultValidatorDelegate ResultValidator = null;

        Task<Ty> promptTask = null;
        CancellationTokenSource taskCancel;

        protected StringBuilder UserInput => Buffer;
        protected ManualResetEventSlim UserInputSignal;
        #endregion

        #region Constructors
        protected Widget_PromptBase(string Prompt_Message, string Initial_Value = null, PromptInputValidatorDelegate input_validator = null, PromptResultValidatorDelegate result_validator = null, bool Conceal_Input = false) : base(ConsoleWidgetType.Input)
        {
            Set_Message(Prompt_Message);
            Set_Input(Initial_Value ?? string.Empty);
            MaskUserInput = Conceal_Input;

            InputValidator = input_validator;
            ResultValidator = result_validator;
            UserInputSignal = new ManualResetEventSlim();

            /*taskCancel = new CancellationTokenSource();
            promptTask = Task.Run(Run_Prompt_Async, taskCancel.Token);*/
        }

        public override void Dispose()
        {
            End();

            if (!ReferenceEquals(null, promptTask))
            {
                promptTask.Dispose();
                promptTask = null;
            }

            if (!ReferenceEquals(null, taskCancel))
            {
                taskCancel.Dispose();
                taskCancel = null;
            }

            base.Dispose();
        }
        #endregion

        #region Prompt Loop

        public Ty Run_Prompt()
        {
            if (!ReferenceEquals(null, taskCancel) && taskCancel.IsCancellationRequested)
            {
                taskCancel.Dispose();
                taskCancel = null;
            }

            if (ReferenceEquals(null, taskCancel))
            {
                taskCancel = new CancellationTokenSource();
            }

            return Await_User_Input();
        }

        private async Task<Ty> Run_Prompt_Async()
        {
            return await Task.Run(Await_User_Input);
        }


        private Ty Await_User_Input()
        {
            string userInput = string.Empty;
            try
            {
                InputContext = ConsoleInput.Capture(Pre_Handle_Input_Key);

                while (true)
                {
                    List<string> opts = Get_Valid_Options()?.ToList();
                    if (!ReferenceEquals(opts, null) && opts.Count > 0)
                    {
                        string strOpts = string.Join(", ", opts);
                        xLogEngine.Console(string.Concat(ANSI.WhiteBright("Options: "), ANSI.White(strOpts)));
                        Update();
                    }

                    try
                    {
                        UserInputSignal.Wait(taskCancel.Token);
                    }
                    catch (OperationCanceledException)
                    {/* Ignore */
                        Set_Input(string.Empty);
                        return default;
                    }

                    if (taskCancel.IsCancellationRequested)
                        return default;

                    userInput = UserInput.ToString();
                    if (Validate_Result(userInput))
                    {
                        break;
                    }
                    else
                    {
                        xLogEngine.Console(ANSI.RedBright($"Invalid Response: \"{userInput}\""));
                        Set_Input(string.Empty);
                    }
                }
            }
            finally
            {
                ConsoleInput.Release(InputContext);
            }


            if (bUserInputVisible) xLogEngine.Interface(string.Empty, string.Concat(Message, ANSI.White(userInput)));
            return Translate_UserInput(userInput);
        }

        public void Cancel()
        {
            taskCancel.Cancel();
        }
        #endregion

        #region Prompt Task

        async Task<Ty> Start()
        {
            if (Disposed == 1)
            {
                return default;
            }

            if (!ReferenceEquals(null, promptTask) ^ !ReferenceEquals(null, taskCancel))
            {/* Needs to be stopped */
                End();
            }

            if (ReferenceEquals(null, promptTask) && ReferenceEquals(null, taskCancel))
            {/* Both null, safe to initialize */
                taskCancel = new CancellationTokenSource();
                promptTask = Task.Run(Run_Prompt_Async, taskCancel.Token);
            }

            Ty result = await promptTask.ConfigureAwait(false);
            // Print the users input value
            if (bUserInputVisible) xLogEngine.Interface(null, string.Concat(Message, result));
            return result;
        }

        void End()
        {
            if (!ReferenceEquals(null, promptTask))
            {
                taskCancel?.Cancel();

                promptTask.Wait();
                promptTask.Dispose();
                promptTask = null;
            }

            if (!ReferenceEquals(null, taskCancel))
            {
                taskCancel.Dispose();
                taskCancel = null;
            }
        }
        #endregion

        #region Task Implementation
        public Ty Result => promptTask.Result;
        public Task<Ty> ToTask() => promptTask;
        public TaskAwaiter<Ty> GetAwaiter() => promptTask.GetAwaiter();
        public ConfiguredTaskAwaitable<Ty> ConfigureAwait(bool continueOnCapturedContext) => promptTask.ConfigureAwait(continueOnCapturedContext);

        public void Wait() => promptTask.Wait();
        #endregion

        #region Input Options
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

        #region Setters
        /// <summary>
        /// Sets the prompts message text
        /// </summary>
        /// <param name="msg"></param>
        protected void Set_Message(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
            {
                Message = string.Empty;
                return;
            }

            // Remove any periods from the end and any whitespace around the message
            msg = ANSI.WhiteBright(msg.TrimEnd(new char[] { ' ', '.' }));

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
        protected void Set_Input(string Input)
        {
            if (Disposed == 1)
                return;

            UserInput.Clear();
            UserInput.Append(Input);
            Set_Cursor(int.MaxValue);
            Update();
        }

        protected virtual void Set_Cursor(int index)
        {
            if (Disposed == 1)
                return;

            index = Math.Max(0, Math.Min(UserInput.Length, index));
            if (CursorPos != index)
            {
                CursorPos = index;
                Line.Set_Cursor_Pos(Message.Length + CursorPos);
            }
        }
        #endregion

        #region User Input
        /// <summary>
        /// Translates a string of user input into the preferred type for the prompt object
        /// </summary>
        /// <param name="UserInput">String of input entered by the user</param>
        protected abstract Ty Translate_UserInput(string UserInput);


        /// <summary>
        /// This is a sort of base input handler that implements functionality which applies to ALL possible prompt controls
        /// </summary>
        protected virtual void Pre_Handle_Input_Key(ConsoleKeyInfo key, KeyState state)
        {
            if (Disposed == 1)
                return;

            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    {/* Abort the prompt */
                        taskCancel.Cancel(throwOnFirstException: false);
                    }
                    break;
                case ConsoleKey.Enter:
                    {/* Signal that user input is complete */
                        UserInputSignal.Set();
                    }
                    break;
                default:
                    {/* Pass the event on to the normal event */
                        Handle_Input_Key(key, state);
                    }
                    break;
            }
        }

        protected virtual void Handle_Input_Key(ConsoleKeyInfo key, KeyState state)
        {
            if (Disposed == 1)
                return;

            lock (UserInput)
            {
                switch (key.Key)
                {
                    case ConsoleKey.Delete:
                        {
                            if (CursorPos < UserInput.Length)
                            {
                                UserInput.Remove(CursorPos, 1);
                            }
                        }
                        break;
                    case ConsoleKey.Backspace:
                        {
                            if (CursorPos > 0)
                            {
                                Set_Cursor(CursorPos - 1);
                                UserInput.Remove(CursorPos, 1);
                            }
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        {
                            Set_Cursor(CursorPos - 1);
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        {
                            Set_Cursor(CursorPos + 1);
                        }
                        break;
                    case ConsoleKey.Home:
                        {
                            Set_Cursor(0);
                        }
                        break;
                    case ConsoleKey.End:
                        {
                            Set_Cursor(int.MaxValue);
                        }
                        break;
                    default:
                        {
                            string Post = string.Concat(UserInput.ToString(), key.KeyChar);
                            if (!Validate_Input(UserInput.ToString(), Post, key))
                            {
                                return;
                            }

                            if (key.KeyChar != '\u0000' && !char.IsControl(key.KeyChar))
                            {
                                UserInput.Insert(CursorPos++, key.KeyChar);// Add users input to the input buffer
                            }
                        }
                        break;
                }
            }

            Update();
        }
        #endregion

        protected virtual void Update()
        {
            if (Disposed == 1)
                return;

            lock (StaticLines)
            {
                // Update input
                string displayedInput = MaskUserInput ? new string('*', UserInput.Length) : UserInput.ToString();// Apply input masking if needed
                StaticLines[0].Set(string.Concat(Message, displayedInput));// Update the userinput display line
            }
        }


    }
}