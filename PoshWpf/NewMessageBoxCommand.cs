using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Windows;

namespace PoshWpf
{
    [Cmdlet(VerbsCommon.New, "MessageBox", DefaultParameterSetName = "DataTemplate", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None)]
    public class NewMessageBoxCommand : WpfCommandBase
    {
        protected override void ProcessRecord()
        {
            if (Content != null)
            {
                _dispatcher.Invoke((Action)(() =>
                {
                    WriteObject(MessageBox.Show(Content.BaseObject.ToString()));
                }));
            }
        }
    }
}
