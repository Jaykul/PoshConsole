using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Windows.Threading;
using System.Threading;
using Huddled.Hotkeys;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Media.Animation;

namespace Huddled.PoshConsole
{

	/// <summary>
	/// Implementation of a WPF host for PowerShell
	/// </summary>
	public partial class PoshConsole : System.Windows.Window
	{
		/// <summary>
		/// The PSHostUserInterface implementation
		/// </summary>
		private PoshUI myUI;
		private PoshRawUI myRawUI;

		/// <summary>
		/// The PSHost implementation for this interpreter.
		/// </summary>
		private PoshHost myHost;

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
		/// Used to serialize access to instance data...
		/// </summary>
		private object instanceLock = new object();

		/// <summary>
		/// A global hotkey manager
		/// </summary>
		WPFHotkeyManager hkManager;

		/// <summary>
		/// A hotkey used to bring our window into focus
		/// </summary>
		Hotkey FocusKey;

		double characterWidth = 1.0;


        // Universal Delegates
        delegate void voidDelegate();
        delegate void passDelegate<T>(T input);
        delegate RET returnDelegate<RET>();
        delegate RET passReturnDelegate<T,RET>(T input);
        

		/// <summary>
		/// Initializes a new instance of the <see cref="PoshConsole"/> class.
		/// </summary>
		public PoshConsole()
		{
			// Create the host and runspace instances for this interpreter. Note that
			// this application doesn't support console files so only the default snapins
			// will be available.

			InitializeComponent();

			// before we start animating, set the animation endpoints to the current values.
			hideOpacityAnimations.From = showOpacityAnimation.To = Opacity;
			hideHeightAnimations.From = showHeightAnimation.To = this.Height;

			myRawUI = new PoshRawUI(buffer);

            // buffer.TitleChanged += new passDelegate<string>(delegate(string val) { Title = val; });
			buffer.ConsoleBackground = myRawUI.BackgroundColor;
			buffer.ConsoleForeground = myRawUI.ForegroundColor;

			myUI = new PoshUI(myRawUI);
			myHost = new PoshHost(myUI);

			// problems with data binding
			this.WindowStyle = Properties.Settings.Default.WindowStyle;

            // Some delegates we think we can get away with making only once...
            endOutput = new ConsoleTextBox.EndOutputDelegate(buffer.EndOutput);
            prompt = new ConsoleTextBox.PromptDelegate(buffer.Prompt);


			Properties.Settings.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(SettingsPropertyChanged);
			//buffer.Background = ConsoleBrush(myUI.CurrentBackground);
			//buffer.Foreground = ConsoleBrush(myUI.CurrentForeground);

			myHost.ShouldExit += new PoshHost.ExitHandler(WeShouldExit);
			myUI.Input += new PoshUI.InputHandler(GetInput);
			myUI.Output += new PoshUI.OutputHandler(OnOutput);
			myUI.OutputLine += new PoshUI.OutputHandler(OnOutputLine);
            myUI.ProgressUpdate += new PoshUI.WriteProgressDelegate(OnProgressUpdate);

			myRunSpace = RunspaceFactory.CreateRunspace(myHost);
			myRunSpace.Open();
		}

        private delegate void SettingsChangedDelegate(object sender, System.ComponentModel.PropertyChangedEventArgs e);
		void SettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new SettingsChangedDelegate(SettingsPropertyChanged), sender, new object[] { e });
                return;
            }

			switch(e.PropertyName)
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
				case "WindowStyle":
					{
						this.WindowStyle = Properties.Settings.Default.WindowStyle;
					} break;
				case "ShowInTaskbar":
					{
						this.ShowInTaskbar = Properties.Settings.Default.ShowInTaskbar;
					} break;
				case "WindowHeight":
					{
						this.Height = Properties.Settings.Default.WindowHeight;
					} break;
				case "WindowLeft":
					{
						this.Left = Properties.Settings.Default.WindowLeft;
					} break;
				case "WindowWidth":
					{
						this.Width = Properties.Settings.Default.WindowWidth;
					} break;
				case "WindowTop":
					{
						this.Top = Properties.Settings.Default.WindowTop;
					} break;
				case "Animate":
					{
						// do nothing, this setting is checked for each animation.
					} break;
				case "SnapToScreenEdge":
					{
						// do nothing, this setting is checked for each move
					} break;
				case "SnapDistance":
					{
						// do nothing, this setting is checked for each move
					} break;
				case "AlwaysOnTop":
					{
						this.Topmost = Properties.Settings.Default.AlwaysOnTop;
					} break;
				case "Opacity":
					{
						this.Opacity = Properties.Settings.Default.Opacity;
					} break;
				case "BorderColorTopLeft":
					{
						// todo: don't know how to handle this
					} break;
				case "BorderColorBottomRight":
					{
						// todo: don't know how to handle this
					} break;
				case "BorderThickness":
					{
						// todo: don't know how to handle this
					} break;
				case "FocusKey":
					{
						if(FocusKey != null && FocusKey.Id != 0) hkManager.Unregister(FocusKey);

						if(Properties.Settings.Default.FocusKey != null)
						{
							FocusKey = Properties.Settings.Default.FocusKey;
							hkManager.Register(FocusKey);
						}
					} break;
				default:
					break;
			}
			// we save on every change.
			Properties.Settings.Default.Save();
		}

        void OnProgressUpdate(long sourceId, ProgressRecord record)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new PoshUI.WriteProgressDelegate(OnProgressUpdate), sourceId, record);
            }
            else
            {
                if (record.RecordType == ProgressRecordType.Completed)
                {
                    progress.Visibility = Visibility.Collapsed;
                }
                else
                {
                    progress.Visibility = Visibility.Visible;

                    progress.Activity = record.Activity;
                    progress.Status = record.StatusDescription;
                    progress.Operation = record.CurrentOperation;
                    progress.PercentComplete = record.PercentComplete;
                    progress.TimeRemaining = TimeSpan.FromSeconds(record.SecondsRemaining);
                }
            }
        }

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
			if(Dispatcher.CheckAccess())
			{
				Application.Current.Shutdown(exitCode);
			}
			else
			{
                this.IsClosing = true;
				ExitDelegate ex = new ExitDelegate(WeShouldExit);
				Dispatcher.BeginInvoke(DispatcherPriority.Send, ex, exitCode);
			}
		}

        ///// <summary>
        ///// A Delegate for setting the window title.
        ///// </summary>
        //private delegate void SetTitleDelegate(string title);
        ///// <summary>
        ///// Sets the title of the window in a thread-safe way.
        ///// </summary>
        ///// <param name="title">The requested window title.</param>
        //public void SetTitle(string title)
        //{
        //    if(Dispatcher.CheckAccess())
        //    {
        //        Title = title;
        //    }
        //    else
        //    {
        //        SetTitleDelegate std = new SetTitleDelegate(SetTitle);
        //        Dispatcher.BeginInvoke(DispatcherPriority.Normal, std, title);
        //    }
        //}



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
			if(Dispatcher.CheckAccess())
			{
				buffer.WriteOutput(foreground, background, text, lineBreak);
			}
			else
			{
				ConsoleTextBox.WriteOutputDelegate sod = new ConsoleTextBox.WriteOutputDelegate(buffer.WriteOutput);
				Dispatcher.BeginInvoke(DispatcherPriority.Render, sod, foreground, new object[] { background, text, lineBreak });
			}
		}

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

			result = lastInputString;
			lastInputString = null;

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
			if(ready.WaitOne(10000, true))
			{
				// Ignore empty command lines.
				if(String.IsNullOrEmpty(cmd))
					return null;

				// Create the pipeline object and make it available
				// to the ctrl-C handle through the currentPipeline instance
				// variable.
				lock(instanceLock)
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
					lock(instanceLock)
					{
						currentPipeline.Dispose();
						currentPipeline = null;
					}
				}
                ready.Set();
			}
			return output;
		}

		List<string> cmdHistory = new List<string>();

		ManualResetEvent ready = new ManualResetEvent(true);

		/// <summary>
		/// A helper method which builds and executes a pipeline that writes to the default output.
		/// Any exceptions that are thrown are just passed to the caller. 
		/// Since all output goes to the default output, this method won't return anything.
		/// </summary>
		/// <param name="cmd">The script to run</param>
		/// <param name="input">Any input arguments to pass to the script.</param>
		void ExecuteHelper(string cmd, object input, bool history, bool suppressOutput)
		{
			// Ignore empty command lines.
			if(String.IsNullOrEmpty(cmd))
				return;

			if(ready.WaitOne(10000, true))
			{

				if(history) cmdHistory.Add(cmd);

				// Create the pipeline object and make it available
				// to the ctrl-C handle through the currentPipeline instance
				// variable.
				lock(instanceLock)
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
					if(!suppressOutput) currentPipeline.Commands.Add("out-default");
					currentPipeline.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);

					currentPipeline.InvokeAsync();
					if(input != null)
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
					lock(instanceLock)
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
			if(e.PipelineStateInfo.State != PipelineState.Running && e.PipelineStateInfo.State != PipelineState.Stopping)
			{
                Dispatcher.BeginInvoke(DispatcherPriority.Render, new voidDelegate( delegate{ this.Cursor = Cursors.IBeam; } ));
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
				lock(instanceLock)
				{
					currentPipeline.Dispose();
					currentPipeline = null;
				}
                ready.Set();
                if( !this.IsClosing ) Prompt();

			}
		}


        private const string PromptPadding = " ";

        private readonly ConsoleTextBox.EndOutputDelegate  endOutput;// = new ConsoleTextBox.EndOutputDelegate(buffer.EndOutput);
        private readonly ConsoleTextBox.PromptDelegate     prompt;// = new ConsoleTextBox.PromptDelegate(buffer.Prompt);
		/// <summary>
		/// Invoke's the user's PROMPT function to display a prompt.
		/// Called after each command completes
		/// </summary>
		private void Prompt()
		{
            StringBuilder sb = new StringBuilder();
            try
            {
                //// paragraph break before each prompt ensure the command and it's output are in a paragraph on their own
                //// This means that the paragraph select key (and triple-clicking) gets you a command and all it's output
                //// NOTE: manually use the dispatcher, otherwise it will print before the output of the command
                Dispatcher.BeginInvoke(DispatcherPriority.Render, endOutput);

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
                ExecuteHelper("write-host \"ERROR: Your prompt function crashed!\n\" -fore darkyellow", null, false, false);
                ExecuteHelper("write-host ($input | out-string) -fore darkyellow", rte.ErrorRecord, false, false);
                sb.Append("\n> ");
            }
            finally
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Render, prompt, sb.ToString());
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
				ExecuteHelper(cmd, null, true, false);
			}
			catch(RuntimeException rte)
			{
				// TODO: handle the "incomplete" commands by displaying an additional prompt?
				// An exception occurred that we want to display ...
				// We have to run another pipeline, and pass in the error record.
				// The runtime will bind the input to the $input variable
				ExecuteHelper("write-host ($input | out-string) -fore darkyellow", rte.ErrorRecord, false, false);
			}
		}



		/// <summary>
		/// Method used to handle control-C's from the user. It calls the
		/// pipeline Stop() method to stop execution. If any exceptions occur,
		/// they are printed to the console; otherwise they are ignored.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.Windows.Input.ExecutedRoutedEventArgs"/> instance containing the event data.</param>
		void HandleControlC(object sender, ExecutedRoutedEventArgs e)
		{
			try
			{
				lock(instanceLock)
				{
					if(currentPipeline != null && currentPipeline.PipelineStateInfo.State == PipelineState.Running)
						currentPipeline.Stop();
				}
				e.Handled = true;
			}
			catch(Exception exception)
			{
				this.myHost.UI.WriteErrorLine(exception.ToString());
			}
		}


		void CanHandleControlC(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		/// <summary>
		/// Loads the shell profile(s).
		/// </summary>
		private void LoadShellProfile()
		{
            Cursor = Cursors.AppStarting;
			//* %windir%\system32\WindowsPowerShell\v1.0\profile.ps1
			//  This profile applies to all users and all shells.
			//* %windir%\system32\WindowsPowerShell\v1.0\ Microsoft.PowerShell_profile.ps1
			//  This profile applies to all users, but only to the Microsoft.PowerShell shell.
			//* %UserProfile%\My Documents\WindowsPowerShell\profile.ps1
			//  This profile applies only to the current user, but affects all shells.
			//* %UserProfile%\\My Documents\WindowsPowerShell\Microsoft.PowerShell_profile.ps1
			//  This profile applies only to the current user and the Microsoft.PowerShell shell.

			StringBuilder cmd = new StringBuilder();
			foreach(string path in
				 new string[3] {
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.SystemDirectory , @"WindowsPowerShell\v1.0\profile.ps1")),
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WindowsPowerShell\profile.ps1")),
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WindowsPowerShell\Huddled.PoshConsole_profile.ps1")),
                })
			{
				if(File.Exists(path))
				{
					cmd.AppendFormat(". {0};", path);
				}
			}
            if (cmd.Length > 0)
            {
                try
                {
                    ExecuteHelper(cmd.ToString(), null, false, false);
                }
			    catch(RuntimeException rte)
			    {
				    // An exception occurred that we want to display ...
				    // We have to run another pipeline, and pass in the error record.
				    // The runtime will bind the input to the $input variable
				    ExecuteHelper("write-host ($input | out-string) -fore darkyellow", rte.ErrorRecord, false, false);
			    }
            }
            else
                Prompt();
		}

		protected int MaxBufferLength = 500;

		/// <summary>
		/// Recalculates various sizes that PSRawUI needs
		/// <remarks>This should be called any time the font or window size change</remarks>
		/// </summary>
		private void RecalculateSizes()
		{
			int imHigh = (int)(double.IsPositiveInfinity(buffer.MaxHeight) ? System.Windows.SystemParameters.MaximumWindowTrackHeight : buffer.MaxHeight);
			int imWide = (int)(double.IsPositiveInfinity(buffer.MaxWidth) ? System.Windows.SystemParameters.MaximumWindowTrackWidth : buffer.MaxWidth);
			double fontSize = (buffer.FontSize * characterWidth); // (72/96) convert from points to device independent pixels ... 

			int linHeight = (int)(double.IsNaN(buffer.Document.LineHeight) ? fontSize : buffer.Document.LineHeight);

			//System.Drawing.Font f = new System.Drawing.Font(buffer.FontFamily, buffer.FontSize, buffer.FontStyle, System.Drawing.GraphicsUnit.Point);
			//TextRenderer.MeasureText("W", f).Width

			//myRawUI._BufferSize = buffer.BufferSize;

			//myRawUI._WindowSize = buffer.WindowSize;

			myRawUI._MaxWindowSize = buffer.MaxWindowSize;

			// TODO: What's the difference between MaxWindowSize and MaxPhysicalWindowSize?
			myRawUI._MaxPhysicalWindowSize = buffer.MaxPhysicalWindowSize;

			//myRawUI._CursorPosition = buffer.CursorPosition;

			//myRawUI._WindowPosition = new Coordinates((int)this.Left, (int)this.Top);

		}

		private void buffer_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			RecalculateSizes();
		}

		private void buffer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
			{
				buffer.FontSize += (e.Delta > 0) ? 1 : -1;
				RecalculateSizes();
				e.Handled = true;
			}
		}

		/// <summary>
		/// Handles the CommandEntered event of the Console buffer
		/// </summary>
		/// <param name="command">The command.</param>
		private void buffer_CommandEntered(string command)
		{
			if(waitingForInput)
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
                historyIndex = cmdHistory.Count;
            }
			if(historyIndex > 0 && historyIndex <= cmdHistory.Count)
			{
				return cmdHistory[cmdHistory.Count - historyIndex];
			}
			else
			{
				historyIndex = 0;
				return string.Empty;
			}
		}

		string tabCompleteLast = String.Empty;
		int tabCompleteCount = 0;
		private string buffer_TabComplete(string cmdline)
		{
			string completion = cmdline;
            int tc = 0;
			// if they're asking for another tab complete of the same thing as last time
			// we'll look at the next thing in the list, otherwise, start at zero
            if (tabCompleteLast.Equals(cmdline))
            {
                tc = ++tabCompleteCount;
            }
            else
            {
                tabCompleteCount = 0;
                tabCompleteLast = cmdline;
            }

			System.Text.RegularExpressions.Regex splitter = new System.Text.RegularExpressions.Regex("[^ \"']+|[\"'][^\"']+[\"']", System.Text.RegularExpressions.RegexOptions.Compiled);
			System.Text.RegularExpressions.MatchCollection words = splitter.Matches(cmdline);

            // Still need to do more Tab Completion
            // TODO: Make "PowerTab" obsolete for PoshConsole users.
            // TODO: TabComplete Cmdlets inside the pipeline
            // TODO: TabComplete Parameters
            // TODO: TabComplete Variables
            // TODO: TabComplete Aliases
            // TODO: TabComplete Paths
            // TODO: TabComplete Executables in (current?) path

			if(words.Count == 1)
			{
				foreach(RunspaceConfigurationEntry cmdlet in myRunSpace.RunspaceConfiguration.Cmdlets)
				{
					if(cmdlet.Name.StartsWith(cmdline))
					{
						completion = cmdlet.Name;
                        if (0 == tc--) return completion;
					}
				}
			}
            if (words.Count >= 1)
            {
                System.Text.RegularExpressions.Match lastword = words[words.Count - 1];
                try
                {
                    Collection<PSObject> tabCompletion = InvokeHelper("TabExpansion '" + cmdline + "' '" + lastword.Value + "'", null);
                    if (tabCompletion.Count > tabCompleteCount)
                    {
                        completion = tabCompletion[tabCompleteCount].ToString();
                        return cmdline.Substring(0, lastword.Index) + completion;
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

		/// <summary>
		/// Handles the SourceInitialized event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void Window_SourceInitialized(object sender, EventArgs e)
		{
			// make the whole window glassy
			Win32.Vista.Glass.ExtendGlassFrame(this, new Thickness(-1));
			// hook mousedown and call DragMove() to make the whole window a drag handle

			buffer.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(DragHandler);

            hkManager = new WPFHotkeyManager(this);
            hkManager.HotkeyPressed += new WPFHotkeyManager.HotkeyPressedEvent(Hotkey_Pressed);

            FocusKey = Properties.Settings.Default.FocusKey;

			if(FocusKey == null)
			{
				Properties.Settings.Default.FocusKey = new Hotkey(Modifiers.Win, Keys.Oemtilde);
			}
			
			// this shouldn't be needed, because we hooked the settings.change event earlier
			if(FocusKey == null || FocusKey.Id == 0)
			{
				hkManager.Register(FocusKey);
			}

		}

		void DragHandler(object sender, MouseButtonEventArgs e)
		{
			if((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control ||
					  (buffer.InputHitTest(e.GetPosition((Border)buffer.Template.FindName("Border", buffer))) is Border))
			{
				DragMove();
				e.Handled = true;
			}
		}

		/// <summary>
		/// Handles the Loaded event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			buffer.Document.IsColumnWidthFlexible = false;

			RecalculateSizes();
			LoadShellProfile();
		}

		/// <summary>
		/// Handles the Activated event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void Window_Activated(object sender, EventArgs e)
		{
			if(Properties.Settings.Default.Animate)
			{
				ObjectAnimationUsingKeyFrames visi = new ObjectAnimationUsingKeyFrames();
				visi.Duration = lasts;
				visi.KeyFrames.Add(visKeyVisible);

				// Go!
				this.BeginAnimation(HeightProperty, showHeightAnimation, HandoffBehavior.SnapshotAndReplace);
				this.BeginAnimation(OpacityProperty, showOpacityAnimation, HandoffBehavior.SnapshotAndReplace);
				this.BeginAnimation(VisibilityProperty, visi, HandoffBehavior.SnapshotAndReplace);
			}
			buffer.Focus();
		}


		bool IsClosing = false;

		private static Duration lasts = Duration.Automatic;
		private DoubleAnimation hideHeightAnimations = new DoubleAnimation(0.0, lasts);
		private DoubleAnimation hideOpacityAnimations = new DoubleAnimation(0.0, lasts);
		private DoubleAnimation showOpacityAnimation = new DoubleAnimation(1.0, lasts);
		private DoubleAnimation showHeightAnimation = new DoubleAnimation(1.0, lasts);
		private static DiscreteObjectKeyFrame visKeyHidden = new DiscreteObjectKeyFrame(Visibility.Hidden, KeyTime.FromPercent(1.0));
		private static DiscreteObjectKeyFrame visKeyVisible = new DiscreteObjectKeyFrame(Visibility.Visible, KeyTime.FromPercent(0.0));
		private static DiscreteBooleanKeyFrame trueEndFrame = new DiscreteBooleanKeyFrame(true, KeyTime.FromPercent(1.0));
		/// <summary>
		/// Handles the Deactivated event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void Window_Deactivated(object sender, EventArgs e)
		{
			if(!IsClosing && Properties.Settings.Default.Animate)
			{
				ObjectAnimationUsingKeyFrames visi = new ObjectAnimationUsingKeyFrames();
				visi.Duration = lasts;
				visi.KeyFrames.Add(visKeyHidden);

				hideOpacityAnimations.AccelerationRatio = 0.25;
				hideHeightAnimations.AccelerationRatio = 0.5;
				showOpacityAnimation.AccelerationRatio = 0.25;
				showHeightAnimation.AccelerationRatio = 0.5;

				// before we start animating, set the animation endpoints to the current values.
				hideOpacityAnimations.From = showOpacityAnimation.To = (double)this.GetAnimationBaseValue(OpacityProperty);
				hideHeightAnimations.From = showHeightAnimation.To = (double)this.GetAnimationBaseValue(HeightProperty);

				// GO!
				this.BeginAnimation(HeightProperty, hideHeightAnimations, HandoffBehavior.SnapshotAndReplace);
				this.BeginAnimation(OpacityProperty, hideOpacityAnimations, HandoffBehavior.SnapshotAndReplace);
				this.BeginAnimation(VisibilityProperty, visi, HandoffBehavior.SnapshotAndReplace);
			}
		}



		/// <summary>
		/// Handles the LocationChanged event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void Window_LocationChanged(object sender, EventArgs e)
		{
			//System.Windows.SystemParameters.VirtualScreenHeight
			if(Properties.Settings.Default.SnapToScreenEdge)
			{
				CornerRadius radi = new CornerRadius(5.0);

                Rect workarea = new Rect(SystemParameters.VirtualScreenLeft,
                                          SystemParameters.VirtualScreenTop,
                                          SystemParameters.VirtualScreenWidth,
                                          SystemParameters.VirtualScreenHeight);

				if(Properties.Settings.Default.SnapDistance > 0)
				{
					if(this.Left - workarea.Left < Properties.Settings.Default.SnapDistance) this.Left = workarea.Left;
					if(this.Top - workarea.Top < Properties.Settings.Default.SnapDistance) this.Top = workarea.Top;
					if(workarea.Right - this.RestoreBounds.Right < Properties.Settings.Default.SnapDistance) this.Left = workarea.Right - this.RestoreBounds.Width;
					if(workarea.Bottom - this.RestoreBounds.Bottom < Properties.Settings.Default.SnapDistance) this.Top = workarea.Bottom - this.RestoreBounds.Height;
				}

				if(this.Left <= workarea.Left)
				{
					radi.BottomLeft = 0.0;
					radi.TopLeft = 0.0;
					this.Left = workarea.Left;
				}
				if(this.Top <= workarea.Top)
				{
					radi.TopLeft = 0.0;
					radi.TopRight = 0.0;
					this.Top = workarea.Top;
				}
				if(this.RestoreBounds.Right >= workarea.Right)
				{
					radi.TopRight = 0.0;
					radi.BottomRight = 0.0;
					this.Left = workarea.Right - this.RestoreBounds.Width;
				}
				if(this.RestoreBounds.Bottom >= workarea.Bottom)
				{
					radi.BottomRight = 0.0;
					radi.BottomLeft = 0.0;
					this.Top = workarea.Bottom - this.RestoreBounds.Height;
				}

				Border border = (Border)buffer.Template.FindName("Border", buffer);
				if(border != null)
				{
					border.CornerRadius = radi;
				}
			}


			Properties.Settings.Default.WindowLeft = Left;
			Properties.Settings.Default.WindowTop = Top;
			RecalculateSizes();
		}


		/// <summary>
		/// Handles the Closing event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			IsClosing = true;

			Properties.Settings.Default.WindowStyle = WindowStyle;
			Properties.Settings.Default.ShowInTaskbar = ShowInTaskbar;
			Properties.Settings.Default.FocusKey = FocusKey;

			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// Handles the SizeChanged event of the Window control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.SizeChangedEventArgs"/> instance containing the event data.</param>
		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			// we only recalculate when something other than animation changes the window size
			double h = (double)this.GetAnimationBaseValue(HeightProperty);
			if(Properties.Settings.Default.WindowHeight != h)
			{
				Properties.Settings.Default.WindowHeight = h;
				Properties.Settings.Default.WindowWidth = (double)this.GetAnimationBaseValue(WidthProperty);
				RecalculateSizes();
			}
		}


		private delegate void VoidVoidDelegate();

		/// <summary>
		/// Handles the HotkeyPressed event from the Hotkey Manager
		/// </summary>
		/// <param name="window">The window.</param>
		/// <param name="hotkey">The hotkey.</param>
		void Hotkey_Pressed(Window window, Hotkey hotkey)
		{
			if(hotkey.Equals(FocusKey))
			{
				if(!IsActive)
				{
					Activate();
					if(Properties.Settings.Default.Animate)
					{
						Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new VoidVoidDelegate(delegate { buffer.Focus(); }));
					}
				}
				else
				{
					Win32.Application.ActivateNextWindow();
				}
			}
		}

	}
}