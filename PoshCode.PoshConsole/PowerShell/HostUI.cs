using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Windows.Controls;
using System.Windows.Threading;
using PoshCode.Controls;
using PoshCode.Interop;
using PoshCode.Properties;

namespace PoshCode.PowerShell
{
    class HostUI : PSHostUserInterface
    {
        private readonly ConsoleControl _control;
        private readonly PSHostRawUserInterface _rawUI;
        

        public HostUI(ConsoleControl control, Panel progress)
        {
            ProgressPanel = progress;
            _control = control;
            _rawUI = new HostRawUI(control);
        }

        // Possibly an alternative panel that pops up and can be closed?
        #region IPSConsole Members

        public override PSHostRawUserInterface RawUI
        {
            get { return _rawUI; }
        }

        #region ReadLine
        /// <summary>
        /// Provides a way for scripts to request user input ...
        /// </summary>
        /// <returns></returns>
        public override string ReadLine()
        {
            return _control.ReadLine();
        }

        public override SecureString ReadLineAsSecureString()
        {
            return _control.ReadLineAsSecureString();
        }
        #endregion ReadLine

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            // TODO: allow overriding with an event handler
            if (Settings.Default.UseCredentialUI)
            {
                return CredentialUI.PromptForWindowsCredentials(caption, message, IntPtr.Zero, userName, string.Empty);
            }
            else
            {
                return _control.PromptForCredentialInline(caption, message, userName, targetName);
            }
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            if (Settings.Default.UseCredentialUI)
            {
                // TODO: allow overriding with an event handler
                if (string.IsNullOrEmpty(targetName))
                    targetName = Environment.UserDomainName;

                if (string.IsNullOrEmpty(caption))
                    caption = "Credentials";

                if (string.IsNullOrEmpty(message))
                    message = "Please enter your credentials";

                var credentialsOptions = new CredentialUI.PromptForCredentialsOptions(targetName, caption, message);

                if (allowedCredentialTypes == PSCredentialTypes.Generic || allowedCredentialTypes == PSCredentialTypes.Default)
                    credentialsOptions.Flags |= CredentialUI.PromptForCredentialsFlag.CREDUI_FLAGS_GENERIC_CREDENTIALS;

                if (options.HasFlag(PSCredentialUIOptions.AlwaysPrompt))
                    credentialsOptions.Flags |= CredentialUI.PromptForCredentialsFlag.CREDUI_FLAGS_ALWAYS_SHOW_UI;

                if (options.HasFlag(PSCredentialUIOptions.ReadOnlyUserName))
                    credentialsOptions.Flags |= CredentialUI.PromptForCredentialsFlag.CREDUI_FLAGS_KEEP_USERNAME;

                if (options.HasFlag(PSCredentialUIOptions.ValidateUserNameSyntax) && allowedCredentialTypes == PSCredentialTypes.Generic)
                    credentialsOptions.Flags |= CredentialUI.PromptForCredentialsFlag.CREDUI_FLAGS_VALIDATE_USERNAME;

                return CredentialUI.PromptForCredentials(credentialsOptions, userName, String.Empty);
            }
            else
            {
                return _control.PromptForCredentialInline(caption, message, userName, targetName, allowedCredentialTypes, options);
            }
            //CredentialUI.PromptForWindowsCredentials()
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            return _control.OnPromptForChoice(new PromptForChoiceEventArgs(caption, message, choices, defaultChoice));
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            return _control.OnPromptForObject(new PromptForObjectEventArgs(caption, message, descriptions));
        }


        public override void Write(string value)
        {
            _control.Write(null, null, value);
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            // Write is Dispatcher checked
            _control.Write(foregroundColor, backgroundColor, value);
        }

        public override void WriteLine(string value)
        {
            _control.Write(value + "\n");
        }

        public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            // Write is Dispatcher checked
            _control.Write(foregroundColor, backgroundColor, value + "\n");
        }

        public override void WriteDebugLine(string message)
        {
            // Write is Dispatcher checked
            _control.Write(ConsoleBrushes.DebugForeground, ConsoleBrushes.DebugBackground, $"DEBUG: {message}\n");
        }

        public override void WriteErrorLine(string value)
        {
            // Write is Dispatcher checked
            _control.Write(ConsoleBrushes.ErrorForeground, ConsoleBrushes.ErrorBackground, value + "\n");
        }

        public override void WriteVerboseLine(string message)
        {
            // Write is Dispatcher checked
            _control.Write(ConsoleBrushes.VerboseForeground, ConsoleBrushes.VerboseBackground, $"VERBOSE: {message}\n");
        }

        public override void WriteWarningLine(string message)
        {
            // Write is Dispatcher checked
            _control.Write(ConsoleBrushes.WarningForeground, ConsoleBrushes.WarningBackground, $"WARNING: {message}\n", _control.Current);
        }


        protected readonly Dictionary<int, ProgressPanel> ProgressRecords = new Dictionary<int, ProgressPanel>();


        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            if (ProgressPanel != null)
            {
                if (!_control.Dispatcher.CheckAccess())
                {
                    _control.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => WriteProgress(sourceId, record)));
                }
                else
                {
                    if (ProgressRecords.ContainsKey(record.ActivityId))
                    {
                        if (record.RecordType == ProgressRecordType.Completed)
                        {
                            ProgressPanel.Children.Remove(ProgressRecords[record.ActivityId]);
                            ProgressRecords.Remove(record.ActivityId);
                        }
                        else
                        {
                            ProgressRecords[record.ActivityId].Record = record;
                        }
                    }
                    else
                    {
                        ProgressRecords[record.ActivityId] = new ProgressPanel(record);
                        if (record.ParentActivityId < 0 || !ProgressRecords.ContainsKey(record.ParentActivityId))
                        {
                            ProgressPanel.Children.Add(ProgressRecords[record.ActivityId]);
                        }
                        else
                        {
                            ProgressPanel.Children.Insert(ProgressPanel.Children.IndexOf(ProgressRecords[record.ParentActivityId]) + 1, ProgressRecords[record.ActivityId]);
                        }
                    }
                }
            }
        }

        public Panel ProgressPanel { get; private set; }

        #endregion

    }
}
