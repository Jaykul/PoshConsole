using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Huddled.Interop.Hotkeys;
using System.Management.Automation;
using PoshConsole.Controls;
using Huddled.WPF.Controls;

namespace PoshConsole.PSHost
{
   /// <summary>
   /// A <see cref="WindowCommand"/> which toggles the visibility of the window
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
   }
}
