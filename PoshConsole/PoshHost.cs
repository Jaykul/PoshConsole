//
// Copyright (c) 2006 Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
// PARTICULAR PURPOSE.
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Globalization;
using System.Windows.Input;
using System.Threading;
using System.Collections.ObjectModel;
using System.Windows;
using System.IO;

namespace Huddled.PoshConsole
{
    /// <summary>
    /// A sample implementation of the PSHost abstract class for console
    /// applications. Not all members are implemented. Those that are 
    /// not implemented throw a NotImplementedException exception.
    /// </summary>
    public partial class PoshHost : PSHost
    {
        /// <summary>
        /// The PSHostUserInterface implementation
        /// </summary>
        private PoshUI myUI;
        private PoshRawUI myRawUI;
        /// <summary>
        /// The runspace for this interpeter.
        /// </summary>
        private Runspace myRunSpace;

        /// <summary>
        /// The currently executing pipeline 
        /// (So it can be stopped by Control-C or Break).
        /// </summary>
        private Pipeline currentPipeline;
        
        /// <summary>
        /// A Console window wrapper that hides the console
        /// </summary>
        private NativeConsole console = null;

        /// <summary>
        /// Used to serialize access to instance data...
        /// </summary>
        private object instanceLock = new object();

        /// <summary>
        /// A ConsoleRichTextBox for output
        /// </summary>
        private IPoshConsoleControl buffer;
        private IPSUI PsUi;

        public bool IsClosing = false;


        // Universal Delegates
        delegate void voidDelegate();

        //private IInput inputHandler = null; 
        internal PoshOptions Options;
        //internal List<string> StringHistory;
        public PoshHost(IPSUI PsUi)
        {
            this.buffer = PsUi.Console;
            //StringHistory = new List<string>();
            Options = new PoshOptions(this, buffer);
            this.PsUi = PsUi;
           
            try
            {
                // we have to be careful here, because this is an interface member ...
                // but in the current implementation, buffer.RawUI returns buffer
                myRawUI = new PoshRawUI(buffer.RawUI);
                myUI = new PoshUI(myRawUI, PsUi);

                // precreate this
                outDefault = new Command("Out-Default");
                // for now, merge the errors with the rest of the output
                outDefault.MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
                outDefault.MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Error | PipelineResultTypes.Output;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't create PowerShell interface, are you sure PowerShell is installed? \n" + ex.Message + "\nAt:\n" + ex.Source, "Error Starting PoshConsole", MessageBoxButton.OK, MessageBoxImage.Stop);
                throw;
            }

            buffer.Expander.TabComplete += new TabExpansionLister(buffer_TabComplete);
            buffer.ProcessCommand += new CommandHandler(OnGotUserInput);
            //buffer.CommandEntered +=new ConsoleRichTextBox.CommandHandler(buffer_CommandEntered);

            //buffer.GetHistory +=new ConsoleRichTextBox.HistoryHandler(buffer_GetHistory);

            // this.ShouldExit += new ExitHandler(WeShouldExit);
            //myUI.ProgressUpdate += new PoshUI.WriteProgressDelegate( delegate(long sourceId, ProgressRecord record){if(ProgressUpdate!=null) ProgressUpdate(sourceId, record);} );
            //myUI.Input += new PoshUI.InputDelegate(GetInput);
            //myUI.Output += new PoshUI.OutputDelegate(OnOutput);
            //myUI.OutputLine += new PoshUI.OutputDelegate(OnOutputLine);
            //myUI.WritePrompt += new PoshUI.PromptDelegate(WritePrompt);

            // Some delegates we think we can get away with making only once...
            Properties.Settings.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(SettingsPropertyChanged);
            // Properties.Settings.Default.SettingChanging += new System.Configuration.SettingChangingEventHandler(SettingsSettingChanging);
            // Properties.Colors.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(ColorsPropertyChanged);

            myRunSpace = RunspaceFactory.CreateRunspace(this);
            myRunSpace.StateChanged += new EventHandler<RunspaceStateEventArgs>(myRunSpace_StateChanged);
            myRunSpace.OpenAsync();
            MakeConsole();

            //// Finally, STARTUP!
            //ExecuteStartupProfile();
        }

        void myRunSpace_StateChanged(object sender, RunspaceStateEventArgs e)
        {
            if (e.RunspaceStateInfo.State == RunspaceState.Opened)
            {
                ExecuteStartupProfile();
            }
        }

        /// <summary>
        /// Handler for the IInput.GotUserInput event.
        /// </summary>
        /// <param name="commandLine">The command line.</param>
        void OnGotUserInput(string commandLine)
        {
            if (native == 0)
            {
                Execute(commandLine);
            }
            else
            {
                if (commandLine[commandLine.Length - 1].Equals('\n'))
                {
                    console.WriteInput(commandLine);
                } else console.WriteInput(commandLine + '\n');
            } 
        }

        private void MakeConsole()
        {
            if (console == null)
            {
                console = new NativeConsole();
                console.WriteOutputLine += new NativeConsole.OutputDelegate(delegate(string error) { buffer.WriteNativeLine(error.TrimEnd('\n')); });
                console.WriteErrorLine += new NativeConsole.OutputDelegate(delegate(string error) { buffer.WriteNativeErrorLine(error.TrimEnd('\n')); });
            }
        }

        public void KillConsole()
        {
            if( console != null ) console.Dispose();
            console = null;
        }

        #region ConsoleRichTextBox Event Handlers

        /// <summary>
        /// Indicate to the host application that exit has
        /// been requested. Pass the exit code that the host
        /// application should use when exiting the process.
        /// </summary>
        /// <param name="exitCode"></param>
        public override void SetShouldExit(int exitCode)
        {
            IsClosing = true; 
            // Application.Current.Shutdown(exitCode);
            PsUi.SetShouldExit(exitCode);
        }





        private List<string> buffer_TabComplete(string cmdline)
        {
            List<string> completions = new List<string>();
            Collection<PSObject> set;
            string lastWord = Utilities.GetLastWord(cmdline);

            // Still need to do more Tab Completion
            // TODO: Make "PowerTab" obsolete for PoshConsole users.
            // TODO: Make each TabExpansion optional -- maybe plugins?
            //   TODO: TabComplete Parameters
            //   TODO: TabComplete Variables
            //   TODO: TabComplete Aliases
            //   TODO: TabComplete Executables in (current?) path

            if (lastWord != null)
            {
                // TODO: TabComplete Cmdlets inside the pipeline
                foreach (RunspaceConfigurationEntry cmdlet in myRunSpace.RunspaceConfiguration.Cmdlets)
                {
                    if (cmdlet.Name.StartsWith(lastWord, true, null))
                    {
                        completions.Add(cmdlet.Name);
                    }
                }

                // TODO: TabComplete Paths
                try
                {
                    if (lastWord[0] == '$')
                    {
                        set = InvokePipeline("get-variable " + lastWord.Substring(1) + "*");
                        if (set != null)
                        {
                            foreach(PSObject opt in set)
                            {
                                PSVariable var = opt.ImmediateBaseObject as PSVariable;
                                if (var != null)
                                {
                                    completions.Add("$" + var.Name);
                                }
                            }
                        }
                        set = null;
                    }

                    set = InvokePipeline("resolve-path \"" + lastWord + "*\"");
                    if (set != null)
                    {
                        foreach (PSObject opt in set)
                        {
                            string completion = opt.ToString();
                            if (completion.Contains(" "))
                            {
                                completions.Add(string.Format("\"{0}\"", completion));
                            }
                            else completions.Add(completion);
                        }
                    }
                    set = null;

                    // Finally, call the TabExpansion string
                    set = InvokePipeline("TabExpansion '" + cmdline + "' '" + lastWord + "'");
                    if (set != null)
                    {
                        foreach (PSObject opt in set)
                        {
                            completions.Add(opt.ToString());
                        }
                    }
                }
                catch (RuntimeException)
                {
                    // hide the error
                }
            }
            return completions;
        }
        #endregion

        /// <summary>
        /// Method used to handle control-C's from the user. It calls the
        /// pipeline Stop() method to stop execution. If any exceptions occur,
        /// they are printed to the console; otherwise they are ignored.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.Input.ExecutedRoutedEventArgs"/> instance containing the event data.</param>
        //public void StopPipeline()
        //{
        //    lock (instanceLock)
        //    {
        //        if (currentPipeline != null && currentPipeline.PipelineStateInfo.State == PipelineState.Running)
        //        {
        //            if (native > 0)
        //            {
        //                console.WriteInput("\n" + (char)26);
        //            }
        //            else
        //            {
        //                currentPipeline.StopAsync();
        //            }
        //        }
        //        //else
        //        //    ApplicationCommands.Copy.Execute(null, buffer);
        //    }
        //}


        public void StopPipeline()
        {
            Pipeline pipe = currentPipeline;

            if (pipe != null && pipe.PipelineStateInfo.State == PipelineState.Running)
            {
                pipe.StopAsync();
            }
        }

        /// <summary>
        /// Executes the startup profile(s).
        /// </summary>
        internal void ExecuteStartupProfile()
        {
            //* %windir%\system32\WindowsPowerShell\v1.0\profile.ps1
            //  This profile applies to all users and all shells.
            //* %windir%\system32\WindowsPowerShell\v1.0\Huddled.PoshConsole_profile.ps1
            //  This profile applies to all users, but only to the Microsoft.PowerShell shell.
            //* %UserProfile%\My Documents\WindowsPowerShell\profile.ps1
            //  This profile applies only to the current user, but affects all shells.
            //* %UserProfile%\My Documents\WindowsPowerShell\Huddled.PoshConsole_profile.ps1
            //  This profile applies only to the current user and the Microsoft.PowerShell shell.

            StringBuilder cmd = new StringBuilder();
            foreach (string path in
                 new string[4] {
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.SystemDirectory , @"WindowsPowerShell\v1.0\profile.ps1")),
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.SystemDirectory , @"WindowsPowerShell\v1.0\Huddled.PoshConsole_profile.ps1")),
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WindowsPowerShell\profile.ps1")),
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WindowsPowerShell\Huddled.PoshConsole_profile.ps1")),
                })
            {
                if (File.Exists(path))
                {
                    cmd.AppendFormat(". \"{0}\";\n", path);
                }
            }
            if (cmd.Length > 0)
            {
                ExecutePipelineOutDefault(cmd.ToString(), false, 
                    (PipelineOutputHandler)delegate(PipelineExecutionResult result) 
                    {
                        buffer.CommandFinished(result.State); 
                        ExecutePromptFunction(); 
                    });

                //try
                //{
                //    ExecutePipelineOutDefault(cmd.ToString(), null, false);
                //}
                //catch (RuntimeException rte)
                //{
                //    // An exception occurred that we want to display ...
                //    // We have to run another pipeline, and pass in the error record.
                //    // The runtime will bind the input to the $input variable
                //    ExecuteHelper("write-host ($input | out-string) -fore darkyellow", rte.ErrorRecord, false);
                //}
            }
            else
            {
                ExecutePromptFunction();
            }
        }


        /// <summary>
        /// Executes the shutdown profile(s).
        /// </summary>
        internal void ExecuteShutdownProfile()
        {
            //* %windir%\system32\WindowsPowerShell\v1.0\profile_exit.ps1
            //  This profile applies to all users and all shells.
            //* %windir%\system32\WindowsPowerShell\v1.0\Huddled.PoshConsole_profile_exit.ps1
            //  This profile applies to all users, but only to the Huddled.PoshConsole shell.
            //* %UserProfile%\My Documents\WindowsPowerShell\profile_exit.ps1
            //  This profile applies only to the current user, but affects all shells.
            //* %UserProfile%\\My Documents\WindowsPowerShell\Huddled.PoshConsole_profile_exit.ps1
            //  This profile applies only to the current user and the Huddled.PoshConsole shell.

            StringBuilder cmd = new StringBuilder();
            foreach (string path in
                 new string[4] {
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.SystemDirectory , @"WindowsPowerShell\v1.0\profile_exit.ps1")),
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.SystemDirectory , @"WindowsPowerShell\v1.0\Huddled.PoshConsole_profile_exit.ps1")),
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WindowsPowerShell\profile_exit.ps1")),
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WindowsPowerShell\Huddled.PoshConsole_profile_exit.ps1")),
                })
            {
                if (File.Exists(path))
                {
                    cmd.AppendFormat(". \"{0}\";", path);
                }
            }
            if (cmd.Length > 0)
            {
                ExecutePipeline(new Command(cmd.ToString(), true, false), null);
                //try
                //{
                //    ExecuteHelper(cmd.ToString(), null, false);
                //}
                //catch (RuntimeException rte)
                //{
                //    // An exception occurred that we want to display ...
                //    // We have to run another pipeline, and pass in the error record.
                //    // The runtime will bind the input to the $input variable
                //    ExecuteHelper("write-host ($input | out-string) -fore darkyellow", rte.ErrorRecord, false);
                //}
            }
            else
            {
                ExecutePromptFunction();
            }
        }


        /// <summary>
        /// Basic script execution routine - any runtime exceptions are
        /// caught and passed back into the runtime to display.
        /// </summary>
        /// <param name="cmd">The command to execute</param>

        private void Execute(string cmd)
        {
            if(!string.IsNullOrEmpty(cmd))
            {
                ExecutePipelineOutDefault(cmd, true, (PipelineOutputHandler)delegate(PipelineExecutionResult result)
                {
                    if (result.Failure != null)
                    {
                        WriteErrorRecord(((RuntimeException)(result.Failure)).ErrorRecord);
                    }
                    if (!IsClosing)
                    {
                        buffer.CommandFinished(result.State);
                        ExecutePromptFunction();
                    }
                });
            }
            else if (!IsClosing)
            {
                buffer.CommandFinished(PipelineState.NotStarted);
                ExecutePromptFunction();
            }
        }

        private void WriteErrorRecord(ErrorRecord record)
        {
            buffer.WriteErrorRecord(record);
        }

        //void Execute(string cmd)
        //{
        //    try
        //    {
        //        // execute the command with no input...
        //        ExecuteHelper(cmd, null, true);
        //    }
        //    catch (RuntimeException rte)
        //    {
        //        // TODO: handle the "incomplete" commands by displaying an additional prompt?
        //        // An exception occurred that we want to display ...
        //        // We have to run another pipeline, and pass in the error record.
        //        // The runtime will bind the input to the $input variable
        //        ExecuteHelper("write-host ($input | out-string) -fore darkyellow", rte.ErrorRecord, false);
        //    }
        //}


        ///// <summary>
        ///// Invoke's the user's PROMPT function to display a prompt.
        ///// Called after each command completes
        ///// </summary>
        //internal void Prompt()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    try
        //    {
        //        Collection<PSObject> output = InvokePipeline("prompt");

        //        foreach (PSObject thing in output)
        //        {
        //            sb.Append(thing.ToString());
        //        }
        //        //sb.Append(PromptPadding);
        //    }
        //    catch (RuntimeException rte)
        //    {
        //        // An exception occurred that we want to display ...
        //        // We have to run another pipeline, and pass in the error record.
        //        // The runtime will bind the input to the $input variable
        //        ExecuteHelper("write-host \"ERROR: Your prompt function crashed!\n\" -fore darkyellow", null, false);
        //        ExecuteHelper("write-host ($input | out-string) -fore darkyellow", rte.ErrorRecord, false);
        //        sb.Append("\n> ");
        //    }
        //    finally
        //    {
        //        buffer.Prompt(sb.ToString());
        //    }
        //}


        private void ExecutePromptFunction()
        {
            //buffer.EndOutput();

            ExecutePipeline(new Command("Prompt"), (PipelineOutputHandler)delegate(PipelineExecutionResult result) 
            {
                StringBuilder str = new StringBuilder();

                foreach (PSObject obj in result.Output)
                {
                    str.Append(obj);
                }

                buffer.Prompt(str.ToString());
            });
        }





        protected int MaxBufferLength = 500;


        #region




        ///// <summary>
        ///// A helper method which builds and executes a pipeline that returns it's output.
        ///// </summary>
        ///// <param name="cmd">The script to run</param>
        ///// <param name="input">Any input arguments to pass to the script.</param>
        //public Collection<PSObject> InvokeHelper(string cmd, object input)
        //{
        //    Collection<PSObject> output = new Collection<PSObject>();
        //    if (_ready.WaitOne(10000, true))
        //    {
        //        // Ignore empty command lines.
        //        if (String.IsNullOrEmpty(cmd))
        //            return null;

        //        // Create the pipeline object and make it available
        //        // to the ctrl-C handle through the currentPipeline instance
        //        // variable.
        //        lock (instanceLock)
        //        {
        //            _ready.Reset();
        //            currentPipeline = myRunSpace.CreatePipeline(cmd, false);
        //        }

        //        // Create a pipeline for this execution. Place the result in the currentPipeline
        //        // instance variable so that it is available to be stopped.
        //        try
        //        {
        //            // currentPipeline.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);

        //            // If there was any input specified, pass it in, and execute the pipeline.
        //            if (input != null)
        //            {
        //                output = currentPipeline.Invoke(new object[] { input });
        //            }
        //            else
        //            {
        //                output = currentPipeline.Invoke();
        //            }

        //        }

        //        finally
        //        {
        //            // Dispose of the pipeline line and set it to null, locked because currentPipeline
        //            // may be accessed by the ctrl-C handler.
        //            lock (instanceLock)
        //            {
        //                currentPipeline.Dispose();
        //                currentPipeline = null;
        //            }
        //        }
        //        _ready.Set();
        //    }
        //    return output;
        //}


        ManualResetEvent _ready = new ManualResetEvent(true);
        Command outDefault;

        ///// <summary>
        ///// A helper method which builds and executes a pipeline that writes to the default output.
        ///// Any exceptions that are thrown are just passed to the caller. 
        ///// Since all output goes to the default output, this method won't return anything.
        ///// </summary>
        ///// <param name="cmd">The script to run</param>
        ///// <param name="input">Any input arguments to pass to the script.</param>
        //void ExecuteHelper(string cmd, object input, bool history)
        //{
        //    //// Ignore empty command lines.
        //    if (String.IsNullOrEmpty(cmd))
        //    {
        //        history = false;
        //        //return;
        //    }

        //    if (_ready.WaitOne(10000, true))
        //    {
        //        //if (history) StringHistory.Add(cmd);

        //        // Create the pipeline object and make it available
        //        // to the ctrl-C handle through the currentPipeline instance
        //        // variable.
        //        lock (instanceLock)
        //        {
        //            currentPipeline = myRunSpace.CreatePipeline(cmd, history);
        //            currentPipeline.StateChanged += new EventHandler<PipelineStateEventArgs>(Pipeline_StateChanged);
        //        }

        //        outDefault.MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Error | PipelineResultTypes.Output;

        //        // Create a pipeline for this execution. Place the result in the currentPipeline
        //        // instance variable so that it is available to be stopped.
        //        try
        //        {
        //            // currentPipeline.Commands.AddScript(cmd);

        //            //foreach (Command c in currentPipeline.Commands)
        //            //{
        //            //    c.MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
        //            //}

        //            // Now add the default outputter to the end of the pipe and indicate
        //            // that it should handle both output and errors from the previous
        //            // commands. This will result in the output being written using the PSHost
        //            // and PSHostUserInterface classes instead of returning objects to the hosting
        //            // application.
        //            currentPipeline.Commands.Add( outDefault );

        //            currentPipeline.InvokeAsync();
        //            if (input != null)
        //            {
        //                currentPipeline.Input.Write(input);
        //            }
        //            currentPipeline.Input.Close();

        //            _ready.Reset();
        //        }
        //        catch
        //        {
        //            // Dispose of the pipeline line and set it to null, locked because currentPipeline
        //            // may be accessed by the ctrl-C handler.
        //            lock (instanceLock)
        //            {
        //                currentPipeline.Dispose();
        //                currentPipeline = null;
        //            }
        //        }
        //    }
        //    else
        //    {
        //        buffer.WriteErrorLine("Timeout - Console Busy, To Cancel Running Pipeline press Esc");
        //    }
        //}





        #endregion ConsoleRichTextBox Event Handlers

        #region Settings

        void SettingsSettingChanging(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            //e.SettingClass
        }

        void SettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CopyOnMouseSelect":
                    {
                        // do nothing, this setting is checked each time you select
                    } break;
                case "ScrollBarVisibility":
                    {
                        buffer.VerticalScrollBarVisibility = (ConsoleScrollBarVisibility)(int)Properties.Settings.Default.ScrollBarVisibility;
                    } break;
                default:
                    break;
            }
            // we save on every change.
            Properties.Settings.Default.Save();
        }


        #endregion

        /// <summary>
        /// Return the culture info to use - this implementation just snapshots the
        /// culture info of the thread that created this object.
        /// </summary>
        public override CultureInfo CurrentCulture
        {
            get { return originalCultureInfo; }
        }
        private CultureInfo originalCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;

        /// <summary>
        /// Return the UI culture info to use - this implementation just snapshots the
        /// UI culture info of the thread that created this object.
        /// </summary>
        public override CultureInfo CurrentUICulture
        {
            get { return originalUICultureInfo; }
        }
        private CultureInfo originalUICultureInfo = System.Threading.Thread.CurrentThread.CurrentUICulture;

        /// <summary>
        /// Not implemented by this example class. The call fails with an exception.
        /// </summary>
        public override void EnterNestedPrompt()
        {
            throw new NotImplementedException("Cannot suspend the shell, EnterNestedPrompt() method is not implemented by MyHost.");
        }

        /// <summary>
        /// Not implemented by this example class. The call fails with an exception.
        /// </summary>
        public override void ExitNestedPrompt()
        {
            throw new NotImplementedException("The ExitNestedPrompt() method is not implemented by MyHost.");
        }

        private static Guid instanceId = Guid.NewGuid();
        /// <summary>
        /// This implementation always returns the GUID allocated at instantiation time.
        /// </summary>
        public override Guid InstanceId
        {
            get { return instanceId; }
        }

        /// <summary>
        /// Return an appropriate string to identify your host implementation.
        /// Keep in mind that this string may be used by script writers to identify
        /// when your host is being used.
        /// </summary>
        public override string Name
        {
            get { return "PoshConsole"; }

        }

        /// <summary>
        /// This API is called before an external application process is started.
        /// </summary>
        int native = 0;
        public override void NotifyBeginApplication()
        {
            savedTitle = myUI.RawUI.WindowTitle;

            native++;
            //MakeConsole();
        }

        private string savedTitle = String.Empty;

        /// <summary>
        /// This API is called after an external application process finishes.
        /// </summary>
        public override void NotifyEndApplication()
        {
            myUI.RawUI.WindowTitle = savedTitle;
            
            native--;
            //if (native == 0) KillConsole();
        }


        /// <summary>
        /// Return an instance of the implementation of the PSHostUserInterface
        /// class for this application. This instance is allocated once at startup time
        /// and returned every time thereafter.
        /// </summary>
        public override PSHostUserInterface UI
        {
            get { return myUI; }
        }
        //private PoshUI myHostUserInterface = new PoshUI();

        /// <summary>
        /// Return the version object for this application. Typically this should match the version
        /// resource in the application.
        /// </summary>
        public override Version Version
        {
            get { return new Version(1, 0, 2007, 7310); }
        }

        public override PSObject PrivateData
        {
            get
            {
                return PSObject.AsPSObject( Options );
            }
        }
    }
}
