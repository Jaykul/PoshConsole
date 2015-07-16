using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PoshCode.PowerShell
{
    internal delegate void PipelineOutputHandler(PipelineExecutionResult result);

    public struct PipelineExecutionResult
    {
        private readonly Collection<object> _errors;
        private readonly Exception _failure;
        private readonly Collection<PSObject> _output;
        private readonly PipelineState _state;

        public PipelineExecutionResult(Collection<PSObject> output, Collection<Object> errors, Exception failure,
            PipelineState state)
        {
            _failure = failure;
            _errors = errors ?? new Collection<Object>();
            _output = output ?? new Collection<PSObject>();
            _state = state;
        }

        public Collection<PSObject> Output
        {
            get { return _output; }
        }

        public Collection<Object> Errors
        {
            get { return _errors; }
        }

        public Exception Failure
        {
            get { return _failure; }
        }

        public PipelineState State
        {
            get { return _state; }
        }
    }
}