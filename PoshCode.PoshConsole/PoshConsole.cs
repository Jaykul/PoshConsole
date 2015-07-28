using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using PoshCode.Controls;
using PoshCode.PowerShell;
using PoshCode.Utility;

namespace PoshCode
{
    public class PoshConsole : ConsoleControl, IRichConsole
    {
        internal RunspaceProxy Runner { get; set; }
        private Host _host;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_host != null)
                {
                    _host.Dispose();
                    _host = null;
                }
            }
            base.Dispose(disposing);
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


        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            CommandBox.IsEnabled = false;
            _host = new Host(this, ProgressPanel, new Options(this));

            Loaded += (sender, ignored) =>
            {

            Runner = new RunspaceProxy(_host);
            Runner.RunspaceReady += (source, args) => Dispatcher.BeginInvoke((Action)(() =>
            {
                CommandBox.IsEnabled = true;
                ExecutePromptFunction(null, PipelineState.Completed);
            }));

            // TODO: Improve this interface
            Expander.TabComplete = Runner.CompleteInput;
            };
        }

        public void WriteErrorRecord(ErrorRecord errorRecord)
        {
            // NOTE: Write is Dispatcher checked
            if (errorRecord.InvocationInfo != null)
            {
                Write(Brushes.ErrorForeground, Brushes.ErrorBackground, errorRecord.InvocationInfo.MyCommand != null
                                                                            ? $"{errorRecord.InvocationInfo.MyCommand} : {errorRecord}\n"
                                                                            : $"{errorRecord}\n");
                Write(Brushes.ErrorForeground, Brushes.ErrorBackground, errorRecord.InvocationInfo.PositionMessage + "\n");
            }
            else
            {
                Write(Brushes.ErrorForeground, Brushes.ErrorBackground, $"{errorRecord}\n");
            }

            // TODO: support error formatting preference:
            Write(Brushes.ErrorForeground, Brushes.ErrorBackground, $"   + CategoryInfo            : {errorRecord.CategoryInfo}\n");
            Write(Brushes.ErrorForeground, Brushes.ErrorBackground, $"   + FullyQualifiedErrorId   : {errorRecord.FullyQualifiedErrorId}\n");
    }

        protected override void OnCommand(CommandEventArgs command)
        {
            ExecuteCommand(command.Command);
            base.OnCommand(command);
        }

        //        public async Task<PSDataCollection<PSObject>> InvokeCommand(string command)
        public void ExecuteCommand(string command, bool showInGui = false, bool defaultOutput = true, bool secret = false, Action<RuntimeException> onErrorAction = null, Action<Collection<PSObject>> onSuccessAction = null)
        {
            Runner.Enqueue(
                new CallbackCommand(command, PipelineOutputHandler(secret, onErrorAction, onSuccessAction, new List<Command>( new[] { new Command(command, true, true)} ) ) )
                {
                    Secret = secret,
                    DefaultOutput = defaultOutput
                });
        }


        public void ExecuteCommand(Command command, bool showInGui = false, bool defaultOutput = true, bool secret = false, Action<RuntimeException> onErrorAction = null, Action<Collection<PSObject>> onSuccessAction = null )
        {
            defaultOutput &= !secret;

            var commands = new[] {command}.ToList();

            if (showInGui)
                commands.Add(new Command("Out-PoshConsole"));

            Runner.Enqueue(
                new CallbackCommand( commands, PipelineOutputHandler(secret, onErrorAction, onSuccessAction, commands))
                {
                    Secret = secret,
                    DefaultOutput = defaultOutput
                });
        }

        private PipelineOutputHandler PipelineOutputHandler(bool secret, Action<RuntimeException> onErrorAction, Action<Collection<PSObject>> onSuccessAction, List<Command> commands)
        {
            return result =>
            {

                if (result.Failure != null)
                {
                    onErrorAction?.Invoke((RuntimeException) result.Failure);

                    // ToDo: if( result.Failure is IncompleteParseException ) { // trigger multiline entry
                    WriteErrorRecord(((RuntimeException) (result.Failure)).ErrorRecord);
                }
                else
                {
                    onSuccessAction?.Invoke(result.Output);
                }

                foreach (var err in result.Errors)
                {
                    var pso = (err as PSObject)?.BaseObject ?? err;
                    var error = pso as ErrorRecord;
                    if (error == null)
                    {
                        var exception = pso as Exception;
                        if (exception == null)
                        {
                            WriteErrorRecord(new ErrorRecord(null, pso.ToString(), ErrorCategory.NotSpecified, pso));
                            continue;
                        }
                        WriteErrorRecord(new ErrorRecord(exception, "Unspecified", ErrorCategory.NotSpecified, pso));
                        continue;
                    }
                    WriteErrorRecord(error);
                }
                if (!secret || !result.Errors.Any() || !result.Output.Any())
                {   // we don't need the Prompt if there was no output
                    ExecutePromptFunction(commands, result.State);
                }

            };
        }
     
        void ExecutePromptFunction(IEnumerable<Command> command, PipelineState lastState)
        {
            OnCommandFinished(command, lastState);
            Runner.Enqueue(_promptSequence);
        }

        readonly CallbackCommand _promptSequence;

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

            _promptSequence = new CallbackCommand(
            new[]
            {
                new Command("New-Paragraph", true, true),
                new Command("Prompt", false, true)
            }, result =>
            {
                var str = new StringBuilder();

                foreach (PSObject obj in result.Output)
                {
                    str.Append(obj);
                }
                Prompt(str.ToString());
            }) { DefaultOutput = false, Secret = true }; 
        }

        public Command DefaultOutputCommand { get; set; }
        public Command ContentOutputCommand { get; set; }

    }
}