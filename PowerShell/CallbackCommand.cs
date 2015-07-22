using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Text;

namespace PoshCode.PowerShell
{
    internal struct CallbackCommand
    {
        public bool AddToHistory;
        public PipelineOutputHandler Callback;
        public IEnumerable<Command> Commands;
        public bool DefaultOutput;
        public bool RunAsScript;
        public bool UseLocalScope;
        public bool Secret;

        //public Pipeline Pipeline;

        public CallbackCommand(IEnumerable<Command> commands, PipelineOutputHandler callback)
        {
            //Pipeline = pipeline;
            Commands = commands;
            Callback = callback;

            AddToHistory = true;
            DefaultOutput = true;
            RunAsScript = true;
            UseLocalScope = false;
            Secret = false;
        }

        public CallbackCommand(IEnumerable<Command> commands, bool addToHistory, PipelineOutputHandler callback)
        {
            //Pipeline = pipeline;
            Commands = commands;
            Callback = callback;
            AddToHistory = addToHistory;

            DefaultOutput = true;
            RunAsScript = true;
            UseLocalScope = false;
            Secret = false;
        }

        public override string ToString()
        {
            // TODO: 1) special-case strings and numbers (and arrays of them) so they're not wrapped in ${}
            // TODO: 2) check for the script flag and wrap the command in &{ }

            var output = new StringBuilder();
            foreach (var cmd in Commands)
            {
                if (cmd.IsScript) { output.Append("&{ "); }

                output.Append(cmd.CommandText);
                foreach (var param in cmd.Parameters)
                {
                    output.AppendFormat(" -{0} {1}", param.Name, GetParameterValue(param.Value));
                }
               
                output.Append(cmd.IsScript ? " } | " : " | ");
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