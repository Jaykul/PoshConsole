using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public IEnumerable<TResult> GetOutputObjects<TResult>()
        {
            return  Output.Select(pso => pso.BaseObject).Cast<TResult>();
        }

        public Collection<object> Errors { get; }

        public IEnumerable<KeyValuePair<string, Exception>> GetExceptions()
        {
            if (!HadErrors) return new KeyValuePair<string, Exception>[0];

            return Errors.Select(AsException);
        }

        public Collection<Command> Commands { get; }

        public bool HadErrors { get; }
        public long InstanceId { get; }
        public PipelineState State { get; }


        private static KeyValuePair<string,Exception> AsException(object error)
        {
            var exception = error as Exception;
            string message;
            if (exception == null)
            {
                var er = error as ErrorRecord;
                if (er != null)
                {
                    message = er.ErrorDetails.Message;
                    exception = er.Exception;
                }
                else
                {
                    message = error.ToString();
                }
            }
            else
            {
                message = exception.Message;
            }


            var cme = exception as CmdletInvocationException;
            if (cme != null)
            {
                exception = cme.ErrorRecord.Exception;
            }
            return new KeyValuePair<string, Exception>(message, exception);
        }
    }
}