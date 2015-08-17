using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using PoshCode.Properties;

namespace PoshCode.PowerShell
{

    internal class CallbackCommand
    {
        public readonly Collection<Command> Commands;
        public readonly bool ScriptCommand;
        public bool DefaultOutput;
        public bool Secret;

        //public Pipeline Pipeline;

        public CallbackCommand(IList<Command> commands, bool defaultOutput = true, bool secret = false, Action<PipelineFinishedEventArgs> onFinished = null, Action<RuntimeException> onFailure = null, Action<Collection<PSObject>> onSuccess = null)
        {
            Commands = new Collection<Command>(commands);

            if(onFinished != null)
                this.Finished += (sender, args) => onFinished(args);

            if (onFailure != null)
                this.Error += (sender, args) => onFailure((RuntimeException)args.Failure);

            if (onSuccess != null)
                this.Success += (sender, args) => onSuccess(args.Output);

            ScriptCommand = Commands[0].IsScript;
            DefaultOutput = defaultOutput;
            Secret = secret;
        }

        public CallbackCommand(string script, bool defaultOutput = true, bool secret = false, Action<PipelineFinishedEventArgs> onFinished = null, Action<RuntimeException> onFailure = null, Action<Collection<PSObject>> onSuccess = null) :
            this(new[] { new Command(script, true, true) }, defaultOutput, secret, onFinished, onFailure, onSuccess)
        {
        }

        public override string ToString()
        {
            // TODO: 1) special-case strings and numbers (and arrays of them) so they're not wrapped in ${}
            // TODO: 2) check for the script flag and wrap the command in &{ }

            var output = new StringBuilder();
            var enumerator = Commands.GetEnumerator();
            var more = enumerator.MoveNext();
            while (more)
            {
                var cmd = enumerator.Current;
                more = enumerator.MoveNext();
                var script = cmd.IsScript && more;

                if (script) output.Append("&{ ");

                output.Append(cmd.CommandText);
                foreach (var param in cmd.Parameters)
                {
                    output.AppendFormat(" -{0} {1}", param.Name, GetParameterValue(param.Value));
                }

                output.Append(script ? " } | " : " | ");
            }

            return output.ToString().TrimEnd(' ', '|');
        }

        private static string GetParameterValue(object value)
        {
            var stringValue = value as string;
            if (stringValue != null)
            {
                return $"\"{value}\"";
            }

            var items = value as IEnumerable;
            if (items != null)
            {
                stringValue = $"@( {string.Join(", ", from object item in items select GetParameterValue(item))})";
            }
            
            if (value is int || value is short || value is long || value is uint || value is ushort || value is ulong)
            {
                stringValue = $"{value:D}";
            }

            if (value is double)
            {
                stringValue = $"{value:F}";
            }

            if (value is decimal)
            {
                stringValue = $"{value:F}D";
            }

            return stringValue ?? $"${{{value}}}";
        }


        public event CompletedHandler Error;

        public event CompletedHandler Finished;

        public event CompletedHandler Success;

        // Invoke the Error event; called when there's an error
        protected virtual void OnError(PipelineFinishedEventArgs e)
        {
            Error?.Invoke(this, e);
        }


        // Invoke the Error event; called when there's an error
        protected virtual void OnSuccess(PipelineFinishedEventArgs e)
        {
            Success?.Invoke(this, e);
        }

        public virtual void OnFinished(PipelineFinishedEventArgs e)
        {
            Finished?.Invoke(this, e);

            if (e.Failure != null)
            {
                OnError(e);

                // ToDo: if( result.Failure is IncompleteParseException ) { // trigger multiline entry
                PoshConsole.CurrentConsole.WriteErrorRecord(((RuntimeException)(e.Failure)).ErrorRecord);
            }
            else
            {
                OnSuccess(e);
            }

            foreach (var err in e.Errors)
            {
                var pso = (err as PSObject)?.BaseObject ?? err;
                var error = pso as ErrorRecord;
                if (error == null)
                {
                    var exception = pso as Exception;
                    if (exception == null)
                    {
                        PoshConsole.CurrentConsole.WriteErrorRecord(new ErrorRecord(null, pso.ToString(), ErrorCategory.NotSpecified, pso));
                        continue;
                    }
                    PoshConsole.CurrentConsole.WriteErrorRecord(new ErrorRecord(exception, "Unspecified", ErrorCategory.NotSpecified, pso));
                    continue;
                }
                PoshConsole.CurrentConsole.WriteErrorRecord(error);
            }
            if (!Secret || !e.Errors.Any() || !e.Output.Any())
            {
                PoshConsole.CurrentConsole.OnCommandFinished(Commands, e.State);
            }
        }
    }

    // A delegate type for hooking up change notifications.
    public delegate void CompletedHandler(object sender, PipelineFinishedEventArgs e);

    
}