using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using Huddled.Interop.Hotkeys;
using System.Windows.Input;
using PoshConsole.PSHost;

namespace PoshConsole.Cmdlets
{
   [Cmdlet(VerbsCommon.Add, "Hotkey", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "KeyBinding")]
   public class AddHotkeyCommand : PSCmdlet
   {
      #region Parameters
      [Parameter(
         Position = 0,
         ParameterSetName = "KeyGesture",
         Mandatory = true,
         ValueFromPipeline = true,
         HelpMessage = "The keystroke to bind")]
      [Alias("KeyStroke")]
      public KeyGesture KeyGesture { get; set; }

      //       [Parameter(
      //   Position = 1,
      //   ParameterSetName="ByText",
      //   Mandatory = true,
      //   ValueFromPipeline = true,
      //   HelpMessage = "The key to bind")]
      //public string KeyName { get; set; }

      [Parameter(
         Position = 10,
         Mandatory = true,
         ValueFromPipeline = false,
         HelpMessage = "XAML template file")]
      [Alias("Template")]
      public ScriptBlock Action { get; set; }
      #endregion

      #region [rgn] Methods (1)

      // [rgn] Protected Methods (1)

      protected override void ProcessRecord()
      {
         try
         {
           ((PoshConsole.PSHost.PoshOptions)Host.PrivateData.BaseObject).BgHost.RegisterHotkey(KeyGesture, Action);
         }
         catch ( Exception ex )
         {
            ThrowTerminatingError(new ErrorRecord(ex, "Exception Thrown", ErrorCategory.NotSpecified, KeyGesture));
         }
      }

      #endregion [rgn]
   }
}
