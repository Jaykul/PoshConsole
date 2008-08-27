using System;
using System.Security;
using System.Xml;
using System.IO;
using Huddled.WPF.Controls.Interfaces;

namespace Huddled.WPF.Controls.Interfaces
{
   [Serializable]
   public enum ConsoleScrollBarVisibility
   {
      Disabled = 0,
      Auto = 1,
      Hidden = 2,
      Visible = 3,
   }

   public delegate void CommandHandler(string commandLine);

   public enum CommandResults
   {
      Stopped, Failed, Completed
   }

   public interface IPoshConsoleControl : IPSXamlConsole, IPSConsole
   {
      event CommandHandler ProcessCommand;

      void CommandFinished(System.Management.Automation.Runspaces.PipelineState results);
      void Prompt(string text);

      string CurrentCommand { get; set; }

      // TODO: reimplement History and TabExpansion
      //CommandHistory History { get; }
      //TabExpansion Expander { get; set; }

      ConsoleScrollBarVisibility VerticalScrollBarVisibility { get; set; }
      ConsoleScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
   }
}