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


      #endregion [rgn]

      #region [rgn] Methods (18)

      // [rgn] Public Methods (1)

      public bool IsBusy()
      {
         return _ready.WaitOne(0, false);
      }

      // [rgn] Private Methods (17)




      //private PipelineState _state = PipelineState.NotStarted;
      //void pipeline_StateChanged(object sender, PipelineStateEventArgs e)
      //{
      //   _state = e.PipelineStateInfo.State;
      //}

      //private void ExecutePipeline(Command command, PipelineOutputHandler callback)
      //{
      //   ExecutePipeline(command, EmptyArray, callback);
      //}

      //private void ExecutePipeline(Command command, IEnumerable input, PipelineOutputHandler callback)
      //{
      //   ExecutePipeline(new Command[] { command }, input, callback);
      //}

      //private void ExecutePipeline(Command[] commands, PipelineOutputHandler callback)
      //{
      //   ExecutePipeline(commands, EmptyArray, callback);
      //}

      private void ExecutePipelineOutDefault(string command, bool addToHistory, PipelineOutputHandler callback)
      {
         ExecutePipelineOutDefault( command, EmptyArray, addToHistory, callback);
      }

      private void ExecutePipelineOutDefault(string command, IEnumerable input, bool addToHistory, PipelineOutputHandler callback)
      {
         ExecutePipeline((new[] { command }), input, callback); 
         //  CreatePipelineOutDefault(command, addToHistory), input, callback);
      }


      private void ExecutePipeline(string[] commands, IEnumerable input, PipelineOutputHandler callback)
      {
         // TODO: Solve the threading problem without breaking COM
         // The PowerShell runspace isn't threaded, so it can't INVOKE a pipeline while there's another one open...
         // having a bunch of BackgroundWorkers "magically" solved this for me, but ...
         // apparently causes COM's RCW(Runtime Callable Wrapper) to be disposed (or abandoned in another thread)
         // It seems the best way to multithread the UI is to have a persistent background worker thread
         // which would pull the pipeline and Input objects out and run them as fast as it could, 
         // while the front end queues them up as fast as it *wants* to.
         _runner.Enqueue(new InputBoundCommand(commands, input, callback));
      }


      ManualResetEvent _ready = new ManualResetEvent(true);

      //private PipelineExecutionResult ExecutePipelineSync(Command cmd)
      //{
      //   return ExecutePipelineSync(cmd, EmptyArray);
      //}

      //private PipelineExecutionResult ExecutePipelineSync(Command cmd, IEnumerable input)
      //{

      //}

      //private Collection<PSObject> InvokePipeline(string cmd)
      //{
      //   return InvokePipeline(cmd, );
      //}

      //private Collection<PSObject> InvokePipeline(Command cmd)
      //{
      //   Collection<Object> errors;
      //   return InvokePipeline(cmd, out errors);
      //}

      private Collection<PSObject> InvokePipeline(string cmd) //, out Collection<Object> errors
      {
         PipelineExecutionResult result = new PipelineExecutionResult();
         AutoResetEvent syncRoot = new AutoResetEvent(false);

         ExecutePipeline(new[]{cmd}, EmptyArray, (r)=> 
            {
               result = r;
               syncRoot.Set();
            });

         syncRoot.WaitOne();

         return result.Output;

         //PipelineExecutionResult result = ExecutePipelineSync(cmd);

         //if (result.Failure != null)
         //{
         //   throw result.Failure;
         //}

         //errors = result.Errors;
         //return result.Output;
      }

      //private T InvokePipelineSelectFirst<T>(Command cmd)
      //{
      //   PSObject psobj = InvokePipelineSelectFirst(cmd);

      //   if (psobj != null)
      //   {
      //      return (T)psobj.BaseObject;
      //   }

      //   return default(T);
      //}

      //private PSObject InvokePipelineSelectFirst(Command cmd)
      //{
      //   return InvokePipeline(cmd)[0]; // OR default?
      //}



      #endregion [rgn]

   }
}