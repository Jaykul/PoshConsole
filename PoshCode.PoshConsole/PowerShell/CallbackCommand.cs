using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Text;

namespace PoshCode.PowerShell
{
    internal struct CallbackCommand
    {
        public readonly PipelineOutputHandler Callback;
        public readonly IEnumerable<Command> Commands;
        public readonly bool ScriptCommand;
        public bool DefaultOutput;
        public bool Secret;

        //public Pipeline Pipeline;

        public CallbackCommand(IEnumerable<Command> commands, PipelineOutputHandler callback)
        {
            Commands = commands;
            Callback = callback;

            ScriptCommand = false;
            DefaultOutput = true;
            Secret = false;
        }

        public CallbackCommand(string script, PipelineOutputHandler callback)
        {
            Commands = new[] {new Command(script, true, true)};
            Callback = callback;
            ScriptCommand = true;
            DefaultOutput = true;
            Secret = false;
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
                return string.Format("\"{0}\"", value);
            }

            var items = value as IEnumerable;
            if (items != null)
            {
                stringValue = "@( " + string.Join(", ", from object item in items select GetParameterValue(item)) + ")";
            }
            
            if (value is int || value is short || value is long || value is uint || value is ushort || value is ulong)
            {
                stringValue = string.Format("{0:D}", value);
            }

            if (value is double)
            {
                stringValue = string.Format("{0:F}", value);
            }

            if (value is decimal)
            {
                stringValue = string.Format("{0:F}D", value);
            }

            return stringValue ?? string.Format("${{{0}}}", value);
        }
    }

}