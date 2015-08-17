using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PoshCode.PowerShell
{

    public class PipelineFinishedEventArgs : EventArgs
    {
        
        public PipelineFinishedEventArgs(Collection<PSObject> output = null, Collection<object> errors = null, Exception failure = null, PipelineState state = PipelineState.NotStarted, IEnumerable<Command> commands = null, long instanceId = 0)
        {
            Commands = commands;
            Output = output ?? new Collection<PSObject>();
            Errors = errors ?? new Collection<object>();
            HadErrors = Errors.Count > 0;
            InstanceId = instanceId;
            Failure = failure;
            State = state;
        }

        public static PipelineFinishedEventArgs FromPipeline(Pipeline pipeline, PipelineStateInfo info)
        {
            var errors = pipeline.Error.ReadToEnd();
            var results = pipeline.Output.ReadToEnd();

            return new PipelineFinishedEventArgs(results, errors, info.Reason, info.State, pipeline.Commands, pipeline.InstanceId);
        }

        public Collection<PSObject> Output { get; }
        public Collection<object> Errors { get; }
        public IEnumerable<Command> Commands;
        public Exception Failure { get; }


        public bool HadErrors { get; }
        public long InstanceId { get; }
        public PipelineState State { get; }
    }
}