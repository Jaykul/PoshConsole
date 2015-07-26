using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace PoshCode
{
    public class PoshConsole : ConsoleControl, IDisposable, IRichConsole
    {
        internal RunspaceProxy Runner { get; set; }
        private Host _host;

        public new void Dispose()
        {
            if (_host != null)
            {
                _host.Dispose();
                _host = null;
            }
            base.Dispose();
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

            Runner = new RunspaceProxy(_host);
            Runner.RunspaceReady += (source, args) => Dispatcher.BeginInvoke((Action)(() =>
            {
                CommandBox.IsEnabled = true;
                ExecutePromptFunction(null, PipelineState.Completed);
            }));
        }

        public void WriteErrorRecord(ErrorRecord errorRecord)
        {
            // Write is Dispatcher checked
            Write(Brushes.ErrorForeground, Brushes.ErrorBackground, errorRecord + "\n");
            if (errorRecord.InvocationInfo != null)
            {
                // Write is Dispatcher checked
                Write(Brushes.ErrorForeground, Brushes.ErrorBackground, errorRecord.InvocationInfo.PositionMessage + "\n");
            }
        }

        protected override void OnCommand(CommandEventArgs command)
        {
            ExecuteCommand(command.Command);
            base.OnCommand(command);
        }

        //        public async Task<PSDataCollection<PSObject>> InvokeCommand(string command)
        public void ExecuteCommand(string command, bool showInGui = false, bool defaultOutput = true, bool secret = false)
        {
            ExecuteCommand(new Command(command, true, true), showInGui, defaultOutput, secret);
        }


        public void ExecuteCommand(Command command, bool showInGui = false, bool defaultOutput = true, bool secret = false, Action<RuntimeException> onErrorAction = null, Action<Collection<PSObject>> onSuccessAction = null )
        {
            if (secret)
            {
                defaultOutput = false;
            }

            var commands = new[] {command}.ToList();

            if (showInGui)
                commands.Add(new Command("Out-PoshConsole"));

            Runner.Enqueue(
                new CallbackCommand(
                    commands,
                    defaultOutput,
                    result =>
                    {

                        if (result.Failure != null)
                        {
                            if (onErrorAction != null)
                            {
                                onErrorAction.Invoke((RuntimeException) result.Failure);
                            }

                            // ToDo: if( result.Failure is IncompleteParseException ) { // trigger multiline entry
                            WriteErrorRecord(((RuntimeException) (result.Failure)).ErrorRecord);
                        }
                        else
                        {
                            if (onSuccessAction != null)
                            {
                                onSuccessAction.Invoke(result.Output);
                            }
                        }
                        if (!secret)
                        {   // we don't need the Prompt if there was no output
                            ExecutePromptFunction(commands, result.State);
                        }
                    }) { Secret = secret });
        }

        //private PipelineExecutionResult InvokePipeline(Pipeline pipeline, bool showInGUI = false, bool defaultOutput = true)
        //{
        //    if(showInGUI)
        //        pipeline.Commands.Add(ContentOutputCommand);

        //    if(defaultOutput)
        //        pipeline.Commands.Add(DefaultOutputCommand);

        //    Collection<PSObject> result = null;
        //    Collection<object> errors = null;
        //    try
        //    {
        //        result = pipeline.Invoke();
        //        errors = pipeline.Error.ReadToEnd();
        //    }
        //    catch (Exception pe)
        //    {
        //        errors = pipeline.Error.ReadToEnd();
        //        errors.Add(pe);
        //        _host.UI.WriteErrorLine(pe.Message);
        //    }

        //    var output = new PipelineExecutionResult(result, errors, pipeline.PipelineStateInfo.Reason, pipeline.PipelineStateInfo.State);

        //    pipeline.Dispose();

        //    if (defaultOutput)
        //        ExecutePromptFunction(pipeline.Commands, pipeline.PipelineStateInfo.State);

        //    return output;
        //}

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
            }, false, result =>
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