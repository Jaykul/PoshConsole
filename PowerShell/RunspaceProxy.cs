
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Host;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PoshCode.PowerShell
{
    internal delegate void PipelineOutputHandler(PipelineExecutionResult result);

	public class RunspaceProxy
	{
		private Runspace _runSpace;
		private Pipeline _pipeline;
		public Command DefaultOutputCommand { get; private set; }

		// TODO: Do we need to queue things like PoshConsole does?
		// public Queue<InputBoundCommand> CommandQueue { get; private set; }
		public RunspaceProxy(PSHost host)
		{
			// CommandQueue = new Queue<InputBoundCommand>();

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
		    string programFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsPowerShell");
			string systemProfilePath = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell\\v1.0");


			// Import the PoshWPF module automatically
			// iss.ImportPSModule(new[] { Path.Combine(Path.GetDirectoryName(poshModule.Location), "PoshWpf.dll") });

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
			iss.ThreadOptions = PSThreadOptions.ReuseThread;
			// iss.Variables
			// Load all the Cmdlets that are in this assembly automatically.
			foreach (Type t in poshModule.GetTypes())
			{
				var cmdlets = t.GetCustomAttributes(typeof(CmdletAttribute), false) as CmdletAttribute[];

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

			_runSpace = RunspaceFactory.CreateRunspace(host, iss);
			// TODO: can we handle

			/*
			   RunspaceConfiguration conf = RunspaceConfiguration.Create();
			   conf.InitializationScripts.Append(new ScriptConfigurationEntry("ImportPoshWpf", "$Foo = 'This is foo'")); // Import-Module .\\PoshWPF.dll
			   _runSpace = RunspaceFactory.CreateRunspace(host, conf);
			*/

			// Set the default runspace, so that event handlers (and Tasks) can run in the same runspace as commands.
			Runspace.DefaultRunspace = _runSpace;
		}


        //private void On_StateChanged(object sender, PipelineStateEventArgs e)
        //{
        //    Trace.WriteLine("Pipeline is " + e.PipelineStateInfo.State);
        //    if (e.PipelineStateInfo.IsDone())
        //    {
        //        Trace.WriteLine("Pipeline is Done");
        //        Pipeline completed = Interlocked.Exchange(ref _pipeline, null);
        //        if (completed != null)
        //        {
        //            Exception failure = e.PipelineStateInfo.Reason;
        //            if (failure != null)
        //            {
        //                Debug.WriteLine(failure.GetType(), "PipelineFailure");
        //                Debug.WriteLine(failure.Message, "PipelineFailure");
        //            }
        //            Collection<Object> errors = completed.Error.ReadToEnd();
        //            Collection<PSObject> results = completed.Output.ReadToEnd();
        //            completed.Dispose();
        //            //_SyncEvents.PipelineFinishedEvent.Set();
        //            new PipelineExecutionResult(results, errors, failure, e.PipelineStateInfo.State);
        //        }
        //    }
        //}
        //public PipelineExecutionResult InvokeAsync(String command, bool addToHistory = true)
        //{
        //    Pipeline pipeline = _runSpace.CreatePipeline(command, addToHistory);
        //    //for (int c = 1; c < boundCommand.Commands.Length; c++)
        //    //{
        //    //	pipeline.Commands.Add(new Command(boundCommand.Commands[c], boundCommand.RunAsScript,
        //    //									  boundCommand.UseLocalScope));
        //    //}
        //    //if (boundCommand.DefaultOutput)
        //    //{
        //    //	pipeline.Commands.Add(DefaultOutputCommand);
        //    //}

        //    // Trace.WriteLineIf(threadTrace.TraceVerbose, "Executing " + pipeline.Commands[0] + "... Items remaining: " + _CommandQueue.Count.ToString(), "threading");

        //    _pipeline = pipeline;
			
        //    // This is a dynamic anonymous delegate so that it can access the Callback parameter
        //    _pipeline.StateChanged += (EventHandler<PipelineStateEventArgs>)On_StateChanged;

        //    // I thought that maybe invoke instead of InvokeAsync() would stop the (COM) thread problems
        //    // it didn't, but it means I don't need the sync, so I might as well leave it...
        //    try
        //    {
        //        _pipeline.InvokeAsync(boundCommand.Input);
        //    }
        //    catch (Exception ipe)
        //    {
        //        // TODO: Handle IncompleteParseException with some elegance!
        //        //    klumsy suggested we could prevent these by using the tokenizer 
        //        // Tokenizing in OnEnterPressed (before sending it to the CommandRunner)
        //        //    would allow us to let {Enter} be handled nicely ... 
        //        // Tokenizing in KeyDown would let us do live syntax highlighting,
        //        //    is it fast enough to work?
        //        Debug.WriteLine(ipe.Message);
        //    }

        //}
	}
}
