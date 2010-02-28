using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;
using PoshConsole.Properties;

// ReSharper disable CheckNamespace
namespace PoshConsole
{
   internal struct PipelineExecutionResult
   {
      private readonly Collection<Object> _errors;
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

   internal delegate void PipelineOutputHandler(PipelineExecutionResult result);

   public delegate void ShouldExitHandler(object source, int exitCode);

   public delegate void RunspaceReadyHandler(object source, RunspaceState stateEventArgs);


   internal struct InputBoundCommand
   {
      public bool AddToHistory;
      public PipelineOutputHandler Callback;
      public string[] Commands;
      public bool DefaultOutput;
      public IEnumerable Input;
      public bool RunAsScript;
      public bool UseLocalScope;

      //public Pipeline Pipeline;

      public InputBoundCommand( /*Pipeline pipeline,*/
         string[] commands, IEnumerable input, PipelineOutputHandler callback)
      {
         //Pipeline = pipeline;
         Commands = commands;
         Input = input;
         Callback = callback;

         AddToHistory = true;
         DefaultOutput = true;
         RunAsScript = true;
         UseLocalScope = false;
      }

      public InputBoundCommand( /*Pipeline pipeline,*/
         string[] commands, IEnumerable input, bool addToHistory, PipelineOutputHandler callback)
      {
         //Pipeline = pipeline;
         Commands = commands;
         Input = input;
         Callback = callback;
         AddToHistory = addToHistory;

         DefaultOutput = true;
         RunAsScript = true;
         UseLocalScope = false;
      }
   }


   public class SyncEvents
   {
      // the exit thread event needs to be in every signalling array
      private readonly EventWaitHandle _abortProcessingEvent;
      private readonly EventWaitHandle _emptyQueueEvent;
      private readonly WaitHandle[] _endQueueHandles;
      private readonly EventWaitHandle _exitThreadEvent;
      // this event signals new items
      private readonly EventWaitHandle _newItemEvent;
      // these events signal the end of processing
      // the empty queue event: temporary end of processing because there's no items left


      private readonly WaitHandle[] _newItemHandles;
      private readonly EventWaitHandle _pipelineFinishedEvent;


      public SyncEvents()
      {
         _newItemEvent = new AutoResetEvent(false);
         _exitThreadEvent = new ManualResetEvent(false);
         _emptyQueueEvent = new ManualResetEvent(false);
         _abortProcessingEvent = new ManualResetEvent(false);
         _pipelineFinishedEvent = new AutoResetEvent(false);

         _newItemHandles = new WaitHandle[3];
         _newItemHandles[0] = _exitThreadEvent;
         _newItemHandles[1] = _abortProcessingEvent;
         _newItemHandles[2] = _newItemEvent;

         _endQueueHandles = new WaitHandle[3];
         _endQueueHandles[0] = _exitThreadEvent;
         _endQueueHandles[1] = _abortProcessingEvent;
         _endQueueHandles[2] = _emptyQueueEvent;
      }

      public EventWaitHandle PipelineFinishedEvent
      {
         get { return _pipelineFinishedEvent; }
      }

      /// <summary>
      /// Gets the exit thread event.
      /// </summary>
      /// <value>The exit thread event.</value>
      public EventWaitHandle ExitThreadEvent
      {
         get { return _exitThreadEvent; }
      }

      /// <summary>
      /// Gets the new item event.
      /// </summary>
      /// <value>The new item event.</value>
      public EventWaitHandle NewItemEvent
      {
         get { return _newItemEvent; }
      }

      /// <summary>
      /// Gets the empty queue event.
      /// </summary>
      /// <value>The empty queue event.</value>
      public EventWaitHandle EmptyQueueEvent
      {
         get { return _emptyQueueEvent; }
      }

      /// <summary>
      /// Gets the abort queue event.
      /// </summary>
      /// <value>The abort queue event.</value>
      public EventWaitHandle AbortQueueEvent
      {
         get { return _abortProcessingEvent; }
      }


      public WaitHandle[] NewItemEvents
      {
         get { return _newItemHandles; }
      }

      public WaitHandle[] TerminationEvents
      {
         get { return _endQueueHandles; }
      }
   }


   internal class CommandRunner : IDisposable
   {
#if DEBUG
      protected static TraceSwitch ParseTrace = new TraceSwitch("parsing",
                                                                "Controls the output level of the parsing tracers", "4");

      protected static TraceSwitch ThreadTrace = new TraceSwitch("threading",
                                                                 "Controls the output level of the thread interaction tracers",
                                                                 "4");
#else
        protected static TraceSwitch ParseTrace = new TraceSwitch("parsing", "Controls the output level of the parsing tracers", "1");
        protected static TraceSwitch ThreadTrace = new TraceSwitch("threading", "Controls the output level of the thread interaction tracers", "0");
#endif
      protected Queue<InputBoundCommand> CommandQueue;
      protected Thread WorkerThread;

      private SyncEvents _syncEvents;

      public SyncEvents SyncEvents
      {
         get { return _syncEvents; }
         set { _syncEvents = value; }
      }

      private Pipeline _pipeline;
      private readonly Runspace _runSpace;

      public Pipeline CurrentPipeline
      {
         get { return _pipeline; }
      }

      public readonly Command DefaultOutputCommand;
      public event ShouldExitHandler ShouldExit;
      public event RunspaceReadyHandler RunspaceReady;

      /// <summary> Initializes a new instance of the <see cref="CommandRunner"/> class 
      /// with no cookies.
      /// </summary>
      public CommandRunner(PSHost host)
      {
         CommandQueue = new Queue<InputBoundCommand>();
         _syncEvents = new SyncEvents();

         // pre-create this
         DefaultOutputCommand = new Command("Out-Default");
         // for now, merge the errors with the rest of the output
         DefaultOutputCommand.MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
         DefaultOutputCommand.MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Error |
                                                                     PipelineResultTypes.Output;

         // Create the default initial session state and add the module.
         InitialSessionState iss = InitialSessionState.CreateDefault();

         Assembly poshModule = Assembly.GetEntryAssembly();
         string currentUserProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WindowsPowerShell");
         string allUsersProfilePath = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell\\v1.0");


         // Import the PoshWPF module automatically

         //if(!Environment.ExpandEnvironmentVariables("%PSModulePath%").Contains( Path.GetDirectoryName(poshModule.Location) )) {
         //   Environment.SetEnvironmentVariable( "PSModulePath", Environment.GetEnvironmentVariable("PSModulePath") +
         //                                                ";" + Path.Combine(currentUserProfilePath, "Modules") +
         //                                                ";" + Path.Combine(Path.GetDirectoryName(poshModule.Location), "Modules"));
         //}

         iss.ImportPSModule(new[] {Path.Combine(Path.GetDirectoryName(poshModule.Location), "PoshWpf.dll")});
         // We need STA so we can do WPF stuff from our console thread.
         iss.ApartmentState = ApartmentState.STA;
         // We need ReuseThread so that we behave, well, the way that PowerShell.exe and ISE do.
         iss.ThreadOptions = PSThreadOptions.ReuseThread;
         // iss.Variables
         // Load all the Cmdlets that are in this assembly automatically.
         foreach (Type t in poshModule.GetTypes())
         {
            var cmdlets = t.GetCustomAttributes(typeof (CmdletAttribute), false) as CmdletAttribute[];

            if (cmdlets != null)
            {
               foreach (CmdletAttribute cmdlet in cmdlets)
               {
                  iss.Commands.Add(new SessionStateCmdletEntry(
                                      string.Format("{0}-{1}", cmdlet.VerbName, cmdlet.NounName), t,
                                      string.Format("{0}.xml", t.Name)));
               }
            }
         }


         var profile = new PSObject(Path.GetFullPath(Path.Combine(currentUserProfilePath, host.Name + "_profile.ps1")));
         //* %windir%\system32\WindowsPowerShell\v1.0\profile.ps1
         //  This profile applies to all users and all shells.
         profile.Properties.Add(new PSNoteProperty("AllUsersAllHosts",
                                                   Path.GetFullPath(Path.Combine(allUsersProfilePath, "Profile.ps1"))));
         //* %windir%\system32\WindowsPowerShell\v1.0\PoshConsole_profile.ps1
         //  This profile applies to all users, but only to the Current shell.
         profile.Properties.Add(new PSNoteProperty("AllUsersCurrentHost",
                                                   Path.GetFullPath(Path.Combine(allUsersProfilePath,
                                                                                 host.Name + "_profile.ps1"))));
         //* %UserProfile%\My Documents\WindowsPowerShell\profile.ps1
         //  This profile applies only to the current user, but affects all shells.
         profile.Properties.Add(new PSNoteProperty("CurrentUserAllHosts",
                                                   Path.GetFullPath(Path.Combine(currentUserProfilePath, "Profile.ps1"))));
         //* %UserProfile%\My Documents\WindowsPowerShell\PoshConsole_profile.ps1
         //  This profile applies only to the current user and the Current shell.
         profile.Properties.Add(new PSNoteProperty("CurrentUserCurrentHost", profile.ImmediateBaseObject));

         iss.Variables.Add(new SessionStateVariableEntry("profile", profile,
                                                         "The enumeration of all the available profiles the user could edit."));

         _runSpace = RunspaceFactory.CreateRunspace(host, iss);

         /*
            RunspaceConfiguration conf = RunspaceConfiguration.Create();
            conf.InitializationScripts.Append(new ScriptConfigurationEntry("ImportPoshWpf", "$Foo = 'This is foo'")); // Import-Module .\\PoshWPF.dll
            _runSpace = RunspaceFactory.CreateRunspace(host, conf);
         */

         // Set the default runspace, so that event handlers can run in the same runspace as commands.
         // We _could_ hypothetically make this a different runspace, but it would probably cause issues.
         Runspace.DefaultRunspace = _runSpace;

         // we could hypothetically make several threads to do this work...
         WorkerThread = new Thread(ThreadRun) {Name = "CommandRunner"};
         WorkerThread.SetApartmentState(ApartmentState.STA);
         WorkerThread.Start();
      }

      private void StartRunspace()
      {
         /*
                    foreach (var t in System.Reflection.Assembly.GetEntryAssembly().GetTypes())
                    {
                       var cmdlets = t.GetCustomAttributes(typeof(CmdletAttribute), false) as CmdletAttribute[];

                       if (cmdlets != null)
                       {
                          foreach (var cmdlet in cmdlets)
                          {
                             _runSpace.RunspaceConfiguration.Cmdlets.Append(new CmdletConfigurationEntry(
                                                                               string.Format("{0}-{1}", cmdlet.VerbName, cmdlet.NounName), t,
                                                                               string.Format("{0}.xml", t.Name)));
                          }
                       }
                    }

         //// Must compile against v2 for this
         //#if POWERSHELL2
         //           if (_runSpace.Version.Major >= 2)
         //           {
                       _runSpace.ApartmentState = ApartmentState.STA;
                       _runSpace.ThreadOptions = PSThreadOptions.ReuseThread;
         //           }
         //#endif
                    //_runSpace.StateChanged += (sender, e) =>
                    //                             {
                    //                                if (e.RunspaceStateInfo.State == RunspaceState.Opened && RunspaceReady != null)
                    //                                   {
                    //                                      RunspaceReady(sender, e);
                    //                                   }
                    //                             };
          */
         _runSpace.Open();
         ExecuteStartupProfile();
      }

      public void Enqueue(InputBoundCommand command)
      {
         lock (((ICollection) CommandQueue).SyncRoot)
         {
            CommandQueue.Enqueue(command);
         }
         _syncEvents.NewItemEvent.Set();
      }


      /// <summary>
      /// The ThreadStart delegate
      /// </summary>
      public void ThreadRun()
      {
         StartRunspace();
         InputBoundCommand boundCommand;
         //_ExitException = null;

         int sync = WaitHandle.WaitAny(_syncEvents.NewItemEvents);
         while (sync > 0)
         {
            Trace.WriteLineIf(ThreadTrace.TraceVerbose, "Signalled. Items in queue: " + CommandQueue.Count, "threading");
            while (CommandQueue.Count > 0)
            {
               lock (((ICollection) CommandQueue).SyncRoot)
               {
                  boundCommand = CommandQueue.Dequeue();
               }

               Pipeline pipeline = _runSpace.CreatePipeline(boundCommand.Commands[0], boundCommand.AddToHistory);
               for (int c = 1; c < boundCommand.Commands.Length; c++)
               {
                  pipeline.Commands.Add(new Command(boundCommand.Commands[c], boundCommand.RunAsScript,
                                                    boundCommand.UseLocalScope));
               }
               if (boundCommand.DefaultOutput)
               {
                  pipeline.Commands.Add(DefaultOutputCommand);
               }

               // Trace.WriteLineIf(threadTrace.TraceVerbose, "Executing " + pipeline.Commands[0] + "... Items remaining: " + _CommandQueue.Count.ToString(), "threading");

               _pipeline = pipeline;

               // This is a dynamic anonymous delegate so that it can access the Callback parameter
               _pipeline.StateChanged +=
                  (EventHandler<PipelineStateEventArgs>) delegate(object sender, PipelineStateEventArgs e) // =>
                                                            {
                                                               Trace.WriteLine("Pipeline is " +
                                                                               e.PipelineStateInfo.State);

                                                               if (e.PipelineStateInfo.IsDone())
                                                               {
                                                                  Trace.WriteLine("Pipeline is Done");

                                                                  Pipeline completed =
                                                                     Interlocked.Exchange(ref _pipeline, null);
                                                                  if (completed != null)
                                                                  {
                                                                     Exception failure = e.PipelineStateInfo.Reason;

                                                                     if (failure != null)
                                                                     {
                                                                        Debug.WriteLine(failure.GetType(),
                                                                                        "PipelineFailure");
                                                                        Debug.WriteLine(failure.Message,
                                                                                        "PipelineFailure");
                                                                     }
                                                                     Collection<Object> errors =
                                                                        completed.Error.ReadToEnd();
                                                                     Collection<PSObject> results =
                                                                        completed.Output.ReadToEnd();

                                                                     completed.Dispose();
                                                                     //_SyncEvents.PipelineFinishedEvent.Set();

                                                                     if (boundCommand.Callback != null)
                                                                     {
                                                                        boundCommand.Callback(
                                                                           new PipelineExecutionResult(results, errors,
                                                                                                       failure,
                                                                                                       e.
                                                                                                          PipelineStateInfo
                                                                                                          .State));
                                                                     }
                                                                  }
                                                               }
                                                            };

               // I thought that maybe invoke instead of InvokeAsync() would stop the (COM) thread problems
               // it didn't, but it means I don't need the sync, so I might as well leave it...
               try
               {
                  _pipeline.Invoke(boundCommand.Input);
               }
               catch (Exception ipe)
               {
                  // TODO: Handle IncompleteParseException with some elegance!
                  //    klumsy suggested we could prevent these by using the tokenizer 
                  // Tokenizing in OnEnterPressed (before sending it to the CommandRunner)
                  //    would allow us to let {Enter} be handled nicely ... 
                  // Tokenizing in KeyDown would let us do live syntax highlighting,
                  //    is it fast enough to work?
                  Debug.WriteLine(ipe.Message);
               }
               //catch (ParseException pe)
               //{
               //   // TODO: Handle ParseException with some elegance!
               //}
               //_Pipeline.InvokeAsync();
               //_Pipeline.Input.Write(boundCommand.Input, true);
               //_Pipeline.Input.Close();

               //_SyncEvents.PipelineFinishedEvent.WaitOne();
            }
            Trace.WriteLineIf(ThreadTrace.TraceVerbose, "Done. No items in Queue.", "threading");
            _syncEvents.EmptyQueueEvent.Set();
            sync = WaitHandle.WaitAny(_syncEvents.NewItemEvents);
         }

         if (_runSpace.RunspaceStateInfo.State != RunspaceState.Closing
             && _runSpace.RunspaceStateInfo.State != RunspaceState.Closed)
         {
            _runSpace.Close();
         }
      }

      /// <summary>
      /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
      /// </summary>
      public void Dispose()
      {
         _syncEvents.ExitThreadEvent.Set();
         WorkerThread.Join();

         _runSpace.Dispose();
      }

      public void StopPipeline()
      {
         if (_pipeline != null && _pipeline.PipelineStateInfo.State == PipelineState.Running)
         {
            _pipeline.StopAsync();
         }

      }

      public InitialSessionState InitialSessionState
      {
         get { return _runSpace.InitialSessionState; }
      }

      public RunspaceConfiguration RunspaceConfiguration
      {
         get { return _runSpace.RunspaceConfiguration; }
      }

      public RunspaceStateInfo RunspaceStateInfo
      {
         get { return _runSpace.RunspaceStateInfo; }
      }


      /// <summary>
      /// Executes the shutdown profile(s).
      /// </summary>
      internal void ExecuteShutdownProfile(int exitCode)
      {
         //* %windir%\system32\WindowsPowerShell\v1.0\profile_exit.ps1
         //  This profile applies to all users and all shells.
         //* %windir%\system32\WindowsPowerShell\v1.0\PoshConsole_profile_exit.ps1
         //  This profile applies to all users, but only to the PoshConsole shell.
         //* %UserProfile%\My Documents\WindowsPowerShell\profile_exit.ps1
         //  This profile applies only to the current user, but affects all shells.
         //* %UserProfile%\\My Documents\WindowsPowerShell\PoshConsole_profile_exit.ps1
         //  This profile applies only to the current user and the PoshConsole shell.

         // just for the sake of the profiles...
         var existing = new List<string>(new[]
            {
               // Global Exit Profiles
               Path.GetFullPath(Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\profile_exit.ps1")), 
               Path.GetFullPath(Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\PoshConsole_profile_exit.ps1")),
               // User Exit Profiles
               Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WindowsPowerShell\profile_exit.ps1")), 
               Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WindowsPowerShell\PoshConsole_profile_exit.ps1")),
            }.Where(File.Exists));

         //StringBuilder cmd = new StringBuilder();

         if (existing.Count > 0)
         {
            Enqueue(new InputBoundCommand(existing.ToArray(), new object[0], result =>
                                                                                {
                                                                                   if (result.Failure != null)
                                                                                   {
                                                                                      // WriteErrorRecord(((RuntimeException)(result.Failure)).ErrorRecord);
                                                                                   }

                                                                                   if (ShouldExit != null)
                                                                                   {
                                                                                      ShouldExit(this, exitCode);
                                                                                   }
                                                                                })
                       {AddToHistory = false, RunAsScript = false});


            //try
            //{
            //    ExecuteHelper(cmd.ToString(), null, false);
            //}
            //catch (RuntimeException rte)
            //{
            //    // An exception occurred that we want to display ...
            //    // We have to run another pipeline, and pass in the error record.
            //    // The runtime will bind the Input to the $Input variable
            //    ExecuteHelper("write-host ($Input | out-string) -fore darkyellow", rte.ErrorRecord, false);
            //}
         }
         else
         {
            if (ShouldExit != null)
            {
               ShouldExit(this, exitCode);
            }
         }


         //else
         //{
         //   ExecutePromptFunction();
         //}
      }

      /// <summary>
      /// Executes the startup profile(s).
      /// </summary>
      public void ExecuteStartupProfile()
      {
         CommandQueue.Clear();

         Enqueue(new InputBoundCommand(new[] {Resources.Prompt}, new object[0], false, null));

         var existing = new List<string>(4);
         existing.AddRange(from profileVariable in InitialSessionState.Variables["profile"]
                           from pathProperty in ((PSObject) profileVariable.Value).Properties.Match("*Host*", PSMemberTypes.NoteProperty)
                           where File.Exists(pathProperty.Value.ToString())
                           select pathProperty.Value.ToString());
         // This might be nice to have too (in case anyone was using it):
         _runSpace.SessionStateProxy.SetVariable("profiles", existing.ToArray());

         if (existing.Count > 0)
         {
            existing.TrimExcess();
            Enqueue(new InputBoundCommand(
                       new[] {". \"" + string.Join("\";. \"", existing.ToArray()) + "\";"},
                       new object[0],
                       false,
                       ignored => RunspaceReady(this, _runSpace.RunspaceStateInfo.State))); // this is super important
         }
         else
         {
            Enqueue(new InputBoundCommand(
                       new[] {"New-Paragraph"},
                       new object[0],
                       false,
                       ignored => RunspaceReady(this, _runSpace.RunspaceStateInfo.State))); // this is super important
         }
      }

      //existing.Add();

      ////ExecutePipelineOutDefault(, false, result => { });

      //result =>
      //   {
      //      StringBuilder str = new StringBuilder();

      //      foreach (PSObject obj in result.Output)
      //      {
      //         str.Append(obj);
      //      }
      //      // ToDo: write errors from PROMPT the same as we would for a regular command...
      //      //if(result.State == PipelineState.Failed ) {
      //      //   str.Append(result.Failure.Message);
      //      //   str.Append(result.Failure.Message);

      //      _buffer.Prompt(str.ToString());
      //   }


      //if (cmd.Length > 0)
      //{
      //   ExecutePipelineOutDefault(cmd.ToString(), false, result => ExecutePromptFunction(result.State));
      //}
      //else
      //{
      //   ExecutePromptFunction(PipelineState.NotStarted);
      //}
      //}
   }
}