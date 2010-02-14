using System;
using System.Management.Automation;
using Huddled.Wpf;
using Huddled.WPF.Controls;

namespace PoshConsole.Host
{
   /// <summary>
   /// A <see cref="WindowCommand"/> which executes a script
   /// </summary>
   public class ScriptCommand : WindowCommand
   {
      private ScriptBlock _script;
      private CommmandDelegate _handler;
      public ScriptCommand(CommmandDelegate handler, ScriptBlock script)
         : base()
      {
         _handler = handler;
         _script = script;
      }

      protected override void IfNoHandlerOnCanExecute(object window, WindowCanExecuteArgs e)
      {
         e.CanExecute = e.Window.IsLoaded;
      }

      protected override void IfNoHandlerOnExecute(object window, WindowOnExecuteArgs e)
      {
         _handler.Invoke(window, new CommandEventArgs { Command = _script.ToString() });
      }

      public static implicit operator ScriptBlock(ScriptCommand command)
      {
         return command._script;
      } 
   }
}
