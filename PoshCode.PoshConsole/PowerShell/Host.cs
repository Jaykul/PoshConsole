using PoshCode.Controls;
using PoshCode.Native;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;

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
        private NativeConsole _nativeConsole;
        /// <summary>
        /// This API is called before an external application process is started.
        /// </summary>
        int _native;

        internal PoshConsole PoshConsole { get; }

        /// <summary>
        /// Store the window title
        /// </summary>
        private string _savedTitle = String.Empty;
        private readonly PSHostUserInterface _UI;
        private readonly Options _options;

        internal Host(PoshConsole control, Options options)
        {
            PoshConsole = control;
            _options = options;
            _UI = new HostUI(PoshConsole);

            MakeConsole();
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
            // TODO: properly implement PSHost.SetShouldExit
            KillConsole();
            // throw new NotImplementedException("Cannot Exit, I'm just a Control");
        }

        /// <summary>
        /// Not implemented by PoshCode.Console yet. The call fails with an exception.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "EnterNestedPrompt"), SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "MyHost")]
        public override void EnterNestedPrompt()
        {
            // TODO: IMPLEMENT PSHost.EnterNestedPrompt()
            throw new NotImplementedException("Cannot suspend the shell, EnterNestedPrompt() method is not implemented by MyHost.");
        }

        /// <summary>
        /// Not implemented by PoshCode.Console yet. The call fails with an exception.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ExitNestedPrompt"), SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "MyHost")]
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


        public void WriteNativeOutput(string message)
        {
            // Write is Dispatcher checked
            PoshConsole.Write(ConsoleBrushes.NativeOutputForeground, ConsoleBrushes.NativeOutputBackground, message, PoshConsole.Current);
            PoshConsole.Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)(()=>PoshConsole.SetPrompt()));
            // TODO: REIMPLEMENT NATIVE prompt using Begin/End and Prompt()
        }

        public void WriteNativeError(string message)
        {
            // Write is Dispatcher checked
            PoshConsole.Write(ConsoleBrushes.NativeErrorForeground, ConsoleBrushes.NativeErrorBackground, message, PoshConsole.Current);
            PoshConsole.Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)(() => PoshConsole.SetPrompt()));
        }


        private void MakeConsole()
        {
            if (_nativeConsole == null)
            {
                try
                {
                    // don't initialize in the constructor
                    _nativeConsole = new NativeConsole(false);
                    // this way, we can handle (report) any exception...
                    _nativeConsole.Initialize();
                    _nativeConsole.WriteOutput += (source, args) => WriteNativeOutput(args.Text.TrimEnd('\n'));
                    _nativeConsole.WriteError += (source, args) => WriteNativeError(args.Text.TrimEnd('\n'));
                }
                catch (ConsoleInteropException cie)
                {
                    WriteNativeError("Couldn't initialize the Native Console: " + cie.Message);
                }
            }
        }

        public void KillConsole()
        {
            _nativeConsole?.Dispose();
            _nativeConsole = null;
        }


        public void Dispose()
        {
            KillConsole();
        }
    }
}
