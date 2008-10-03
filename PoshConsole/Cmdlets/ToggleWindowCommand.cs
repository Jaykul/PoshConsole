using System;
using System.Management.Automation;

namespace PoshConsole.Cmdlets
{
   // BugBug: "Toggle" is not an official verb. But "Show" and "Hide" are ...
   [Cmdlet("Toggle", "Window", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None)]
   public class ToggleWindowCommand : PSCmdlet
   {
      #region Parameters
      [Parameter(HelpMessage = "Toggle the window visible")]
      public SwitchParameter ShowWindow{ get; set; }

      [Parameter(HelpMessage = "Toggle the window invisible")]
      public SwitchParameter HideWindow { get; set; }
      #endregion

      #region Override Methods

      protected override void ProcessRecord()
      {
         try
         {
            if(ShowWindow.IsPresent)
            {
               Huddled.Interop.Hotkeys.GlobalCommands.ShowWindow.Execute(((PSHost.PoshOptions)Host.PrivateData.BaseObject).XamlUI.RootWindow);               
            } 
            else if(HideWindow.IsPresent)
            {
               Huddled.Interop.Hotkeys.GlobalCommands.HideWindow.Execute(((PSHost.PoshOptions)Host.PrivateData.BaseObject).XamlUI.RootWindow);
            } else
            {
               Huddled.Interop.Hotkeys.GlobalCommands.ToggleWindow.Execute(((PSHost.PoshOptions)Host.PrivateData.BaseObject).XamlUI.RootWindow);
            }
         }
         catch (Exception ex)
         {
            ThrowTerminatingError(new ErrorRecord(ex, "Exception Thrown", ErrorCategory.NotSpecified, this));
         }
      }

      #endregion [rgn]
   }
}
