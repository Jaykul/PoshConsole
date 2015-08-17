using System.Collections.ObjectModel;
using System.Management.Automation;
using Microsoft.PowerShell.Commands;

namespace PoshCode.Commands
{
    [Cmdlet("Tee", "Default")]

    public class TeeDefault : OutDefaultCommand
    {
        protected override void ProcessRecord()
        {
            // This command has passthru
            WriteObject(InputObject, false);
            base.ProcessRecord();
        }
    }
}