using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation.Host;
using System.Globalization;
using System.Management.Automation;
using PoshCode.Native;
using System.Reflection;
using System.Threading;
using ICSharpCode.AvalonEdit;
using System.Management.Automation.Runspaces;

namespace PoshCode.PowerShell
{
	public class Host : PSHost, IDisposable
	{
		private readonly CultureInfo _originalCultureInfo = Thread.CurrentThread.CurrentCulture;
		private readonly CultureInfo _originalUICultureInfo = Thread.CurrentThread.CurrentUICulture;
		private static readonly Guid _INSTANCE_ID = Guid.NewGuid();

		/// <summary>
		/// A Console Window wrapper that hides the console
		/// </summary>
		private NativeConsole _console;
		/// <summary>
		/// This API is called before an external application process is started.
		/// </summary>
		int _native;

		/// <summary>
		/// Store the window title 
		/// </summary>
		private string _savedTitle = String.Empty;
		private PSHostUserInterface _UI;
		private TextEditor _control;
		private Command _outDefault;
	    private Options _options;

	    internal Host(TextEditor control, Options options)
		{
			_control = control;
		    _options = options;
	        _UI = new HostUI(_control);
			MakeConsole();

			// pre-create this
			_outDefault = new Command("Out-Default");
			// for now, merge the errors with the rest of the output
			_outDefault.MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
			_outDefault.MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Error | PipelineResultTypes.Output;

		}
		/// <summary>
		/// Return the culture info to use - this implementation just snapshots the
		/// culture info of the thread that created this object.
		/// </summary>
		public override CultureInfo CurrentCulture
		{
			get { return _originalCultureInfo; }
		}

		/// <summary>
		/// Return the UI culture info to use - this implementation just snapshots the
		/// UI culture info of the thread that created this object.
		/// </summary>
		public override CultureInfo CurrentUICulture
		{
			get { return _originalUICultureInfo; }
		}

		/// <summary>
		/// This implementation always returns the GUID allocated at instantiation time.
		/// </summary>
		public override Guid InstanceId
		{
			get { return _INSTANCE_ID; }
		}

		/// <summary>
		/// Return an appropriate string to identify your host implementation.
		/// Keep in mind that this string may be used by script writers to identify
		/// when your host is being used.
		/// </summary>
		public override string Name
		{
			get { return "PoshCode.Console.Host"; }
		}

        public override PSObject PrivateData
        {
            get
            {
                return PSObject.AsPSObject(_options);
            }
        }
		public override PSHostUserInterface UI
		{
			get
			{
				return _UI;
			}
		}

		/// <summary>
		/// Return the version object for this application. Typically this should match the version
		/// resource in the application.
		/// </summary>
		public override Version Version
		{
			get
			{
				return Assembly.GetEntryAssembly().GetName().Version;
			}
		}


		/// <summary>
		/// Indicate to the host application that exit has been requested. Pass the exit code that the host application should use when exiting the process.
		/// </summary>
		/// <param name="exitCode"></param>
		public override void SetShouldExit(int exitCode)
		{
			KillConsole();
			// TODO: IMPLEMENT PSHost.SetShouldExit
			throw new NotImplementedException("Cannot Exit, I'm just a Control");
		}

		/// <summary>
		/// Not implemented by PoshCode.Console yet. The call fails with an exception.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "EnterNestedPrompt"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "MyHost")]
		public override void EnterNestedPrompt()
		{
			// TODO: IMPLEMENT PSHost.EnterNestedPrompt()
			throw new NotImplementedException("Cannot suspend the shell, EnterNestedPrompt() method is not implemented by MyHost.");
		}

		/// <summary>
		/// Not implemented by PoshCode.Console yet. The call fails with an exception.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ExitNestedPrompt"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "MyHost")]
		public override void ExitNestedPrompt()
		{
			// TODO: IMPLEMENT PSHost.ExitNestedPrompt()
			throw new NotImplementedException("The ExitNestedPrompt() method is not implemented by MyHost.");
		}

		public override void NotifyBeginApplication()
		{
			_savedTitle = UI.RawUI.WindowTitle;

			_native++;
			//MakeConsole();
		}

		/// <summary>
		/// This API is called after an external application process finishes.
		/// </summary>
		public override void NotifyEndApplication()
		{
			UI.RawUI.WindowTitle = _savedTitle;

			_native--;
			//if (_native == 0) KillConsole();
		}


		private void MakeConsole()
		{
			if (_console == null)
			{
				try
				{
					// don't initialize in the constructor
					_console = new NativeConsole(false);
					// this way, we can handle (report) any exception...
					_console.Initialize();
//TODO:					_console.WriteOutput += (source, args) => _buffer.WriteNativeOutput(args.Text.TrimEnd('\n'));
//TODO:					_console.WriteError += (source, args) => _buffer.WriteNativeError(args.Text.TrimEnd('\n'));
				}
				catch (ConsoleInteropException /*cie*/)
				{
//TODO:					_buffer.WriteErrorRecord(new ErrorRecord(cie, "Couldn't initialize the Native Console", ErrorCategory.ResourceUnavailable, null));
				}
			}
		}

		public void KillConsole()
		{
			if (_console != null)
			{
				_console.Dispose();
				// TODO: _runner.Dispose();
			}
			_console = null;
		}


		public void Dispose()
		{
			KillConsole();
		}
	}
}
