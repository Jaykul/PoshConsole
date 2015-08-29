using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PoshCode.PowerShell
{

    public class PoshConsolePipelineResults
    {
        
        public PoshConsolePipelineResults(long instanceId, Collection<Command> commands, Collection<PSObject> output = null, Collection<object> errors = null, PipelineState state = PipelineState.NotStarted)
        {
            InstanceId = instanceId;
            Commands = commands;
            Output = output ?? new Collection<PSObject>();
            Errors = errors ?? new Collection<object>();
            HadErrors = Errors.Count > 0;
            State = state;
        }

        public Collection<PSObject> Output { get; }
        public Collection<object> Errors { get; }
        public Collection<Command> Commands { get; }

        public bool HadErrors { get; }
        public long InstanceId { get; }
        public PipelineState State { get; }
    }
}