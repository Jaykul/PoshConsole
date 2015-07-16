using System.Collections.Generic;
using System.Management.Automation.Runspaces;

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
        }
    }

}