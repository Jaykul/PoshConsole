using System;
using System.Security;
using System.Windows.Documents;
using System.Xml;
using System.IO;
using Huddled.WPF.Controls.Interfaces;

namespace Huddled.WPF.Controls
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

      public interface IPoshConsoleControl : IPSXamlConsole, IPSConsole
      {
         event CommmandDelegate Command;

         void CommandFinished(System.Management.Automation.Runspaces.PipelineState results);
         void Prompt(string text);

         string CurrentCommand { get; set; }

         // TODO: reimplement History and TabExpansion
         CommandHistory History { get; }
         TabExpansion Expander { get; set; }

         // TODO: reimplement scrollbar visibility options
         //ConsoleScrollBarVisibility VerticalScrollBarVisibility { get; set; }
         //ConsoleScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
      }
   }
   public delegate void CommmandDelegate(Object source, CommandEventArgs command);

   public class CommandEventArgs
   {
      public string Command;
      public Block OutputBlock;
   }
}