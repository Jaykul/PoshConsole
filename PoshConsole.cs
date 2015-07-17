using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using PoshCode.PowerShell;
using PoshCode.Wpf;
using PoshCode.Wpf.Controls;

namespace PoshCode
{
    public class PoshConsole : ConsoleControl, IDisposable, IRichConsole
    {
        internal RunspaceProxy Runner { get; set; }
        private Host _host;

        public void Dispose()
        {
            if (_host != null)
            {
                _host.Dispose();
                _host = null;
            }
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

        private Panel _progressPanel = null;
        public Panel ProgressPanel
        {
            get
            {
                if (RootWindow == null) return null;
                return _progressPanel ?? (_progressPanel = RootWindow.FindName(Progress) as Panel);
            }
        }

        private Panel _contentControl = null;
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
        public void ExecuteCommand(string command, bool contentOutput = false, bool defaultOutput = true)
        {
            Write(command + "\n");
            var commands = new[] {new Command(command, true, true)}.ToList();

            if (contentOutput)
                commands.Add(new Command("Out-PoshConsole"));

            Runner.Enqueue(
                new CallbackCommand(
                    commands, 
                    defaultOutput, 
                    result => {
                        if (result.Failure != null)
                        {
                            // ToDo: if( result.Failure is IncompleteParseException ) { // trigger multiline entry
                            WriteErrorRecord(((RuntimeException)(result.Failure)).ErrorRecord);
                        }
                        ExecutePromptFunction(commands, result.State);
                    }));

            //var result = await Task.Factory.FromAsync(_shell.AddScript(command).BeginInvoke(), handle => _shell.EndInvoke(handle));
            //return InvokePipeline(pipeline, contentOutput, defaultOutput);
        }


        public void ExecuteCommand(Command command, bool contentOutput = false, bool defaultOutput = true)
        {
            // Echo to console
            Write(command + "\n");

            var commands = new[] {command}.ToList();

            if (contentOutput)
                commands.Add(new Command("Out-PoshConsole"));

            Runner.Enqueue(
                new CallbackCommand(
                    commands,
                    defaultOutput,
                    result =>
                    {
                        if (result.Failure != null)
                        {
                            // ToDo: if( result.Failure is IncompleteParseException ) { // trigger multiline entry
                            WriteErrorRecord(((RuntimeException) (result.Failure)).ErrorRecord);
                        }
                        ExecutePromptFunction(commands, result.State);
                    }));
        }

        //private PipelineExecutionResult InvokePipeline(Pipeline pipeline, bool contentOutput = false, bool defaultOutput = true)
        //{
        //    if(contentOutput)
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

        private void ExecutePromptFunction(IEnumerable<Command> command, PipelineState lastState)
        {
            OnCommandFinished(command, lastState);
            Runner.Enqueue(_promptSequence);
        }

        private readonly CallbackCommand _promptSequence;

        public PoshConsole() :base()
        {
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
            }) { DefaultOutput = false }; 
        }

        public Command DefaultOutputCommand { get; set; }
        public Command ContentOutputCommand { get; set; }

    }
}