
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using System.Threading;
using PoshCode.Properties;

namespace PoshCode.PowerShell
{
    internal class RunspaceProxy
    {
#if DEBUG
        protected static TraceSwitch ParseTrace = new TraceSwitch("parsing", "Controls the output level of the parsing tracers", "4");
        protected static TraceSwitch ThreadTrace = new TraceSwitch("threading", "Controls the output level of the thread interaction tracers", "4");
#else
        protected static TraceSwitch ParseTrace = new TraceSwitch("parsing", "Controls the output level of the parsing tracers", "1");
        protected static TraceSwitch ThreadTrace = new TraceSwitch("threading", "Controls the output level of the thread interaction tracers", "0");
#endif

        public delegate void ShouldExitHandler(object source, int exitCode);
        public event ShouldExitHandler ShouldExit;

        public delegate void RunspaceReadyHandler(object source, RunspaceState stateEventArgs);
        public event RunspaceReadyHandler RunspaceReady;


        private readonly SyncEvents _syncEvents = new SyncEvents();
        private Runspace _runSpace;

        private Pipeline _pipeline;

        public Command DefaultOutputCommand { get; private set; }
        public Command ContentOutputCommand { get; set; }


        private Queue<CallbackCommand> CommandQueue { get; }
        private Thread WorkerThread;


        protected InitialSessionState InitialSessionState => _runSpace.InitialSessionState;

        protected RunspaceConfiguration RunspaceConfiguration => _runSpace.RunspaceConfiguration;

        protected RunspaceStateInfo RunspaceStateInfo => _runSpace.RunspaceStateInfo;

        private readonly Host _host;

        private CallbackCommand _promptSequence;

        public RunspaceProxy(Host host)
        {
            _host = host;
            CommandQueue = new Queue<CallbackCommand>();

            _promptSequence = new CallbackCommand(
                new[]
                {
                    new Command("New-Paragraph", false, true),
                    new Command("Prompt", false, true)
                },
                onFinished:
                    result =>
                    {
                        var str = new StringBuilder();

                        foreach (var obj in result.Output)
                        {
                            str.Append(obj);
                        }

                        var prompt = str.ToString();
                        host.PoshConsole.SetPrompt(prompt);
                    },
                secret: true,
                defaultOutput: false
                );
        }

        public bool IsInitialized => _runSpace != null;

        public void Initialize() { 
            // pre-create reusable commands
            DefaultOutputCommand = new Command("Tee-Default");
            //// for now, merge the errors with the rest of the output
            //DefaultOutputCommand.MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
            //DefaultOutputCommand.MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Error |
            //                                                            PipelineResultTypes.Output;

            // pre-create this
            ContentOutputCommand = new Command("Out-PoshConsole");
            //// for now, merge the errors with the rest of the output
            //ContentOutputCommand.MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
            //ContentOutputCommand.MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Error |
            //                                                            PipelineResultTypes.Output;

            // Create the default initial session state and add the module.
            var iss = InitialSessionState.CreateDefault2();

            var poshModule = Assembly.GetExecutingAssembly();
            var currentUserProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WindowsPowerShell");
            var programFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsPowerShell");
            var systemProfilePath = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell\\v1.0");

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("%PSModulePath%")))
            {
                Environment.SetEnvironmentVariable("PSModulePath", string.Format("{0};{1};{2}",
                    Path.Combine(currentUserProfilePath, "Modules"),
                    Path.Combine(systemProfilePath, "Modules"),
                    Path.Combine(programFilesPath, "Modules")));
            }

            // We need STA so we can do WPF stuff from our console thread.
            iss.ApartmentState = ApartmentState.STA;
            // We need ReuseThread so that we behave the way that PowerShell.exe and ISE do.
            iss.ThreadOptions = PSThreadOptions.ReuseThread;
            // iss.Variables
            // Load all the Cmdlets that are in this assembly automatically.
            foreach (var t in poshModule.GetTypes())
            {
                var cmdlets = t.GetCustomAttributes(typeof(CmdletAttribute), false) as CmdletAttribute[];

                if (cmdlets != null)
                {
                    foreach (var cmdlet in cmdlets)
                    {
                        iss.Commands.Add(new SessionStateCmdletEntry( $"{cmdlet.VerbName}-{cmdlet.NounName}", t, $"{t.Name}.xml"));
                    }
                }
            }
            // And pre-import the modules from our app's module folder
            var path = Path.GetDirectoryName(poshModule.Location);
            iss.ImportPSModulesFromPath(Path.Combine(path, "Modules"));

            var profile = new PSObject(Path.GetFullPath(Path.Combine(currentUserProfilePath, _host.Name + "_profile.ps1")));
            //* %windir%\system32\WindowsPowerShell\v1.0\profile.ps1
            //  This profile applies to all users and all shells.
            profile.Properties.Add(new PSNoteProperty("AllUsersAllHosts", Path.GetFullPath(Path.Combine(systemProfilePath, "Profile.ps1"))));
            //* %windir%\system32\WindowsPowerShell\v1.0\PoshConsole_profile.ps1
            //  This profile applies to all users, but only to the Current shell.
            profile.Properties.Add(new PSNoteProperty("AllUsersCurrentHost", Path.GetFullPath(Path.Combine(systemProfilePath, _host.Name + "_profile.ps1"))));
            //* %UserProfile%\My Documents\WindowsPowerShell\profile.ps1
            //  This profile applies only to the current user, but affects all shells.
            profile.Properties.Add(new PSNoteProperty("CurrentUserAllHosts", Path.GetFullPath(Path.Combine(currentUserProfilePath, "Profile.ps1"))));
            //* %UserProfile%\My Documents\WindowsPowerShell\PoshConsole_profile.ps1
            //  This profile applies only to the current user and the Current shell.
            profile.Properties.Add(new PSNoteProperty("CurrentUserCurrentHost", profile.ImmediateBaseObject));

            iss.Variables.Add(new SessionStateVariableEntry("profile", profile, "The enumeration of all the available profiles the user could edit."));

            // I'm not sure why, but the default InitialSessionState doesn't match PowerShell anymore?
            /*            
            iss.Assemblies.Clear();
            var sma = typeof(PSParser).Assembly;
            iss.Assemblies.Add(new SessionStateAssemblyEntry(sma.FullName, sma.CodeBase));
            */

            _runSpace = RunspaceFactory.CreateRunspace(_host, iss);

            // TODO: can we handle profiles this way?
            /*
               RunspaceConfiguration conf = RunspaceConfiguration.Create();
               conf.InitializationScripts.Append(new ScriptConfigurationEntry("ImportPoshWpf", "$Foo = 'This is foo'")); // Import-Module .\\PoshWPF.dll
               _runSpace = RunspaceFactory.CreateRunspace(host, conf);
            */

            // Set the default runspace, so that event handlers (and Tasks) can run in the same runspace as commands.
            Runspace.DefaultRunspace = _runSpace;

            // we could hypothetically make several threads to do this work...
            WorkerThread = new Thread(ThreadRun) { Name = "CommandRunner" };
            WorkerThread.SetApartmentState(ApartmentState.STA);
            WorkerThread.Start();
        }

        private void StartRunspace()
        {
            _runSpace.Open();
            ExecuteStartupProfile();
        }


        /// <summary>
        /// The ThreadStart delegate
        /// </summary>
        public void ThreadRun()
        {
            StartRunspace();
            CallbackCommand boundCommand;
            //_ExitException = null;

            int sync = WaitHandle.WaitAny(_syncEvents.NewItemEvents);
            while (sync > 0)
            {
                Trace.WriteLineIf(ThreadTrace.TraceVerbose, "Signalled. Items in queue: " + CommandQueue.Count, "threading");
                while (CommandQueue.Count > 0)
                {
                    lock (((ICollection)CommandQueue).SyncRoot)
                    {
                        boundCommand = CommandQueue.Dequeue();
                    }

                    Pipeline pipeline;

                    if (boundCommand.ScriptCommand)
                    {
                        var command = boundCommand.Commands.First();
                        pipeline = _runSpace.CreatePipeline(command.CommandText, !boundCommand.Secret);
                    }
                    else
                    {
                        pipeline = _runSpace.CreatePipeline();

                        foreach (var command in boundCommand.Commands)
                        {
                            pipeline.Commands.Add(command);
                        }
                    }

                    if (boundCommand.DefaultOutput)
                    {
                        pipeline.Commands.Add(DefaultOutputCommand);
                    }

                    // Trace.WriteLineIf(threadTrace.TraceVerbose, "Executing " + pipeline.Commands[0] + "... Items remaining: " + _CommandQueue.Count.ToString(), "threading");

                    _pipeline = pipeline;

                    if (!boundCommand.Secret)
                    {
                        _host.UI.WriteLine(boundCommand.ToString());
                    }

                    // This is a dynamic anonymous delegate so that we can encapsulate the callback
                    var callbackCommand = boundCommand;

                    _pipeline.StateChanged += 
                        (sender, e) =>
                        {
                            Trace.WriteLine("Pipeline is " + e.PipelineStateInfo.State);

                            if (e.PipelineStateInfo.IsDone())
                            {
                                Trace.WriteLine("Pipeline is Done");

                                var completed = Interlocked.Exchange(ref _pipeline, null);
                                if (completed != null)
                                {
                                    var failure = e.PipelineStateInfo.Reason;

                                    if (failure != null)
                                    {
                                        Debug.WriteLine(failure.GetType(), "PipelineFailure");
                                        Debug.WriteLine(failure.Message, "PipelineFailure");
                                    }

                                    // Collect output for event before disposing of pipeline
                                    callbackCommand.OnFinished(PipelineFinishedEventArgs.FromPipeline(completed, e.PipelineStateInfo));
                                    completed.Dispose();
                                    //_SyncEvents.PipelineFinishedEvent.Set();
                                }
                            }
                        };

                    // I thought that maybe invoke instead of InvokeAsync() would stop the (COM) thread problems
                    // it didn't, but it means I don't need the sync, so I might as well leave it...
                    try
                    {
                        _pipeline.Invoke();
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



        public void Enqueue(CallbackCommand command)
        {
            lock (((ICollection)CommandQueue).SyncRoot)
            {
                CommandQueue.Enqueue(command);

                if (!command.Secret)
                {
                    CommandQueue.Enqueue(_promptSequence);
                }
            }
            _syncEvents.NewItemEvent.Set();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _syncEvents.ExitThreadEvent.Set();
            WorkerThread.Join(3000);
            _runSpace.Dispose();
        }

        public void StopPipeline()
        {
            if (_pipeline != null && _pipeline.PipelineStateInfo.State == PipelineState.Running)
            {
                _pipeline.StopAsync();
            }

        }

        public bool IsRemote => _runSpace?.ConnectionInfo != null;

        public CommandCompletion CompleteInput(string input, int cursorIndex)
        {

            System.Management.Automation.PowerShell ps;
            if (!IsRemote)
            {
                ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace);
            }
            else
            {
                ps = System.Management.Automation.PowerShell.Create();
                ps.Runspace = _runSpace;
            }
            return CommandCompletion.CompleteInput(input, cursorIndex, null, ps);
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
            var shutDownProfiles = new[]
            {
               // Global Exit Profiles
               Path.GetFullPath(Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\profile_exit.ps1")),
               Path.GetFullPath(Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\PoshConsole_profile_exit.ps1")),
               // User Exit Profiles
               Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WindowsPowerShell\profile_exit.ps1")),
               Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WindowsPowerShell\PoshConsole_profile_exit.ps1"))
            }.Where(File.Exists).Select(path => new Command(path, false, true)).ToArray();

            //StringBuilder cmd = new StringBuilder();

            if (shutDownProfiles.Any())
            {
                Enqueue(
                    new CallbackCommand( shutDownProfiles, onFinished: result =>
                        {
                            var failure = result.Failure as RuntimeException;
                            if (failure != null)
                            {
                                PoshConsole.CurrentConsole.WriteErrorRecord(failure.ErrorRecord);
                            }

                            ShouldExit?.Invoke(this, exitCode);
                        })
                    );


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
                ShouldExit?.Invoke(this, exitCode);
            }


            //else
            //{
            //   ExecutePromptFunction();
            //}
        }

        /// <summary>
        /// Executes the startup profile(s).
        /// </summary>
        private void ExecuteStartupProfile()
        {
            // we're going to ensure the startup profile goes _first_
            lock (((ICollection) CommandQueue).SyncRoot)
            {
                CallbackCommand[] commands = new CallbackCommand[CommandQueue.Count];
                CommandQueue.CopyTo(commands, 0);
                CommandQueue.Clear();

                var existing = (
                    from profileVariable in InitialSessionState.Variables["profile"]
                    from pathProperty in ((PSObject) profileVariable.Value).Properties.Match("*Host*", PSMemberTypes.NoteProperty)
                    where File.Exists(pathProperty.Value.ToString())
                    select pathProperty.Value.ToString()
                    ).Select(path => new Command(path, false, true)).ToArray();
                // This might be nice to have too (in case anyone was using it):
                _runSpace.SessionStateProxy.SetVariable("profiles", existing.ToArray());

                if (existing.Any())
                {
                    CommandQueue.Enqueue(new CallbackCommand(existing, true, true,
                        ignored => RunspaceReady?.Invoke(this, _runSpace.RunspaceStateInfo.State)));
                        // this is super important
                }

                CommandQueue.Enqueue(new CallbackCommand(Resources.Prompt, true, true));

                foreach (var command in commands)
                {
                    CommandQueue.Enqueue(command);
                }
            }
        }

    }
}
