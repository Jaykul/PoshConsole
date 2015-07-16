using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using PoshCode.PowerShell;
using PoshCode.Wpf.Controls;

namespace PoshCode
{
    public class PoshConsole : ConsoleControl, IDisposable, IContentControl
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
            "Progress", typeof(object), typeof(PoshConsole), new PropertyMetadata(default(object)));

        public Panel Progress
        {
            get { return (Panel)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }


        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content", typeof (object), typeof (PoshConsole), new PropertyMetadata(default(object)));

        public object Content
        {
            get { return (object) GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            CommandBox.IsEnabled = false;

            _host = new Host(this, Progress, new Options(this));

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
            var commands = new[] {new Command(command, true, true)};

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

            var commands = new[] {command};
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

    public interface IContentControl
    {
        object Content { get; set; }
    }
}