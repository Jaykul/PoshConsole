using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PoshCode.PowerShell;

namespace PoshCode.Commands
{
    [Cmdlet(VerbsData.Out, "PoshConsole")]
    public class OutPoshConsoleCommand : PSCmdlet
    {

        [Parameter()]
        public PoshConsole PoshConsole { get; set; }


        [Parameter(
            Position = 0,
            Mandatory = false,
            ValueFromPipeline = true,
            HelpMessage = "Data to bind to the wpf control")]
        public PSObject InputObject { get; set; }

        protected override void BeginProcessing()
        {

            if (PoshConsole == null)
            {
                if (Host.PrivateData != null && Host.PrivateData.BaseObject is Options)
                {
                    //((IPSWpfHost)).GetWpfConsole();
                    PoshConsole = ((Options)Host.PrivateData.BaseObject).Console;
                }
            }

            if (PoshConsole == null)
            {
                throw new InvalidOperationException("Out-PoshConsole only works in the PoshConsole custom host when the control has the ContentControl set");
            }
            base.BeginProcessing();
        }

        protected override void EndProcessing()
        {
            PoshConsole.Dispatcher.InvokeAsync(() =>
            {
                PoshConsole.SetValue(PoshConsole.ContentProperty, InputObject.BaseObject);
            });
            base.EndProcessing();
        }
    }
}
