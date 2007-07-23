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
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Threading;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using System.IO;

namespace Huddled.PoshConsole
{
    /// <summary>
    /// A sample implementation of the PSHost abstract class for console
    /// applications. Not all members are implemented. Those that are 
    /// not implemented throw a NotImplementedException exception.
    /// </summary>
    class PoshHost : PSHost
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
        private Console console;

        /// <summary>
        /// Used to serialize access to instance data...
        /// </summary>
        private object instanceLock = new object();

        /// <summary>
        /// A ConsoleTextBox for output
        /// </summary>
        private ConsoleTextBox buffer;

        public bool IsClosing = false;


        // Universal Delegates
        delegate void voidDelegate();
        public delegate void WriteProgressDelegate(long sourceId, ProgressRecord record);

        public event WriteProgressDelegate ProgressUpdate;


        internal PoshOptions Options;
        internal List<string> StringHistory;
        public PoshHost(ConsoleTextBox buffer)
        {
            this.buffer = buffer;
            StringHistory = new List<string>();
            Options = new PoshOptions(this);
            console = new Console();
            try
            {
                myRawUI = new PoshRawUI(buffer);
                myUI = new PoshUI(myRawUI);
                // prebuild this
                outDefault = new Command("Out-Default");
                // for now, merge the errors with the rest of the output
                outDefault.MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
            }
            catch
            {
                MessageBox.Show("Can't create PowerShell interface, are you sure PowerShell is installed?", "Error Starting PoshConsole", MessageBoxButton.OK, MessageBoxImage.Stop);
                Application.Current.Shutdown(1);
            }

            buffer.ConsoleBackground = myRawUI.BackgroundColor;
            buffer.ConsoleForeground = myRawUI.ForegroundColor;

            buffer.TabComplete +=new ConsoleTextBox.TabCompleteHandler(buffer_TabComplete);
            buffer.CommandEntered +=new ConsoleTextBox.CommandHandler(buffer_CommandEntered);
            buffer.GetHistory +=new ConsoleTextBox.HistoryHandler(buffer_GetHistory);

            this.ShouldExit += new ExitHandler(WeShouldExit);
            myUI.ProgressUpdate += new PoshUI.WriteProgressDelegate( delegate(long sourceId, ProgressRecord record){if(ProgressUpdate!=null) ProgressUpdate(sourceId, record);} );
            myUI.Input += new PoshUI.InputDelegate(GetInput);
            myUI.Output += new PoshUI.OutputDelegate(OnOutput);
            myUI.OutputLine += new PoshUI.OutputDelegate(OnOutputLine);
            myUI.WritePrompt += new PoshUI.PromptDelegate(WritePrompt);

            // Some delegates we think we can get away with making only once...
            endOutput = new ConsoleTextBox.EndOutputDelegate(buffer.EndOutput);
            prompt = new ConsoleTextBox.PromptDelegate(buffer.Prompt);

            Properties.Settings.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(SettingsPropertyChanged);

            myRunSpace = RunspaceFactory.CreateRunspace(this);



            myRunSpace.Open();
        }

        #region ConsoleTextBox Event Handlers



        /// <summary>
        /// A Delegate for calling WeShouldExit
        /// </summary>
        private delegate void ExitDelegate(int exitCode);
        /// <summary>
        /// Shuts down the console in a thread-safe manner
        /// </summary>
        /// <param name="exitCode">The exit code.</param>
        void WeShouldExit(int exitCode)
        {
            if (buffer.Dispatcher.CheckAccess())
            {
                Application.Current.Shutdown(exitCode);
            }
            else
            {
                this.IsClosing = true;
                ExitDelegate ex = new ExitDelegate(WeShouldExit);
                buffer.Dispatcher.BeginInvoke(DispatcherPriority.Send, ex, exitCode);
            }
        }

        /// <summary>
        /// Writes the prompt.
        /// </summary>
        /// <param name="foreground">The foreground.</param>
        /// <param name="background">The background.</param>
        /// <param name="text">The text.</param>
        void WritePrompt(ConsoleColor foreground, ConsoleColor background, string text)
        {
            // TODO: Colors
            // Oh, my! .CheckAccess() is there, but intellisense-invisible!
            if (buffer.Dispatcher.CheckAccess())
            {
                buffer.Prompt(text);
            }
            else
            {
                buffer.Dispatcher.BeginInvoke(DispatcherPriority.Render, new ConsoleTextBox.ColoredPromptDelegate(buffer.Prompt), foreground, background, text);
            }
        }

        /// <summary>
        /// Write text to the console buffer
        /// </summary>
        /// <param name="foreground">The foreground color.</param>
        /// <param name="background">The background color.</param>
        /// <param name="text">The text.</param>
        void OnOutput(ConsoleColor foreground, ConsoleColor background, string text)
        {
            WriteOutput(foreground, background, text, false);
        }
        /// <summary>
        /// Write a line of text to the console buffer
        /// </summary>
        /// <param name="foreground">The foreground color.</param>
        /// <param name="background">The background color.</param>
        /// <param name="text">The text.</param>
        void OnOutputLine(ConsoleColor foreground, ConsoleColor background, string text)
        {
            WriteOutput(foreground, background, text, true);
        }

        /// <summary>
        /// A Delegate for invoking WriteOutput
        /// </summary>
        private delegate void WriteOutputDelegate(ConsoleColor foreground, ConsoleColor background, string text, bool lineBreak);
        /// <summary>
        /// Writes text to the console buffer in a thread-safe manner.
        /// </summary>
        /// <param name="foreground">The foreground color.</param>
        /// <param name="background">The background color.</param>
        /// <param name="text">The text.</param>
        /// <param name="lineBreak">if set to <c>true</c> [line break].</param>
        /// <param name="prompt">if set to <c>true</c> [prompt].</param>
        void WriteOutput(ConsoleColor foreground, ConsoleColor background, string text, bool lineBreak)
        {
            // TODO: Colors
            // Oh, my! .CheckAccess() is there, but intellisense-invisible!
            if (buffer.Dispatcher.CheckAccess())
            {
                buffer.WriteOutput(foreground, background, text, lineBreak);
            }
            else
            {
                ConsoleTextBox.WriteOutputDelegate sod = new ConsoleTextBox.WriteOutputDelegate(buffer.WriteOutput);
                buffer.Dispatcher.BeginInvoke(DispatcherPriority.Render, sod, foreground, new object[] { background, text, lineBreak });
            }
        }


        #region
        /// <summary>
        /// Handles the CommandEntered event of the Console buffer
        /// </summary>
        /// <param name="command">The command.</param>
        private void buffer_CommandEntered(string command)
        {
            lastInputString = command;
            if (waitingForInput)
            {
                GotInput.Set();
            }
            else
            {
                Execute(command);
            }
        }

        private string buffer_GetHistory(ref int historyIndex)
        {
            if (historyIndex == -1)
            {
                historyIndex = StringHistory.Count;
            }
            if (historyIndex > 0 && historyIndex <= StringHistory.Count)
            {
                return StringHistory[StringHistory.Count - historyIndex];
            }
            else
            {
                historyIndex = 0;
                return string.Empty;
            }
        }

        //Regex splitter = new Regex("[^ \"']+|\"[^\"]+\"[^ \"']*|'[^']+'[^ \"']*|[\"'][^\"']+$", RegexOptions.Compiled);
        static Regex chunker = new Regex("[^ \"']+|([\"'])[^\\1]*?\\1[^ \"']*|([\"'])[^\\1]*$", RegexOptions.Compiled);

        string tabCompleteLast = String.Empty;
        int tabCompleteCount = 0;
        private string buffer_TabComplete(string cmdline)
        {
            string completion = cmdline;
            string lastWord = null;
            int lastIndex = 0, lastLength = 0, tabCount = 0;
            // if they're asking for another tab complete of the same thing as last time
            // we'll look at the next thing in the list, otherwise, start at zero
            if (tabCompleteLast.Equals(cmdline))
            {
                tabCount = ++tabCompleteCount;
            }
            else
            {
                tabCompleteCount = 0;
                tabCompleteLast = cmdline;
            }

            MatchCollection words = chunker.Matches(cmdline);

            if (words.Count >= 1)
            {
                System.Text.RegularExpressions.Match lw = words[words.Count - 1];
                lastWord = lw.Value;
                lastIndex = lw.Index;
                lastLength = lw.Length;
                if (lastWord[0] == '"')
                {
                    lastWord = lastWord.Replace("\"", string.Empty);
                }
                else if (lastWord[0] == '\'')
                {
                    lastWord = lastWord.Replace("'", string.Empty);
                }

            }

            // Still need to do more Tab Completion
            // TODO: Make "PowerTab" obsolete for PoshConsole users.
            //   TODO: TabComplete Parameters
            //   TODO: TabComplete Variables
            //   TODO: TabComplete Aliases
            //   TODO: TabComplete Executables in (current?) path

            if (lastWord != null)
            {
                // TODO: TabComplete Cmdlets inside the pipeline
                foreach (RunspaceConfigurationEntry cmdlet in myRunSpace.RunspaceConfiguration.Cmdlets)
                {
                    if (cmdlet.Name.StartsWith(lastWord))
                    {
                        completion = cmdlet.Name;
                        if (0 == tabCount--) return completion;
                    }
                }

                // TODO: TabComplete Paths
                try
                {
                    Collection<PSObject> tabCompletion = null;
                    if (lastWord[0] == '$')
                    {
                        tabCompletion = InvokeHelper("get-variable " + lastWord.Substring(1) + "*", null);
                        if (tabCompletion.Count > tabCount)
                        {
                            PSVariable var = tabCompletion[tabCount].ImmediateBaseObject as PSVariable;
                            if (var != null)
                            {
                                completion = "$" + var.Name;
                                if (completion.Contains(" "))
                                {
                                    return string.Format("{0}\"{1}\"", cmdline.Substring(0, lastIndex), completion);
                                }
                                else return cmdline.Substring(0, lastIndex) + completion;
                            }
                        }
                        else
                        {
                            tabCount -= tabCompletion.Count;
                        }
                    }

                    tabCompletion = InvokeHelper("resolve-path \"" + lastWord + "*\"", null);
                    if (tabCompletion.Count > tabCount)
                    {
                        completion = tabCompletion[tabCount].ToString();
                        if (completion.Contains(" "))
                        {
                            return string.Format("{0}\"{1}\"", cmdline.Substring(0, lastIndex), completion);
                        }
                        else return cmdline.Substring(0, lastIndex) + completion;
                    }
                    else
                    {
                        tabCount -= tabCompletion.Count;
                    }
                    //}
                    //catch (RuntimeException)
                    //{
                    //    // hide the error
                    //}
                    //
                    //// Invoke the TabComplete function
                    //try
                    //{
                    tabCompletion = InvokeHelper("TabExpansion '" + cmdline + "' '" + lastWord + "'", null);
                    if (tabCompletion.Count > tabCount)
                    {
                        completion = tabCompletion[tabCount].ToString();
                        return cmdline.Substring(0, lastIndex) + completion;
                    }
                    else
                    {
                        tabCount -= tabCompletion.Count;
                    }
                }
                catch (RuntimeException)
                {
                    // hide the error
                }
            }
            // failed to find a match, reset so we can cycle back through
            tabCompleteCount = 0;
            tabCompleteLast = String.Empty;
            return cmdline;
        }
        #endregion






        /// <summary>
        /// Method used to handle control-C's from the user. It calls the
        /// pipeline Stop() method to stop execution. If any exceptions occur,
        /// they are printed to the console; otherwise they are ignored.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.Input.ExecutedRoutedEventArgs"/> instance containing the event data.</param>
        public void StopPipeline()
        {
                lock (instanceLock)
                {
                    if (currentPipeline != null && currentPipeline.PipelineStateInfo.State == PipelineState.Running)
                        currentPipeline.Stop();
                    //else
                    //    ApplicationCommands.Copy.Execute(null, buffer);
                }
        }

        /// <summary>
        /// Executes the startup profile(s).
        /// </summary>
        internal void ExecuteStartupProfile()
        {
            //this.Cursor = Cursors.AppStarting;
            buffer.Cursor = Cursors.AppStarting;

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
                    cmd.AppendFormat(". \"{0}\";", path);
                }
            }
            if (cmd.Length > 0)
            {
                try
                {
                    ExecuteHelper(cmd.ToString(), null, false);
                }
                catch (RuntimeException rte)
                {
                    // An exception occurred that we want to display ...
                    // We have to run another pipeline, and pass in the error record.
                    // The runtime will bind the input to the $input variable
                    ExecuteHelper("write-host ($input | out-string) -fore darkyellow", rte.ErrorRecord, false);
                }
            }
            else
            {
                Prompt();
                // we have to reset the cursors if we don't execute a command
                //this.Cursor = Cursors.SizeAll;
                buffer.Cursor = Cursors.IBeam;
            }
        }

        /// <summary>
        /// Executes the shutdown profile(s).
        /// </summary>
        internal void ExecuteShutdownProfile()
        {
            //this.Cursor = Cursors.AppStarting;
            buffer.Cursor = Cursors.AppStarting;

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
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.SystemDirectory , @"WindowsPowerShell\v1.0\PoshConsole_profile_exit.ps1")),
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
                try
                {
                    ExecuteHelper(cmd.ToString(), null, false);
                }
                catch (RuntimeException rte)
                {
                    // An exception occurred that we want to display ...
                    // We have to run another pipeline, and pass in the error record.
                    // The runtime will bind the input to the $input variable
                    ExecuteHelper("write-host ($input | out-string) -fore darkyellow", rte.ErrorRecord, false);
                }
            }
            else
            {
                Prompt();
                // we have to reset the cursors if we don't execute a profile script in here
                //this.Cursor = Cursors.SizeAll;
                buffer.Cursor = Cursors.IBeam;
            }
        }

        protected int MaxBufferLength = 500;


        #region

        string lastInputString = null;
        AutoResetEvent GotInput = new AutoResetEvent(false);
        public bool waitingForInput = false;
        /// <summary>
        /// Provides a way for scripts to request user input ...
        /// </summary>
        /// <returns></returns>
        string GetInput()
        {
            string result = null;

            waitingForInput = true;
            GotInput.WaitOne();
            waitingForInput = false;

            result = lastInputString;
            return result;
        }



        /// <summary>
        /// A helper method which builds and executes a pipeline that returns it's output.
        /// </summary>
        /// <param name="cmd">The script to run</param>
        /// <param name="input">Any input arguments to pass to the script.</param>
        public Collection<PSObject> InvokeHelper(string cmd, object input)
        {
            Collection<PSObject> output = new Collection<PSObject>();
            if (ready.WaitOne(10000, true))
            {
                // Ignore empty command lines.
                if (String.IsNullOrEmpty(cmd))
                    return null;

                // Create the pipeline object and make it available
                // to the ctrl-C handle through the currentPipeline instance
                // variable.
                lock (instanceLock)
                {
                    ready.Reset();
                    currentPipeline = myRunSpace.CreatePipeline(cmd, false);
                }

                // Create a pipeline for this execution. Place the result in the currentPipeline
                // instance variable so that it is available to be stopped.
                try
                {
                    // currentPipeline.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);

                    // If there was any input specified, pass it in, and execute the pipeline.
                    if (input != null)
                    {
                        output = currentPipeline.Invoke(new object[] { input });
                    }
                    else
                    {
                        output = currentPipeline.Invoke();
                    }

                }

                finally
                {
                    // Dispose of the pipeline line and set it to null, locked because currentPipeline
                    // may be accessed by the ctrl-C handler.
                    lock (instanceLock)
                    {
                        currentPipeline.Dispose();
                        currentPipeline = null;
                    }
                }
                ready.Set();
            }
            return output;
        }


        ManualResetEvent ready = new ManualResetEvent(true);

        Command outDefault;

        /// <summary>
        /// A helper method which builds and executes a pipeline that writes to the default output.
        /// Any exceptions that are thrown are just passed to the caller. 
        /// Since all output goes to the default output, this method won't return anything.
        /// </summary>
        /// <param name="cmd">The script to run</param>
        /// <param name="input">Any input arguments to pass to the script.</param>
        void ExecuteHelper(string cmd, object input, bool history)
        {
            //// Ignore empty command lines.
            if (String.IsNullOrEmpty(cmd))
            {
                history = false;
                //return;
            }

            if (ready.WaitOne(10000, true))
            {
                if (history) StringHistory.Add(cmd);

                // Create the pipeline object and make it available
                // to the ctrl-C handle through the currentPipeline instance
                // variable.
                lock (instanceLock)
                {
                    currentPipeline = myRunSpace.CreatePipeline(cmd, history);
                    currentPipeline.StateChanged += new EventHandler<PipelineStateEventArgs>(Pipeline_StateChanged);
                }

                // Create a pipeline for this execution. Place the result in the currentPipeline
                // instance variable so that it is available to be stopped.
                try
                {
                    // currentPipeline.Commands.AddScript(cmd);

                    // Now add the default outputter to the end of the pipe and indicate
                    // that it should handle both output and errors from the previous
                    // commands. This will result in the output being written using the PSHost
                    // and PSHostUserInterface classes instead of returning objects to the hosting
                    // application.
                    
                    currentPipeline.Commands.Add( outDefault );

                    currentPipeline.InvokeAsync();
                    if (input != null)
                    {
                        currentPipeline.Input.Write(input);
                    }
                    currentPipeline.Input.Close();

                    ready.Reset();
                }
                catch
                {
                    // Dispose of the pipeline line and set it to null, locked because currentPipeline
                    // may be accessed by the ctrl-C handler.
                    lock (instanceLock)
                    {
                        currentPipeline.Dispose();
                        currentPipeline = null;
                    }
                }
            }
            else
            {
                WriteOutput(ConsoleColor.Red, ConsoleColor.Black, "Timeout - Console Busy, To Cancel Running Pipeline press Esc", true);
                buffer.CurrentCommand = cmd;
            }
        }



        /// <summary>
        /// Handles the StateChanged event of the Pipeline control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Management.Automation.Runspaces.PipelineStateEventArgs"/> instance containing the event data.</param>
        void Pipeline_StateChanged(object sender, PipelineStateEventArgs e)
        {
            if (e.PipelineStateInfo.State != PipelineState.Running && e.PipelineStateInfo.State != PipelineState.Stopping)
            {
                buffer.Dispatcher.BeginInvoke(DispatcherPriority.Render, new voidDelegate(delegate { buffer.Cursor = Cursors.IBeam; }));
                //if(currentPipeline.Commands[0].CommandText.Equals("prompt"))
                //{
                //    ConsoleTextBox.PromptDelegate np = new ConsoleTextBox.PromptDelegate(buffer.PostPrompt);
                //    Dispatcher.BeginInvoke(DispatcherPriority.Render, np);
                //}
                //else
                //{
                //    ConsoleTextBox.PromptDelegate np = new ConsoleTextBox.PromptDelegate(Prompt);
                //    Dispatcher.BeginInvoke(DispatcherPriority.Render, np);
                //}

                // Dispose of the pipeline line and set it to null
                // locked because currentPipeline may be accessed by the ctrl-C handler, etc
                lock (instanceLock)
                {
                    currentPipeline.Dispose();
                    currentPipeline = null;
                }
                ready.Set();
                if (!this.IsClosing) Prompt();

            }
        }


        private const string PromptPadding = " ";

        private readonly ConsoleTextBox.EndOutputDelegate endOutput;// = new ConsoleTextBox.EndOutputDelegate(buffer.EndOutput);
        private readonly ConsoleTextBox.PromptDelegate prompt;// = new ConsoleTextBox.PromptDelegate(buffer.Prompt);
        /// <summary>
        /// Invoke's the user's PROMPT function to display a prompt.
        /// Called after each command completes
        /// </summary>
        internal void Prompt()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                //// paragraph break before each prompt ensure the command and it's output are in a paragraph on their own
                //// This means that the paragraph select key (and triple-clicking) gets you a command and all it's output
                //// NOTE: manually use the dispatcher, otherwise it will print before the output of the command
                buffer.Dispatcher.BeginInvoke(DispatcherPriority.Render, endOutput);

                Collection<PSObject> output = InvokeHelper("prompt", null);

                foreach (PSObject thing in output)
                {
                    sb.Append(thing.ToString());
                }
                //sb.Append(PromptPadding);
            }
            catch (RuntimeException rte)
            {
                // An exception occurred that we want to display ...
                // We have to run another pipeline, and pass in the error record.
                // The runtime will bind the input to the $input variable
                ExecuteHelper("write-host \"ERROR: Your prompt function crashed!\n\" -fore darkyellow", null, false);
                ExecuteHelper("write-host ($input | out-string) -fore darkyellow", rte.ErrorRecord, false);
                sb.Append("\n> ");
            }
            finally
            {
                buffer.Dispatcher.BeginInvoke(DispatcherPriority.Render, prompt, sb.ToString());
            }
        }

        /// <summary>
        /// Basic script execution routine - any runtime exceptions are
        /// caught and passed back into the runtime to display.
        /// </summary>
        /// <param name="cmd">The command to execute</param>
        void Execute(string cmd)
        {
            try
            {
                // execute the command with no input...
                ExecuteHelper(cmd, null, true);
            }
            catch (RuntimeException rte)
            {
                // TODO: handle the "incomplete" commands by displaying an additional prompt?
                // An exception occurred that we want to display ...
                // We have to run another pipeline, and pass in the error record.
                // The runtime will bind the input to the $input variable
                ExecuteHelper("write-host ($input | out-string) -fore darkyellow", rte.ErrorRecord, false);
            }
        }

        #endregion



        #endregion ConsoleTextBox Event Handlers

        #region Settings

        private delegate void SettingsChangedDelegate(object sender, System.ComponentModel.PropertyChangedEventArgs e);
        void SettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!buffer.Dispatcher.CheckAccess())
            {
                buffer.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new SettingsChangedDelegate(SettingsPropertyChanged), sender, new object[] { e });
                return;
            }

            switch (e.PropertyName)
            {
                case "ConsoleBlack": goto case "ConsoleColors";
                case "ConsoleBlue": goto case "ConsoleColors";
                case "ConsoleCyan": goto case "ConsoleColors";
                case "ConsoleDarkBlue": goto case "ConsoleColors";
                case "ConsoleDarkCyan": goto case "ConsoleColors";
                case "ConsoleDarkGray": goto case "ConsoleColors";
                case "ConsoleDarkGreen": goto case "ConsoleColors";
                case "ConsoleDarkMagenta": goto case "ConsoleColors";
                case "ConsoleDarkRed": goto case "ConsoleColors";
                case "ConsoleDarkYellow": goto case "ConsoleColors";
                case "ConsoleGray": goto case "ConsoleColors";
                case "ConsoleGreen": goto case "ConsoleColors";
                case "ConsoleMagenta": goto case "ConsoleColors";
                case "ConsoleRed": goto case "ConsoleColors";
                case "ConsoleWhite": goto case "ConsoleColors";
                case "ConsoleYellow": goto case "ConsoleColors";
                case "ConsoleColors":
                    {
                        // These are read for each color change.
                        // but if the one that was changed is the default background or foreground color ...
                        if (myRawUI.BackgroundColor == (ConsoleColor)Enum.Parse(typeof(ConsoleColor), e.PropertyName.Substring(7)))
                        {
                            // this will cause the color to update, even if it's the same color ...
                            myRawUI.BackgroundColor = myRawUI.BackgroundColor;
                        }
                        if (myRawUI.ForegroundColor == (ConsoleColor)Enum.Parse(typeof(ConsoleColor), e.PropertyName.Substring(7)))
                        {
                            myRawUI.ForegroundColor = myRawUI.ForegroundColor;
                        }

                    } break;
                case "CopyOnMouseSelect":
                    {
                        // do nothing, this setting is checked each time you select
                    } break;
                case "ConsoleDefaultForeground":
                    {
                        myRawUI.ForegroundColor = Properties.Settings.Default.ConsoleDefaultForeground;
                    } break;
                case "ConsoleDefaultBackground":
                    {
                        myRawUI.BackgroundColor = Properties.Settings.Default.ConsoleDefaultBackground;
                    } break;
                case "ScrollBarVisibility":
                    {
                        buffer.VerticalScrollBarVisibility = Properties.Settings.Default.ScrollBarVisibility;
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
        public override void NotifyBeginApplication()
        {
            savedTitle = myUI.RawUI.WindowTitle;
            
            return;  // Do nothing...
            
        }

        private string savedTitle = String.Empty;

        /// <summary>
        /// This API is called after an external application process finishes.
        /// </summary>
        public override void NotifyEndApplication()
        {
            myUI.RawUI.WindowTitle = savedTitle;
            return; // Do nothing...
        }

        /// <summary>
        /// Indicate to the host application that exit has
        /// been requested. Pass the exit code that the host
        /// application should use when exiting the process.
        /// </summary>
        /// <param name="exitCode"></param>
        public override void SetShouldExit(int exitCode)
        {
            if( null != ShouldExit ) ShouldExit(exitCode);
        }

        public delegate void ExitHandler(int exitCode);
        public event ExitHandler ShouldExit;



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
            get { return new Version(1, 0, 0, 0); }
        }

		 public override PSObject PrivateData
		 {
			 get
			 {
                 return PSObject.AsPSObject( Options );
			 }
		 }

		 public class PoshOptions : DependencyObject {

             PoshHost MyHost;
             public PoshOptions(PoshHost myHost)
             {
                 MyHost = myHost;
             }

			 public Properties.Settings Settings
			 {
				 get
				 {
					 return Huddled.PoshConsole.Properties.Settings.Default;
				 }
				 //set
				 //{
				 //   Huddled.PoshConsole.Properties.Settings.Default = value;
				 //}
			 }

             private delegate string GetStringDelegate();
             private delegate void  SetStringDelegate( /* string value */ );

             public static DependencyProperty StatusTextProperty = DependencyProperty.Register("StatusText", typeof(string), typeof(PoshOptions));

             public string StatusText
             {
                 get
                 {
                     if (!Dispatcher.CheckAccess())
                     {
                         return (string)Dispatcher.Invoke(DispatcherPriority.Normal, (GetStringDelegate)delegate { return StatusText; });
                     }
                     else
                     {
                         return (string)base.GetValue(StatusTextProperty);
                     }
                 }
                 set 
                 {
                     if (!Dispatcher.CheckAccess())
                     {
                         Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SetStringDelegate)delegate { StatusText = value; });
                     }
                     else
                     {
                         base.SetValue(StatusTextProperty, value);
                     }
                 }
             }
	

             public double FullPrimaryScreenWidth
             {
                 get
                 {
                     return System.Windows.SystemParameters.FullPrimaryScreenWidth;
                 }
             }
             public double FullPrimaryScreenHeight
             {
                 get
                 {
                     return System.Windows.SystemParameters.FullPrimaryScreenHeight;
                 }
             }

             public List<string> History {
                 get
                 {
                     return MyHost.StringHistory;
                 }
             }
         }
    }
}