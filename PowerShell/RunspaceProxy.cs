
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


        private SyncEvents _syncEvents = new SyncEvents();
	    private readonly Runspace _runSpace;

	    public Pipeline _pipeline;

	    public Command DefaultOutputCommand { get; private set; }
	    public Command ContentOutputCommand { get; set; }


		protected Queue<CallbackCommand> CommandQueue { get; private set; }
        protected Thread WorkerThread;



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

	    public RunspaceProxy(Host host)
		{
			CommandQueue = new Queue<CallbackCommand>();


            // pre-create reusable commands
            DefaultOutputCommand = new Command("Out-Default");
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
			InitialSessionState iss = InitialSessionState.CreateDefault();

			Assembly poshModule = Assembly.GetExecutingAssembly();
			string currentUserProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WindowsPowerShell");
		    string programFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsPowerShell");
			string systemProfilePath = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell\\v1.0");

			if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("%PSModulePath%")))
			{
				Environment.SetEnvironmentVariable("PSModulePath", string.Format("{0};{1};{2}",
					Path.Combine(currentUserProfilePath, "Modules"),
					Path.Combine(systemProfilePath, "Modules"),
					Path.Combine(programFilesPath, "Modules")));
			}

			// We need STA so we can do WPF stuff from our console thread.
			iss.ApartmentState = ApartmentState.STA;
			// We need ReuseThread so that we behave, well, the way that PowerShell.exe and ISE do.
            // TODO: we might need to UseCurrentThread so the output can go in our UI
			iss.ThreadOptions = PSThreadOptions.ReuseThread;
			// iss.Variables
			// Load all the Cmdlets that are in this assembly automatically.
			foreach (Type t in poshModule.GetTypes())
			{
				var cmdlets = t.GetCustomAttributes(typeof(CmdletAttribute), false) as CmdletAttribute[];

				if (cmdlets != null)
				{
					foreach (var cmdlet in cmdlets)
					{
						iss.Commands.Add(new SessionStateCmdletEntry(
											string.Format("{0}-{1}", cmdlet.VerbName, cmdlet.NounName), t,
											string.Format("{0}.xml", t.Name)));
					}
				}
			}
            // And pre-import the modules from our app's module folder
            string path = Path.GetDirectoryName(poshModule.Location);
		    iss.ImportPSModulesFromPath(Path.Combine(path, "Modules"));

			var profile = new PSObject(Path.GetFullPath(Path.Combine(currentUserProfilePath, host.Name + "_profile.ps1")));
			//* %windir%\system32\WindowsPowerShell\v1.0\profile.ps1
			//  This profile applies to all users and all shells.
			profile.Properties.Add(new PSNoteProperty("AllUsersAllHosts",
													  Path.GetFullPath(Path.Combine(systemProfilePath, "Profile.ps1"))));
			//* %windir%\system32\WindowsPowerShell\v1.0\PoshConsole_profile.ps1
			//  This profile applies to all users, but only to the Current shell.
			profile.Properties.Add(new PSNoteProperty("AllUsersCurrentHost",
													  Path.GetFullPath(Path.Combine(systemProfilePath,
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

            // I'm not sure why, but the default InitialSessionState doesn't match PowerShell anymore?
	        iss.Assemblies.Clear();
            var sma = typeof (PSParser).Assembly;
            iss.Assemblies.Add( new SessionStateAssemblyEntry(sma.FullName, sma.CodeBase));

			_runSpace = RunspaceFactory.CreateRunspace(host, iss);

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

					var pipeline = _runSpace.CreatePipeline(string.Empty, boundCommand.AddToHistory);

                    foreach(var command in boundCommand.Commands) {
						pipeline.Commands.Add(command);
					}

					if (boundCommand.DefaultOutput)
					{
						pipeline.Commands.Add(DefaultOutputCommand);
					}

					// Trace.WriteLineIf(threadTrace.TraceVerbose, "Executing " + pipeline.Commands[0] + "... Items remaining: " + _CommandQueue.Count.ToString(), "threading");

					_pipeline = pipeline;

					// This is a dynamic anonymous delegate so that it can access the Callback parameter
					_pipeline.StateChanged +=
					   (EventHandler<PipelineStateEventArgs>)delegate(object sender, PipelineStateEventArgs e) // =>
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
			var existingProfiles = new[]
            {
               // Global Exit Profiles
               Path.GetFullPath(Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\profile_exit.ps1")), 
               Path.GetFullPath(Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\PoshConsole_profile_exit.ps1")),
               // User Exit Profiles
               Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WindowsPowerShell\profile_exit.ps1")), 
               Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WindowsPowerShell\PoshConsole_profile_exit.ps1")),
            }.Where(File.Exists).Select(path => new Command(path, true, true)).ToArray();

			//StringBuilder cmd = new StringBuilder();

			if (existingProfiles.Any())
			{
				Enqueue(
                    new CallbackCommand(
                        existingProfiles,
                        result =>
                        {
                            if (result.Failure != null)
							{
								// WriteErrorRecord(((RuntimeException)(result.Failure)).ErrorRecord);
							}

                            if (ShouldExit != null) 
                                ShouldExit(this, exitCode);
                        }) { AddToHistory = false, RunAsScript = false });


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
                    ShouldExit(this, exitCode);
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

			Enqueue(new CallbackCommand(new[] { new Command(Resources.Prompt,true,true) }, false, null));

		    var existing = (
                from profileVariable in InitialSessionState.Variables["profile"]
		        from pathProperty in ((PSObject) profileVariable.Value).Properties.Match("*Host*", PSMemberTypes.NoteProperty)
		        where File.Exists(pathProperty.Value.ToString())
		        select pathProperty.Value.ToString()
            ).Select(path => new Command(path, true, true)).ToArray();
			// This might be nice to have too (in case anyone was using it):
			_runSpace.SessionStateProxy.SetVariable("profiles", existing.ToArray());

			if (existing.Any())
			{
				Enqueue(new CallbackCommand(
						   existing,
						   false,
						   ignored => RunspaceReady(this, _runSpace.RunspaceStateInfo.State))); // this is super important
			}
			else
			{
				Enqueue(new CallbackCommand(
						   new[] { new Command("New-Paragraph", true, true) },
						   false,
						   ignored => RunspaceReady(this, _runSpace.RunspaceStateInfo.State))); // this is super important
			}
		}

    }
}
