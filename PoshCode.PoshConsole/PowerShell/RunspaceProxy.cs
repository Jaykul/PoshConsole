
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
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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

        private Runspace _runSpace;
        private Pipeline _pipeline;

        public Command DefaultOutputCommand { get; private set; }

        private BufferBlock<PoshConsolePipeline> CommandQueue { get; }
        private Thread WorkerThread;


        protected InitialSessionState InitialSessionState => _runSpace.InitialSessionState;

        protected RunspaceConfiguration RunspaceConfiguration => _runSpace.RunspaceConfiguration;

        protected RunspaceStateInfo RunspaceStateInfo => _runSpace.RunspaceStateInfo;

        private readonly Host _host;
        private CancellationTokenSource _cancellationSource;

        public RunspaceProxy(Host host)
        {
            _host = host;
            CommandQueue = new BufferBlock<PoshConsolePipeline>();
        }

        public bool IsInitialized => _runSpace != null;

        public void Initialize() { 
            // pre-create reusable commands
            DefaultOutputCommand = new Command("Tee-Default");
            //// for now, merge the errors with the rest of the output
            //DefaultOutputCommand.MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
            //DefaultOutputCommand.MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Error |
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
            iss.Commands.Add(new SessionStateFunctionEntry("prompt", Resources.Prompt));
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


            _cancellationSource = new CancellationTokenSource();

            // we could hypothetically make several threads to do this work...
            WorkerThread = new Thread(ThreadRun) { Name = "CommandRunner" };
            WorkerThread.SetApartmentState(ApartmentState.STA);
            WorkerThread.Start(_cancellationSource.Token);
        }


        /// <summary>
        /// The ThreadStart delegate
        /// </summary>
        public void ThreadRun(object cancellationToken)
        {
            _runSpace.Open();
            ExecuteStartupProfile();

            // this is super important
            RunspaceReady?.Invoke(this, _runSpace.RunspaceStateInfo.State);

            // Run the prompt for the first time
            InvokePipeline(new PoshConsolePipeline("Prompt", output: ConsoleOutput.OutputOnly));


            var cancel = (CancellationToken) cancellationToken;
            do
            {
                try
                {
                    var pcPipeline = CommandQueue.Receive(cancel);

                    if (pcPipeline.Output == ConsoleOutput.CommandOnly || pcPipeline.Output == ConsoleOutput.Default)
                    {
                        _host.UI.WriteLine(pcPipeline.ToString());
                    }

                    InvokePipeline(pcPipeline);
                    // Secret commands have no visible output, and thus don't need reprompting
                    if (pcPipeline.Output != ConsoleOutput.None)
                    {
                        InvokePrompt();
                    }
                }
                catch (OperationCanceledException)
                {
                    if (_runSpace.RunspaceStateInfo.State != RunspaceState.Closing
                        && _runSpace.RunspaceStateInfo.State != RunspaceState.Closed)
                    {
                        _runSpace.Close();
                    }
                    break;
                }
            } while (true);
        }

        private void InvokePipeline(PoshConsolePipeline pcp)
        {
            _pipeline = GetPowerShellPipeline(pcp);
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
                            // Collect output for event before disposing of pipeline
                            PipelineFinished(pcp, e, completed);

                            if (pcp.Output != ConsoleOutput.None)
                            {
                                PoshConsole.CurrentConsole.OnCommandFinished(pcp.Commands, e.PipelineStateInfo.State);
                            }

                            completed.Dispose();
                            //_SyncEvents.PipelineFinishedEvent.Set();
                        }
                    }
                };

            try
            {
                if (pcp.Input == null)
                {
                    _pipeline.Invoke();
                }
                else
                {
                    _pipeline.Invoke(pcp.Input);
                }
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
        }

        private void InvokePrompt()
        {
            PoshConsole.CurrentConsole.NewParagraph();
            var pipeline = _runSpace.CreatePipeline("Prompt");
            var output = pipeline.Invoke();

            // NOTE: The default host doesn't write errors for prompt functions

            var str = new StringBuilder();

            foreach (var obj in output)
            {
                str.Append(obj);
            }

            var prompt = str.ToString();
            PoshConsole.CurrentConsole.SetPrompt(prompt);
        }

        private Pipeline GetPowerShellPipeline(PoshConsolePipeline boundCommand)
        {
            Pipeline pipeline;

            if (boundCommand.IsScript)
            {
                var command = boundCommand.Commands.First();
                pipeline = _runSpace.CreatePipeline(command.CommandText, boundCommand.Output == ConsoleOutput.Default || boundCommand.Output == ConsoleOutput.CommandOnly );
            }
            else
            {
                pipeline = _runSpace.CreatePipeline();

                foreach (var command in boundCommand.Commands)
                {
                    pipeline.Commands.Add(command);
                }
            }

            if(boundCommand.Output == ConsoleOutput.Default || boundCommand.Output == ConsoleOutput.OutputOnly)
            {
                pipeline.Commands.Add(DefaultOutputCommand);
            }

            return pipeline;
        }

        private static void PipelineFinished(PoshConsolePipeline commands, PipelineStateEventArgs e, Pipeline pipeline)
        {
            // collect output
            var errors = pipeline.Error.ReadToEnd();
            var results = pipeline.Output.ReadToEnd();
            var failure = e.PipelineStateInfo.Reason;

            if (commands.Output == ConsoleOutput.CommandOnly || commands.Output == ConsoleOutput.Default)
            {
                PoshConsole.CurrentConsole.WriteErrorRecords(errors);
            }

            // Fail the task, if applicable
            if (failure != null)
            {
                errors.Insert(0, failure);

                Debug.WriteLine(failure.GetType(), "PipelineFailure");
                Debug.WriteLine(failure.Message, "PipelineFailure");
                if (commands.Output != ConsoleOutput.None)
                {
                    PoshConsole.CurrentConsole.WriteErrorRecord(((RuntimeException) failure).ErrorRecord);
                }
                // commands.TaskSource.SetException(failure);
            }

            commands.TaskSource.SetResult(
                new PoshConsolePipelineResults(pipeline.InstanceId, commands.Commands, results, errors, pipeline.PipelineStateInfo.State));


        }


        public void Enqueue(PoshConsolePipeline pipeline)
        {
            // Even though CommandQueue is threaded
            // We NEED to ensure that our sets of commands stay together
            CommandQueue.Post(pipeline);
        }



        public Task<PoshConsolePipelineResults> Invoke(PoshConsolePipeline pipeline)
        {
            // Even though CommandQueue is threaded
            // We NEED to ensure that our sets of commands stay together
            CommandQueue.Post(pipeline);
            return pipeline.Task;
        }

        public Task<PoshConsolePipelineResults> Invoke(IList<Command> commands, IEnumerable input = null, ConsoleOutput output = ConsoleOutput.Default)
        {
            var pipeline = new PoshConsolePipeline(commands, input, output);
            // Even though CommandQueue is threaded
            // We NEED to ensure that our sets of commands stay together
            CommandQueue.Post(pipeline);
            return pipeline.Task;
        }



        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _cancellationSource.Cancel();
            // WorkerThread.Abort(exitCode);

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
        internal async void ExecuteShutdownProfile(int exitCode)
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

            try
            {
                if (shutDownProfiles.Any())
                {
                    await Invoke(shutDownProfiles);
                }
            }
            catch (RuntimeException failure)
            {
                PoshConsole.CurrentConsole.WriteErrorRecord(failure.ErrorRecord);
            }
            finally
            {
                ShouldExit?.Invoke(this, exitCode);
            }
        }

        /// <summary>
        /// Executes the startup profile(s).
        /// </summary>
        private void ExecuteStartupProfile()
        {
            // Before we run the user's profile, set the prompt function to a better default

            var existing = (
                from profileVariable in InitialSessionState.Variables["profile"]
                from pathProperty in
                    ((PSObject)profileVariable.Value).Properties.Match("*Host*", PSMemberTypes.NoteProperty)
                where File.Exists(pathProperty.Value.ToString())
                select pathProperty.Value.ToString()
                ).Select(path => new Command(path, false, true)).ToArray();

            if (existing.Any())
            {
                // go around the thread runner ...
                InvokePipeline(new PoshConsolePipeline(existing, output: ConsoleOutput.OutputOnly));
            }
            else
            {
                // if there's no profile, run the prompt instead
                InvokePrompt();
            }
        }
    }

    public enum ConsoleOutput
    {
        None = 0,
        OutputOnly = 1,
        CommandOnly = 2,
        Default = 3
    }
}
