using System;
using System.Security;
using System.Windows.Documents;
using System.Xml;
using System.IO;
using Huddled.Wpf.Controls.Interfaces;
using System.Windows.Controls;
using System.Management.Automation.Runspaces;
using Huddled.Wpf.Controls;


namespace System.Management.Automation.Host
{
	public interface IPoshConsoleControl : IPSWpfConsole, IPSConsole
	{
		event CommmandDelegate Command;
      event PipelineFinished Finished;

      void OnCommandFinished(String command, PipelineState results);
		void Prompt(string text);

		string CurrentCommand { get; set; }
		RichTextBox CommandBox { get; }
		System.Windows.Media.Color CaretColor { get; set; }

		CommandHistory History { get; }
		TabExpansion Expander { get; set; }

		ConsoleScrollBarVisibility VerticalScrollBarVisibility { get; set; }
		ConsoleScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
	}
}
namespace Huddled.Wpf.Controls
{

   namespace Interfaces
   {
      [Serializable]
      public enum ConsoleScrollBarVisibility
      {
         Disabled = 0,
         Auto = 1,
         Hidden = 2,
         Visible = 3,
      }

      public enum CommandResults
      {
         Stopped, Failed, Completed
      }

   }
   public delegate void CommmandDelegate(Object source, CommandEventArgs command);
   public delegate void PipelineFinished(Object source, FinishedEventArgs results);

   public class FinishedEventArgs
   {
      public string Command;
      public PipelineState Results;
   }
   public class CommandEventArgs
   {
      public string Command;
      public Block OutputBlock;
   }
}