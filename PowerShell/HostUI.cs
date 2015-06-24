using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Host;
using System.Text;

namespace PoshCode.PowerShell
{
	class HostUI : PSHostUserInterface
	{
		private readonly ICSharpCode.AvalonEdit.TextEditor _control;
		private readonly PSHostRawUserInterface _rawUI;

		private readonly ConsoleBrushes _brushes;
		

		public HostUI(ICSharpCode.AvalonEdit.TextEditor control)
		{
			this._brushes = new ConsoleBrushes();
			this._control = control;
			this._rawUI = new HostRawUI(control);
		}
		public override Dictionary<string, System.Management.Automation.PSObject> Prompt(string caption, string message, System.Collections.ObjectModel.Collection<FieldDescription> descriptions)
		{
			throw new NotImplementedException();
		}

		public override int PromptForChoice(string caption, string message, System.Collections.ObjectModel.Collection<ChoiceDescription> choices, int defaultChoice)
		{
			throw new NotImplementedException();
		}

		public override System.Management.Automation.PSCredential PromptForCredential(string caption, string message, string userName, string targetName, System.Management.Automation.PSCredentialTypes allowedCredentialTypes, System.Management.Automation.PSCredentialUIOptions options)
		{
			throw new NotImplementedException();
		}

		public override System.Management.Automation.PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
		{
			throw new NotImplementedException();
		}

		public override PSHostRawUserInterface RawUI
		{
			get { return _rawUI; }
		}

		public override string ReadLine()
		{
			throw new NotImplementedException();
		}

		public override System.Security.SecureString ReadLineAsSecureString()
		{
			throw new NotImplementedException();
		}

		public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
		{
			_control.AppendText(value);
		}

		public override void Write(string value)
		{
			Write(ConsoleColor.Black, ConsoleColor.White, value);
		}

		public override void WriteDebugLine(string message)
		{
			Write(ConsoleColor.DarkYellow, ConsoleColor.White, string.Format("DEBUG: {0}\n", message));
		}

		public override void WriteErrorLine(string value)
		{
			Write(ConsoleColor.Red, ConsoleColor.White, string.Format("ERROR: {0}\n", value));
		}

		public override void WriteLine(string value)
		{
			Write(_rawUI.ForegroundColor, _rawUI.BackgroundColor, value + "\n");
		}

		public override void WriteProgress(long sourceId, System.Management.Automation.ProgressRecord record)
		{
            if (!string.IsNullOrEmpty(record.Activity))
                Write(_rawUI.ForegroundColor, _rawUI.BackgroundColor, record.Activity + "\n");

            if (!string.IsNullOrEmpty(record.StatusDescription))
                Write(_rawUI.ForegroundColor, _rawUI.BackgroundColor, record.StatusDescription + "\n");

            if (record.PercentComplete > 0)
                Write(_rawUI.ForegroundColor, _rawUI.BackgroundColor, record.PercentComplete + " Percent Complete \n");
        }

		public override void WriteVerboseLine(string message)
		{
			Write(ConsoleColor.Cyan, ConsoleColor.White, string.Format("VERBOSE: {0}\n", message));
		}

		public override void WriteWarningLine(string message)
		{
			Write(ConsoleColor.DarkRed, ConsoleColor.White, string.Format("WARNING: {0}\n", message));
		}
	}
}
