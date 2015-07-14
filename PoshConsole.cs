using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;
using System.Windows;
using PoshCode.PowerShell;

namespace PoshCode
{
    public class PoshConsole : ICSharpCode.AvalonEdit.TextEditor, IDisposable, IContentControl
    {
        private Runspace _runSpace;
        private const string DefaultPrompt = "PS>";
        // private string hostName = "PoshConsole";
        private Host _host;

        public void Dispose()
        {
            if (_host != null)
            {
                _host.Dispose();
                _host = null;
            }
        }

        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content", typeof (object), typeof (PoshConsole), new PropertyMetadata(default(object)));

        public object Content
        {
            get { return (object) GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            _host = new Host(this, new Options(this));

            // pre-create this
            DefaultOutputCommand = new Command("Out-Default");
            // for now, merge the errors with the rest of the output
            DefaultOutputCommand.MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
            DefaultOutputCommand.MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Error |
                                                                        PipelineResultTypes.Output;

            // pre-create this
            ContentOutputCommand = new Command("Out-PoshConsole");
            // for now, merge the errors with the rest of the output
            ContentOutputCommand.MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
            ContentOutputCommand.MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Error |
                                                                        PipelineResultTypes.Output;


            // Create the default initial session state and add the module.
            InitialSessionState iss = InitialSessionState.CreateDefault();

            string currentUserProfilePath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WindowsPowerShell");
            string allUsersProfilePath = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell\\v1.0");


            // Import the PoshWPF module automatically
            // iss.ImportPSModule(new[] { Path.Combine(Path.GetDirectoryName(poshModule.Location), "PoshWpf.dll") });

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("%PSModulePath%")))
            {
                Environment.SetEnvironmentVariable("PSModulePath", string.Format("{0};{1};{2}",
                    Path.Combine(currentUserProfilePath, "Modules"),
                    Path.Combine(allUsersProfilePath, "Modules"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                        "WindowsPowerShell\\Modules")));
            }


            // We need STA so we can do WPF stuff from our console thread.
            iss.ApartmentState = ApartmentState.STA;
            // We need ReuseThread so that we behave, well, the way that PowerShell.exe and ISE do.
            // We need UseCurrentThread so the output can go in our UI (not sure how to fix that).
            iss.ThreadOptions = PSThreadOptions.UseCurrentThread;
            // iss.Variables
            // Load all the Cmdlets that are in this assembly automatically.
            Assembly entryAssembly = Assembly.GetExecutingAssembly();

            string path = Path.GetDirectoryName(entryAssembly.Location);
            if (!string.IsNullOrEmpty(path))
            {
                // because this should work with PowerShell2, we can't just use ImportPSModulesFromPath
                path = Path.Combine(path, "Modules");
                if (Directory.Exists(path))
                {
                    iss.ImportPSModule(Directory.GetDirectories(path));
                }
                else
                {
                    // Load the A2A.Management module
                    path = Path.Combine(path, "A2A.Management.psd1");
                    if(File.Exists(path))
                    {
                        iss.ImportPSModule(new []{path});
                    }
                }
            }
            iss.LoadCmdlets(entryAssembly);



            /* TODO: Make sure we actually run the profiles, then re-enable this...
         * 
        var profile = new PSObject(Path.GetFullPath(Path.Combine(currentUserProfilePath, hostName + "_profile.ps1")));
        //* %windir%\system32\WindowsPowerShell\v1.0\profile.ps1
        //  This profile applies to all users and all shells.
        profile.Properties.Add(new PSNoteProperty("AllUsersAllHosts",
                                                  Path.GetFullPath(Path.Combine(allUsersProfilePath, "Profile.ps1"))));
        //* %windir%\system32\WindowsPowerShell\v1.0\PoshConsole_profile.ps1
        //  This profile applies to all users, but only to the Current shell.
        profile.Properties.Add(new PSNoteProperty("AllUsersCurrentHost",
                                                  Path.GetFullPath(Path.Combine(allUsersProfilePath,
                                                                                hostName + "_profile.ps1"))));
        //* %UserProfile%\My Documents\WindowsPowerShell\profile.ps1
        //  This profile applies only to the current user, but affects all shells.
        profile.Properties.Add(new PSNoteProperty("CurrentUserAllHosts",
                                                  Path.GetFullPath(Path.Combine(currentUserProfilePath, "Profile.ps1"))));
        //* %UserProfile%\My Documents\WindowsPowerShell\PoshConsole_profile.ps1
        //  This profile applies only to the current user and the Current shell.
        profile.Properties.Add(new PSNoteProperty("CurrentUserCurrentHost", profile.ImmediateBaseObject));

        iss.Variables.Add(new SessionStateVariableEntry("profile", profile,
                                                        "The enumeration of all the available profiles the user could edit."));
        */

            _runSpace = RunspaceFactory.CreateRunspace(_host, iss);


            // TODO: can we handle profiles this way??
            /*
           RunspaceConfiguration conf = RunspaceConfiguration.Create();
           conf.InitializationScripts.Append(new ScriptConfigurationEntry("ImportPoshWpf", "$Foo = 'This is foo'")); // Import-Module .\\PoshWPF.dll
           _runSpace = RunspaceFactory.CreateRunspace(host, conf);
        */

            // Set the default runspace, so that event handlers (and Tasks) can run in the same runspace as commands.
            Runspace.DefaultRunspace = _runSpace;
            // Don't openAsync, it fails to load the modules (in time?)
            _runSpace.Open();
            AppendText(DefaultPrompt + " ");
        }

        //        public async Task<PSDataCollection<PSObject>> InvokeCommand(string command)
        public Collection<PSObject> InvokeCommand(string command, bool contentOutput = false)
        {
            AppendText(command + "\n");
            var pipeline = _runSpace.CreatePipeline(command, true);

            //var result = await Task.Factory.FromAsync(_shell.AddScript(command).BeginInvoke(), handle => _shell.EndInvoke(handle));
            return InvokePipeline(pipeline, contentOutput);
        }

        public Collection<PSObject> InvokeCommand(Command command, bool contentOutput = false)
        {
            // Echo to console
            AppendText(command.CommandText + "\n");

            var pipeline = _runSpace.CreatePipeline();
            pipeline.Commands.Add(command);
            return InvokePipeline(pipeline, contentOutput);
        }

        private Collection<PSObject> InvokePipeline(Pipeline pipeline, bool contentOutput = false)
        {
            if(contentOutput)
                pipeline.Commands.Add(ContentOutputCommand);

            pipeline.Commands.Add(DefaultOutputCommand);

            Collection<PSObject> result = null;
            try
            {
                result = pipeline.Invoke();
            }
            catch (Exception pe)
            {
                _host.UI.WriteErrorLine(pe.Message);
            }
            AppendText("\n" + DefaultPrompt + " ");
            return result;
        }

        public Command DefaultOutputCommand { get; set; }
        public Command ContentOutputCommand { get; set; }

    }

    public interface IContentControl
    {
        object Content { get; set; }
    }
}