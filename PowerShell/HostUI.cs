using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using PoshCode.Wpf.Controls;

namespace PoshCode.PowerShell
{
    class HostUI : PSHostUserInterface
    {
        private readonly ConsoleControl _control;
        private readonly PSHostRawUserInterface _rawUI;

        private readonly ConsoleBrushes _brushes;


        public HostUI(ConsoleControl control, Panel progress)
        {
            ProgressPanel = progress;
            _brushes = new ConsoleBrushes();
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
        public bool WaitingForInput;

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
            return _control.PromptForCredentialInline(caption, message, userName, targetName);
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            // TODO: allow overriding with an event handler
            return _control.PromptForCredentialInline(caption, message, userName, targetName, allowedCredentialTypes, options);
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            return _control.PromptForChoice(caption, message, choices, defaultChoice);
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            return _control.Prompt(caption, message, descriptions);
        }


        public override void Write(string message)
        {
            _control.Write(null, null, message);
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string message)
        {
            // Write is Dispatcher checked
            _control.Write(foregroundColor, backgroundColor, message);
        }

        public override void WriteLine(string message)
        {
            _control.Write(message + "\n");
        }

        public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string message)
        {
            // Write is Dispatcher checked
            _control.Write(foregroundColor, backgroundColor, message + "\n");
        }

        public override void WriteDebugLine(string message)
        {
            // Write is Dispatcher checked
            _control.Write(_brushes.DebugForeground, _brushes.DebugBackground, String.Format("DEBUG: {0}\n", message));
        }

        public override void WriteErrorLine(string message)
        {
            // Write is Dispatcher checked
            _control.Write(_brushes.ErrorForeground, _brushes.ErrorBackground, message + "\n");
        }

        public override void WriteVerboseLine(string message)
        {
            // Write is Dispatcher checked
            _control.Write(_brushes.VerboseForeground, _brushes.VerboseBackground, String.Format("VERBOSE: {0}\n", message));
        }

        public override void WriteWarningLine(string message)
        {
            // Write is Dispatcher checked
            _control.Write(_brushes.WarningForeground, _brushes.WarningBackground, String.Format("WARNING: {0}\n", message), _control.Current);
        }


        protected Dictionary<int, ProgressPanel> ProgressRecords = new Dictionary<int, ProgressPanel>();


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
