using System.Management.Automation;
using System.Management.Automation.Host;

namespace PoshCode.Commands
{
    [Cmdlet(VerbsCommon.New, "Paragraph", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "KeyBinding")]
    public class NewParagraphCommand : PSCmdlet
    {
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            ((IPSWpfOptions)Host.PrivateData.BaseObject).WpfConsole.NewParagraph();
        }
    }
}
