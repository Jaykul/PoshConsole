using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using PoshCode.Controls;
using PoshCode.Controls.Utility;
using PoshCode.PowerShell;

namespace PoshCode
{
    public class PoshConsole : ConsoleControl, IRichConsole
    {

        public static PoshConsole CurrentConsole
        {
            get;
            private set;
        }

        internal RunspaceProxy Runner { get; set; }
        private Host _host;

        protected override void Dispose(bool disposing)
        {
            _host?.Dispose();
            _host = null;

            Runner?.Dispose();

            base.Dispose(disposing);
        }

        public PoshConsole()
        {
            // Initialize the document.
            // If they set it themselves, that's fine, but we have to make sure there is one!
            var document = new FlowDocument();

            document.Blocks.Add(new Paragraph { ClearFloaters = WrapDirection.Both });
            var consoleFont = new Style();
            consoleFont.Setters.Add(new Setter(TextElement.FontSizeProperty, 12));
            consoleFont.Setters.Add(new Setter(TextElement.FontFamilyProperty, "Consolas"));
            document.Resources.Add("ConsoleFont", consoleFont);
            Document = document;

            Runner = new RunspaceProxy(_host);
            Runner.RunspaceReady += (source, args) => Dispatcher.BeginInvoke((Action)(() =>
            {
                CommandBox.IsEnabled = true;
                OnCommandFinished(null, PipelineState.Completed);
            }));

            // TODO: Improve this interface
            Expander.TabComplete = Runner.CompleteInput;
            CurrentConsole = this;
        }

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            "Progress", typeof(string), typeof(PoshConsole), new PropertyMetadata(null));

        public string Progress
        {
            get { return (string)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }


        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content", typeof(string), typeof(PoshConsole), new PropertyMetadata(null));

        public string Content
        {
            get { return (string)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        private Panel _progressPanel;
        public Panel ProgressPanel
        {
            get
            {
                if (RootWindow == null) return null;
                return _progressPanel ?? (_progressPanel = RootWindow.FindName(Progress) as Panel);
            }
        }

        private Panel _contentControl;
        public Panel ContentPanel
        {
            get
            {
                if (RootWindow == null) return null;
                return _contentControl ?? (_contentControl = RootWindow.FindName(Content) as Panel);
            }
        }

        public IntPtr WindowHandle => this.GetHandle();

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            CommandBox.IsEnabled = false;
            _host = new Host(this, ProgressPanel, new Options(this));

            Loaded += (sender, ignored) =>
            {
                if (!Runner.IsInitialized)
                {
                    Runner.Initialize();
                }

                Document.PagePadding = Padding;
            };
        }

        public void WriteErrorRecord(ErrorRecord errorRecord)
        {
            // NOTE: Write is Dispatcher checked
            if (errorRecord.InvocationInfo != null)
            {
                Write(ConsoleBrushes.ErrorForeground, ConsoleBrushes.ErrorBackground, errorRecord.InvocationInfo.MyCommand != null
                                                                            ? $"{errorRecord.InvocationInfo.MyCommand} : {errorRecord}\n"
                                                                            : $"{errorRecord}\n");
                Write(ConsoleBrushes.ErrorForeground, ConsoleBrushes.ErrorBackground, errorRecord.InvocationInfo.PositionMessage + "\n");
            }
            else
            {
                Write(ConsoleBrushes.ErrorForeground, ConsoleBrushes.ErrorBackground, $"{errorRecord}\n");
            }

            // TODO: support error formatting preference:
            Write(ConsoleBrushes.ErrorForeground, ConsoleBrushes.ErrorBackground, $"   + CategoryInfo            : {errorRecord.CategoryInfo}\n");
            Write(ConsoleBrushes.ErrorForeground, ConsoleBrushes.ErrorBackground, $"   + FullyQualifiedErrorId   : {errorRecord.FullyQualifiedErrorId}\n");
        }


        public void WriteErrorRecords(Collection<object> errorRecords)
        {
            foreach (var err in errorRecords)
            {
                var pso = (err as PSObject)?.BaseObject ?? err;
                var error = pso as ErrorRecord;
                if (error == null)
                {
                    var exception = pso as Exception;
                    if (exception == null)
                    {
                        WriteErrorRecord(new ErrorRecord( new Exception($"{pso}"), $"{pso}", ErrorCategory.NotSpecified, pso));
                        continue;
                    }
                    WriteErrorRecord(new ErrorRecord(exception, "Unspecified", ErrorCategory.NotSpecified, pso));
                    continue;
                }
                WriteErrorRecord(error);
            }
        }

        protected override void OnCommand(CommandEventArgs command)
        {
            InvokeAsync(command.Command);
            base.OnCommand(command);
        }

        /// <summary>
        /// Invoke the specified commands in a pipeline asynchronously, optionally providing input to the first command, and with the specified output handling.
        /// </summary>
        /// <param name="commands">Commands from which the pipeline will be constructed</param>
        /// <param name="input">Optional Pipeline Input. If this is specified, your first command must accept pipeline input</param>
        /// <param name="output">Optionally: what to print to the console (prints everything, by default, as though the user had typed the commands in the console)</param>
        /// <returns>A Task which returns the <see cref="PoshConsolePipelineResults"/> results, including the pipeline output</returns>
        public Task<PoshConsolePipelineResults> InvokeAsync(Command[] commands, IEnumerable input = null, ConsoleOutput output = ConsoleOutput.Default)
        {
            return Runner.Invoke(commands, input, output);
        }

        /// <summary>
        /// Invoke the specified command in a pipeline asynchronously, optionally providing input to the first command, and with the specified output handling.
        /// </summary>
        /// <param name="command">The Command from which the pipeline will be constructed.</param>
        /// <param name="input">Optional Pipeline Input. If this is specified, your first command must accept pipeline input</param>
        /// <param name="output">Optionally: what to print to the console (prints everything, by default, as though the user had typed the commands in the console)</param>
        /// <returns>A Task which returns the <see cref="PoshConsolePipelineResults"/> results, including the pipeline output</returns>
        public Task<PoshConsolePipelineResults> InvokeAsync(Command command, IEnumerable input = null, ConsoleOutput output = ConsoleOutput.Default)
        {
            return Runner.Invoke(new []{command}, input, output);
        }

        /// <summary>
        /// Invoke the specified command in a pipeline asynchronously, optionally providing input to the first command, and with the specified output handling.
        /// </summary>
        /// <param name="command">The Command from which the pipeline will be constructed.</param>
        /// <param name="isScript">Whether the command is a script (defaults to true). You should set this to false if you are just naming a command.</param>
        /// <param name="useLocalScope">Whether the command should use it's own local scope -- only valid for scripts (defaults to false)</param>
        /// <param name="input">Optional Pipeline Input. If this is specified, your first command must accept pipeline input</param>
        /// <param name="output">Optionally: what to print to the console (prints everything, by default, as though the user had typed the commands in the console)</param>
        /// <returns>A Task which returns the <see cref="PoshConsolePipelineResults"/> results, including the pipeline output</returns>
        public Task<PoshConsolePipelineResults> InvokeAsync(string command, bool isScript = true, bool useLocalScope = false, IEnumerable input = null, ConsoleOutput output = ConsoleOutput.Default)
        {
            return Runner.Invoke(new[] { new Command(command, isScript, useLocalScope) }, input, output);
        }


        /// <summary>
        /// Invoke the specified script, synchronously, and return the pipeline output.
        /// </summary>
        /// <param name="script">The script from which the pipeline will be constructed. Can be just a command name, but is executed as though typed on the console.</param>
        /// <param name="output">Optionally: what to print to the console (prints everything, by default, as though the user had typed the commands in the console)</param>
        /// <returns>The pipeline output</returns>
        public Collection<PSObject> Invoke(string script, ConsoleOutput output = ConsoleOutput.Default)
        {
            return Runner.Invoke(new[] { new Command(script, true) }, null, output).GetAwaiter().GetResult().Output;
        }


        #region PromptForUserInput (PowerShell-specific console-based user interface)
        public event EventHandler<PromptForObjectEventArgs> PromptForObject;
        public Dictionary<string, PSObject> OnPromptForObject(PromptForObjectEventArgs e)
        {

            EventHandler<PromptForObjectEventArgs> handler = PromptForObject;
            if (handler != null)
            {
                handler(this, e);
                return e.Results;
            }

            if (!string.IsNullOrEmpty(e.Caption))
                Write(e.Caption + "\n");
            if (!string.IsNullOrEmpty(e.Message))
                Write(e.Message + "\n");

            var results = new Dictionary<string, PSObject>();
            foreach (var fd in e.Descriptions)
            {
                Type type = Type.GetType(fd.ParameterAssemblyFullName);

                string prompt = string.IsNullOrEmpty(fd.Label) ? fd.Name : fd.Label;

                if (type != null && type.IsArray)
                {
                    type = type.GetElementType();
                    var output = new List<PSObject>();
                    int count = 0;
                    do
                    {
                        PSObject single = GetSingle(e.Caption, e.Message, $"{prompt}[{count++}]", fd, type);
                        if (single == null) break;

                        if (!(single.BaseObject is string) || ((string)single.BaseObject).Length > 0)
                        {
                            output.Add(single);
                        }
                        else break;
                    } while (true);

                    results[fd.Name] = PSObject.AsPSObject(output.ToArray());
                }
                else
                {
                    var value = GetSingle(e.Caption, e.Message, prompt, fd, type);

                    if (value != null) results[fd.Name] = value;
                }

            }
            return results;
        }
        private PSObject GetSingle(string caption, string message, string prompt, FieldDescription field, Type type)
        {
            string help = field.HelpMessage;
            PSObject psDefault = field.DefaultValue;

            if (null != type && type == typeof(PSCredential))
            {
                var credential = _host.UI.PromptForCredential(caption, message, psDefault?.ToString(), string.Empty, PSCredentialTypes.Default, PSCredentialUIOptions.Default);
                return credential != null ? PSObject.AsPSObject(credential) : null;
            }

            while (true)
            {
                // TODO: Only show the help message if they type '?' as their entry something, in which case show help and re-prompt.
                if (!String.IsNullOrEmpty(help))
                    Write(ConsoleBrushes.ConsoleColorFromBrush(ConsoleBrushes.VerboseForeground), ConsoleBrushes.ConsoleColorFromBrush(ConsoleBrushes.VerboseBackground), help + "\n");

                Write($"{prompt}: ");

                if (null != type && typeof(SecureString) == type)
                {
                    var userData = ReadLineAsSecureString() ?? new SecureString();
                    return PSObject.AsPSObject(userData);
                } // Note: This doesn't look the way it does in PowerShell, but it should work :)
                else
                {
                    if (psDefault != null && psDefault.ToString().Length > 0)
                    {
                        if (Dispatcher.CheckAccess())
                        {
                            CurrentCommand = psDefault.ToString();
                            CommandBox.SelectAll();
                        }
                        else
                        {
                            Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action<string>)(def =>
                            {
                                CurrentCommand = def;
                                CommandBox.SelectAll();
                            }), psDefault.ToString());
                        }
                    }

                    var userData = ReadLine();

                    if (type != null && userData.Length > 0)
                    {
                        object output;
                        var ice = TryConvertTo(type, userData, out output);
                        // Special exceptions that happen when casting to numbers and such ...
                        if (ice == null)
                        {
                            return PSObject.AsPSObject(output);
                        }
                        if ((ice.InnerException is FormatException) || (ice.InnerException is OverflowException))
                        {
                            Write(ConsoleBrushes.ErrorForeground, ConsoleBrushes.ErrorBackground,
                                $@"Cannot recognize ""{userData}"" as a {type.FullName} due to a format error.\n");
                        }
                        else
                        {
                            return PSObject.AsPSObject(String.Empty);
                        }
                    }
                    else if (userData.Length == 0)
                    {
                        return PSObject.AsPSObject(String.Empty);
                    }
                    else return PSObject.AsPSObject(userData);
                }
            }
        }


        // public delegate void PromptForChoiceHandler(object sender, PromptForChoiceEventArgs e);
        public event EventHandler<PromptForChoiceEventArgs> PromptForChoice;
        public int OnPromptForChoice(PromptForChoiceEventArgs e)
        {
            EventHandler<PromptForChoiceEventArgs> handler = PromptForChoice;
            if (handler != null)
            {
                handler(this, e);
                return e.SelectedIndex;
            }

            // Write the caption and message strings in Blue.
            Write(ConsoleColor.Blue, ConsoleColor.Black, e.Caption + "\n" + e.Message + "\n");

            // Convert the choice collection into something that's a little easier to work with
            // See the BuildHotkeysAndPlainLabels method for details.
            var promptData = BuildHotkeysAndPlainLabels(e.Choices, true);


            // Loop reading prompts until a match is made, the default is
            // chosen or the loop is interrupted with ctrl-C.
            while (true)
            {

                // Format the overall choice prompt string to display...
                for (var element = 0; element < promptData.GetLength(1); element++)
                {
                    if (element == e.SelectedIndex)
                    {
                        Write(ConsoleBrushes.VerboseForeground, ConsoleBrushes.VerboseBackground,
                            $"[{promptData[0, element]}] {promptData[1, element]}  ");
                    }
                    else
                    {
                        Write(null, null, $"[{promptData[0, element]}] {promptData[1, element]}  ");
                    }
                }
                Write(null, null, $"(default is \"{promptData[0, e.SelectedIndex]}\"):");

                string data = ReadLine().Trim().ToUpper();

                // If the choice string was empty, use the default selection.
                if (data.Length == 0)
                    return e.SelectedIndex;

                // See if the selection matched and return the
                // corresponding index if it did...
                for (var i = 0; i < e.Choices.Count; i++)
                {
                    if (promptData[0, i][0] == data[0])
                        return i;
                }

                // If they picked the very last thing in the list, they want help
                if (promptData.GetLength(1) > e.Choices.Count && promptData[0, e.Choices.Count] == data)
                {
                    // Show help
                    foreach (var choice in e.Choices)
                    {
                        Write($"{choice.Label.Replace("&", "")} - {choice.HelpMessage}\n");
                    }
                }
                else
                {
                    Write(ConsoleBrushes.ErrorForeground, ConsoleBrushes.ErrorBackground, "Invalid choice: " + data + "\n");
                }
            }

        }

        public PSCredential PromptForCredentialInline(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes = PSCredentialTypes.Generic, PSCredentialUIOptions options = PSCredentialUIOptions.None)
        {

            Collection<FieldDescription> fields;

            // NOTE: I'm not sure this is the right action for the PromptForCredential targetName
            if (!String.IsNullOrEmpty(targetName))
            {
                caption = $"Credential for {targetName}\n\n{caption}";
            }

            if ((options & PSCredentialUIOptions.ReadOnlyUserName) == PSCredentialUIOptions.Default)
            {
                var user = new FieldDescription("User");
                user.SetParameterType(typeof(string));
                user.Label = "Username";
                user.DefaultValue = PSObject.AsPSObject(userName);
                user.IsMandatory = true;

                do
                {
                    fields = new Collection<FieldDescription>(new[] { user });
                    var username = new PromptForObjectEventArgs(caption, message, fields);
                    var login = OnPromptForObject(username);
                    userName = login["User"].BaseObject as string;
                } while (userName != null && userName.Length == 0);
            }

            // I think this is all I can do with the allowedCredentialTypes
            // domain required
            if (allowedCredentialTypes > PSCredentialTypes.Generic)
            {
                // and no domain
                if (userName != null && userName.IndexOfAny(new[] { '\\', '@' }) < 0)
                {
                    userName = $"{targetName}\\{userName}";
                }
            }

            var pass = new FieldDescription("Password");
            pass.SetParameterType(typeof(SecureString));
            pass.Label = "Password for " + userName;
            pass.IsMandatory = true;

            fields = new Collection<FieldDescription>(new[] { pass });
            var pwd = new PromptForObjectEventArgs(string.Empty, string.Empty, fields);
            var password = OnPromptForObject(pwd);

            // TODO: I'm not sure what to do with the PSCredentialUIOptions options, because PowerShell.exe ignores them
            return new PSCredential(userName, (SecureString)password["Password"].BaseObject);
        }

        /// <summary>
        /// Parse a string containing a hotkey character.
        /// 
        /// Take a string of the form: 
        /// "Yes to &amp;all"
        /// And return a two-dimensional array split out as
        ///    "A", "Yes to all".
        /// </summary>
        /// <param name="input">The string to process</param>
        /// <returns>
        /// A two dimensional array containing the parsed components.
        /// </returns>
        private static string[] GetHotkeyAndLabel(string input)
        {
            // ReSharper disable SuggestUseVarKeywordEvident
            string[] result = { String.Empty, String.Empty };
            string[] fragments = input.Split('&');
            // ReSharper restore SuggestUseVarKeywordEvident
            if (fragments.Length == 2)
            {
                if (fragments[1].Length > 0)
                    result[0] = fragments[1][0].ToString().ToUpper();
                result[1] = (fragments[0] + fragments[1]).Trim();
            }
            else
            {
                result[1] = input;
            }
            return result;
        }

        /// <summary>
        /// This is a private worker function that splits out the
        /// accelerator keys from the menu and builds a two dimentional 
        /// array with the first access containing the
        /// accelerator and the second containing the label string
        /// with &amp; removed.
        /// </summary>
        /// <param name="choices">The choice collection to process</param>
        /// <param name="addHelp">Add the 'Help' prompt </param>
        /// <returns>
        /// A two dimensional array containing the accelerator characters
        /// and the cleaned-up labels</returns>
        private static string[,] BuildHotkeysAndPlainLabels(Collection<ChoiceDescription> choices, bool addHelp)
        {
            // Allocate the result array
            var count = addHelp ? choices.Count + 1 : choices.Count;
            var hotkeysAndPlainLabels = new string[2, count];

            for (var i = 0; i < choices.Count; ++i)
            {
                string[] hotkeyAndLabel = GetHotkeyAndLabel(choices[i].Label);

                if (addHelp && hotkeyAndLabel[0] == "?")
                {
                    addHelp = false;
                }
                hotkeysAndPlainLabels[0, i] = hotkeyAndLabel[0];
                hotkeysAndPlainLabels[1, i] = hotkeyAndLabel[1];
            }

            if (addHelp)
            {
                hotkeysAndPlainLabels[0, count - 1] = "?";
                hotkeysAndPlainLabels[1, count - 1] = "Help";  // TODO: Internationalization?
            }

            return hotkeysAndPlainLabels;
        }

        private static PSInvalidCastException TryConvertTo(Type type, string input, out object output)
        {
            // default to string, that seems to be what PowerShell does
            output = input;
            try
            {
                output = LanguagePrimitives.ConvertTo(input, type, CultureInfo.InvariantCulture);
                return null;
            }
            catch (PSInvalidCastException ice)
            {
                // Write(_brushes.ErrorForeground, _brushes.ErrorBackground, ice.Message );
                return ice;
            }
        }
        #endregion
    }
}