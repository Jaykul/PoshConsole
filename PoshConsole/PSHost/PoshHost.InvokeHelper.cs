using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Collections;
using System.ComponentModel;

namespace PoshConsole.PSHost
{
   partial class PoshHost
   {

      #region [rgn] Fields (1)

      private static readonly object[] EmptyArray = new object[0];

      #endregion [rgn]

      #region [rgn] Delegates and Events (1)

      // [rgn] Delegates (1)

      private delegate void PipelineOutputHandler(PipelineExecutionResult result);

      #endregion [rgn]

      #region [rgn] Methods (18)

      // [rgn] Public Methods (1)

      public bool IsBusy()
      {
         return _ready.WaitOne(0, false);
      }

      // [rgn] Private Methods (17)

      private Pipeline CreatePipeline(Command[] commands)
      {
         Pipeline pipeline = _runSpace.CreatePipeline();
         pipeline.StateChanged += new EventHandler<PipelineStateEventArgs>(pipeline_StateChanged);

         foreach (Command cmd in commands)
         {
            pipeline.Commands.Add(cmd);
         }

         return pipeline;
      }

      private PipelineState _state = PipelineState.NotStarted;
      void pipeline_StateChanged(object sender, PipelineStateEventArgs e)
      {
         _state = e.PipelineStateInfo.State;
      }

      private Pipeline CreatePipelineOutDefault(string command, bool addToHistory)
      {
         Pipeline pipe = _runSpace.CreatePipeline(command, addToHistory);
         pipe.StateChanged += new EventHandler<PipelineStateEventArgs>(pipeline_StateChanged);
         pipe.Commands.Add(outDefault);

         return pipe;
      }

      private void ExecutePipeline(Command command, PipelineOutputHandler callback)
      {
         ExecutePipeline(command, EmptyArray, callback);
      }

      private void ExecutePipeline(Command command, IEnumerable input, PipelineOutputHandler callback)
      {
         ExecutePipeline(new Command[] { command }, input, callback);
      }

      private void ExecutePipeline(Command[] commands, PipelineOutputHandler callback)
      {
         ExecutePipeline(commands, EmptyArray, callback);
      }

      private void ExecutePipeline(Command[] commands, IEnumerable input, PipelineOutputHandler callback)
      {
         var runner = new BackgroundWorker();
         runner.DoWork += (a, b) =>
            {
               ExecutePipeline(CreatePipeline(commands), input, callback);
            };
         runner.RunWorkerAsync();
      }


      ManualResetEvent _ready = new ManualResetEvent(true);
      // The PowerShell runspace isn't threaded, so it can't INVOKE a pipeline while there's another one open...
      object _runspaceLock = new object();

      private void ExecutePipeline(Pipeline pipeline, IEnumerable input, PipelineOutputHandler callback)
      {
         // This is a dynamic anonymous delegate so that it can access the callback parameter
         pipeline.StateChanged += (EventHandler<PipelineStateEventArgs>)delegate(object sender, PipelineStateEventArgs e) // =>
         {
            if (e.PipelineStateInfo.IsDone())
            {
               Pipeline completed = (Pipeline)Interlocked.Exchange(ref _pipeline, null);
               if (completed != null)
               {
                  Exception failure = e.PipelineStateInfo.Reason;

                  if (failure != null)
                  {
                     System.Diagnostics.Debug.WriteLine(failure.GetType(), "PipelineFailure");
                     System.Diagnostics.Debug.WriteLine(failure.Message, "PipelineFailure");
                  }
                  Collection<Object> errors = completed.Error.ReadToEnd();
                  Collection<PSObject> results = completed.Output.ReadToEnd();

                  completed.Dispose();
                  _ready.Set();

                  if (callback != null)
                  {
                     callback(new PipelineExecutionResult(results, errors, failure, e.PipelineStateInfo.State));
                  }
               }
            }
         };

         lock (_runspaceLock)
         {  
            if(_pipeline != null)
            {
               System.Diagnostics.Trace.WriteLine(_ready.WaitOne(), "Runspace Unready");
            }

            _ready.Reset();
            pipeline.InvokeAsync();
            pipeline.Input.Write(input, true);
            pipeline.Input.Close();
            _pipeline = pipeline;
         }
      }

      private void ExecutePipelineOutDefault(string command, bool addToHistory, PipelineOutputHandler callback)
      {
         ExecutePipelineOutDefault(command, EmptyArray, addToHistory, callback);
      }

      private void ExecutePipelineOutDefault(string command, IEnumerable input, bool addToHistory, PipelineOutputHandler callback)
      {
         var runner = new BackgroundWorker();
         runner.DoWork += (a, b) =>
            {
               ExecutePipeline(CreatePipelineOutDefault(command, addToHistory), input, callback);
            };
         runner.RunWorkerAsync();
      }


      private PipelineExecutionResult ExecutePipelineSync(Command cmd)
      {
         return ExecutePipelineSync(cmd, EmptyArray);
      }

      private PipelineExecutionResult ExecutePipelineSync(Command cmd, IEnumerable input)
      {
         PipelineExecutionResult result = new PipelineExecutionResult();
         AutoResetEvent syncRoot = new AutoResetEvent(false);

         ExecutePipeline(cmd, input, (PipelineOutputHandler)delegate(PipelineExecutionResult r)
         {
            result = r;
            syncRoot.Set();
            //lock (syncRoot)
            //{
            //  Monitor.Pulse(syncRoot);
            //}
         });

         //lock (syncRoot)
         //{
         //  Monitor.Wait(syncRoot);
         //}
         syncRoot.WaitOne();

         return result;
      }

      private Collection<PSObject> InvokePipeline(string cmd)
      {
         return InvokePipeline(new Command(cmd, true, true));
      }

      private Collection<PSObject> InvokePipeline(Command cmd)
      {
         Collection<Object> errors;
         return InvokePipeline(cmd, out errors);
      }

      private Collection<PSObject> InvokePipeline(Command cmd, out Collection<Object> errors)
      {
         PipelineExecutionResult result = ExecutePipelineSync(cmd);

         if (result.Failure != null)
         {
            throw result.Failure;
         }

         errors = result.Errors;
         return result.Output;
      }

      private T InvokePipelineSelectFirst<T>(Command cmd)
      {
         PSObject psobj = InvokePipelineSelectFirst(cmd);

         if (psobj != null)
         {
            return (T)psobj.BaseObject;
         }

         return default(T);
      }

      private PSObject InvokePipelineSelectFirst(Command cmd)
      {
         return InvokePipeline(cmd)[0]; // OR default?
      }



      #endregion [rgn]
      private struct PipelineExecutionResult
      {
         private Collection<PSObject> _output;
         private Collection<Object> _errors;
         private Exception _failure;
         private PipelineState _state;

         public Collection<PSObject> Output { get { return _output; } }
         public Collection<Object> Errors { get { return _errors; } }
         public Exception Failure { get { return _failure; } }
         public PipelineState State { get { return _state; } }

         public PipelineExecutionResult(Collection<PSObject> output, Collection<Object> errors, Exception failure, PipelineState state)
         {
            _failure = failure;
            _errors = errors ?? new Collection<Object>();
            _output = output ?? new Collection<PSObject>();
            _state = state;
         }
      }
   }
}