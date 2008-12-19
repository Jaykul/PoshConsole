using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;

namespace PoshConsole.Cmdlets
{
   [Cmdlet(VerbsCommon.New, "Paragraph", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "KeyBinding")]
   public class NewParagraphCommand : PSCmdlet
   {
      protected override void BeginProcessing()
      {
         base.BeginProcessing();
         ((PoshConsole.Host.PoshOptions)Host.PrivateData.BaseObject).XamlUI.NewParagraph();
      }
      protected override void ProcessRecord()
      {
      }
   }
}
